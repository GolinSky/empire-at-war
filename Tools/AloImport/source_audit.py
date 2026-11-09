"""Read original ALO/XML inputs without Blender or project mutations.

Chunk layouts are extracted from RebelSpaceStation/Prepare.py and
VerifyGeometry.py. Numeric source indices, never display names, identify objects.
"""
import hashlib
import json
import math
import os
import struct
import xml.etree.ElementTree as ET
from pathlib import Path, PurePosixPath


class SourceError(ValueError):
    def __init__(self, code, message):
        self.code = code
        super().__init__(message)


def resolve_material_policy(recipe,piece_id,mesh_index,slot):
    matches=[]
    for policy in recipe['materials']:
        selector=policy['selector']
        if (selector['piece']==piece_id and selector['shader']==slot['shader']
                and selector.get('meshIndex',mesh_index)==mesh_index
                and selector.get('slot',slot['slot'])==slot['slot']
                and {row['name']:row['value'] for row in selector['parameters']}==slot['properties']):
            matches.append(policy)
    if len(matches)!=1:
        raise SourceError('SOURCE_AMBIGUOUS',f'Material policy must resolve exactly once: {piece_id}:mesh:{mesh_index}:slot:{slot["slot"]}:{slot["signature"]}')
    return matches[0]


def sha256(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def meg_index(path):
    """Read the classic, uncompressed EaW MEG directory without extracting files."""
    path=Path(path)
    size=path.stat().st_size
    def read_exact(stream,count):
        data=stream.read(count)
        if len(data)!=count:
            raise SourceError('INVALID_ARCHIVE','Truncated MEG directory: '+str(path))
        return data
    with path.open('rb') as stream:
        name_count,file_count=struct.unpack('<II',read_exact(stream,8))
        if name_count==0xffffffff:
            raise SourceError('UNSUPPORTED_CAPABILITY','Only classic uncompressed MEG archives are supported: '+str(path))
        if 8+name_count*2+file_count*20>size:
            raise SourceError('INVALID_ARCHIVE','MEG directory counts exceed archive size: '+str(path))
        names=[]
        for _ in range(name_count):
            length=struct.unpack('<H',read_exact(stream,2))[0]
            names.append(read_exact(stream,length).decode('ascii'))
        entries=[struct.unpack('<5I',read_exact(stream,20)) for _ in range(file_count)]
        directory_end=stream.tell()
    members={}
    for _,_,length,offset,name_index in entries:
        if name_index>=len(names) or offset<directory_end or offset+length>size:
            raise SourceError('INVALID_ARCHIVE','MEG member points outside archive data: '+str(path))
        name=names[name_index]
        key=name.replace('\\','/').casefold()
        members.setdefault(key,[]).append({'member':name,'offset':offset,'size':length})
    return {'path':str(path.resolve()),'sha256':sha256(path),'members':members}


def read_meg_member(index,member):
    virtual=PurePosixPath(member.replace('\\','/'))
    if virtual.is_absolute() or not virtual.parts or any(part in ('.','..') or ':' in part for part in virtual.parts):
        raise SourceError('INVALID_SOURCE_PATH','Unsafe MEG member: '+member)
    candidates=index['members'].get(str(virtual).casefold(),[])
    if not candidates:
        raise SourceError('SOURCE_MISSING','MEG member not found: '+index['path']+'::'+member)
    if len(candidates)!=1:
        raise SourceError('SOURCE_AMBIGUOUS','MEG member resolves more than once: '+index['path']+'::'+member)
    row=candidates[0]
    with Path(index['path']).open('rb') as stream:
        stream.seek(row['offset'])
        data=stream.read(row['size'])
    if len(data)!=row['size']:
        raise SourceError('INPUT_CHANGED','MEG member changed while reading: '+member)
    return data,{'archive':index['path'],'archiveSha256':index['sha256'],**row,
                 'sha256':hashlib.sha256(data).hexdigest()}


def resolve_source(root, relative):
    root = Path(root).resolve()
    parts = PurePosixPath(str(relative).replace('\\', '/'))
    if parts.is_absolute() or not parts.parts or any(p in ('..', '.') or ':' in p for p in parts.parts):
        raise SourceError('INVALID_SOURCE_PATH', str(relative))
    current = root
    for part in parts.parts:
        if not current.is_dir():
            raise SourceError('SOURCE_MISSING', str(current / part))
        matches = [p for p in current.iterdir() if p.name.casefold() == part.casefold()]
        if len(matches) > 1:
            raise SourceError('SOURCE_AMBIGUOUS', str(current / part))
        if not matches:
            raise SourceError('SOURCE_MISSING', str(current / part))
        current = matches[0]
        if not current.resolve().is_relative_to(root):
            raise SourceError('INVALID_SOURCE_PATH', 'Source escapes root: ' + str(current))
    if not current.is_file():
        raise SourceError('SOURCE_MISSING', str(current))
    return current


def recipe_source_resolver(recipe,roots,workspace):
    alias=recipe['source']['root']
    entries=recipe['source'].get('archiveEntries',[])
    if entries and workspace is None:
        raise SourceError('INVALID_CONFIG','Archive inputs require an owned audit workspace')
    archives={}
    extracted={}
    provenance=[]
    for entry in entries:
        virtual=PurePosixPath(entry['path'].replace('\\','/'))
        if virtual.is_absolute() or not virtual.parts or any(part in ('.','..') or ':' in part for part in virtual.parts):
            raise SourceError('INVALID_SOURCE_PATH','Unsafe archive input path: '+entry['path'])
        key=str(virtual).casefold()
        if key in extracted:
            raise SourceError('SOURCE_AMBIGUOUS','Duplicate archive input path: '+entry['path'])
        archive_alias=entry.get('root',alias)
        if archive_alias not in roots:
            raise SourceError('SOURCE_MISSING','Unconfigured archive source root: '+archive_alias)
        archive=resolve_source(roots[archive_alias],entry['archive'])
        archive_key=os.path.normcase(str(archive.resolve()))
        if archive_key not in archives:
            archives[archive_key]=meg_index(archive)
        data,evidence=read_meg_member(archives[archive_key],entry['member'])
        destination=Path(workspace).resolve()/'Sources'/str(virtual)
        if not destination.resolve().is_relative_to(Path(workspace).resolve()/'Sources'):
            raise SourceError('INVALID_SOURCE_PATH','Archive input escapes owned workspace')
        destination.parent.mkdir(parents=True,exist_ok=True)
        try:
            with destination.open('xb') as stream:
                stream.write(data)
                stream.flush()
                os.fsync(stream.fileno())
        except FileExistsError as error:
            raise SourceError('OUTPUT_CONFLICT','Audit snapshot already exists: '+str(destination)) from error
        extracted[key]=destination
        provenance.append({'path':entry['path'],'extractedPath':str(destination),'root':archive_alias,**evidence})
    for archive in archives.values():
        if sha256(archive['path'])!=archive['sha256']:
            raise SourceError('INPUT_CHANGED','Archive changed during source extraction: '+archive['path'])
    def resolve(relative):
        key=str(PurePosixPath(relative.replace('\\','/'))).casefold()
        if key in extracted:
            return extracted[key]
        return resolve_source(roots[alias],relative)
    files=[{'path':archive['path'],'sha256':archive['sha256'],'kind':'archive'} for archive in archives.values()]
    return resolve,files,provenance


def chunks(data, micro=False):
    offset, header = 0, 2 if micro else 8
    while offset < len(data):
        if len(data) - offset < header:
            raise SourceError('INVALID_ALO', 'Truncated chunk header')
        kind, size = (data[offset:offset + 2] if micro else struct.unpack_from('<II', data, offset))
        size &= 0x7fffffff
        end = offset + header + size
        if end > len(data):
            raise SourceError('INVALID_ALO', 'Chunk exceeds its parent')
        yield kind, data[offset + header:end]
        offset = end


def _string(data):
    return data.rstrip(b'\0').decode('ascii')


def _material(data):
    result = {'properties': {}}
    for kind, value in chunks(data):
        if kind == 0x10101:
            result['shader'] = _string(value)
        elif kind in (0x10102, 0x10103, 0x10104, 0x10105, 0x10106):
            fields = list(chunks(value, micro=True))
            if len(fields) != 2:
                raise SourceError('INVALID_ALO', 'Invalid material parameter')
            name, raw = _string(fields[0][1]), fields[1][1]
            if kind == 0x10105:
                decoded = _string(raw)
            else:
                code = 'I' if kind == 0x10102 else 'f'
                decoded = list(struct.unpack('<' + code * (len(raw) // 4), raw))
            result['properties'][name] = decoded
    signature = json.dumps(result, sort_keys=True, separators=(',', ':'), allow_nan=False)
    result['signature'] = hashlib.sha256(signature.encode()).hexdigest()
    return result


def audit_alo(path, piece_id, include_geometry=False):
    path = Path(path)
    data = path.read_bytes()
    result = {'pieceId': piece_id, 'file': str(path.resolve()), 'sha256': hashlib.sha256(data).hexdigest(),
              'bones': [], 'meshes': [], 'connections': [], 'proxies': []}
    object_index = 0
    for kind, payload in chunks(data):
        if kind == 0x200:
            for child, value in chunks(payload):
                if child != 0x202:
                    continue
                fields = dict(chunks(value))
                parent, visible, billboard = struct.unpack_from('<III', fields[0x206])
                matrix = list(struct.unpack_from('<12f', fields[0x206], 12))
                if not all(math.isfinite(v) for v in matrix):
                    raise SourceError('INVALID_ALO', 'Nonfinite bone matrix')
                result['bones'].append({'index': len(result['bones']), 'name': _string(fields[0x203]),
                    'parent_index': parent, 'visible': visible, 'billboard': billboard, 'matrix': matrix})
        elif kind == 0x600:
            for child, value in chunks(payload):
                if child == 0x602:
                    result['connections'].append({'object_index': struct.unpack_from('<I', value, 2)[0],
                                                  'bone_index': struct.unpack_from('<I', value, 8)[0]})
                elif child == 0x603:
                    fields = list(chunks(value, micro=True))
                    result['proxies'].append({'name': _string(fields[0][1]), 'bone_index': struct.unpack('<I', fields[1][1])[0]})
        elif kind == 0x1300:
            object_index += 1
        elif kind == 0x400:
            mesh = {'index': len(result['meshes']), 'object_index': object_index, 'materials': []}
            object_index += 1
            for child, value in chunks(payload):
                if child == 0x401:
                    mesh['name'] = _string(value)
                elif child == 0x402:
                    mesh['hidden'], mesh['collision'] = [bool(v) for v in struct.unpack_from('<II', value, 32)]
                elif child == 0x10100:
                    material = _material(value)
                    material['slot'] = len(mesh['materials'])
                    mesh['materials'].append(material)
                elif child == 0x10000:
                    geometry = dict(chunks(value))
                    vertex_count, triangle_count = struct.unpack_from('<II', geometry[0x10001])
                    material = mesh['materials'][-1]
                    material.update(vertexCount=vertex_count, triangleCount=triangle_count)
                    if include_geometry:
                        stride, key = (144, 0x10007) if 0x10007 in geometry else (128, 0x10005)
                        vertices = geometry[key]
                        if len(vertices) != vertex_count * stride:
                            raise SourceError('UNSUPPORTED_CAPABILITY', 'Unknown vertex layout')
                        material['positions'] = [struct.unpack_from('<3f', vertices, i * stride) for i in range(vertex_count)]
                        material['normals'] = [struct.unpack_from('<3f', vertices, i * stride + 12) for i in range(vertex_count)]
                        material['uvs'] = [struct.unpack_from('<2f', vertices, i * stride + 24) for i in range(vertex_count)]
                        material['triangles'] = list(struct.unpack('<' + 'H' * triangle_count * 3, geometry[0x10004]))
                        if any(index >= vertex_count for index in material['triangles']):
                            raise SourceError('INVALID_ALO', 'Triangle index out of range')
            result['meshes'].append(mesh)
    if not result['bones'] or not result['meshes']:
        raise SourceError('INVALID_ALO', 'ALO must contain bones and meshes')
    for bone in result['bones']:
        if bone['parent_index'] != 0xffffffff and bone['parent_index'] >= bone['index']:
            raise SourceError('UNSUPPORTED_CAPABILITY', 'Bone parents must precede children')
    for connection in result['connections']:
        if connection['bone_index']>=len(result['bones']):
            raise SourceError('INVALID_ALO','Connection bone index is outside source skeleton')
    for proxy in result['proxies']:
        if proxy['bone_index']>=len(result['bones']):
            raise SourceError('INVALID_ALO','Proxy bone index is outside source skeleton')
    for mesh in result['meshes']:
        if len([row for row in result['connections'] if row['object_index']==mesh['object_index']])!=1:
            raise SourceError('UNSUPPORTED_CAPABILITY','Mesh requires exactly one source bone connection')
    return result


def read_xml_definitions(paths,selections=None):
    candidates = {}
    selections={} if selections is None else selections
    for path in paths:
        path = Path(path)
        for row in ET.parse(path).getroot():
            name = row.get('Name')
            if not name:
                continue
            candidates.setdefault(name,[]).append((row,path))
    unknown=set(selections)-set(candidates)
    if unknown:
        raise SourceError('SOURCE_MISSING','XML definition override names absent from declared files: '+', '.join(sorted(unknown)))
    definitions={}
    for name,rows in candidates.items():
        origins=[str(row[1].resolve()) for row in rows]
        if name in selections:
            chosen=Path(selections[name]).resolve()
            rows=[row for row in rows if row[1].resolve()==chosen]
        if len(rows)!=1:
            raise SourceError('SOURCE_AMBIGUOUS','XML definition must resolve exactly once: '+name+'; candidates: '+', '.join(origins))
        definitions[name]=rows[0]
    return definitions


def resolve_xml_unit(definitions, name):
    chain, values, files = [], {}, {}
    current = name
    while current:
        if current in chain:
            raise SourceError('INVALID_SOURCE_XML', 'Inheritance cycle: ' + current)
        if current not in definitions:
            raise SourceError('SOURCE_MISSING', 'XML unit: ' + current)
        row, path = definitions[current]
        chain.append(current)
        files[str(path.resolve())] = sha256(path)
        current = (row.findtext('Variant_Of_Existing_Type') or '').strip()
    for ancestor in reversed(chain):
        row, _ = definitions[ancestor]
        for tag in {child.tag for child in row}:
            values[tag] = [ET.tostring(child, encoding='unicode') if len(child) else (child.text or '').strip()
                           for child in row if child.tag == tag]
    return {'inheritance': chain, 'values': values, 'source_hashes': files}


def multiply_matrix(left,right):
    return [[sum(left[row][axis]*right[axis][column] for axis in range(4))
             for column in range(4)] for row in range(4)]


def bone_world_matrices(bones):
    result=[]
    for bone in bones:
        values=bone['matrix']
        matrix=[values[:4],values[4:8],values[8:12],[0,0,0,1]]
        if bone['parent_index']!=0xffffffff:
            matrix=multiply_matrix(result[bone['parent_index']],matrix)
        result.append(matrix)
    return result


def assembly_instances(recipe,models):
    models={model['pieceId']:model for model in models}
    identity=[[1,0,0,0],[0,1,0,0],[0,0,1,0],[0,0,0,1]]
    instances={}
    for piece in recipe['pieces']:
        if piece['purpose'] in {'hull','level','damaged'}:
            instances[piece['id']]={'id':piece['id'],'piece':piece['id'],'parentInstance':None,
                'parentPiece':None,'boneIndex':None,'localMatrix':identity,'worldMatrix':identity,'root':piece['id']}
    pending=list(recipe['attachments'])
    if len({row['id'] for row in pending})!=len(pending) or any(row['id'] in instances for row in pending):
        raise SourceError('SOURCE_AMBIGUOUS','Attachment IDs must be unique and differ from root piece IDs')
    while pending:
        progressed=False
        for row in list(pending):
            parent_piece=row['parentPiece']
            candidates=[item for item in instances.values() if item['piece']==parent_piece]
            if row.get('parentInstance'):
                candidates=[item for item in candidates if item['id']==row['parentInstance']]
            elif sum(item['piece']==parent_piece for item in recipe['attachments'])>1:
                raise SourceError('SOURCE_AMBIGUOUS','Repeated parent piece requires parentInstance: '+row['id'])
            if len(candidates)>1:
                raise SourceError('SOURCE_AMBIGUOUS','Ambiguous parent instance: '+row['id'])
            if not candidates:
                continue
            parent=candidates[0]
            bones=models[parent_piece]['bones']
            if row['boneIndex']>=len(bones):
                raise SourceError('INVALID_SOURCE_ANCHOR','Attachment bone index outside parent skeleton: '+row['id'])
            local=bone_world_matrices(bones)[row['boneIndex']]
            instances[row['id']]={'id':row['id'],'piece':row['piece'],'parentInstance':parent['id'],
                'parentPiece':parent_piece,'boneIndex':row['boneIndex'],'localMatrix':local,
                'worldMatrix':multiply_matrix(parent['worldMatrix'],local),'root':parent['root']}
            if row.get('sourceHardpoint'):
                instances[row['id']]['sourceHardpoint']=row['sourceHardpoint']
            pending.remove(row)
            progressed=True
        if not progressed:
            raise SourceError('INVALID_SOURCE_ANCHOR','Unresolved or cyclic attachment parents: '+', '.join(row['id'] for row in pending))
    unused=[piece['id'] for piece in recipe['pieces'] if piece['purpose']=='attachment'
            and not any(instance['piece']==piece['id'] for instance in instances.values())]
    if unused:
        raise SourceError('INVALID_SOURCE_ANCHOR','Attachment pieces require explicit instances: '+', '.join(unused))
    return list(instances.values())


def _model_reference(value):
    path=PurePosixPath(value.replace('\\','/'))
    return path.with_suffix('.alo').name.casefold() if not path.suffix else path.name.casefold()


def audit_xml_assemblies(recipe,models,definitions,instances):
    if not definitions:
        if recipe['source'].get('unit') or any(row.get('sourceHardpoint') for row in recipe['attachments']):
            raise SourceError('SOURCE_MISSING','Explicit XML files are required for unit/hardpoint provenance')
        return {'units':[],'checks':{'unitProvenance':'not-run','attachmentCoverage':'not-run'}}
    roots=([{'piece':piece['id'],'unit':recipe['source'].get('unit')} for piece in recipe['pieces'] if piece['purpose']=='hull']
           if recipe['kind']=='ship' else [{'piece':level['piece'],'unit':level.get('sourceUnit')} for level in recipe['station']['levels']])
    models={model['pieceId']:model for model in models}
    rows=[]
    for root in roots:
        if not root['unit']:
            raise SourceError('SOURCE_MISSING','Explicit source unit required for assembly root: '+root['piece'])
        unit=resolve_xml_unit(definitions,root['unit'])
        source_models=unit['values'].get('Space_Model_Name',[])
        if len(source_models)!=1 or _model_reference(source_models[0])!=Path(models[root['piece']]['file']).name.casefold():
            raise SourceError('INPUT_CONFLICT','Selected XML unit model does not match root piece: '+root['piece'])
        names=[name.strip() for value in unit['values'].get('HardPoints',[]) for name in value.split(',') if name.strip()]
        attachments=[]
        hardpoints=[]
        projectiles={}
        for name in names:
            hardpoint=resolve_xml_unit(definitions,name)
            hardpoints.append({'name':name,**hardpoint})
            for projectile in hardpoint['values'].get('Fire_Projectile_Type',[]):
                if projectile:
                    projectiles[projectile]=resolve_xml_unit(definitions,projectile)
            model=hardpoint['values'].get('Model_To_Attach',[])
            if not model or not model[0]:
                continue
            bone_name=hardpoint['values'].get('Attachment_Bone',[])
            if len(model)!=1 or len(bone_name)!=1:
                raise SourceError('INVALID_SOURCE_XML','Attachment model/bone must resolve once: '+name)
            candidates=[instance for instance in instances if instance['root']==root['piece']
                and instance.get('sourceHardpoint')==name]
            if len(candidates)!=1:
                raise SourceError('SOURCE_AMBIGUOUS','XML attachment must map to exactly one recipe instance: '+name)
            instance=candidates[0]
            parent=models[instance['parentPiece']]
            bone=parent['bones'][instance['boneIndex']]
            named_bones=[row for row in parent['bones'] if row['name'].casefold()==bone_name[0].casefold()]
            if len(named_bones)!=1:
                raise SourceError('SOURCE_AMBIGUOUS','XML attachment bone does not resolve uniquely: '+name)
            if bone['index']!=named_bones[0]['index'] or _model_reference(model[0])!=Path(models[instance['piece']]['file']).name.casefold():
                raise SourceError('INPUT_CONFLICT','Recipe attachment differs from selected XML model/bone: '+name)
            attachments.append({'hardpoint':name,'instanceId':instance['id'],'piece':instance['piece'],
                'parentPiece':instance['parentPiece'],'boneIndex':instance['boneIndex'],'boneName':bone['name'],
                'inheritance':hardpoint['inheritance'],'values':hardpoint['values'],'source_hashes':hardpoint['source_hashes']})
        declared={instance.get('sourceHardpoint') for instance in instances if instance['root']==root['piece'] and instance.get('sourceHardpoint')}
        if declared!={row['hardpoint'] for row in attachments}:
            raise SourceError('INPUT_CONFLICT','Recipe claims hardpoints outside selected source unit: '+root['piece'])
        rows.append({'piece':root['piece'],'unit':root['unit'],**unit,'hardpoints':hardpoints,
                     'projectiles':projectiles,'attachments':attachments})
    return {'units':rows,'checks':{'unitProvenance':'passed','attachmentCoverage':'passed'}}


def audit_recipe_sources(recipe, roots,workspace=None):
    alias = recipe['source']['root']
    if alias not in roots:
        raise SourceError('SOURCE_MISSING', 'Unconfigured source root: ' + alias)
    resolve,archive_files,archive_provenance=recipe_source_resolver(recipe,roots,workspace)
    models = [audit_alo(resolve(piece['alo']), piece['id'])
              for piece in recipe['pieces']]
    for model in models:
        for mesh in model['meshes']:
            for slot in mesh['materials']:
                resolve_material_policy(recipe,model['pieceId'],mesh['index'],slot)
    files = {model['file']: {'path': model['file'], 'sha256': model['sha256'], 'kind': 'alo'} for model in models}
    files.update({row['path']:row for row in archive_files})
    textures = {}
    overrides={row['source'].casefold():row['path'] for row in recipe['source'].get('textureOverrides',[])}
    for model in models:
        for mesh in model['meshes']:
            for material in mesh['materials']:
                for value in material['properties'].values():
                    if not isinstance(value, str) or not value.lower().endswith(('.dds', '.tga')):
                        continue
                    directory = recipe['source'].get('textureDirectory')
                    relative=overrides.get(value.casefold())
                    if not relative and not directory:
                        raise SourceError('SOURCE_MISSING', 'source.textureDirectory required for: ' + value)
                    path = resolve(relative or directory + '/' + value)
                    textures[value.casefold()] = {'path': str(path.resolve()), 'sha256': sha256(path)}
                    files[str(path.resolve())] = {**textures[value.casefold()], 'kind': 'texture'}
    xml = recipe['source'].get('xml', [])
    xml = [xml] if isinstance(xml, str) else xml
    overrides_xml=recipe['source'].get('definitionOverrides',[])
    xml=list(dict.fromkeys([*xml,*recipe['source'].get('xmlFiles',[]),*[row['file'] for row in overrides_xml]]))
    for relative in [*xml, *recipe['source'].get('archives', [])]:
        path = resolve(relative)
        files[str(path.resolve())] = {'path': str(path.resolve()), 'sha256': sha256(path), 'kind': 'source'}
    for mapping in recipe['materials']:
        for texture in mapping['textures']:
            path = resolve(texture['source'])
            files[str(path.resolve())] = {'path': str(path.resolve()), 'sha256': sha256(path), 'kind': 'texture'}
            textures[texture['source'].casefold()] = {'path': str(path.resolve()), 'sha256': sha256(path)}
    instances=assembly_instances(recipe,models)
    selections={row['name']:resolve(row['file']) for row in overrides_xml}
    definitions=read_xml_definitions([resolve(relative) for relative in xml],selections)
    source_xml=audit_xml_assemblies(recipe,models,definitions,instances)
    return {'files': list(files.values()), 'pieces': models, 'textures': textures,
            'instances':instances,'xml':source_xml,'archiveProvenance':archive_provenance}
