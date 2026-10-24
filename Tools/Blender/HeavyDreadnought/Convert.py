"""Run through Blender MCP with AUDIT injected from Prepare.py's report."""
import bpy
import json
from mathutils import Matrix, Vector

OUTPUT = 'F:/Private/empire-at-war/Temp/HeavyDreadnoughtImport/Output'
scene = bpy.data.scenes['Dreadnaught Source']
bpy.context.window.scene = scene
assert len(scene.objects) == 16, 'Start from the inspected, fresh hull import.'
materials, definitions, rigs = {}, [], {}
images = {}
for filename, record in AUDIT['textures'].items():
    image = bpy.data.images.load(OUTPUT + '/Textures/' + record['stem'] + '.png', check_existing=False)
    image.alpha_mode = 'CHANNEL_PACKED'
    image.colorspace_settings.is_data = filename.lower().endswith('_b.dds')
    image.pack()
    images[filename.lower()] = image


def Repair(objects, key):
    audit = AUDIT['models'][key]
    rig = next(o for o in objects if o.type == 'ARMATURE')
    rig.name = key + 'Rig'
    rigs[key] = rig
    assert len(rig.data.bones) == len(audit['model']['bones'])
    bpy.context.view_layer.objects.active = rig
    rig.hide_set(False)
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for index, record in enumerate(audit['model']['bones']):
        parent = record['parent_index']
        rig.data.edit_bones[index].parent = rig.data.edit_bones[parent] if parent != 4294967295 else None
    bpy.ops.object.mode_set(mode='OBJECT')
    source_meshes = [o for o in objects if o.type == 'MESH']
    assert len(source_meshes) == len(audit['materials']['meshes'])
    for obj, record in zip(source_meshes, audit['materials']['meshes']):
        # Object creation order is the binary mesh order; names may have global .001 suffixes.
        assert obj.name.split('.')[0] == record['name'], (obj.name, record['name'])
        obj.name = key + '__' + record['name']
        obj['EaWName'] = obj.name
        obj['EaWShader'] = record['materials'][0]['shader']
        obj['EaWHidden'] = record['hidden']
        obj.hide_render = record['hidden']
        obj.hide_set(record['hidden'])
        obj.data.materials.clear()
        offset = 0
        for slot, definition in enumerate(record['materials']):
            signature = json.dumps(definition['properties'], sort_keys=True) + definition['shader']
            if signature not in materials:
                props = definition['properties']
                material = bpy.data.materials.new('HeavyDreadnought_Surface%02d' % len(materials))
                material['EaWShader'] = definition['shader']
                material.use_nodes = True
                nodes, links = material.node_tree.nodes, material.node_tree.links
                nodes.clear()
                output, surface = nodes.new('ShaderNodeOutputMaterial'), nodes.new('ShaderNodeBsdfPrincipled')
                links.new(surface.outputs['BSDF'], output.inputs['Surface'])
                surface.inputs['Roughness'].default_value = .65
                base, normal = props.get('BaseTexture'), props.get('NormalTexture')
                if base:
                    node = nodes.new('ShaderNodeTexImage')
                    node.image = images[base.lower()]
                    links.new(node.outputs['Color'], surface.inputs['Base Color'])
                if normal:
                    node, bump = nodes.new('ShaderNodeTexImage'), nodes.new('ShaderNodeNormalMap')
                    node.image = images[normal.lower()]
                    links.new(node.outputs['Color'], bump.inputs['Color'])
                    links.new(bump.outputs['Normal'], surface.inputs['Normal'])
                materials[signature] = material
                definitions.append(dict(name=material.name, shader=definition['shader'],
                                        base=AUDIT['textures'][base]['stem'] if base else None,
                                        normal=AUDIT['textures'][normal]['stem'] if normal else None,
                                        mask=None, properties=props))
            obj.data.materials.append(materials[signature])
            for polygon in obj.data.polygons[offset:offset+definition['triangleCount']]:
                polygon.material_index = slot
            offset += definition['triangleCount']
        assert len(obj.data.polygons) == offset
        for constraint in obj.constraints:
            constraint.inverse_matrix = Matrix.Identity(4)
        bpy.context.view_layer.update()
        world = obj.matrix_world.copy()
        constraint = obj.constraints[0]
        assert len(obj.constraints) == 1 and constraint.type == 'CHILD_OF'
        obj.parent, obj.parent_type, obj.parent_bone = rig, 'BONE', constraint.subtarget
        obj.constraints.remove(constraint)
        bpy.context.view_layer.update()
        obj.matrix_world = world
    if key != 'Hull':
        for bone in rig.data.bones:
            bone.name = key + '__' + bone.name
    return rig


hull = Repair(list(scene.objects), 'Hull')
errors = []
for index in range(12):
    key = 'Turret%02d' % index
    temporary = bpy.data.scenes.new(key + ' Import')
    bpy.context.window.scene = temporary
    try:
        bpy.ops.import_mesh.alo(filepath=AUDIT['models'][key]['source_model'], importAnimations=False)
    except RuntimeError as error:
        # Known ALAMO global cleanup bug: it selects a helper owned by another scene.
        assert 'does not contain object' in str(error), str(error)
        errors.append(key)
    objects = list(temporary.objects)
    rig = Repair(objects, key)
    for obj in objects:
        scene.collection.objects.link(obj)
    bpy.context.window.scene = scene
    socket = hull.data.bones['b_attach_%02d' % index]
    world = hull.matrix_world @ socket.matrix_local @ rig.matrix_world
    rig.parent, rig.parent_type, rig.parent_bone = hull, 'BONE', socket.name
    bpy.context.view_layer.update()
    rig.matrix_world = world
    bpy.data.scenes.remove(temporary)

