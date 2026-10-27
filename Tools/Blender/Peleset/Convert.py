"""Convert each audited Peleset source model through the connected Blender 3.6 session."""
import bpy
import json
from mathutils import Matrix, Vector

TASK = 'F:/Private/empire-at-war/Temp/PelesetImport'
OUTPUT = TASK + '/Output'
SOURCE_AUDIT = __SOURCE_AUDIT__
assert bpy.app.version[:2] == (3, 6), bpy.app.version_string


def Convert(name, record):
    model_name = name
    AUDIT = dict(record, textures=SOURCE_AUDIT['textures'])
    assert name + ' Source' not in bpy.data.scenes, 'Source scene already exists: ' + name
    scene = bpy.data.scenes.new(name + ' Source')
    bpy.context.window.scene = scene
    bpy.ops.import_mesh.alo(filepath=AUDIT['source_model'], importAnimations=False)
    rig = next(obj for obj in scene.objects if obj.type == 'ARMATURE')
    rig.name = name + 'Rig'
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
        image = bpy.data.images.load(OUTPUT + '/Textures/' + record['stem'] + '.png', check_existing=False)
        image.alpha_mode = 'CHANNEL_PACKED'
        image.colorspace_settings.is_data = record['stem'].lower().endswith('_n')
        image.pack()
        images[reference.lower()] = image
    materials, records = [], {m['name']: m for m in AUDIT['materials']['meshes']}
    for obj in scene.objects:
        if obj.type != 'MESH':
            continue
        record = records[obj.name.rsplit('.', 1)[0] if obj.name[-4:-3] == '.' else obj.name]
        shader = record['materials'][0]['shader']
        obj['EaWName'], obj['EaWShader'], obj['EaWAuthoredHidden'] = record['name'], shader, record['hidden']
        obj['EaWHelper'] = shader in ('MeshCollision.fx', 'MeshShadowVolume.fx')
        obj['EaWHidden'] = record['hidden'] or obj['EaWHelper'] or shader == 'MeshAlpha.fx'
        obj.hide_render = obj['EaWHidden']
        obj.hide_set(obj.hide_render)
        obj.data.materials.clear()
        offset = 0
        for index, definition in enumerate(record['materials']):
            props = definition['properties']
            material = bpy.data.materials.new(name + '_' + record['name'] + '_Slot' + str(index))
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
            meshes[name] = dict(exported_name=obj.name, vertices=len(obj.data.vertices), triangles=len(obj.data.loop_triangles), hidden=bool(obj['EaWHidden']),
                                shader=obj['EaWShader'], materials=[m.name.split('.')[0] for m in obj.data.materials],
                                slot_triangles=[sum(t.material_index == i for t in obj.data.loop_triangles) for i in range(len(obj.data.materials))])
            uv = obj.data.uv_layers.active.data
            corners[name] = [list(obj.matrix_world @ obj.data.vertices[obj.data.loops[i].vertex_index].co) + list(uv[i].uv) for t in obj.data.loop_triangles for i in t.loops]
        return dict(meshes=meshes, bones=bones), corners


    before, source_geometry = Snapshot(scene)
    fbx = OUTPUT + '/' + name + '.fbx'
    bpy.ops.wm.save_as_mainfile(filepath=OUTPUT + '/' + name + '.blend', copy=True)
    bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False,
                             use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
    validation = bpy.data.scenes.new(name + ' FBX Validation')
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
    geometry_checks = []
    for mesh_name, corners in source_geometry.items():
        assert len(corners) == len(geometry[mesh_name]), mesh_name
        lookup = {}
        for corner in geometry[mesh_name]:
            lookup.setdefault(tuple(round(v, 5) for v in corner[3:]), []).append(corner)
        position_error = uv_error = 0
        for corner in corners:
            candidates = lookup[tuple(round(v, 5) for v in corner[3:])]
            target = min((sum((corner[i] - p[i]) ** 2 for i in range(3)), p) for p in candidates)[1]
            position_error = max(position_error, max(abs(corner[i] - target[i]) for i in range(3)))
            uv_error = max(uv_error, max(abs(corner[i] - target[i]) for i in range(3, 5)))
        assert position_error < .001 and uv_error < .00001, (mesh_name, position_error, uv_error)
        geometry_checks.append(dict(name=mesh_name, corners=len(corners), position_error=position_error, uv_error=uv_error))
    report['geometry_checks'] = geometry_checks
    # Each editable source scene is saved in its own .blend before this cleanup.
    # Remove only our imported objects so the upstream importer's global loops
    # cannot touch source/validation objects from a different scene.
    for obj in list(validation.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    if model_name != 'Peleset':
        for obj in list(scene.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
    return report, source_geometry, geometry

reports, source_geometry, roundtrip_geometry = {}, {}, {}
previous_scene = bpy.context.scene
for model_name in ('PelesetTurret01', 'PelesetTurret02', 'Peleset'):
    record = SOURCE_AUDIT['models'][model_name]
    reports[model_name], source_geometry[model_name], roundtrip_geometry[model_name] = Convert(model_name, record)
report = dict(models=reports, materials=[m for r in reports.values() for m in r['materials']])
print('PELESET_REPORT ' + json.dumps(report))
bpy.context.window.scene = bpy.data.scenes['Peleset Source']
print('PELESET_CONVERTED', {n:dict(meshes=len(r['before']['meshes']),bones=len(r['before']['bones'])) for n,r in reports.items()})

