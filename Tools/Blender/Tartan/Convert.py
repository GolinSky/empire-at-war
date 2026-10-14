"""Run in Blender 3.6 through MCP; retain the user's existing scenes."""
import bpy
import json
from mathutils import Vector

TASK = 'F:/Private/empire-at-war/Temp/TartanImport'
# The MCP caller supplies the read-only SourceAudit.json as the audit variable.
assert bpy.app.version_string == '3.6.23'
assert bpy.types.blendermcp_server.port == 9885
reports = []
for name, source in audit['models'].items():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mat in list(bpy.data.materials):
        if mat.users == 0:
            bpy.data.materials.remove(mat)
    scene = bpy.data.scenes.new(name + ' Source')
    bpy.context.window.scene = scene
    bpy.ops.import_mesh.alo(filepath=source['file'], importAnimations=False)
    rig = next(o for o in scene.objects if o.type == 'ARMATURE')
    rig.name = name + 'Rig'
    if 'Root' not in rig.data.bones:
        root_source = source['bones'][0]
        assert root_source['name'] == 'Root' and root_source['matrix'] == [1,0,0,0,0,1,0,0,0,0,1,0]
        bpy.context.view_layer.objects.active = rig
        rig.select_set(True)
        bpy.ops.object.mode_set(mode='EDIT')
        root = rig.data.edit_bones.new('Root')
        root.head, root.tail = (0,0,0), (0,1,0)
        for bone in rig.data.edit_bones:
            if bone != root and bone.parent is None:
                bone.parent = root
        bpy.ops.object.mode_set(mode='OBJECT')
    assert len(rig.data.bones) == len(source['bones'])
    materials = []
    meshes = {}
    for row in source['binary']['meshes']:
        obj = next(o for o in scene.objects if o.name == row['name'] or o.name.startswith(row['name'] + '.'))
        obj.name = name + '_' + row['name']
        obj['EaWName'] = row['name']
        obj['EaWHidden'] = obj.hide_render
        hidden = row['hidden'] or any(s['shader'] in ('MeshShadowVolume.fx', 'MeshCollision.fx') for s in row['materials'])
        obj.hide_render = hidden
        meshes[obj.name] = dict(triangles=sum(len(p.vertices)-2 for p in obj.data.polygons), uv_layers=len(obj.data.uv_layers), hidden=hidden)
        if not hidden:
            assert meshes[obj.name]['triangles'] == sum(s['triangleCount'] for s in row['materials'])
        assert len(row['materials']) == 1
        slot = row['materials'][0]
        obj.data.materials.clear()
        material = bpy.data.materials.new(name + '_' + row['name'])
        obj.data.materials.append(material)
        material.use_nodes = True
        material['EaWShader'] = slot['shader']
        nodes = material.node_tree.nodes
        nodes.clear()
        output = nodes.new('ShaderNodeOutputMaterial')
        surface = nodes.new('ShaderNodeBsdfPrincipled')
        material.node_tree.links.new(surface.outputs['BSDF'], output.inputs['Surface'])
        props = slot['properties']
        base = props.get('BaseTexture')
        normal = props.get('NormalTexture') if 'Bump' in slot['shader'] else None
        materials.append(dict(name=material.name, shader=slot['shader'], base=base, normal=normal))
        for filename, socket in [(base, 'Base Color'), (normal, 'Normal')]:
            if not filename or filename == 'None':
                continue
            img = bpy.data.images.load(source['textures'][filename], check_existing=True)
            assert img.size[0] > 0
            img.filepath_raw = TASK + '/Output/Textures/' + filename.rsplit('.', 1)[0] + '.png'
            img.file_format = 'PNG'
            img.save()
            img.pack()
            node = nodes.new('ShaderNodeTexImage')
            node.image = img
            if socket == 'Normal':
                img.colorspace_settings.is_data = True
                bump = nodes.new('ShaderNodeNormalMap')
                material.node_tree.links.new(node.outputs['Color'], bump.inputs['Color'])
                material.node_tree.links.new(bump.outputs['Normal'], surface.inputs[socket])
            else:
                material.node_tree.links.new(node.outputs['Color'], surface.inputs[socket])
        world = obj.matrix_world.copy()
        if obj.constraints:
            constraint = obj.constraints[0]
            assert len(obj.constraints) == 1 and constraint.type == 'CHILD_OF'
            obj.parent, obj.parent_type, obj.parent_bone = constraint.target, 'BONE', constraint.subtarget
            obj.constraints.remove(constraint)
            bpy.context.view_layer.update()
            obj.matrix_world = world
        obj['EaWHidden'] = hidden
    bpy.context.view_layer.update()
    points = [o.matrix_world @ v.co for o in scene.objects if o.type == 'MESH' and not o.hide_render for v in o.data.vertices]
    bones = {b.name:dict(parent=b.parent.name if b.parent else None, head=list(rig.matrix_world @ b.head_local)) for b in rig.data.bones}
    report = dict(name=name, source=dict(meshes=meshes, bones=bones, bounds_min=[min(p[i] for p in points) for i in range(3)], bounds_max=[max(p[i] for p in points) for i in range(3)]), materials=materials)
    bpy.ops.wm.save_as_mainfile(filepath=TASK + '/Output/' + name + '.blend', copy=True)
    bpy.ops.export_scene.fbx(filepath=TASK + '/Output/' + name + '.fbx', use_selection=False, object_types={'MESH','ARMATURE','EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
    validation = bpy.data.scenes.new(name + ' Validation')
    bpy.context.window.scene = validation
    bpy.ops.import_scene.fbx(filepath=TASK + '/Output/' + name + '.fbx', use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
    imported = next(o for o in validation.objects if o.type == 'ARMATURE')
    assert set(bones) == set(imported.data.bones.keys())
    error = max((Vector(bones[b.name]['head']) - imported.matrix_world @ b.head_local).length for b in imported.data.bones)
    assert error < .001
    report['bone_error'] = error
    for obj in validation.objects:
        if obj.type == 'MESH':
            obj.hide_render = bool(obj['EaWHidden'])
    reports.append(report)
print('CONVERSION_JSON ' + json.dumps(reports))
bpy.context.window.scene = scene
for obj in bpy.context.scene.objects:
    obj.hide_set(obj.type == 'ARMATURE' or obj.hide_render)
for area in bpy.context.window.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.shading.type = 'MATERIAL'
        area.spaces.active.overlay.show_overlays = False
        region = area.spaces.active.region_3d
        region.view_location = (Vector(reports[0]['source']['bounds_min']) + Vector(reports[0]['source']['bounds_max'])) * .5
        region.view_distance = 250
        region.view_rotation = Vector((1,-1,1)).to_track_quat('Z','Y')
        region.view_perspective = 'ORTHO'
print(json.dumps([dict(name=r['name'], meshes=len(r['source']['meshes']), bone_error=r['bone_error']) for r in reports]))
