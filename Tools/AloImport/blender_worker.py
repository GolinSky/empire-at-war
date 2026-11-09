"""Blender-only worker. Invoked in an owned factory-startup background process."""
import hashlib
import json
import os
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from source_audit import audit_alo, resolve_material_policy
from fidelity import compare_triangles, source_has_flat_normals
from storage import owner, read_json, write_json

HELPER_SHADERS=frozenset({'MeshShadowVolume.fx','RSkinShadowVolume.fx','MeshCollision.fx'})


def unique_names(rows):
    names=[]
    for row in rows:
        name=row['name']
        if name in names:
            base,separator,suffix=name.rpartition('.')
            base=base if separator and suffix.isdigit() else name
            number=1
            while f'{base}.{number:03}' in names:
                number+=1
            name=f'{base}.{number:03}'
        names.append(name)
    return names


def matrices(bones):
    result=[]
    for bone in bones:
        values=bone['matrix']
        matrix=Matrix((values[:4],values[4:8],values[8:12],(0,0,0,1)))
        if bone['parent_index']!=0xffffffff:
            matrix=result[bone['parent_index']] @ matrix
        result.append(matrix)
    return result


def make_material(spec,piece,mesh_index,slot,cache):
    key=(piece,mesh_index,slot['slot'],slot['signature'])
    if key in cache:
        return cache[key]
    policy=resolve_material_policy(spec['recipe'],piece,mesh_index,slot)
    material=bpy.data.materials.new(f'{piece}_M{mesh_index:03}_S{slot["slot"]:02}_{slot["signature"][:12]}')
    material['EaWShader']=slot['shader']
    material['EaWMaterialSignature']=slot['signature']
    material['EaWMaterialPolicy']=json.dumps(policy,sort_keys=True)
    if policy.get('targetGuid'):
        material['EaWMaterialTargetGuid']=policy['targetGuid']
    material.use_nodes=True
    material.blend_method={'opaque':'OPAQUE','additive':'BLEND','cutout':'CLIP','alpha':'BLEND'}[policy['surface']]
    material.node_tree.nodes.clear()
    nodes,links=material.node_tree.nodes,material.node_tree.links
    surface=nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value=.65
    output=nodes.new('ShaderNodeOutputMaterial')
    links.new(surface.outputs['BSDF'],output.inputs['Surface'])
    for texture in policy['textures']:
        definition=spec['textures'][texture['source'].casefold()]
        image=bpy.data.images.load(definition['path'],check_existing=True)
        image.alpha_mode='CHANNEL_PACKED'
        image.pack()
        node=nodes.new('ShaderNodeTexImage')
        node.image=image
        semantic=texture['semantic']
        if semantic=='normal':
            image.colorspace_settings.name='Non-Color'
            normal=nodes.new('ShaderNodeNormalMap')
            color=node.outputs['Color']
            if policy['normalConvention']=='directx':
                separate=nodes.new('ShaderNodeSeparateRGB')
                combine=nodes.new('ShaderNodeCombineRGB')
                invert=nodes.new('ShaderNodeMath')
                invert.operation='SUBTRACT'
                invert.inputs[0].default_value=1
                links.new(color,separate.inputs['Image'])
                links.new(separate.outputs['R'],combine.inputs['R'])
                links.new(separate.outputs['G'],invert.inputs[1])
                links.new(invert.outputs[0],combine.inputs['G'])
                links.new(separate.outputs['B'],combine.inputs['B'])
                color=combine.outputs['Image']
            links.new(color,normal.inputs['Color'])
            links.new(normal.outputs['Normal'],surface.inputs['Normal'])
        elif semantic=='albedo':
            links.new(node.outputs['Color'],surface.inputs['Base Color'])
            if policy['surface'] in {'cutout','alpha'}:
                links.new(node.outputs['Alpha'],surface.inputs['Alpha'])
            if policy['surface']=='additive':
                links.new(node.outputs['Color'],surface.inputs['Emission'])
        elif semantic=='emissive':
            links.new(node.outputs['Color'],surface.inputs['Emission'])
        elif semantic=='opacity':
            links.new(node.outputs['Color'],surface.inputs['Alpha'])
    cache[key]=material
    return material


