"""Run through Blender MCP after importing the Full Armed ALO in Blender 3.6.

Exports to the project's ignored Temp folder. The source ALO is never modified.
"""
import bpy
import json
from mathutils import Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/BlenderConversion/Output'
TEXTURES = {
    'Rothana_hull_1_Base.dds': 'Rothana_Hull01_Albedo',
    'Rothana_hull_1_norm.dds': 'Rothana_Hull01_Normal',
    'Rothana_hull_2_base.dds': 'Rothana_Hull02_Albedo',
    'Rothana_hull_2_Norm.dds': 'Rothana_Hull02_Normal',
    'Rothana_hull_3_base.dds': 'Rothana_Hull03_Albedo',
    'Rothana_hull_3_norm.dds': 'Rothana_Hull03_Normal',
    'Rothana_engines.dds': 'Rothana_Engines_Albedo',
    'Rothana_engines_Normal.dds': 'Rothana_Engines_Normal',
    'Rothana_reactor.dds': 'Rothana_Reactor_Albedo',
    'Rothana_reactor_nm.dds': 'Rothana_Reactor_Normal',
    'Hanger.dds': 'Rothana_Hangar_Albedo',
    'Hanger_B.dds': 'Rothana_Hangar_Normal',
    'Hull_Lights_Grid_Sparse_Rebel.dds': 'Rothana_Lights_Emissive',
    'Nb_shieldbase.dds': 'Rothana_Shield_Albedo',
    'Nb_shieldripple.dds': 'Rothana_Shield_Distortion',
    'Nb_shieldwave.dds': 'Rothana_Shield_Wave',
}


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [obj for obj in scene.objects if obj.type == 'MESH']
    points = [obj.matrix_world @ vertex.co for obj in meshes if not obj.hide_render for vertex in obj.data.vertices]
    return {
        'meshes': {obj.name: {'vertices': len(obj.data.vertices), 'triangles': sum(len(poly.vertices) - 2 for poly in obj.data.polygons), 'uv_layers': len(obj.data.uv_layers), 'materials': [slot.material.name for slot in obj.material_slots], 'hidden': obj.hide_render} for obj in meshes},
        'bones': {bone.name: {'parent': bone.parent.name if bone.parent else None, 'head': list(obj.matrix_world @ bone.head_local)} for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.data.bones},
        'bounds_min': [min(point[i] for point in points) for i in range(3)],
        'bounds_max': [max(point[i] for point in points) for i in range(3)],
    }


rig = bpy.data.objects['Rothana_Stardestroyer_Full_ArmedRig']
assert len(rig.data.bones) == 99, 'Run once on the original ALO import.'
source_scene = bpy.context.scene
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
# The ALO header contains 100 bones and an identity Root. The add-on removes it.
root = rig.data.edit_bones.new('Root')
root.head = (0, 0, 0)
root.tail = (0, 1, 0)
for bone in rig.data.edit_bones:
    if bone != root and bone.parent is None:
        bone.parent = root
bpy.ops.object.mode_set(mode='OBJECT')
before = Snapshot(source_scene)
for obj in source_scene.objects:
    if obj.type != 'MESH':
        continue
    obj['EaWHidden'] = obj.hide_render
    world = obj.matrix_world.copy()
    assert len(obj.constraints) == 1 and obj.constraints[0].type == 'CHILD_OF'
    constraint = obj.constraints[0]
    obj.parent = constraint.target
    obj.parent_type = 'BONE'
    obj.parent_bone = constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
bpy.context.view_layer.update()
after_parenting = Snapshot(source_scene)
assert max(abs(before[key][i] - after_parenting[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < 0.001

for source_name, target_name in TEXTURES.items():
    image = bpy.data.images[source_name]
    assert image.size[0] > 0
    image.filepath_raw = OUTPUT_DIRECTORY + '/Textures/' + target_name + '.png'
    image.file_format = 'PNG'
    image.save()

materials = []
used_materials = {slot.material.name for obj in source_scene.objects if obj.type == 'MESH' for slot in obj.material_slots}
for material_name in sorted(used_materials):
    material = bpy.data.materials[material_name]
    shader = material.shaderList.shaderList
    base = TEXTURES.get(material.BaseTexture)
    normal = TEXTURES.get(material.NormalTexture) if 'Bump' in shader else None
    materials.append({'name': material.name, 'shader': shader, 'base': base, 'normal': normal})
    material['EaWShader'] = shader
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new('ShaderNodeOutputMaterial')
    surface = nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value = 0.6
    material.node_tree.links.new(surface.outputs['BSDF'], output.inputs['Surface'])
    if base:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = bpy.data.images[material.BaseTexture]
        material.node_tree.links.new(texture.outputs['Color'], surface.inputs['Base Color'])
        if 'Additive' in shader or 'Shield' in shader:
            material.node_tree.links.new(texture.outputs['Color'], surface.inputs['Emission'])
    if normal:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = bpy.data.images[material.NormalTexture]
        normal_map = nodes.new('ShaderNodeNormalMap')
        material.node_tree.links.new(texture.outputs['Color'], normal_map.inputs['Color'])
        material.node_tree.links.new(normal_map.outputs['Normal'], surface.inputs['Normal'])

fbx_path = OUTPUT_DIRECTORY + '/Rothana.fbx'
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/Rothana.blend')
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation_scene = bpy.data.scenes.new('Rothana_FBX_Validation')
bpy.context.window.scene = validation_scene
bpy.ops.import_scene.fbx(filepath=fbx_path, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
for obj in validation_scene.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj.get('EaWHidden', False))
        obj.hide_set(obj.hide_render)
after = Snapshot(validation_scene)
print('ROTHANA_REPORT', json.dumps({'before': before, 'after': after, 'materials': materials, 'textures': TEXTURES, 'fbx': fbx_path}))
bpy.context.window.scene = source_scene
