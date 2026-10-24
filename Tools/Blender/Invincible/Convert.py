"""Run in an isolated Blender 3.6.23 process; keep the user's live session intact."""
import addon_utils
import bpy
import json
from pathlib import Path
from mathutils import Matrix, Vector

TASK = Path(__file__).resolve().parents[3] / 'Temp/InvincibleImport'
OUTPUT = TASK / 'Output'
AUDIT = json.loads((TASK / 'SourceAudit.json').read_text())
assert bpy.app.version[:2] == (3, 6), bpy.app.version_string
addon_utils.enable('io_alamo_tools', default_set=False)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
scene = bpy.context.scene
scene.name = 'Invincible Source'
bpy.ops.import_mesh.alo(filepath=AUDIT['source_model'], importAnimations=False)
rig = next(obj for obj in scene.objects if obj.type == 'ARMATURE')
rig.name = 'InvincibleRig'
bpy.context.view_layer.objects.active = rig
rig.hide_set(False)
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
root_restored = False
source_bone_names = [bone.name for bone in rig.data.edit_bones]
if 'Root' not in rig.data.edit_bones:
    root = next(b for b in AUDIT['model']['bones'] if b['name'] == 'Root')
    assert root['matrix'] == [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0], root
    assert len(rig.data.edit_bones) == len(AUDIT['model']['bones']) - 1
    assert AUDIT['model']['bones'][0]['name'] == 'Root'
    source_bone_names.insert(0, 'Root')
    bone = rig.data.edit_bones.new('Root')
    bone.head, bone.tail = (0, 0, 0), (0, 1, 0)
    root_restored = True
for index, record in enumerate(AUDIT['model']['bones']):
    parent = record['parent_index']
    # The ALO repeats proxy bone names. Blender assigns unique suffixes in source order.
    bone = rig.data.edit_bones[source_bone_names[index]]
    bone.parent = rig.data.edit_bones[source_bone_names[parent]] if parent != 4294967295 else None
bpy.ops.object.mode_set(mode='OBJECT')
assert len(rig.data.bones) == len(AUDIT['model']['bones'])
images = {}
for reference, record in AUDIT['textures'].items():
    image = bpy.data.images.load(str(OUTPUT / 'Textures' / (record['stem'] + '.png')), check_existing=False)
    image.alpha_mode = 'CHANNEL_PACKED'
    image.colorspace_settings.is_data = record['stem'].endswith('_Normal')
    image.pack()
    images[reference.lower()] = image
materials, records = [], {m['name']: m for m in AUDIT['materials']['meshes']}
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    record = records[obj.name]
    shader = record['materials'][0]['shader']
    obj['EaWName'], obj['EaWShader'], obj['EaWAuthoredHidden'] = obj.name, shader, record['hidden']
    obj['EaWHelper'] = shader in ('MeshCollision.fx', 'MeshShadowVolume.fx')
    obj['EaWHidden'] = record['hidden'] or obj['EaWHelper'] or shader == 'MeshAlpha.fx'
    obj.hide_render = obj['EaWHidden']
    obj.hide_set(obj.hide_render)
    obj.data.materials.clear()
    offset = 0
    for index, definition in enumerate(record['materials']):
        props = definition['properties']
        material = bpy.data.materials.new('Invincible_' + obj.name + '_Slot' + str(index))
        material.use_nodes = True
        material['EaWShader'] = shader
        nodes, links = material.node_tree.nodes, material.node_tree.links
        nodes.clear()
        output, surface = nodes.new('ShaderNodeOutputMaterial'), nodes.new('ShaderNodeBsdfPrincipled')
        links.new(surface.outputs['BSDF'], output.inputs['Surface'])
        surface.inputs['Roughness'].default_value = .65
        base, normal, mask = None, None, None
        if props.get('BaseTexture'):
            reference = props['BaseTexture']
            base = AUDIT['textures'][reference]['stem']
            mask = AUDIT['textures'][reference].get('team_mask')
            node = nodes.new('ShaderNodeTexImage')
            node.image = images[reference.lower()]
            links.new(node.outputs['Color'], surface.inputs['Base Color'])
            if shader == 'MeshAdditive.fx':
                links.new(node.outputs['Color'], surface.inputs['Emission'])
        if props.get('NormalTexture'):
            reference = props['NormalTexture']
            normal = AUDIT['textures'][reference]['stem']
            node, mapping = nodes.new('ShaderNodeTexImage'), nodes.new('ShaderNodeNormalMap')
            node.image = images[reference.lower()]
            links.new(node.outputs['Color'], mapping.inputs['Color'])
            links.new(mapping.outputs['Normal'], surface.inputs['Normal'])
        obj.data.materials.append(material)
        for polygon in obj.data.polygons[offset:offset + definition['triangleCount']]:
            polygon.material_index = index
        offset += definition['triangleCount']
        materials.append(dict(name=material.name, shader=shader, base=base, normal=normal, mask=mask, properties=props))
    # ALAMO welds shadow and collision helpers; the visible hull/light corners stay unchanged.
    if not obj['EaWHelper']:
        assert len(obj.data.polygons) == offset, (obj.name, len(obj.data.polygons), offset)
    for constraint in obj.constraints:
        constraint.inverse_matrix = Matrix.Identity(4)
