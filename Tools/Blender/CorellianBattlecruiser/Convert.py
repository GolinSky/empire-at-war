"""Convert each audited hull/turret in its own Blender scene through MCP."""
import bpy
import json
from mathutils import Vector

NAME = 'CorellianBattlecruiser'
TASK = 'F:/Private/empire-at-war/Temp/CorellianBattlecruiserImport'
for stale in list(bpy.data.scenes):
    if stale.name.startswith(NAME) and stale.name!=NAME+' Source':
        for obj in list(stale.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.scenes.remove(stale)
materials = {}
for definition in binary['materials']:
    material = bpy.data.materials.get(definition['name']) or bpy.data.materials.new(definition['name'])
    material.use_nodes = True
    material['EaWShader'] = definition['shader']
    nodes, links = material.node_tree.nodes, material.node_tree.links
    nodes.clear()
    output, surface = nodes.new('ShaderNodeOutputMaterial'), nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value = .65
    links.new(surface.outputs['BSDF'], output.inputs['Surface'])
    for role in ('base','normal'):
        if not definition[role]:
            continue
        image = bpy.data.images.load(TASK+'/Textures/'+definition[role]+'.png', check_existing=True)
        image.alpha_mode = 'CHANNEL_PACKED'
        image.colorspace_settings.is_data = role=='normal'
        image.pack()
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = image
        if role=='base':
            links.new(texture.outputs['Color'], surface.inputs['Base Color'])
            if 'Additive' in definition['shader']:
                links.new(texture.outputs['Color'],surface.inputs['Emission'])
        else:
            normal = nodes.new('ShaderNodeNormalMap')
            links.new(texture.outputs['Color'], normal.inputs['Color'])
            links.new(normal.outputs['Normal'], surface.inputs['Normal'])
    materials[definition['name']] = material
reports = {}
for index, (model_name, model) in enumerate(binary['models'].items()):
    if index==0:
        scene = bpy.data.scenes[NAME+' Source']
    else:
        scene = bpy.data.scenes.new(model_name+' Source')
        bpy.context.window.scene = scene
        # ALAMO's hideLODs scans global objects; temporarily link them to satisfy its view-layer selection.
        borrowed = [(o,o.hide_render,o.hide_viewport) for o in list(bpy.data.objects)]
        for obj,hidden,viewport in borrowed:
            scene.collection.objects.link(obj)
        try:
            bpy.ops.import_mesh.alo(filepath=model['file'],importAnimations=False)
        finally:
            for obj,hidden,viewport in borrowed:
                scene.collection.objects.unlink(obj)
                obj.hide_render,obj.hide_viewport=hidden,viewport
    bpy.context.window.scene = scene
    rig = next(o for o in scene.objects if o.type=='ARMATURE')
    source_bones = list(audit['models'].values())[index]['bones']
    if 'Root' not in rig.data.bones:
        assert source_bones[0]['name']=='Root' and source_bones[0]['matrix']==[1,0,0,0,0,1,0,0,0,0,1,0]
        assert len(rig.data.bones)==len(source_bones)-1
        bpy.context.view_layer.objects.active=rig
        rig.select_set(True)
        bpy.ops.object.mode_set(mode='EDIT')
        root = rig.data.edit_bones.new('Root')
        root.head,root.tail=(0,0,0),(0,1,0)
        for bone in rig.data.edit_bones:
            if bone!=root and bone.parent is None:
                bone.parent=root
        bpy.ops.object.mode_set(mode='OBJECT')
    rig.name=model_name+'Rig'
    meshes = [o for o in scene.objects if o.type=='MESH']
    assert len(meshes)==len(model['meshes'])
    for record,obj in zip(model['meshes'],meshes):
        assert obj.name.split('.')[0]==record['name'], (record['name'],obj.name)
        obj['EaWName']=record['name']
        obj['EaWHidden']=obj.hide_render
        obj['EaWShader']=record['materials'][0]['shader']
        obj.data.materials.clear()
        offset=0
        for slot,definition in enumerate(record['materials']):
            obj.data.materials.append(materials[definition['materialName']])
            for polygon in obj.data.polygons[offset:offset+definition['triangleCount']]:
                polygon.material_index=slot
            offset+=definition['triangleCount']
        assert len(obj.data.polygons)==offset or (obj.hide_render and 'Shadow' in record['materials'][0]['shader'])
    before=Snapshot(scene)
    for obj in meshes:
        if not obj.constraints:
            continue
        world=obj.matrix_world.copy()
        assert len(obj.constraints)==1 and obj.constraints[0].type=='CHILD_OF'
        constraint=obj.constraints[0]
        obj.parent,obj.parent_type,obj.parent_bone=constraint.target,'BONE',constraint.subtarget
        obj.constraints.remove(constraint)
        bpy.context.view_layer.update()
        obj.matrix_world=world
    after_parenting=Snapshot(scene)
    assert max(abs(before[k][i]-after_parenting[k][i]) for k in ('bounds_min','bounds_max') for i in range(3))<.001
    fbx=TASK+'/'+model_name+'.fbx'
    bpy.ops.wm.save_as_mainfile(filepath=TASK+'/'+model_name+'.blend',copy=True)
    bpy.ops.export_scene.fbx(filepath=fbx,use_selection=False,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=False,use_custom_props=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='RELATIVE')
    validation=bpy.data.scenes.new(model_name+' FBX Validation')
    bpy.context.window.scene=validation
    bpy.ops.import_scene.fbx(filepath=fbx,use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
    for obj in validation.objects:
        if obj.type=='MESH':
            obj.hide_render=bool(obj['EaWHidden'])
            obj.hide_set(obj.hide_render)
    after=Snapshot(validation)
    assert set(before['bones'])==set(after['bones'])
    assert all(before['bones'][n]['parent']==after['bones'][n]['parent'] for n in before['bones'])
    bone_error=max((Vector(before['bones'][n]['head'])-Vector(after['bones'][n]['head'])).length for n in before['bones'])
    assert bone_error<.001
    geometry_error=VerifyGeometry(scene,validation)
    reports[model_name]=dict(before=before,after=after,bone_error=bone_error,geometry_error=geometry_error)
report=dict(models=reports,materials=binary['materials'],mesh_materials=binary['models'])
print('CONVERSION_JSON',json.dumps(report))
bpy.context.window.scene=bpy.data.scenes[NAME+' Source']
print('CONVERTED', {k:dict(meshes=len(v['before']['meshes']),bones=len(v['before']['bones']),bone_error=v['bone_error'],geometry_error=v['geometry_error']) for k,v in reports.items()})
