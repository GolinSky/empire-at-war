"""Convert only new Acclamator scenes; preserve all other open Blender work."""
import bpy
import bmesh
import json
from mathutils import Vector

TASK = 'F:/Private/empire-at-war/Temp/AcclamatorAssaultImport'
# Inject the source audit before sending this script through Blender MCP.
AUDIT = __AUDIT__


def MaterialName(material):
    return material.name


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [o for o in scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ v.co for o in meshes if not o.hide_render for v in o.data.vertices]
    return dict(meshes={o['EaWName']:dict(triangles=sum(len(p.vertices)-2 for p in o.data.polygons),
                uv_layers=len(o.data.uv_layers), hidden=o.hide_render,
                materials=[s.material.name for s in o.material_slots]) for o in meshes},
                bones={o['EaWName']+'/'+b.name:dict(parent=b.parent.name if b.parent else None,
                head=list(o.matrix_world @ b.head_local)) for o in scene.objects if o.type == 'ARMATURE' for b in o.data.bones},
                bounds_min=[min(p[i] for p in points) for i in range(3)],
                bounds_max=[max(p[i] for p in points) for i in range(3)])


def Convert(name, audit):
    scene = bpy.data.scenes.new(name+' Source')
    bpy.context.window.scene = scene
    try:
        bpy.ops.import_mesh.alo(filepath=audit['file'], importAnimations=False)
    except RuntimeError as error:
        # Upstream cleanup iterates other scenes after the source is fully imported.
        if 'removeShadowDoubles' not in str(error) or 'does not contain object' not in str(error):
            raise
        print('Completed upstream helper cleanup only in ' + scene.name)
        for obj in scene.objects:
            if obj.type == 'MESH' and obj.material_slots:
                shader = obj.material_slots[0].material.shaderList.shaderList
                if shader in ('MeshCollision.fx','RSkinShadowVolume.fx','MeshShadowVolume.fx'):
                    mesh = bmesh.new()
                    mesh.from_mesh(obj.data)
                    bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=.0001)
                    mesh.to_mesh(obj.data)
                    mesh.free()
                    obj.hide_render = True
                    obj.hide_set(True)
                if obj.Hidden:
                    obj.hide_render = True
                    obj.hide_set(True)
    rig = next(o for o in scene.objects if o.type == 'ARMATURE')
    rig.name = name+'Rig'
    source_names = set()
    for bone in audit['bones']:
        candidate, suffix = bone['name'], 1
        while candidate in source_names:
            candidate = bone['name'] + '.' + str(suffix).zfill(3)
            suffix += 1
        source_names.add(candidate)
    missing = source_names - set(rig.data.bones.keys())
    if missing:
        assert missing == {'Root'}, missing
        root_source = audit['bones'][0]
        assert root_source['name'] == 'Root' and root_source['parent_index'] == 4294967295
        assert root_source['matrix'] == [1,0,0,0,0,1,0,0,0,0,1,0]
        bpy.ops.object.select_all(action='DESELECT')
        rig.hide_set(False)
        rig.select_set(True)
        bpy.context.view_layer.objects.active = rig
        bpy.ops.object.mode_set(mode='EDIT')
        root = rig.data.edit_bones.new('Root')
        root.head, root.tail = (0,0,0), (0,1,0)
        for bone in rig.data.edit_bones:
            if bone != root and bone.parent is None:
                bone.parent = root
        bpy.ops.object.mode_set(mode='OBJECT')
    assert set(rig.data.bones.keys()) == source_names, (source_names-set(rig.data.bones.keys()),set(rig.data.bones.keys())-source_names)
    for obj in scene.objects:
        obj['EaWName'] = obj.name
        if obj.type != 'MESH':
            continue
        obj['EaWHidden'] = obj.hide_render
    before = Snapshot(scene)
    materials = []
    for mat in sorted({s.material for o in scene.objects if o.type=='MESH' for s in o.material_slots}, key=MaterialName):
        shader = mat.shaderList.shaderList
        base, normal = mat.BaseTexture, mat.NormalTexture if 'Bump' in shader else None
        materials.append(dict(name=mat.name, shader=shader, base=base, normal=normal))
        mat['EaWShader'] = shader
        mat.use_nodes = True
        mat.node_tree.nodes.clear()
        output = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
        surface = mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
        mat.node_tree.links.new(surface.outputs['BSDF'], output.inputs['Surface'])
        for filename, socket in [(base,'Base Color'),(normal,'Normal')]:
            if not filename or filename == 'None':
                continue
            image = bpy.data.images.load(audit['textures'][filename], check_existing=False)
            assert image.size[0] > 0, filename
            if socket == 'Normal':
                image.colorspace_settings.is_data = True
            image.filepath_raw = TASK + '/Output/Textures/' + filename[:-4] + '.png'
            image.file_format = 'PNG'
            image.save()
            image.pack()
            node = mat.node_tree.nodes.new('ShaderNodeTexImage')
            node.image = image
            if socket == 'Normal':
                bump = mat.node_tree.nodes.new('ShaderNodeNormalMap')
                mat.node_tree.links.new(node.outputs['Color'], bump.inputs['Color'])
                mat.node_tree.links.new(bump.outputs['Normal'], surface.inputs['Normal'])
            else:
                mat.node_tree.links.new(node.outputs['Color'], surface.inputs['Base Color'])
    for obj in scene.objects:
        if obj.type == 'MESH' and obj.constraints:
            assert len(obj.constraints)==1 and obj.constraints[0].type=='CHILD_OF'
            world = obj.matrix_world.copy()
            constraint = obj.constraints[0]
            obj.parent, obj.parent_type, obj.parent_bone = constraint.target, 'BONE', constraint.subtarget
            obj.constraints.remove(constraint)
            bpy.context.view_layer.update()
            obj.matrix_world = world
    prepared = Snapshot(scene)
    assert max(abs(before[k][i]-prepared[k][i]) for k in ('bounds_min','bounds_max') for i in range(3)) < .001
    bpy.ops.wm.save_as_mainfile(filepath=TASK + '/Output/' + name + '.blend', copy=True)
    fbx = TASK + '/Output/' + name + '.fbx'
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'MESH','ARMATURE','EMPTY'},
        add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True,
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
    validation = bpy.data.scenes.new(name+' Validation')
    bpy.context.window.scene = validation
    bpy.ops.import_scene.fbx(filepath=fbx, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
    for obj in validation.objects:
        if obj.type == 'MESH':
            obj.hide_render = bool(obj['EaWHidden'])
    after = Snapshot(validation)
    assert set(before['meshes']) == set(after['meshes'])
    assert set(before['bones']) == set(after['bones'])
    for key in before['meshes']:
        assert before['meshes'][key]['triangles'] == after['meshes'][key]['triangles'], key
        assert before['meshes'][key]['uv_layers'] == after['meshes'][key]['uv_layers'], key
    assert all(before['bones'][k]['parent']==after['bones'][k]['parent'] for k in before['bones'])
    error = max((Vector(before['bones'][k]['head'])-Vector(after['bones'][k]['head'])).length for k in before['bones'])
    bounds_error = max(abs(before[k][i]-after[k][i]) for k in ('bounds_min','bounds_max') for i in range(3))
    assert error < .001 and bounds_error < .001, (error,bounds_error)
    print(name, len(before['meshes']), sum(m['triangles'] for m in before['meshes'].values()),len(before['bones']),error,bounds_error)
    return dict(name=name, source=before, roundtrip=after, materials=materials, bone_error=error, bounds_error=bounds_error)


assert bpy.app.version_string == '3.6.23'
reports = [Convert(name, audit) for name,audit in AUDIT['models'].items()]
print('CONVERSION_JSON ' + json.dumps(reports))
bpy.context.window.scene = bpy.data.scenes['AcclamatorAssault Source']
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_distance = 800
