"""Export a fresh TIE_AVENGER.ALO import in the TIEAvenger Source scene.

Scope ALAMO's final cleanup to this scene when its global cleanup encounters
objects in another open scene. SourceAudit.json records the 16-bone source.
"""
import bpy
import json
from mathutils import Matrix, Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/TIEAvengerImport/Output'


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [obj for obj in scene.objects if obj.type == 'MESH']
    points = [obj.matrix_world @ vertex.co for obj in meshes if not obj.hide_render for vertex in obj.data.vertices]
    return {
        'meshes': {obj.get('EaWName', obj.name): {
            'triangles': sum(len(poly.vertices) - 2 for poly in obj.data.polygons),
            'uv_layers': len(obj.data.uv_layers), 'hidden': obj.hide_render,
        } for obj in meshes},
        'bones': {bone.name: {'parent': bone.parent.name if bone.parent else None,
                            'head': list(obj.matrix_world @ bone.head_local)}
                  for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.data.bones},
        'bounds_min': [min(point[i] for point in points) for i in range(3)],
        'bounds_max': [max(point[i] for point in points) for i in range(3)],
    }


source_scene = bpy.data.scenes['TIEAvenger Source']
bpy.context.window.scene = source_scene
assert source_scene.name == 'TIEAvenger Source'
rig = next(obj for obj in source_scene.objects if obj.type == 'ARMATURE')
assert len(rig.data.bones) == 16 and 'Root' in rig.data.bones
rig.name = 'TIEAvengerRig'
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
root = rig.data.edit_bones['Root']
for bone in rig.data.edit_bones:
    if bone != root:
        bone.parent = root
bpy.ops.object.mode_set(mode='OBJECT')
for obj in list(source_scene.objects):
    if obj.type != 'MESH':
        continue
    shader = obj.data.materials[0].shaderList.shaderList
    if shader in ('MeshCollision.fx', 'MeshShadowVolume.fx'):
        bpy.ops.object.select_all(action='DESELECT')
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.mesh.remove_doubles()
        bpy.ops.object.mode_set(mode='OBJECT')
    obj.hide_render = obj.name != 'Tie_Adv_LOD2'
    obj.hide_set(obj.hide_render)
    for constraint in obj.constraints:
        constraint.inverse_matrix = Matrix.Identity(4)
before = Snapshot(source_scene)
for obj in source_scene.objects:
    if obj.type != 'MESH':
        continue
    obj['EaWHidden'] = obj.hide_render
    obj['EaWName'] = obj.name
    world = obj.matrix_world.copy()
    assert len(obj.constraints) == 1 and obj.constraints[0].type == 'CHILD_OF'
    constraint = obj.constraints[0]
    obj.parent = constraint.target
    obj.parent_type = 'BONE'
    obj.parent_bone = constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
after_parenting = Snapshot(source_scene)
assert max(abs(before[key][i] - after_parenting[key][i])
           for key in ('bounds_min', 'bounds_max') for i in range(3)) < 0.001

images = {}
for name, is_data in [('TIEAvenger_Hull_Albedo', False), ('TIEAvenger_Hull_Normal', True)]:
    image = bpy.data.images.load(OUTPUT_DIRECTORY + '/Textures/' + name + '.png', check_existing=False)
    image.colorspace_settings.is_data = is_data
    image.pack()
    images[name] = image
materials = {}
for obj in source_scene.objects:
    if obj.type != 'MESH':
        continue
    for slot in obj.material_slots:
        original = slot.material
        if original.name not in materials:
            material = original.copy()
            material.name = 'TIEAvenger_' + ('Hull' if original.BaseTexture != 'None' else original.name.title())
            material['EaWShader'] = original.shaderList.shaderList
            material.use_nodes = True
            material.node_tree.nodes.clear()
            surface = material.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
            surface.inputs['Roughness'].default_value = 0.65
            output = material.node_tree.nodes.new('ShaderNodeOutputMaterial')
            material.node_tree.links.new(surface.outputs['BSDF'], output.inputs['Surface'])
            if original.BaseTexture != 'None':
                texture = material.node_tree.nodes.new('ShaderNodeTexImage')
                texture.image = images['TIEAvenger_Hull_Albedo']
                material.node_tree.links.new(texture.outputs['Color'], surface.inputs['Base Color'])
            materials[original.name] = material
        slot.material = materials[original.name]

fbx_path = OUTPUT_DIRECTORY + '/TIEAvenger.fbx'
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/TIEAvenger.blend')
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False,
    object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False,
    use_armature_deform_only=False, bake_anim=False, use_custom_props=True,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation = bpy.data.scenes.new('TIEAvenger FBX Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=fbx_path, use_anim=False,
    use_custom_props=True, automatic_bone_orientation=False)
for obj in validation.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj['EaWHidden'])
        obj.hide_set(obj.hide_render)
after = Snapshot(validation)
assert before['meshes'] == after['meshes'], (before['meshes'], after['meshes'])
assert set(before['bones']) == set(after['bones'])
assert all(before['bones'][name]['parent'] == after['bones'][name]['parent'] for name in before['bones'])
bone_error = max((Vector(before['bones'][name]['head']) - Vector(after['bones'][name]['head'])).length
                 for name in before['bones'])
bounds_error = max(abs(before[key][i] - after[key][i])
                   for key in ('bounds_min', 'bounds_max') for i in range(3))
assert bone_error < 0.001 and bounds_error < 0.001
report = {'before': before, 'after': after, 'maximum_bone_error': bone_error,
          'maximum_bounds_error': bounds_error}
bpy.context.window.scene = source_scene
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_location = Vector((0, -0.48, 0))
        area.spaces.active.region_3d.view_distance = 12
print(json.dumps(report))
