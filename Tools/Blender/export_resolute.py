"""Run once through Blender MCP on a fresh ReV_resolute.ALO import.

The source contains 78 bones and an identity Root removed by the importer.
Preserves all imported meshes, UVs, attachments and authored hidden state.
"""
import bpy
import json
from mathutils import Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/ResoluteImport/Output'
TEXTURES = {
    'Rev_Resolute.dds': 'Resolute_Hull_Albedo',
    'ReV_venator.dds': 'Resolute_Engines_Albedo',
    'ReV_venatorB.dds': 'Resolute_Hull_Normal',
    'ReV_venatorG.dds': 'Resolute_Lights_Emissive',
    'ReV_VenatorTd.dds': 'Resolute_Turrets_Albedo',
    'Rev_venatorTB.dds': 'Resolute_Turrets_Normal',
    'EngineGlow.dds': 'Resolute_Thruster_Albedo',
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
assert len(rig.data.bones) == 77 and 'Root' not in rig.data.bones
rig.name = 'ResoluteRig'
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
    obj['EaWName'] = obj.name
    # The installed Blender 4.2 importer already creates exportable bone parents.
    assert not obj.constraints and obj.parent == rig and obj.parent_type == 'BONE'
after_parenting = Snapshot(source_scene)
assert max(abs(before[key][i] - after_parenting[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < 0.001

for source_name, target_name in TEXTURES.items():
    image = bpy.data.images[source_name]
    assert image.size[0] > 0
    image.filepath_raw = OUTPUT_DIRECTORY + '/Textures/' + target_name + '.png'
    assert 'PNG' in [item.identifier for item in image.bl_rna.properties['file_format'].enum_items]
    image.file_format = 'PNG'
    image.save()

materials = []
used = {slot.material.name for obj in source_scene.objects if obj.type == 'MESH' for slot in obj.material_slots}
for name in sorted(used):
    material = bpy.data.materials[name]
    shader = material.shaderList.shaderList
    base = TEXTURES.get(material.BaseTexture)
    normal = TEXTURES.get(material.NormalTexture) if 'Bump' in shader else None
    materials.append({'name': name, 'shader': shader, 'base': base, 'normal': normal})
    material['EaWShader'] = shader
    material.use_nodes = True
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
        if 'Additive' in shader:
            emission_input = next(i for i in surface.inputs if i.name.startswith('Emission') and i.type == 'RGBA')
            material.node_tree.links.new(texture.outputs['Color'], emission_input)
    if normal:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = bpy.data.images[material.NormalTexture]
        texture.image.colorspace_settings.is_data = True
        normal_map = nodes.new('ShaderNodeNormalMap')
        material.node_tree.links.new(texture.outputs['Color'], normal_map.inputs['Color'])
        material.node_tree.links.new(normal_map.outputs['Normal'], surface.inputs['Normal'])

bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/Resolute.blend')
fbx_path = OUTPUT_DIRECTORY + '/Resolute.fbx'
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation_scene = bpy.data.scenes.new('Resolute_FBX_Validation')
bpy.context.window.scene = validation_scene
bpy.ops.import_scene.fbx(filepath=fbx_path, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
for obj in validation_scene.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj.get('EaWHidden', False))
        obj.hide_set(obj.hide_render)
after = Snapshot(validation_scene)
assert set(before['bones']) == set(after['bones'])
assert all(before['bones'][name]['parent'] == after['bones'][name]['parent'] for name in before['bones'])
assert max(abs(before['bones'][name]['head'][i] - after['bones'][name]['head'][i]) for name in before['bones'] for i in range(3)) < 0.001
assert len(before['meshes']) == len(after['meshes'])
assert all(mesh['uv_layers'] > 0 for mesh in after['meshes'].values())
assert sum(mesh['triangles'] for mesh in before['meshes'].values()) == sum(mesh['triangles'] for mesh in after['meshes'].values())
assert max(abs(before[key][i] - after[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < 0.001
print('RESOLUTE_REPORT', json.dumps({'before': before, 'after': after, 'materials': materials, 'textures': TEXTURES, 'fbx': fbx_path}))