def expected_corners(mesh,world):
    result=[]
    normal_matrix=world.to_3x3().inverted().transposed()
    for slot in mesh['materials']:
        rows=[]
        for index in slot['triangles']:
            pos=world @ Vector(slot['positions'][index])
            normal=normal_matrix @ Vector(slot['normals'][index])
            normal.normalize()
            uv=slot['uvs'][index]
            rows.append((*pos,uv[0],-uv[1],*normal))
        result.append(rows)
    return result


def verify_bones(rig,audit,names,world,roundtrip=False):
    if len(rig.data.bones)!=len(audit['bones']):
        raise ValueError('Skeleton bone count changed')
    checked={}
    for bone in rig.data.bones:
        index=int(bone['EaWSourceBoneIndex'])
        if index in checked or bone['EaWPieceId']!=audit['pieceId']:
            raise ValueError('Skeleton numeric identity changed')
        checked[index]=bone
    maximum=0
    for index,row in enumerate(audit['bones']):
        bone=checked[index]
        parent=None if row['parent_index']==0xffffffff else row['parent_index']
        actual_parent=None if bone.parent is None else int(bone.parent['EaWSourceBoneIndex'])
        if parent!=actual_parent or json.loads(bone['EaWSourceMatrix'])!=row['matrix']:
            raise ValueError('Skeleton hierarchy or authored matrix metadata changed')
        error=(rig.matrix_world @ bone.head_local-world[index].translation).length
        if not roundtrip:
            error=max(error,max(abs(bone.matrix_local[r][c]-world[index][r][c]) for r in range(4) for c in range(4)))
        maximum=max(maximum,error)
        if error>.001:
            raise ValueError('Skeleton source transform error exceeds .001')
    return {'bones':len(checked),'maxSourceError':maximum,'fullAuthoredMatrixMetadata':'passed',
            'comparison':'head-and-parent' if roundtrip else 'full-matrix-and-parent'}


def actual_corners(obj):
    obj.data.calc_loop_triangles()
    obj.data.calc_normals_split()
    normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
    result=[[] for _ in obj.material_slots]
    if not obj.data.loop_triangles:
        return result
    uv=obj.data.uv_layers.active.data
    for triangle in obj.data.loop_triangles:
        for index in triangle.loops:
            loop=obj.data.loops[index]
            pos=obj.matrix_world @ obj.data.vertices[loop.vertex_index].co
            normal=normal_matrix @ loop.normal
            normal.normalize()
            result[triangle.material_index].append((*pos,*uv[index].uv,*normal))
    return result


def compare(expected,actual,mesh,spec):
    if len(expected)!=len(actual):
        raise ValueError('Material slot count changed')
    checks=[]
    source_id=f'{mesh["pieceId"]}:mesh:{mesh["index"]}'
    ignore_normals=any(row['ruleId']=='helper-normal-representability' and source_id in row['sourceIds']
                       for row in spec['recipe'].get('exceptions',[]))
    if ignore_normals and not all(slot['shader'] in HELPER_SHADERS for slot in mesh['materials']):
        raise ValueError('Helper normal exception cannot affect visible art')
    for slot_index,(wanted,got) in enumerate(zip(expected,actual)):
        policy=resolve_material_policy(spec['recipe'],mesh['pieceId'],mesh['index'],mesh['materials'][slot_index])
        texture_size=max([max(spec['textures'][t['source'].casefold()]['size']) for t in policy['textures']]+[1])
        try:
            checks.append({'slot':slot_index,**compare_triangles(wanted,got,texture_size,ignore_normals)})
        except ValueError as error:
            raise ValueError(f'{source_id}:slot:{slot_index}: {error}') from error
    return {'slots':checks}


