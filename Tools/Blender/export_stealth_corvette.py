"""Run once through Blender MCP on a fresh ReV_Stealthship.ALO import.

The binary source has 15 bones including an identity Root removed by ALAMO.
Export static geometry; preserve the stealth shell, helpers and attachments.
Run prepare_stealth_corvette_textures.py first to stage lossless DDS conversions.
"""
import bpy
import json
from mathutils import Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/StealthCorvetteImport/Output'
TEXTURES = {
    'ReV_Stealthship.dds': ('StealthCorvette_Hull_Albedo', False),
    'ReV_Stealthship_B.dds': ('StealthCorvette_Hull_Normal', True),
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
        # FBX float precision can open repeated-position shadow faces slightly.
        if min((corners[a][0] - corners[b][0]).length for a, b in ((0, 1), (1, 2), (2, 0))) > 0.0001 and (corners[1][0] - corners[0][0]).cross(corners[2][0] - corners[0][0]).length > 0.000000001:
            result.append(corners)
    return result


def VerifyGeometry(source, validation):
    from mathutils.kdtree import KDTree
    maximum_error = 0.0
    for obj in source.objects:
        if obj.type != 'MESH':
            continue
        other = next(item for item in validation.objects if item.type == 'MESH' and item.get('EaWName') == obj.name)
        if obj.name == 'Shadow':
            # The disabled shadow volume contains repeated-position faces that
            # FBX retriangulates. Verify all position/UV corners in both directions.
            for original_object, copied_object in ((obj, other), (other, obj)):
                tree = KDTree(len(original_object.data.loops))
                for index, loop in enumerate(original_object.data.loops):
                    tree.insert(original_object.matrix_world @ original_object.data.vertices[loop.vertex_index].co, index)
                tree.balance()
                for index, loop in enumerate(copied_object.data.loops):
                    position = copied_object.matrix_world @ copied_object.data.vertices[loop.vertex_index].co
                    uv = copied_object.data.uv_layers.active.data[index].uv
                    matches = [(distance, original_index) for _, original_index, distance in tree.find_range(position, 0.001) if (original_object.data.uv_layers.active.data[original_index].uv - uv).length < 0.00001]
                    assert matches, (obj.name, index)
                    maximum_error = max(maximum_error, min(matches)[0])
            continue
        original = Triangles(obj)
        copied = Triangles(other)
        assert len(original) == len(copied), (obj.name, len(original), len(copied))
        tree = KDTree(len(original))
        for index, triangle in enumerate(original):
            tree.insert(sum((corner[0] for corner in triangle), Vector()) / 3, index)
        tree.balance()
        remaining = set(range(len(original)))
        for triangle in copied:
            center = sum((corner[0] for corner in triangle), Vector()) / 3
            candidates = []
            for _, index, _ in tree.find_range(center, 0.001):
                if index not in remaining:
                    continue
                error = max(min((point - old[0]).length if (uv - old[1]).length < 0.00001 else 1000 for old in original[index]) for point, uv in triangle)
                candidates.append((error, index))
            assert candidates, obj.name
            error, index = min(candidates)
            assert error < 0.001, (obj.name, error)
            remaining.remove(index)
            maximum_error = max(maximum_error, error)
        assert not remaining, obj.name
    return maximum_error

source_scene = bpy.context.scene
assert source_scene.name == 'StealthCorvette Source'
rig = next(obj for obj in source_scene.objects if obj.type == 'ARMATURE')
assert len(rig.data.bones) == 14 and 'Root' not in rig.data.bones
rig.name = 'StealthCorvetteRig'
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
    materials.append({'name': material.name, 'shader': shader, 'base': base, 'normal': TEXTURES[material.NormalTexture][0] if material.NormalTexture in TEXTURES else None})
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

    if shader == 'MeshBumpColorize.fx':
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images['ReV_Stealthship_B.dds']
        normal_map = nodes.new('ShaderNodeNormalMap')
        material.node_tree.links.new(texture.outputs['Color'], normal_map.inputs['Color'])
        material.node_tree.links.new(normal_map.outputs['Normal'], surface.inputs['Normal'])

for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_location = tuple((before['bounds_min'][i] + before['bounds_max'][i]) * 0.5 for i in range(3))
        area.spaces.active.region_3d.view_distance = 420
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/StealthCorvette.blend')
fbx_path = OUTPUT_DIRECTORY + '/StealthCorvette.fbx'
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation_scene = bpy.data.scenes.new('StealthCorvette_FBX_Validation')
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
print('StealthCorvette_REPORT', json.dumps({'before': before, 'after': after, 'materials': materials, 'textures': TEXTURES, 'bone_error': bone_error, 'geometry_error': geometry_error, 'fbx': fbx_path}))
bpy.context.window.scene = source_scene
