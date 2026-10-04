"""Run through Blender MCP on freshly imported Mandator Source or Wreck scenes.

Both binary sources contain an identity Root omitted by the ALAMO importer.
The intact source has 105 bones; the damaged source has 70. Preserve imported
unique names for duplicate proxy bones and all source geometry/hidden flags.
"""
import bpy
import json
import math
from mathutils import Vector

TEXTURE_DIRECTORY = 'F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Textures'
OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/MandatorImport/Output'
TEXTURES = {
    'rev_mandator_hull': ('ReV_mandator_hull.dds', 'Mandator_Hull_Albedo'),
    'rev_mandator_hull_bump': ('ReV_Mandator_Hull_Bump.dds', 'Mandator_Hull_Normal'),
    'rev_mandator_cortex': ('ReV_Mandator_cortex.dds', 'Mandator_Cortex_Albedo'),
    'rev_mandator_cortex_bump': ('ReV_Mandator_Cortex_Bump.dds', 'Mandator_Cortex_Normal'),
    'rev_mandator_windows': ('ReV_Mandator_windows.dds', 'Mandator_Windows_Albedo'),
    'rev_mandator_windows_bump': ('ReV_Mandator_Windows_Bump.dds', 'Mandator_Windows_Normal'),
    'rev_mandator_thruster': ('ReV_Mandator_thruster.dds', 'Mandator_Thruster_Albedo'),
    'rev_mandator_lights': ('ReV_Mandator_Lights.dds', 'Mandator_Lights_Emissive'),
    'w_blast00': ('W_blast00.dds', 'Mandator_Damage_Albedo'),
}


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [obj for obj in scene.objects if obj.type == 'MESH']
    visible = [obj for obj in meshes if not obj.hide_render and not obj.get('EaWHelper', False)]
    points = [obj.matrix_world @ vertex.co for obj in visible for vertex in obj.data.vertices]
    triangles = []
    for obj in meshes:
        obj.data.calc_loop_triangles()
        uv = obj.data.uv_layers.active
        for triangle in obj.data.loop_triangles:
            corners = []
            for vertex_index, loop_index in zip(triangle.vertices, triangle.loops):
                point = obj.matrix_world @ obj.data.vertices[vertex_index].co
                tex = uv.data[loop_index].uv if uv else (0, 0)
                corners.append(tuple(round(x, 3) for x in (*point, *tex)))
            a, b, c = [obj.matrix_world @ obj.data.vertices[i].co for i in triangle.vertices]
            if (b-a).cross(c-a).length > 0.001:
                triangles.append(tuple(sorted(corners)))
    return {
        'meshes': {obj.name: {'vertices': len(obj.data.vertices), 'triangles': len(obj.data.loop_triangles), 'uv_layers': len(obj.data.uv_layers), 'materials': [slot.material.name for slot in obj.material_slots], 'hidden': obj.hide_render, 'helper': bool(obj.get('EaWHelper', False)), 'canonical': obj.get('EaWName', obj.name)} for obj in meshes},
        'bones': {bone.name: {'parent': bone.parent.name if bone.parent else None, 'head': list(obj.matrix_world @ bone.head_local)} for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.data.bones},
        'bounds_min': [min(point[i] for point in points) for i in range(3)],
        'bounds_max': [max(point[i] for point in points) for i in range(3)],
        'nondegenerate_triangles': len(triangles),
    }


def VerifyGeometry(source, validation):
    maximum_error = 0.0
    for obj in source.objects:
        if obj.type != 'MESH':
            continue
        other = next(o for o in validation.objects if o.type == 'MESH' and o.get('EaWName') == obj.get('EaWName'))
        assert len(obj.data.loop_triangles) == len(other.data.loop_triangles), obj.name
        buckets = {}
        for triangle in obj.data.loop_triangles:
            corners = [(obj.matrix_world @ obj.data.vertices[v].co, obj.data.uv_layers.active.data[l].uv.copy()) for v,l in zip(triangle.vertices, triangle.loops)]
            if (corners[1][0]-corners[0][0]).cross(corners[2][0]-corners[0][0]).length <= 0.001:
                continue
            center = sum((c[0] for c in corners), Vector()) / 3
            key = tuple(math.floor(x/0.01) for x in center)
            buckets.setdefault(key, []).append(corners)
        for triangle in other.data.loop_triangles:
            corners = [(other.matrix_world @ other.data.vertices[v].co, other.data.uv_layers.active.data[l].uv.copy()) for v,l in zip(triangle.vertices, triangle.loops)]
            if (corners[1][0]-corners[0][0]).cross(corners[2][0]-corners[0][0]).length <= 0.001:
                continue
            center = sum((c[0] for c in corners), Vector()) / 3
            key = tuple(math.floor(x/0.01) for x in center)
            matched = False
            for dx in (-1,0,1):
                for dy in (-1,0,1):
                    for dz in (-1,0,1):
                        for candidate in buckets.get((key[0]+dx,key[1]+dy,key[2]+dz), []):
                            errors = [min((point-before[0]).length if (uv-before[1]).length < 0.00001 else 1000 for before in candidate) for point,uv in corners]
                            if max(errors) < 0.001:
                                maximum_error = max(maximum_error, max(errors))
                                matched = True
            assert matched, (obj.name, list(center))
    return maximum_error


