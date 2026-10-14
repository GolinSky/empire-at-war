"""Audit the installed Allegiance model, attachments, weapons and source hashes."""
import hashlib
import json
import re
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data')
OUTPUT = Path('Temp/AllegianceImport')


def Chunks(data):
    offset = 0
    while offset < len(data):
        kind, size = struct.unpack_from('<II', data, offset)
        size &= 0x7fffffff
        yield kind, data[offset+8:offset+8+size]
        offset += 8+size
    assert offset == len(data)


def MicroChunks(data):
    offset = 0
    while offset < len(data):
        kind, size = data[offset:offset+2]
        yield kind, data[offset+2:offset+2+size]
        offset += 2+size
    assert offset == len(data)


def Audit(path):
    meshes = []
    connections = []
    proxies = []
    for kind, payload in Chunks(path.read_bytes()):
        if kind == 0x600:
            for child, content in Chunks(payload):
                if child == 0x602:
                    connections.append(dict(mesh=struct.unpack_from('<I',content,2)[0],bone=struct.unpack_from('<I',content,8)[0]))
                elif child == 0x603:
                    fields = list(MicroChunks(content))
                    proxies.append(dict(name=fields[0][1].rstrip(b'\0').decode('ascii'),bone=struct.unpack('<I',fields[1][1])[0]))
        if kind != 0x400:
            continue
        mesh = dict(materials=[])
        for child, content in Chunks(payload):
            if child == 0x401:
                mesh['name'] = content.rstrip(b'\0').decode('ascii')
            elif child == 0x402:
                mesh['hidden'] = bool(struct.unpack_from('<I', content, 32)[0])
                mesh['collision'] = bool(struct.unpack_from('<I', content, 36)[0])
            elif child == 0x10100:
                material = dict(properties={})
                for parameter, value in Chunks(content):
                    if parameter == 0x10101:
                        material['shader'] = value.rstrip(b'\0').decode('ascii')
                    elif parameter in (0x10102, 0x10103, 0x10104, 0x10105, 0x10106):
                        micros = list(MicroChunks(value))
                        name = micros[0][1].rstrip(b'\0').decode('ascii')
                        raw = micros[1][1]
                        if parameter == 0x10105:
                            result = raw.rstrip(b'\0').decode('ascii')
                        elif parameter == 0x10102:
                            result = struct.unpack('<I', raw)[0]
                        else:
                            result = list(struct.unpack('<'+'f'*(len(raw)//4), raw))
                        material['properties'][name] = result
                mesh['materials'].append(material)
            elif child == 0x10000:
                header = next(value for kind, value in Chunks(content) if kind == 0x10001)
                mesh['materials'][-1]['vertexCount'], mesh['materials'][-1]['triangleCount'] = struct.unpack_from('<II', header)
        meshes.append(mesh)
    return dict(file=str(path), sha256=hashlib.sha256(path.read_bytes()).hexdigest(), meshes=meshes,connections=connections,proxies=proxies)




def Bones(path):
    result = []
    with path.open('rb') as stream:
        stream.seek(16)
        count = struct.unpack('<I', stream.read(4))[0]
        stream.seek(124, 1)
        for _ in range(count):
            stream.seek(12, 1)
            size = struct.unpack('<I', stream.read(4))[0] & 0x7fffffff
            name = stream.read(size).rstrip(b'\0').decode('ascii')
            stream.seek(8, 1)
            parent, visible, billboard = struct.unpack('<III', stream.read(12))
            result.append(dict(name=name, parent_index=parent, visible=visible,
                               matrix=list(struct.unpack('<12f', stream.read(48)))))
    return result


def Values(element):
    return {c.tag: (c.text or '').strip() for c in element}


OUTPUT.mkdir(parents=True, exist_ok=True)
(OUTPUT / 'Output/Textures').mkdir(parents=True, exist_ok=True)
definitions, elements, files = {}, {}, {}
for path in (SOURCE / 'XML').rglob('*'):
    if path.suffix.lower() != '.xml':
        continue
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError:
        continue
    for element in root:
        name = element.attrib.get('Name')
        if name:
            definitions[name] = Values(element)
            elements[name] = element
            files[name] = path


def Resolve(name):
    chain, current = [], name
    while current in definitions:
        if current in chain:
            raise ValueError('Cyclic inheritance: ' + current)
        chain.append(current)
        current = definitions[current].get('Variant_Of_Existing_Type')
    resolved = {}
    for ancestor in reversed(chain):
        resolved.update(definitions[ancestor])
    return dict(inheritance=chain, resolved=resolved)


unit_name = 'Allegiance_Star_Destroyer_MP'
unit = Resolve(unit_name)['resolved']
names = [name.strip() for name in unit['HardPoints'].split(',') if name.strip()]
hardpoints = [dict(name=name, **Resolve(name)['resolved']) for name in names]
models = {'Allegiance': unit['Space_Model_Name']}
for hp in hardpoints:
    filename = hp.get('Model_To_Attach')
    if filename and filename not in models.values():
        models['AllegianceTurret' + str(len(models)).zfill(2)] = filename
hashes, audits = {}, {}
for key, filename in models.items():
    path = next(p for p in (SOURCE / 'ART/MODELS').iterdir() if p.name.lower()==filename.lower())
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    surface_audit = Audit(path)
    refs = sorted({v for m in surface_audit['meshes'] for slot in m['materials'] for k,v in slot['properties'].items() if isinstance(v,str) and v.lower().endswith(('.dds','.tga'))})
    textures = {}
    for ref in refs:
        texture = next((p for p in (SOURCE/'ART/TEXTURES').rglob('*') if p.name.lower() in (ref.lower(), Path(ref).with_suffix('.dds').name.lower())), SOURCE/'ART/TEXTURES'/ref)
        if not texture.exists():
            raise FileNotFoundError(texture)
        hashes[str(texture)] = hashlib.sha256(texture.read_bytes()).hexdigest()
        textures[ref] = str(texture)
    audits[key] = dict(file=str(path), bones=Bones(path), textures=textures, surfaces=surface_audit)
projectiles = {hp['Fire_Projectile_Type']: Resolve(hp['Fire_Projectile_Type'])
               for hp in hardpoints if hp.get('Fire_Projectile_Type')}
for name in [unit_name] + names + [a for p in projectiles.values() for a in p['inheritance']]:
    path = files[name]
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
report = dict(models=audits, hardpoints=hardpoints, projectiles=projectiles,
              unit=unit, abilities=None,
              source_hashes=hashes)
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(dict(models={key: dict(file=data['file'], bones=len(data['bones'])) for key, data in audits.items()},
                     unit={k: v for k, v in unit.items() if any(s in k.lower() for s in ('spawn', 'health', 'shield', 'cost', 'speed', 'build', 'population'))},
                     abilities=report['abilities'],
                     hardpoints=[{k: v for k, v in hp.items() if k in ('name', 'Attachment_Bone', 'Model_To_Attach', 'Fire_Bone_A', 'Fire_Projectile_Type', 'Is_Targetable', 'Type', 'Damage', 'Fire_Pulse_Count', 'Fire_Cone_Width')} for hp in hardpoints]), indent=2))