def convert_piece(spec,piece):
    from io_alamo_tools import import_alo
    audit=audit_alo(piece['file'],piece['pieceId'],include_geometry=True)
    if audit['sha256']!=piece['sha256']:
        raise ValueError('INPUT_CHANGED: '+piece['file'])
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj,do_unlink=True)
    for group in (bpy.data.materials,bpy.data.meshes,bpy.data.armatures):
        for item in list(group):
            if not item.users:
                group.remove(item)
    bpy.ops.import_mesh.alo(filepath=str(Path(piece['file'])),importAnimations=False)
    rigs=[obj for obj in bpy.context.scene.objects if obj.type=='ARMATURE']
    if len(rigs)!=1 or len(import_alo.MeshNameList)!=len(audit['meshes']):
        raise ValueError('Unexpected ALAMO import object inventory')
    rig=rigs[0]
    definition=next(row for row in spec['recipe']['pieces'] if row['id']==piece['pieceId'])
    if 'transportRigName' in definition:
        rig.name=definition['transportRigName']
    transport_rig_name=rig.name
    names=unique_names(audit['bones'])
    world=matrices(audit['bones'])
    root=audit['bones'][0]
    if root['name']!='Root' or root['parent_index']!=0xffffffff or root['matrix']!=[1,0,0,0,0,1,0,0,0,0,1,0]:
        raise ValueError('UNSUPPORTED_CAPABILITY: identity Root repair precondition')
    bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    if 'Root' not in rig.data.edit_bones:
        bone=rig.data.edit_bones.new('Root')
        bone.head,bone.tail=(0,0,0),(0,1,0)
    if set(names)!={b.name for b in rig.data.edit_bones}:
        raise ValueError('ALAMO bone identity allocation mismatch')
    for index,row in enumerate(audit['bones']):
        bone=rig.data.edit_bones[names[index]]
        bone.parent=None if row['parent_index']==0xffffffff else rig.data.edit_bones[names[row['parent_index']]]
        bone.matrix=world[index]
    bpy.ops.object.mode_set(mode='OBJECT')
    for index,name in enumerate(names):
        rig.data.bones[name]['EaWSourceBoneIndex']=index
        rig.data.bones[name]['EaWPieceId']=piece['pieceId']
        rig.data.bones[name]['EaWSourceMatrix']=json.dumps(audit['bones'][index]['matrix'])
    skeleton_source=verify_bones(rig,audit,names,world)
    cache={}
    source_checks=[]
    mesh_map=[]
    repairs=[]
    for mesh,name in zip(audit['meshes'],list(import_alo.MeshNameList)):
        mesh['pieceId']=piece['pieceId']
        obj=bpy.data.objects[name]
        obj['EaWPieceId']=piece['pieceId']
        obj['EaWSourceMeshIndex']=mesh['index']
        obj['EaWSourceName']=mesh['name']
        obj['EaWSourceHidden']=mesh['hidden']
        obj['EaWHidden']=mesh['hidden']
        obj.hide_render=mesh['hidden']
        obj.hide_set(False)
        connections=[c for c in audit['connections'] if c['object_index']==mesh['object_index']]
        if len(connections)!=1:
            raise ValueError('UNSUPPORTED_CAPABILITY: mesh connection count')
        bone_index=connections[0]['bone_index']
        for constraint in list(obj.constraints):
            obj.constraints.remove(constraint)
        obj.parent,obj.parent_type,obj.parent_bone=rig,'BONE',names[bone_index]
        bpy.context.view_layer.update()
        obj.matrix_world=world[bone_index]
        positions=[p for slot in mesh['materials'] for p in slot['positions']]
        normals=[n for slot in mesh['materials'] for n in slot['normals']]
        triangles=[]
        uvs=[]
        offset=0
        for slot in mesh['materials']:
            triangles.extend(tuple(i+offset for i in slot['triangles'][j:j+3]) for j in range(0,len(slot['triangles']),3))
            uvs.extend((u,-v) for u,v in slot['uvs'])
            offset+=slot['vertexCount']
        helper=all(slot['shader'] in HELPER_SHADERS for slot in mesh['materials'])
        flat_helper=helper and source_has_flat_normals(mesh['materials'])
        if helper or len(obj.data.vertices)!=len(positions) or len(obj.data.polygons)!=len(triangles):
            if not helper:
                raise ValueError('Unexpected ALAMO topology change')
            original_data=obj.data
            original_name=original_data.name
            repaired=bpy.data.meshes.new(original_name+'_SourceTopology')
            repaired.from_pydata(positions,[],triangles)
            obj.data=repaired
            if original_data.users:
                raise ValueError('Cannot preserve helper mesh data identity while original data has other users')
            bpy.data.meshes.remove(original_data)
            repaired.name=original_name
            if repaired.name!=original_name:
                raise ValueError('Cannot preserve original helper mesh data name')
            uv=repaired.uv_layers.new(name='UVMap')
            for loop in repaired.loops:
                uv.data[loop.index].uv=uvs[loop.vertex_index]
            repairs.append({'meshIndex':mesh['index'],'repair':'restore-original-helper-topology'})
        obj.data.materials.clear()
        polygon_offset=0
        for slot in mesh['materials']:
            material=make_material(spec,piece['pieceId'],mesh['index'],slot,cache)
            obj.data.materials.append(material)
            for polygon in obj.data.polygons[polygon_offset:polygon_offset+slot['triangleCount']]:
                polygon.material_index=slot['slot']
                polygon.use_smooth=not flat_helper
            polygon_offset+=slot['triangleCount']
        obj.data.use_auto_smooth=True
        obj.data['EaWSourceNormalLayout']='material-slot-vertex-order'
        obj.data['EaWSourceNormals']=[component for normal in normals for component in normal]
        obj.data['EaWSourceNormalSlotVertexCounts']=[slot['vertexCount'] for slot in mesh['materials']]
        if not flat_helper:
            obj.data.normals_split_custom_set_from_vertices(normals)
        else:
            repairs.append({'meshIndex':mesh['index'],'repair':'retain-authored-flat-helper-shading'})
        expected=expected_corners(mesh,world[bone_index])
        source_checks.append({'meshIndex':mesh['index'],**compare(expected,actual_corners(obj),mesh,spec)})
        mesh_map.append({'index':mesh['index'],'objectIndex':mesh['object_index'],'boneIndex':bone_index,
            'exportedName':obj.name,'sourceName':mesh['name'],'hidden':mesh['hidden'],
            'shading':'source-flat-helper' if flat_helper else 'source-custom-smooth',
            'slots':[{'index':slot['slot'],'signature':slot['signature'],
                      'exportedMaterialName':obj.material_slots[slot['slot']].material.name} for slot in mesh['materials']]})
    bpy.context.view_layer.update()
    output=Path(spec['output'])
    stem=piece['pieceId']
    write_json(output/(stem+'.source-normals.json'),{'schemaVersion':1,'pieceId':stem,
        'sourceSha256':audit['sha256'],'meshes':[{'index':mesh['index'],'slots':[
            {'index':slot['slot'],'normals':slot['normals']} for slot in mesh['materials']]} for mesh in audit['meshes']]})
    bpy.ops.wm.save_as_mainfile(filepath=str(output/(stem+'.blend')),copy=True)
    fbx=output/(stem+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(fbx),object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,
        use_armature_deform_only=False,bake_anim=False,use_custom_props=True,axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,path_mode='RELATIVE',mesh_smooth_type='OFF')
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj,do_unlink=True)
    bpy.ops.import_scene.fbx(filepath=str(fbx),use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
    imported_rigs=[obj for obj in bpy.context.scene.objects if obj.type=='ARMATURE']
    if len(imported_rigs)!=1:
        raise ValueError('FBX skeleton inventory changed')
    skeleton_fbx=verify_bones(imported_rigs[0],audit,names,world,roundtrip=True)
    actual={int(obj['EaWSourceMeshIndex']):obj for obj in bpy.context.scene.objects if obj.type=='MESH'}
    if set(actual)!=set(range(len(audit['meshes']))):
        raise ValueError('FBX lost source mesh identities')
    roundtrip=[]
    for mesh in audit['meshes']:
        connection=next(c for c in audit['connections'] if c['object_index']==mesh['object_index'])
        obj=actual[mesh['index']]
        if obj['EaWPieceId']!=piece['pieceId'] or [s.material['EaWMaterialSignature'] for s in obj.material_slots]!=[s['signature'] for s in mesh['materials']]:
            raise ValueError('FBX piece identity or material signature changed')
        roundtrip.append({'meshIndex':mesh['index'],**compare(expected_corners(mesh,world[connection['bone_index']]),actual_corners(obj),mesh,spec)})
    return {'pieceId':stem,'transportRigName':transport_rig_name,'source':source_checks,'fbx':roundtrip,'repairs':repairs,'skeletonSource':skeleton_source,
            'skeletonFbx':skeleton_fbx,'bones':len(audit['bones']),'meshes':len(audit['meshes']),
            'triangles':sum(s['triangleCount'] for m in audit['meshes'] for s in m['materials']),
            'sourceMap':{'bones':[{'index':index,'exportedName':name} for index,name in enumerate(names)],'meshes':mesh_map}}


def main():
    spec=json.loads(Path(sys.argv[sys.argv.index('--')+1]).read_text())
    record=Path(spec['processRecord'])
    identity=read_json(record)
    if identity['nonce']!=spec['nonce'] or identity['job']!=str(Path(spec['output']).parent):
        raise ValueError('OPERATION_UNRESOLVED: owned process launch identity changed')
    identity.update(owner=owner(),state='running')
    write_json(record,identity)
    write_json(Path(spec['output']).parent/'BlenderProcess.json',identity)
    if bpy.app.version[:2]!=(3,6):
        raise ValueError('UNSUPPORTED_CAPABILITY: verified worker requires Blender 3.6')
    if spec.get('alamoAddon'):
        addon = Path(spec['alamoAddon'])
        sys.path.insert(0, str(addon.parent if addon.name=='io_alamo_tools' else addon))
    bpy.ops.preferences.addon_enable(module='io_alamo_tools')
    bpy.ops.preferences.addon_enable(module='io_scene_fbx')
    from io_alamo_tools import import_alo
    reports=[]
    for piece in spec['audit']['pieces']:
        journal=Path(spec['output']).parent/'Journal.json'
        if journal.exists() and any(op.get('cancelRequested') for op in json.loads(journal.read_text())['operations'].values()):
            raise ValueError('CANCELLED: safe piece boundary')
        reports.append(convert_piece(spec,piece))
    paths=[Path(spec['output'])/(piece['pieceId']+extension) for piece in spec['audit']['pieces']
           for extension in ('.fbx','.blend','.source-normals.json')]
    paths+=list({Path(texture['path']) for texture in spec['textures'].values()})
    outputs=[{'path':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()} for path in paths]
    report={'schemaVersion':1,'nonce':spec['nonce'],'pid':os.getpid(),'blenderVersion':bpy.app.version_string,
            'alamoImporterHash':hashlib.sha256(Path(import_alo.__file__).read_bytes()).hexdigest(),
            'pieces':reports,'outputs':outputs,'checks':{'staticMeshGeometry':'passed','materialSlots':'passed',
                'fbxRoundTrip':'passed','animation':'not-run','proxyAnimation':'not-run','unity':'not-run'}}
    (Path(spec['output'])/'ConversionReport.json').write_text(json.dumps(report,indent=2))
    identity['state']='succeeded'
    write_json(record,identity)
    write_json(Path(spec['output']).parent/'BlenderProcess.json',identity)


if __name__=='__main__':
    try:
        main()
    except Exception as error:
        request=Path(sys.argv[sys.argv.index('--')+1])
        failure_spec=read_json(request)
        message=str(error)
        code=getattr(error,'code',None)
        if code is None:
            code=next((prefix for prefix in ('CANCELLED','INPUT_CHANGED','OPERATION_UNRESOLVED','UNSUPPORTED_CAPABILITY')
                       if message.startswith(prefix+':')),'VERIFICATION_FAILED')
        write_json(Path(failure_spec['output'])/'WorkerFailure.json',
            {'nonce':failure_spec['nonce'],'code':code,'summary':message})
        process_record=Path(failure_spec['processRecord'])
        failure_identity=read_json(process_record)
        if failure_identity['nonce']==failure_spec['nonce']:
            failure_identity['state']='cancelled' if code=='CANCELLED' else 'failed'
            write_json(process_record,failure_identity)
            write_json(Path(failure_spec['output']).parent/'BlenderProcess.json',failure_identity)
        raise