# FBX merges nested armatures into extra synthetic bones. Join the authored bones
# explicitly, retaining every mesh's world transform and XML attachment parent.
mesh_parents = [(o, o.parent_bone, o.matrix_world.copy()) for o in scene.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for rig in rigs.values():
    rig.select_set(True)
bpy.context.view_layer.objects.active = hull
bpy.ops.object.join()
bpy.ops.object.mode_set(mode='EDIT')
for index in range(12):
    hull.data.edit_bones['Turret%02d__Root' % index].parent = hull.data.edit_bones['b_attach_%02d' % index]
bpy.ops.object.mode_set(mode='OBJECT')
for obj, bone, world in mesh_parents:
    obj.parent, obj.parent_type, obj.parent_bone = hull, 'BONE', bone
    bpy.context.view_layer.update()
    obj.matrix_world = world


def Snapshot(target):
    bpy.context.window.scene = target
    bpy.context.view_layer.update()
    meshes, bones, corners = {}, {}, {}
    for obj in target.objects:
        if obj.type == 'ARMATURE':
            for bone in obj.data.bones:
                bones[bone.name] = dict(parent=bone.parent.name if bone.parent else None, head=list(obj.matrix_world @ bone.head_local))
        if obj.type != 'MESH':
            continue
        obj.data.calc_loop_triangles()
        meshes[obj['EaWName']] = dict(exported_name=obj.name, triangles=len(obj.data.loop_triangles), hidden=bool(obj['EaWHidden']), shader=obj['EaWShader'], materials=[m.name for m in obj.data.materials], slot_triangles=[sum(t.material_index == i for t in obj.data.loop_triangles) for i in range(len(obj.data.materials))])
        uv = obj.data.uv_layers.active.data
        corners[obj['EaWName']] = [tuple(obj.matrix_world @ obj.data.vertices[obj.data.loops[i].vertex_index].co) + tuple(uv[i].uv) for t in obj.data.loop_triangles for i in t.loops]
    return dict(meshes=meshes, bones=bones), corners


before, source_geometry = Snapshot(scene)
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT + '/HeavyDreadnought.blend', copy=True)
bpy.ops.export_scene.fbx(filepath=OUTPUT + '/HeavyDreadnought.fbx', use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation = bpy.data.scenes.new('Dreadnaught FBX Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=OUTPUT + '/HeavyDreadnought.fbx', use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
after, roundtrip_geometry = Snapshot(validation)
assert set(before['meshes']) == set(after['meshes'])
assert set(before['bones']) == set(after['bones'])
bone_error = max((Vector(b['head']) - Vector(after['bones'][name]['head'])).length for name,b in before['bones'].items())
assert bone_error < .002, bone_error
geometry = []
for name, corners in source_geometry.items():
    assert before['meshes'][name]['slot_triangles'] == after['meshes'][name]['slot_triangles']
    assert before['meshes'][name]['materials'] == [m.split('.')[0] for m in after['meshes'][name]['materials']]
    lookup = {}
    for corner in roundtrip_geometry[name]:
        lookup.setdefault(tuple(round(v, 5) for v in corner[3:]), []).append(corner)
    position_error, uv_error = 0, 0
    for corner in corners:
        candidates = lookup[tuple(round(v, 5) for v in corner[3:])]
        other = min((sum((corner[i]-c[i])**2 for i in range(3)), c) for c in candidates)[1]
        position_error = max(position_error, max(abs(corner[i]-other[i]) for i in range(3)))
        uv_error = max(uv_error, max(abs(corner[i]-other[i]) for i in range(3,5)))
    assert position_error < .002 and uv_error < .00001, (name,position_error,uv_error)
    geometry.append(dict(name=name, corners=len(corners), position_error=position_error, uv_error=uv_error))
print('DREADNAUGHT_REPORT ' + json.dumps(dict(before=before, after=after, materials=definitions, bone_error=bone_error, geometry=geometry, importer_cleanup_errors=errors)))
print('DREADNAUGHT_CORNERS ' + json.dumps({n:v[::32] for n,v in source_geometry.items()}))
bpy.context.window.scene = scene
for obj in scene.objects:
    if obj.type == 'ARMATURE':
        obj.hide_set(True)
    elif obj.type == 'MESH':
        obj.hide_set(obj['EaWShader'] != 'MeshBumpColorize.fx')
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.shading.type = 'MATERIAL'
        area.spaces.active.overlay.show_overlays = False
        area.spaces.active.region_3d.view_location = Vector((0, 0, 0))
        area.spaces.active.region_3d.view_distance = 550
        area.spaces.active.region_3d.view_rotation = Vector((1, -1, 1)).to_track_quat('Z', 'Y')
        area.spaces.active.region_3d.view_perspective = 'ORTHO'
