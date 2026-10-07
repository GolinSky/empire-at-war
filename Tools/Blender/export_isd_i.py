"""Blender 3.6 MCP conversion body; prepend VARIANT, SOURCE, AUDIT and OUTPUT.

Use an isolated Blender instance. Import/export one model at a time to avoid
ALAMO's global shadow cleanup touching objects in unrelated scenes.
"""
import bpy
import json
import math
import itertools
from mathutils import Vector


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [o for o in scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ v.co for o in meshes if not o.hide_render for v in o.data.vertices]
    return {
        'meshes': {o.get('EaWName', o.name): dict(vertices=len(o.data.vertices), triangles=sum(len(p.vertices)-2 for p in o.data.polygons), uv_layers=len(o.data.uv_layers), hidden=o.hide_render, materials=[s.material.name for s in o.material_slots]) for o in meshes},
        'bones': {b.name: dict(parent=b.parent.name if b.parent else None, head=list(o.matrix_world @ b.head_local)) for o in scene.objects if o.type == 'ARMATURE' for b in o.data.bones},
        'bounds_min': [min(p[i] for p in points) for i in range(3)],
        'bounds_max': [max(p[i] for p in points) for i in range(3)],
    }


def Triangles(obj):
    obj.data.calc_loop_triangles()
    return [[(obj.matrix_world @ obj.data.vertices[v].co, obj.data.uv_layers.active.data[l].uv.copy()) for v, l in zip(t.vertices, t.loops)] for t in obj.data.loop_triangles]


def VerifyGeometry(source, validation):
    maximum = 0
    for obj in source.objects:
        if obj.type != 'MESH':
            continue
        other = next(o for o in validation.objects if o.type == 'MESH' and o.get('EaWName') == obj.get('EaWName'))
        original, copied = Triangles(obj), Triangles(other)
        buckets = {}
        def Key(triangle):
            return tuple(math.floor(sum(p[0][i] for p in triangle)/3/.01) for i in range(3))
        for index, triangle in enumerate(original):
            buckets.setdefault(Key(triangle), []).append(index)
        used = set()
        for triangle in copied:
            center = Key(triangle)
            candidates = [i for offset in itertools.product((-1, 0, 1), repeat=3) for i in buckets.get(tuple(center[j]+offset[j] for j in range(3)), [])]
            error, index = min((max(min((p-q).length if (uv-old_uv).length < .00001 else 1000 for q, old_uv in original[i]) for p, uv in triangle), i) for i in candidates)
            assert error < .001, (obj.name, error)
            maximum = max(maximum, error)
            used.add(index)
        for index, triangle in enumerate(original):
            if index in used:
                continue
            a, b, c = [p[0] for p in triangle]
            if (b-a).cross(c-a).length < .0000001:
                continue
            candidates = [i for i in used if Key(original[i]) == Key(triangle)]
            assert any(max(min((p-q).length if (uv-old_uv).length < .00001 else 1000 for q, old_uv in original[i]) for p, uv in triangle) < .001 for i in candidates), obj.name
    return maximum


assert bpy.app.version_string == '3.6.23'
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
scene = bpy.context.scene
scene.name = VARIANT + ' Source'
bpy.ops.import_mesh.alo(filepath=SOURCE, importAnimations=False)
rig = next(o for o in scene.objects if o.type == 'ARMATURE')
rig.name = VARIANT + 'Rig'
if len(rig.data.bones) == len(AUDIT['bones']) - 1:
    root = AUDIT['bones'][0]
    assert root['name'] == 'Root' and root['matrix'] == [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0]
    assert 'Root' not in rig.data.bones
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bone = rig.data.edit_bones.new('Root')
    bone.head, bone.tail = (0, 0, 0), (0, 1, 0)
    for child in rig.data.edit_bones:
        if child != bone and child.parent is None:
            child.parent = bone
    bpy.ops.object.mode_set(mode='OBJECT')
assert len(rig.data.bones) == len(AUDIT['bones'])
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    obj['EaWName'], obj['EaWHidden'] = obj.name, obj.hide_render
before = Snapshot(scene)
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    world = obj.matrix_world.copy()
    if obj.constraints:
        assert len(obj.constraints) == 1 and obj.constraints[0].type == 'CHILD_OF'
        constraint = obj.constraints[0]
        obj.parent, obj.parent_type, obj.parent_bone = constraint.target, 'BONE', constraint.subtarget
        obj.constraints.remove(constraint)
        bpy.context.view_layer.update()
        obj.matrix_world = world
after_parenting = Snapshot(scene)
assert max(abs(before[key][i]-after_parenting[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < .001
materials = []
for material in {s.material for o in scene.objects if o.type == 'MESH' for s in o.material_slots}:
    shader, base = material.shaderList.shaderList, material.BaseTexture
    normal = material.NormalTexture if 'Bump' in shader else 'None'
    record = dict(name=material.name, shader=shader, base=AUDIT['textures'][base]['stem'] if base != 'None' else None, normal=AUDIT['textures'][normal]['stem'] if normal != 'None' else None)
    materials.append(record)
    material['EaWShader'] = shader
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output, surface = nodes.new('ShaderNodeOutputMaterial'), nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value = .65
    material.node_tree.links.new(surface.outputs['BSDF'], output.inputs['Surface'])
    for name, is_data in [(record['base'], False), (record['normal'], True)]:
        if name is None:
            continue
        image = bpy.data.images.load(OUTPUT + '/Textures/' + name + '.png', check_existing=False)
        assert image.size[0] > 0
        image.alpha_mode = 'CHANNEL_PACKED'
        image.colorspace_settings.is_data = is_data
        image.pack()
        node = nodes.new('ShaderNodeTexImage')
        node.image = image
        if is_data:
            normal_map = nodes.new('ShaderNodeNormalMap')
            material.node_tree.links.new(node.outputs['Color'], normal_map.inputs['Color'])
            material.node_tree.links.new(normal_map.outputs['Normal'], surface.inputs['Normal'])
        else:
            material.node_tree.links.new(node.outputs['Color'], surface.inputs['Base Color'])
            if 'Additive' in shader:
                material.node_tree.links.new(node.outputs['Color'], surface.inputs['Emission'])
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT + '/' + VARIANT + '.blend')
fbx = OUTPUT + '/' + VARIANT + '.fbx'
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation = bpy.data.scenes.new(VARIANT + ' FBX Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=fbx, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
for obj in validation.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj['EaWHidden'])
        obj.hide_set(obj.hide_render)
after = Snapshot(validation)
assert set(before['bones']) == set(after['bones'])
assert all(before['bones'][n]['parent'] == after['bones'][n]['parent'] for n in before['bones'])
bone_error = max((Vector(before['bones'][n]['head'])-Vector(after['bones'][n]['head'])).length for n in before['bones'])
assert bone_error < .001
assert len(before['meshes']) == len(after['meshes'])
geometry_error = VerifyGeometry(scene, validation)
assert all(m['uv_layers'] == 1 for m in after['meshes'].values())
assert max(abs(before[key][i]-after[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < .001
print('REPORT_JSON ' + json.dumps(dict(variant=VARIANT, before=before, after=after, materials=materials, bone_error=bone_error, geometry_error=geometry_error)))
bpy.context.window.scene = scene
