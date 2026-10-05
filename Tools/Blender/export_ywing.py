"""Run once through Blender MCP on a fresh ReV_ywing.ALO import.

The binary source has 13 bones including an identity Root removed by ALAMO.
Export static geometry; preserve the hull, hidden helpers and attachments.
Run prepare_ywing_textures.py first to stage lossless DDS conversions.
"""
import bpy
import json
from mathutils import Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/YWingImport/Output'
TEXTURES = {
    'ReV_ywing.dds': ('YWing_Hull_Albedo', False),
    'ReV_ywing_b.dds': ('YWing_Hull_Normal', True),
    'flash_red.dds': ('YWing_MuzzleFlash_Albedo', False),
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


def Triangles(obj):
    obj.data.calc_loop_triangles()
    result = []
    for triangle in obj.data.loop_triangles:
        corners = [(obj.matrix_world @ obj.data.vertices[v].co, obj.data.uv_layers.active.data[l].uv.copy()) for v, l in zip(triangle.vertices, triangle.loops)]
        if (corners[1][0] - corners[0][0]).cross(corners[2][0] - corners[0][0]).length > 0.000000001:
            result.append(corners)
    return result


def VerifyGeometry(source, validation):
    maximum_error = 0.0
    for obj in source.objects:
        if obj.type != 'MESH':
            continue
        other = next(item for item in validation.objects if item.type == 'MESH' and item.get('EaWName') == obj.name)
        original = Triangles(obj)
        copied = Triangles(other)
        if obj.name.endswith('_Flash'):
            # Blender's FBX importer collapses duplicate two-sided helper faces.
            # Their unique position/UV triangles must remain unchanged.
            def Signature(triangle):
                return tuple(sorted(tuple(round(x, 3) for x in (*point, *uv)) for point, uv in triangle))
            assert {Signature(tri) for tri in original} == {Signature(tri) for tri in copied}, obj.name
            continue
        assert len(original) == len(copied), obj.name
        # Match triangle centers, then all three positions and UV coordinates.
        def Center(triangle):
            return tuple(round(sum(c[0][i] for c in triangle) / 3, 2) for i in range(3))
        original.sort(key=Center)
        copied.sort(key=Center)
        for before, after in zip(original, copied):
            errors = [min((point - old[0]).length if (uv - old[1]).length < 0.00001 else 1000 for old in before) for point, uv in after]
            assert max(errors) < 0.001, (obj.name, errors)
            maximum_error = max(maximum_error, max(errors))
    return maximum_error


source_scene = bpy.context.scene
assert source_scene.name == 'YWing Source'
rig = next(obj for obj in source_scene.objects if obj.type == 'ARMATURE')
assert len(rig.data.bones) == 12 and 'Root' not in rig.data.bones
rig.name = 'YWingRig'
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
for source_name, (target_name, is_data) in TEXTURES.items():
    image = bpy.data.images.load(OUTPUT_DIRECTORY + '/Textures/' + target_name + '.png', check_existing=False)
    assert image.size[0] > 0
    image.alpha_mode = 'CHANNEL_PACKED'
    image.colorspace_settings.is_data = is_data
    image.pack()
    images[source_name] = image

materials = []
used_materials = {slot.material.name for obj in source_scene.objects if obj.type == 'MESH' for slot in obj.material_slots}
for material_name in sorted(used_materials):
    material = bpy.data.materials[material_name]
    shader = material.shaderList.shaderList
    base = TEXTURES[material.BaseTexture][0] if material.BaseTexture in TEXTURES else None
    materials.append({'name': material.name, 'shader': shader, 'base': base, 'normal': 'YWing_Hull_Normal' if base == 'YWing_Hull_Albedo' else None})
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
    if base == 'YWing_Hull_Albedo':
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images['ReV_ywing_b.dds']
        normal_map = nodes.new('ShaderNodeNormalMap')
        material.node_tree.links.new(texture.outputs['Color'], normal_map.inputs['Color'])
        material.node_tree.links.new(normal_map.outputs['Normal'], surface.inputs['Normal'])

for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_location = tuple((before['bounds_min'][i] + before['bounds_max'][i]) * 0.5 for i in range(3))
        area.spaces.active.region_3d.view_distance = 320
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/YWing.blend')
fbx_path = OUTPUT_DIRECTORY + '/YWing.fbx'
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation_scene = bpy.data.scenes.new('YWing_FBX_Validation')
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
geometry_error = VerifyGeometry(source_scene, validation_scene)
assert all(mesh['uv_layers'] == 1 for mesh in after['meshes'].values())
assert max(abs(before[key][i] - after[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < 0.001
print('YWing_REPORT', json.dumps({'before': before, 'after': after, 'materials': materials, 'textures': TEXTURES, 'bone_error': bone_error, 'geometry_error': geometry_error, 'fbx': fbx_path}))
bpy.context.window.scene = source_scene
