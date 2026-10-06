"""Run through Blender MCP on audited fresh AOTR hull/turret scenes.

ALAMO's global final cleanup fails on meshes in other scenes. Complete only
this model's parenting, inverse matrices and helper cleanup here.
"""
import bpy
import json
from mathutils import Matrix, Vector
from mathutils.kdtree import KDTree

VARIANT = 'Hull'
OUTPUT = 'F:/Private/empire-at-war/Temp/YWingBomberImport/Output'
STEM = 'YWingBomber' if VARIANT == 'Hull' else 'YWingBomberTurret'
SCENE = STEM + ' Source'


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [o for o in scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ v.co for o in meshes if not o.hide_render for v in o.data.vertices]
    return {
        'meshes': {o.get('EaWName', o.name): {'triangles': sum(len(p.vertices)-2 for p in o.data.polygons),
            'uv_layers': len(o.data.uv_layers), 'hidden': o.hide_render} for o in meshes},
        'bones': {b.name: {'parent': b.parent.name if b.parent else None, 'head': list(o.matrix_world @ b.head_local)}
            for o in scene.objects if o.type == 'ARMATURE' for b in o.data.bones},
        'bounds_min': [min(p[i] for p in points) for i in range(3)],
        'bounds_max': [max(p[i] for p in points) for i in range(3)],
    }


def Geometry(obj):
    obj.data.calc_loop_triangles()
    result = []
    for triangle in obj.data.loop_triangles:
        points = [obj.matrix_world @ obj.data.vertices[v].co for v in triangle.vertices]
        if (points[1]-points[0]).cross(points[2]-points[0]).length < 1e-9:
            continue
        result.extend((p,obj.data.uv_layers.active.data[l].uv.copy()) for p,l in zip(points,triangle.loops))
    return result


def VerifyCorners(original, copied):
    points = Geometry(original)
    tree = KDTree(len(points))
    for index,(point,uv) in enumerate(points):
        tree.insert(point,index)
    tree.balance()
    maximum = 0.0
    for point,uv in Geometry(copied):
        errors = [distance for position,index,distance in tree.find_range(point,0.001)
            if (uv-points[index][1]).length < 0.00001]
        assert errors, (original.name,list(point),list(uv))
        maximum = max(maximum,min(errors))
    return maximum


scene = bpy.data.scenes[SCENE]
bpy.context.window.scene = scene
rig = next(o for o in scene.objects if o.type == 'ARMATURE')
assert len(rig.data.bones) == (17 if VARIANT == 'Hull' else 6)
assert 'Root' in rig.data.bones
rig.name = STEM + 'Rig'
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
root = rig.data.edit_bones['Root']
for bone in rig.data.edit_bones:
    if bone != root and bone.parent is None:
        bone.parent = root
bpy.ops.object.mode_set(mode='OBJECT')

for obj in list(scene.objects):
    if obj.type != 'MESH':
        continue
    shader = obj.data.materials[0].shaderList.shaderList
    obj.hide_render = shader in ('MeshCollision.fx', 'MeshShadowVolume.fx')
    if obj.hide_render:
        bpy.ops.object.select_all(action='DESELECT')
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.mesh.remove_doubles()
        bpy.ops.object.mode_set(mode='OBJECT')
    for constraint in obj.constraints:
        constraint.inverse_matrix = Matrix.Identity(4)
    obj['EaWName'] = obj.name
    obj['EaWHidden'] = obj.hide_render
    obj['EaWShader'] = shader
bpy.context.view_layer.update()
before = Snapshot(scene)
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    world = obj.matrix_world.copy()
    assert len(obj.constraints) == 1
    constraint = obj.constraints[0]
    obj.parent = constraint.target
    obj.parent_type = 'BONE'
    obj.parent_bone = constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
    obj.hide_set(obj.hide_render)
parented = Snapshot(scene)
assert max(abs(before[k][i]-parented[k][i]) for k in ('bounds_min','bounds_max') for i in range(3)) < 0.001

materials = {}
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    for slot in obj.material_slots:
        original = slot.material
        if original.name not in materials:
            part = 'Engine' if original.BaseTexture == 'Y_Wing_Engine_Glow.dds' else 'Hull' if original.BaseTexture == 'REB_YWing_C.dds' else 'Helper'
            material = original.copy()
            material.name = STEM + '_' + part
            material['EaWShader'] = original.shaderList.shaderList
            material.use_nodes = True
            material.node_tree.nodes.clear()
            surface = material.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
            surface.inputs['Roughness'].default_value = 0.65
            output = material.node_tree.nodes.new('ShaderNodeOutputMaterial')
            material.node_tree.links.new(surface.outputs['BSDF'], output.inputs['Surface'])
            if part != 'Helper':
                image = bpy.data.images.load(OUTPUT+'/Textures/YWingBomber_'+part+'_Albedo.png',check_existing=True)
                image.alpha_mode = 'CHANNEL_PACKED'
                image.pack()
                texture = material.node_tree.nodes.new('ShaderNodeTexImage')
                texture.image = image
                material.node_tree.links.new(texture.outputs['Color'],surface.inputs['Base Color'])
            materials[original.name] = material
        slot.material = materials[original.name]

fbx = OUTPUT+'/'+STEM+'.fbx'
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT+'/'+STEM+'.blend')
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=False,object_types={'MESH','ARMATURE','EMPTY'},
    add_leaf_bones=False,use_armature_deform_only=False,bake_anim=False,use_custom_props=True,
    axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='RELATIVE')
validation = bpy.data.scenes.new(STEM+' FBX Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=fbx,use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
for obj in validation.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj['EaWHidden'])
        obj.hide_set(obj.hide_render)
after = Snapshot(validation)
assert set(before['bones']) == set(after['bones'])
assert all(before['bones'][n]['parent'] == after['bones'][n]['parent'] for n in before['bones'])
bone_error = max((Vector(before['bones'][n]['head'])-Vector(after['bones'][n]['head'])).length for n in before['bones'])
bounds_error = max(abs(before[k][i]-after[k][i]) for k in ('bounds_min','bounds_max') for i in range(3))
assert bone_error < 0.001 and bounds_error < 0.001
assert len(before['meshes']) == len(after['meshes'])
geometry_error = 0.0
for obj in scene.objects:
    if obj.type == 'MESH':
        other = next(o for o in validation.objects if o.type == 'MESH' and o['EaWName'] == obj['EaWName'])
        geometry_error = max(geometry_error,VerifyCorners(obj,other),VerifyCorners(other,obj))
report = {'before':before,'after':after,'maximum_bone_error':bone_error,'maximum_bounds_error':bounds_error,'maximum_geometry_error':geometry_error,'position_uv_geometry_verified':True}
bpy.context.window.scene = scene
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_location = Vector(tuple((before['bounds_min'][i]+before['bounds_max'][i])/2 for i in range(3)))
        area.spaces.active.region_3d.view_distance = 12
print('CONVERSION_REPORT '+json.dumps(report))
