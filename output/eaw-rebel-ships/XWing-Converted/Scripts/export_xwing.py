"""Export the vanilla RV_XWING import after Temp/XWingImport/ImportAnimations.py.

The original-format ALA parser stages Deploy/Undeploy actions without modifying
the source files or the installed ALAMO importer.
"""
import bpy,json
from mathutils import Vector

OUTPUT_DIRECTORY='F:/Private/empire-at-war/Temp/XWingImport/Output'
TEXTURES={'rv_xwing.dds':'XWing_Hull_Albedo','W_Laser_Small.dds':'XWing_MuzzleFlash_Albedo'}

def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes=[o for o in scene.objects if o.type=='MESH']
    points=[o.matrix_world@v.co for o in meshes if not o.hide_render for v in o.data.vertices]
    return {'meshes':{o.name:{'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'uv_layers':len(o.data.uv_layers),'hidden':o.hide_render,'skinned':any(m.type=='ARMATURE' for m in o.modifiers)} for o in meshes},'bones':{b.name:{'parent':b.parent.name if b.parent else None,'head':list(o.matrix_world@b.head_local)} for o in scene.objects if o.type=='ARMATURE' for b in o.data.bones},'bounds_min':[min(p[i] for p in points) for i in range(3)],'bounds_max':[max(p[i] for p in points) for i in range(3)]}

def PoseBounds(scene):
    points=[]
    graph=bpy.context.evaluated_depsgraph_get()
    for o in scene.objects:
        if o.type!='MESH' or o.hide_render:
            continue
        evaluated=o.evaluated_get(graph)
        mesh=evaluated.to_mesh()
        points.extend(evaluated.matrix_world@v.co for v in mesh.vertices)
        evaluated.to_mesh_clear()
    return [[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]]

scene=bpy.context.scene
assert scene.name=='XWing Source'
rig=next(o for o in scene.objects if o.type=='ARMATURE')
assert len(rig.data.bones)==14 and 'Root' in rig.data.bones
scene.frame_set(30)
before=Snapshot(scene)
for o in scene.objects:
    if o.type!='MESH':
        continue
    o['EaWHidden']=o.hide_render
    o['EaWName']=o.name
    world=o.matrix_world.copy()
    if o.constraints:
        assert len(o.constraints)==1 and o.constraints[0].type=='CHILD_OF'
        constraint=o.constraints[0]
        o.parent=constraint.target
        o.parent_type='BONE'
        o.parent_bone=constraint.subtarget
        o.constraints.remove(constraint)
    else:
        o.parent=rig
    bpy.context.view_layer.update()
    o.matrix_world=world

materials=[]
images={}
for source_name,target in TEXTURES.items():
    image=bpy.data.images.load(OUTPUT_DIRECTORY+'/Textures/'+target+'.png',check_existing=False)
    image.alpha_mode='CHANNEL_PACKED'
    image.pack()
    images[source_name]=image
for material in {s.material for o in scene.objects if o.type=='MESH' for s in o.material_slots}:
    base=material.BaseTexture
    shader=material.shaderList.shaderList
    material['EaWShader']=shader
    materials.append({'name':material.name,'base':base,'shader':shader})
    material.use_nodes=True
    material.node_tree.nodes.clear()
    surface=material.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value=.65
    output=material.node_tree.nodes.new('ShaderNodeOutputMaterial')
    material.node_tree.links.new(surface.outputs['BSDF'],output.inputs['Surface'])
    if base in images:
        texture=material.node_tree.nodes.new('ShaderNodeTexImage')
        texture.image=images[base]
        material.node_tree.links.new(texture.outputs['Color'],surface.inputs['Base Color'])

source_poses={}
for name in ['Deploy','Undeploy']:
    rig.animation_data.action=bpy.data.actions[name]
    source_poses[name]={}
    for frame in [0,15,30]:
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        source_poses[name][str(frame)]={'bounds':PoseBounds(scene),'heads':{b.name:list(rig.matrix_world@b.head) for b in rig.pose.bones}}
rig.animation_data.action=bpy.data.actions['Undeploy']
scene.frame_set(30)
fbx=OUTPUT_DIRECTORY+'/XWing.fbx'
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY+'/XWing.blend')
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=False,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_simplify_factor=0,use_custom_props=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='RELATIVE')
validation=bpy.data.scenes.new('XWing_FBX_Validation')
bpy.context.window.scene=validation
bpy.ops.import_scene.fbx(filepath=fbx,use_anim=True,use_custom_props=True,automatic_bone_orientation=False)
for o in validation.objects:
    if o.type=='MESH':
        o.hide_render=bool(o.get('EaWHidden',False))
        o.hide_set(o.hide_render)
after=Snapshot(validation)
assert set(before['bones'])==set(after['bones'])
assert len(before['meshes'])==len(after['meshes'])
bone_error=max((Vector(before['bones'][n]['head'])-Vector(after['bones'][n]['head'])).length for n in before['bones'])
assert bone_error<.001
assert all(before['bones'][n]['parent']==after['bones'][n]['parent'] for n in before['bones'])
assert all(m['uv_layers']==1 for m in after['meshes'].values())
copied_rig=next(o for o in validation.objects if o.type=='ARMATURE')
animation_error=0.
bound_error=0.
for name in ['Deploy','Undeploy']:
    action=next(a for a in bpy.data.actions if a.name not in ['Deploy','Undeploy'] and a.name.endswith('|'+name))
    copied_rig.animation_data.action=action
    # Blender's FBX importer offsets action keys by one frame.
    offset=action.frame_range[0]
    for frame in [0,15,30]:
        validation.frame_set(int(frame+offset))
        bpy.context.view_layer.update()
        expected=source_poses[name][str(frame)]
        actual=PoseBounds(validation)
        bound_error=max(bound_error,max(abs(actual[j][i]-expected['bounds'][j][i]) for j in range(2) for i in range(3)))
        animation_error=max(animation_error,max((copied_rig.matrix_world@b.head-Vector(expected['heads'][b.name])).length for b in copied_rig.pose.bones))
assert animation_error<.001,(animation_error,bound_error)
assert bound_error<.001,(animation_error,bound_error)
print('XWING_REPORT',json.dumps({'before':before,'after':after,'materials':materials,'source_poses':source_poses,'bone_error':bone_error,'animation_error':animation_error,'bound_error':bound_error}))
bpy.context.window.scene=scene
rig.animation_data.action=bpy.data.actions['Undeploy']
scene.frame_set(30)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_location=(0,0,0)
        area.spaces.active.region_3d.view_distance=75
print('XWing export and animated FBX round trip complete.')