bpy.context.view_layer.update()
parenting_error = 0
for obj in scene.objects:
    if obj.type != 'MESH' or not obj.constraints:
        continue
    world = obj.matrix_world.copy()
    before = [world @ vertex.co for vertex in obj.data.vertices]
    assert len(obj.constraints) == 1 and obj.constraints[0].type == 'CHILD_OF'
    constraint = obj.constraints[0]
    obj.parent, obj.parent_type, obj.parent_bone = rig, 'BONE', constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
    bpy.context.view_layer.update()
    parenting_error = max(parenting_error, max((a - obj.matrix_world @ b.co).length for a, b in zip(before, obj.data.vertices)))
assert parenting_error < .001, parenting_error


def Snapshot(target):
    bpy.context.window.scene = target
    bpy.context.view_layer.update()
    meshes, bones, corners = {}, {}, {}
    for obj in target.objects:
        if obj.type == 'ARMATURE':
            bones.update({b.name: dict(parent=b.parent.name if b.parent else None, head=list(obj.matrix_world @ b.head_local)) for b in obj.data.bones})
        if obj.type != 'MESH':
            continue
        obj.data.calc_loop_triangles()
        name = obj['EaWName']
        meshes[name] = dict(exported_name=name, vertices=len(obj.data.vertices), triangles=len(obj.data.loop_triangles), hidden=bool(obj['EaWHidden']),
                            shader=obj['EaWShader'], materials=[m.name.split('.')[0] for m in obj.data.materials],
                            slot_triangles=[sum(t.material_index == i for t in obj.data.loop_triangles) for i in range(len(obj.data.materials))])
        uv = obj.data.uv_layers.active.data
        corners[name] = [list(obj.matrix_world @ obj.data.vertices[obj.data.loops[i].vertex_index].co) + list(uv[i].uv) for t in obj.data.loop_triangles for i in t.loops]
    return dict(meshes=meshes, bones=bones), corners


before, source_geometry = Snapshot(scene)
fbx = OUTPUT / 'Invincible.fbx'
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT / 'Invincible.blend'), copy=True)
bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False,
                         use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation = bpy.data.scenes.new('Invincible FBX Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=str(fbx), use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
after, geometry = Snapshot(validation)
assert set(before['meshes']) == set(after['meshes'])
assert set(before['bones']) == set(after['bones'])
assert all(before['bones'][name]['parent'] == after['bones'][name]['parent'] for name in before['bones'])
bone_error = max((Vector(b['head']) - Vector(after['bones'][name]['head'])).length for name, b in before['bones'].items())
assert bone_error < .001, bone_error
for name in before['meshes']:
    assert before['meshes'][name]['triangles'] == after['meshes'][name]['triangles'], name
    assert before['meshes'][name]['slot_triangles'] == after['meshes'][name]['slot_triangles'], name
    assert before['meshes'][name]['materials'] == after['meshes'][name]['materials'], name
report = dict(before=before, after=after, materials=materials, bone_error=bone_error, parenting_error=parenting_error, root_restored=root_restored,
              source_model=AUDIT['source_model'], source_bone_names=source_bone_names, fbx=str(fbx))
(TASK / 'ConversionReport.json').write_text(json.dumps(report, indent=2))
(TASK / 'SourceGeometry.json').write_text(json.dumps(source_geometry))
(TASK / 'RoundtripGeometry.json').write_text(json.dumps(geometry))
print('INVINCIBLE_CONVERTED', len(before['meshes']), 'meshes', len(before['bones']), 'bones', sum(m['triangles'] for m in before['meshes'].values()), 'triangles', flush=True)
