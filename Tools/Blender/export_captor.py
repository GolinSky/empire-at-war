"""Run once through Blender MCP after a fresh intact CIS_Carrier.ALO import.

The checked binary source has 64 bones and an identity Root; the importer
removes Root. This model-specific export restores it and preserves helpers.
"""
import bpy
import json
from mathutils import Vector

SOURCE_DIRECTORY = 'C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Carrier'
OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/CaptorImport/Output'
TEXTURES = {
    'CIS_CARRIER.dds': (SOURCE_DIRECTORY + '/CIS_CARRIER.dds', 'Captor_Hull_Albedo'),
    'CIS_CARRIER_B.dds': (SOURCE_DIRECTORY + '/CIS_CARRIER_B.dds', 'Captor_Hull_Normal'),
    'Yellow_thruster.dds': ('C:/Users/golin/Documents/CIS_Hero_Units_Pack_2014/Admiral Trench/Yellow_thruster.dds', 'Captor_Thruster_Albedo'),
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


source_scene = bpy.context.scene
rig = next(obj for obj in source_scene.objects if obj.type == 'ARMATURE')
assert len(rig.data.bones) == 63 and 'Root' not in rig.data.bones
rig.name = 'CaptorRig'
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
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
    assert 'BONE' in [item.identifier for item in obj.bl_rna.properties['parent_type'].enum_items]
    obj.parent_type = 'BONE'
    obj.parent_bone = constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
after_parenting = Snapshot(source_scene)
assert max(abs(before[key][i] - after_parenting[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < 0.001

images = {}
for source_name, (disk_path, target_name) in TEXTURES.items():
    image = bpy.data.images.load(disk_path, check_existing=True)
    assert image.size[0] > 0
    images[source_name] = image
    image.filepath_raw = OUTPUT_DIRECTORY + '/Textures/' + target_name + '.png'
    assert 'PNG' in [item.identifier for item in image.bl_rna.properties['file_format'].enum_items]
    image.file_format = 'PNG'
    image.save()

materials = []
used_materials = {slot.material.name for obj in source_scene.objects if obj.type == 'MESH' for slot in obj.material_slots}
for material_name in sorted(used_materials):
    material = bpy.data.materials[material_name]
    shader = material.shaderList.shaderList
    base = TEXTURES[material.BaseTexture][1] if material.BaseTexture in TEXTURES else None
    normal = TEXTURES[material.NormalTexture][1] if 'Bump' in shader else None
    materials.append({'name': material.name, 'shader': shader, 'base': base, 'normal': normal})
    material['EaWShader'] = shader
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new('ShaderNodeOutputMaterial')
    surface = nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value = 0.65
    material.node_tree.links.new(surface.outputs['BSDF'], output.inputs['Surface'])
    if base:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images[material.BaseTexture]
        material.node_tree.links.new(texture.outputs['Color'], surface.inputs['Base Color'])
    if normal:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images[material.NormalTexture]
        texture.image.colorspace_settings.is_data = True
        normal_map = nodes.new('ShaderNodeNormalMap')
        material.node_tree.links.new(texture.outputs['Color'], normal_map.inputs['Color'])
        material.node_tree.links.new(normal_map.outputs['Normal'], surface.inputs['Normal'])

for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_location = (0, 0, 55)
        area.spaces.active.region_3d.view_distance = 500
        area.spaces.active.clip_end = 5000
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/Captor.blend')
fbx_path = OUTPUT_DIRECTORY + '/Captor.fbx'
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation_scene = bpy.data.scenes.new('Captor_FBX_Validation')
bpy.context.window.scene = validation_scene
bpy.ops.import_scene.fbx(filepath=fbx_path, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
for obj in validation_scene.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj.get('EaWHidden', False))
        obj.hide_set(obj.hide_render)
after = Snapshot(validation_scene)
assert set(before['bones']) == set(after['bones'])
bone_error = max((Vector(before['bones'][name]['head']) - Vector(after['bones'][name]['head'])).length for name in before['bones'])
assert bone_error < 0.001
assert all(before['bones'][name]['parent'] == after['bones'][name]['parent'] for name in before['bones'])
assert len(before['meshes']) == len(after['meshes'])
assert sum(m['triangles'] for m in before['meshes'].values()) == sum(m['triangles'] for m in after['meshes'].values())
assert all(m['uv_layers'] == 1 for m in after['meshes'].values())
assert max(abs(before[key][i] - after[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < 0.001
print('CAPTOR_REPORT', json.dumps({'before': before, 'after': after, 'materials': materials, 'textures': TEXTURES, 'bone_error': bone_error, 'fbx': fbx_path}))
bpy.context.window.scene = source_scene