source_scene = bpy.context.scene
is_wreck = source_scene.name == 'Mandator Wreck Source'
assert source_scene.name in ('Mandator Source', 'Mandator Wreck Source')
asset_name = 'MandatorDamaged' if is_wreck else 'Mandator'
rig = next(obj for obj in source_scene.objects if obj.type == 'ARMATURE')
assert len(rig.data.bones) == (69 if is_wreck else 104), 'Run once on the original matching import.'
rig.name = asset_name + 'Rig'
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
assert 'EDIT' in [item.identifier for item in bpy.ops.object.mode_set.get_rna_type().properties['mode'].enum_items]
bpy.ops.object.mode_set(mode='EDIT')
root = rig.data.edit_bones.new('Root')
root.head = (0, 0, 0)
root.tail = (0, 1, 0)
for bone in rig.data.edit_bones:
    if bone != root and bone.parent is None:
        bone.parent = root
bpy.ops.object.mode_set(mode='OBJECT')
for obj in source_scene.objects:
    if obj.type == 'MESH':
        obj['EaWName'] = obj.name
        obj['EaWHidden'] = obj.hide_render
        obj['EaWHelper'] = all(slot.material.shaderList.shaderList in ('MeshCollision.fx', 'MeshShadow.fx') or slot.material.BaseTexture.lower().startswith('w_blast00') for slot in obj.material_slots)
before = Snapshot(source_scene)
for obj in source_scene.objects:
    if obj.type != 'MESH':
        continue
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
assert max(abs(before[key][i]-after_parenting[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < 0.001

images = {}
for stem, (disk_name, target_name) in TEXTURES.items():
    image = bpy.data.images.load(TEXTURE_DIRECTORY + '/' + disk_name, check_existing=True)
    assert image.size[0] > 0, disk_name
    images[stem] = image
    image.filepath_raw = OUTPUT_DIRECTORY + '/Textures/' + target_name + '.png'
    assert 'PNG' in [item.identifier for item in image.bl_rna.properties['file_format'].enum_items]
    image.file_format = 'PNG'
    image.save()

materials = []
used_materials = {slot.material.name for obj in source_scene.objects if obj.type == 'MESH' for slot in obj.material_slots}
for material_name in sorted(used_materials):
    material = bpy.data.materials[material_name]
    shader = material.shaderList.shaderList
    base_stem = material.BaseTexture.rsplit('.', 1)[0].lower()
    normal_stem = material.NormalTexture.rsplit('.', 1)[0].lower()
    base = TEXTURES[base_stem][1] if base_stem in TEXTURES else None
    normal = TEXTURES[normal_stem][1] if 'Bump' in shader else None
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
        texture.image = images[base_stem]
        material.node_tree.links.new(texture.outputs['Color'], surface.inputs['Base Color'])
    if normal:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images[normal_stem]
        texture.image.colorspace_settings.is_data = True
        normal_map = nodes.new('ShaderNodeNormalMap')
        material.node_tree.links.new(texture.outputs['Color'], normal_map.inputs['Color'])
        material.node_tree.links.new(normal_map.outputs['Normal'], surface.inputs['Normal'])
fbx_path = OUTPUT_DIRECTORY + '/' + asset_name + '.fbx'
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/' + asset_name + '.blend')
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation_scene = bpy.data.scenes.new(asset_name + '_FBX_Validation')
bpy.context.window.scene = validation_scene
bpy.ops.import_scene.fbx(filepath=fbx_path, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
for obj in validation_scene.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj.get('EaWHidden', False))
        obj.hide_set(obj.hide_render)
after = Snapshot(validation_scene)
assert set(before['bones']) == set(after['bones'])
bone_error = max((Vector(before['bones'][name]['head'])-Vector(after['bones'][name]['head'])).length for name in before['bones'])
assert bone_error < 0.001
assert all(before['bones'][name]['parent'] == after['bones'][name]['parent'] for name in before['bones'])
assert before['nondegenerate_triangles'] == after['nondegenerate_triangles']
geometry_error = VerifyGeometry(source_scene, validation_scene)
print('MANDATOR_REPORT', json.dumps({'asset': asset_name, 'before': before, 'after': after, 'materials': materials, 'textures': TEXTURES, 'bone_error': bone_error, 'geometry_error': geometry_error, 'fbx': fbx_path}))
bpy.context.window.scene = source_scene
