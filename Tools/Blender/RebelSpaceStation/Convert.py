"""Run through Blender MCP with AUDIT injected by ConvertRequest.py; isolated scene only."""
import bpy
import json
from mathutils import Matrix, Vector

OUTPUT = 'F:/Private/empire-at-war/Temp/RebelStationImport/Output'
assert bpy.types.blendermcp_server.port == 9885


def ClearScene():
    for obj in list(bpy.context.scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for collection in (bpy.data.materials, bpy.data.meshes, bpy.data.armatures):
        for item in list(collection):
            if item.users == 0:
                collection.remove(item)


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [o for o in scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ v.co for o in meshes if not o.hide_render for v in o.data.vertices]
    return dict(meshes={o['EaWName']: dict(triangles=sum(len(p.vertices)-2 for p in o.data.polygons),
                    hidden=o.hide_render, materials=[s.material.name.split('.')[0] for s in o.material_slots],
                    parent=o.parent_bone) for o in meshes},
                bones={b.name: dict(parent=b.parent.name if b.parent else None,
                                    head=list(o.matrix_world @ b.head_local))
                       for o in scene.objects if o.type == 'ARMATURE' for b in o.data.bones},
                bounds_min=[min(p[i] for p in points) for i in range(3)],
                bounds_max=[max(p[i] for p in points) for i in range(3)])


def Corners(obj):
    obj.data.calc_loop_triangles()
    uv = obj.data.uv_layers.active.data
    return sorted([(*tuple(obj.matrix_world @ obj.data.vertices[obj.data.loops[i].vertex_index].co),
                    *tuple(uv[i].uv)) for triangle in obj.data.loop_triangles for i in triangle.loops])


def Convert(name, audit):
    ClearScene()
    scene = bpy.context.scene
    scene.name = name + ' Source'
    bpy.ops.import_mesh.alo(filepath=audit['file'], importAnimations=False)
    rig = next(o for o in scene.objects if o.type == 'ARMATURE')
    rig.name = name + 'Rig'
    # The vanilla binaries contain a genuine identity root removed by ALAMO.
    root = audit['bones'][0]
    assert root['name'] == 'Root' and root['parent_index'] == 4294967295
    assert root['matrix'] == [1,0,0,0,0,1,0,0,0,0,1,0]
    assert len(rig.data.bones) == len(audit['bones']) - 1
    # Blender enumerates bones in hierarchy order, not the binary's creation order.
    # Reproduce Blender's unique-name allocation in source order, then address bones by that name.
    bone_names = []
    for row in audit['bones']:
        bone_name = row['name']
        if bone_name in bone_names:
            base, separator, suffix = bone_name.rpartition('.')
            base = base if separator and suffix.isdigit() else bone_name
            number = 1
            bone_name = base + '.' + str(number).zfill(3)
            while bone_name in bone_names:
                number += 1
                bone_name = base + '.' + str(number).zfill(3)
        bone_names.append(bone_name)
    assert set(bone_names) == {b.name for b in rig.data.bones} | {'Root'}
    source_names = {name: row['name'] for name, row in zip(bone_names, audit['bones'])}
    world_matrices = []
    for row in audit['bones']:
        values = row['matrix']
        matrix = Matrix((values[0:4], values[4:8], values[8:12], (0,0,0,1)))
        if row['parent_index'] != 4294967295:
            matrix = world_matrices[row['parent_index']] @ matrix
        world_matrices.append(matrix)
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    root_bone = rig.data.edit_bones.new('Root')
    root_bone.head, root_bone.tail = (0,0,0), (0,1,0)
    # ALAMO resolves parents by name; vanilla station placeholder bones repeat names.
    # Restore the source index-based hierarchy before exporting those unique FBX names.
    for index, row in enumerate(audit['bones']):
        bone = rig.data.edit_bones[bone_names[index]]
        bone.parent = None if row['parent_index'] == 4294967295 else rig.data.edit_bones[bone_names[row['parent_index']]]
        bone.matrix = world_matrices[index]
    bpy.ops.object.mode_set(mode='OBJECT')
    source_names['Root'] = 'Root'
    source_bone_error = max((rig.data.bones[name].head_local-world_matrices[index].translation).length for index,name in enumerate(bone_names))
    assert source_bone_error < .002, source_bone_error
    for index, bone_name in enumerate(bone_names):
        parent_index = audit['bones'][index]['parent_index']
        expected_parent = None if parent_index == 4294967295 else bone_names[parent_index]
        actual_parent = rig.data.bones[bone_name].parent
        assert (actual_parent.name if actual_parent else None) == expected_parent, bone_name
    materials = {}
    for row in audit['meshes']:
        obj = scene.objects[row['name']]
        obj['EaWName'] = row['name']
        shader = row['materials'][0]['shader']
        # Damage overlays need the source destruction shader/animation; keep them in FBX, hidden in gameplay.
        obj.hide_render = row['hidden'] or 'Shadow' in shader or row['name'].endswith('_Blast')
        obj.hide_set(obj.hide_render)
        obj['EaWHidden'] = obj.hide_render
        obj['EaWSourceHidden'] = row['hidden']
        obj['EaWShader'] = shader
        connection = next(c for c in audit['connections'] if c['mesh'] == row['object_index'])
        world = world_matrices[connection['bone']]
        constraint = obj.constraints[0]
        assert constraint.type == 'CHILD_OF' and len(obj.constraints) == 1
        obj.parent, obj.parent_type, obj.parent_bone = rig, 'BONE', bone_names[connection['bone']]
        obj.constraints.remove(constraint)
        bpy.context.view_layer.update()
        obj.matrix_world = world
        obj.data.materials.clear()
        offset = 0
        for slot, definition in enumerate(row['materials']):
            signature = json.dumps(definition['properties'], sort_keys=True) + definition['shader']
            if signature not in materials:
                material = bpy.data.materials.new('RebelStationSlot' + str(len(materials)).zfill(2))
                material['EaWShader'] = definition['shader']
                material.use_nodes = True
                material.node_tree.nodes.clear()
                nodes, links = material.node_tree.nodes, material.node_tree.links
                surface, output = nodes.new('ShaderNodeBsdfPrincipled'), nodes.new('ShaderNodeOutputMaterial')
                surface.inputs['Roughness'].default_value = .65
                links.new(surface.outputs['BSDF'], output.inputs['Surface'])
                for key, socket in [('BaseTexture','Base Color'), ('NormalTexture','Normal')]:
                    ref = definition['properties'].get(key)
                    if not ref:
                        continue
                    texture = AUDIT['textures'][ref.lower()]
                    image = bpy.data.images.load(texture['png'], check_existing=True)
                    image.alpha_mode = 'CHANNEL_PACKED'
                    image.colorspace_settings.is_data = key == 'NormalTexture'
                    image.pack()
                    node = nodes.new('ShaderNodeTexImage')
                    node.image = image
                    if key == 'NormalTexture':
                        bump = nodes.new('ShaderNodeNormalMap')
                        links.new(node.outputs['Color'], bump.inputs['Color'])
                        links.new(bump.outputs['Normal'], surface.inputs[socket])
                    else:
                        links.new(node.outputs['Color'], surface.inputs[socket])
                        if 'Additive' in definition['shader']:
                            links.new(node.outputs['Color'], surface.inputs['Emission'])
                materials[signature] = (material, definition)
            material = materials[signature][0]
            obj.data.materials.append(material)
            for polygon in obj.data.polygons[offset:offset + definition['triangleCount']]:
                polygon.material_index = slot
            offset += definition['triangleCount']
        if 'Shadow' in shader:
            # ALAMO welds shadow-volume doubles and removes degenerate faces on import.
            assert len(obj.data.polygons) <= offset, row['name']
        else:
            assert offset == len(obj.data.polygons), row['name']
    before = Snapshot(scene)
    corners = {o['EaWName']: Corners(o) for o in scene.objects if o.type == 'MESH'}
    bpy.ops.wm.save_as_mainfile(filepath=OUTPUT + '/' + name + '.blend', copy=True)
    fbx = OUTPUT + '/' + name + '.fbx'
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'MESH','ARMATURE','EMPTY'},
        add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True,
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
    validation = bpy.data.scenes.new(name + ' Validation')
    bpy.context.window.scene = validation
    bpy.ops.import_scene.fbx(filepath=fbx, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
    geometry_error = uv_error = 0
    for obj in validation.objects:
        if obj.type != 'MESH':
            continue
        obj.hide_render = bool(obj['EaWHidden'])
        actual = Corners(obj)
        expected = corners[obj['EaWName']]
        assert len(actual) == len(expected)
        # Nearest corner per source position + UV handles FBX vertex/triangle ordering.
        buckets = {}
        for corner in expected:
            key = tuple(round(v, 3) for v in corner[:3])
            buckets.setdefault(key, []).append(corner)
        for corner in actual:
            key = tuple(round(v, 3) for v in corner[:3])
            candidates = buckets.get(key, expected)
            def Distance(candidate):
                return sum((candidate[i]-corner[i])**2 for i in range(5))
            match = min(candidates, key=Distance)
            geometry_error = max(geometry_error, (Vector(corner[:3])-Vector(match[:3])).length)
            uv_error = max(uv_error, max(abs(corner[i]-match[i]) for i in (3,4)))
    after = Snapshot(validation)
    assert set(before['bones']) == set(after['bones'])
    assert all(before['bones'][b]['parent'] == after['bones'][b]['parent'] for b in before['bones'])
    bone_error = max((Vector(before['bones'][b]['head'])-Vector(after['bones'][b]['head'])).length for b in before['bones'])
    assert bone_error < .002 and geometry_error < .002 and uv_error < .00001, (bone_error, geometry_error, uv_error)
    for mesh in before['meshes']:
        assert before['meshes'][mesh]['triangles'] == after['meshes'][mesh]['triangles']
    report = dict(name=name, level=audit['level'], source=before, roundtrip=after, source_bone_names=source_names,
                  materials=[dict(name=m.name, **d) for m,d in materials.values()],
                  source_bone_error=source_bone_error, bone_error=bone_error, geometry_error=geometry_error, uv_error=uv_error)
    ClearScene()
    bpy.context.window.scene = scene
    bpy.data.scenes.remove(validation)
    return report


reports = [Convert(name, model) for name, model in AUDIT['models'].items()]
print('CONVERSION_JSON ' + json.dumps(reports))
