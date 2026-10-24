# AUDIT is injected from SourceAudit.json by the MCP caller.
"""Convert RaW's Centax through Blender MCP; preserve all other scenes."""
import bpy
import bmesh
import json
from mathutils import Matrix, Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/CentaxImport/Output'


def Snapshot(scene):
    bpy.context.window.scene = scene
    bpy.context.view_layer.update()
    meshes = [obj for obj in scene.objects if obj.type == 'MESH']
    points = [obj.matrix_world @ vertex.co for obj in meshes if not obj.hide_render and obj['EaWShader'] in ('MeshBumpColorize.fx', 'MeshGloss.fx') for vertex in obj.data.vertices]
    return dict(
        meshes={obj['EaWName']: dict(exported_name=obj.name, vertices=len(obj.data.vertices), triangles=sum(len(p.vertices)-2 for p in obj.data.polygons), uv_layers=len(obj.data.uv_layers), hidden=obj.hide_render, materials=[slot.material.name for slot in obj.material_slots], slot_triangles=[sum(len(p.vertices)-2 for p in obj.data.polygons if p.material_index == slot) for slot in range(len(obj.material_slots))]) for obj in meshes},
        bones={bone.name: dict(parent=bone.parent.name if bone.parent else None, head=list(obj.matrix_world @ bone.head_local)) for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.data.bones},
        bounds_min=[min(p[i] for p in points) for i in range(3)],
        bounds_max=[max(p[i] for p in points) for i in range(3)])


scene = bpy.data.scenes['Centax Source']
bpy.context.window.scene = scene
rig = next(obj for obj in scene.objects if obj.type == 'ARMATURE')
assert len(rig.data.bones) == len(AUDIT['model']['bones'])
rig.name = 'CentaxRig'
# The importer stopped before its final hierarchy cleanup; restore audited parents.
bpy.context.view_layer.objects.active = rig
rig.hide_set(False)
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for index, record in enumerate(AUDIT['model']['bones']):
    bone = rig.data.edit_bones[index]
    parent = record['parent_index']
    bone.parent = rig.data.edit_bones[parent] if parent != 4294967295 else None
bpy.ops.object.mode_set(mode='OBJECT')
textures = {}
for filename, record in AUDIT['textures'].items():
    image = bpy.data.images.load(OUTPUT_DIRECTORY + '/Textures/' + record['stem'] + '.png', check_existing=False)
    image.alpha_mode = 'CHANNEL_PACKED'
    image.colorspace_settings.is_data = filename.lower().endswith('_b.dds')
    image.pack()
    textures[filename.lower()] = image

materials = []
records = {record['name']: record for record in AUDIT['materials']['meshes']}
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    name = obj.name.rsplit('.', 1)[0] if obj.name[-4:-3] == '.' else obj.name
    record = records[name]
    obj['EaWName'] = name
    shader = record['materials'][0]['shader']
    obj['EaWShader'] = shader
    obj.hide_render = bool(record['hidden'])
    obj.hide_set(obj.hide_render)
    obj['EaWHidden'] = obj.hide_render
    obj['EaWHelper'] = shader in ('MeshShadowVolume.fx', 'MeshCollision.fx', 'MeshShield.fx', 'alDefault.fx')
    # Complete only this scene's cleanup after the importer's global cleanup hit another scene.
    if shader == 'MeshCollision.fx':
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=.0001)
        mesh.to_mesh(obj.data)
        mesh.free()
    obj.data.materials.clear()
    offset = 0
    for index, definition in enumerate(record['materials']):
        props = definition['properties']
        material = bpy.data.materials.new('Centax_' + name + '_Slot' + str(index))
        material['EaWShader'] = definition['shader']
        material.use_nodes = True
        nodes, links = material.node_tree.nodes, material.node_tree.links
        nodes.clear()
        output, surface = nodes.new('ShaderNodeOutputMaterial'), nodes.new('ShaderNodeBsdfPrincipled')
        links.new(surface.outputs['BSDF'], output.inputs['Surface'])
        surface.inputs['Roughness'].default_value = .65
        base_name, normal_name = props.get('BaseTexture', '').lower(), props.get('NormalTexture', '').lower()
        base, normal = None, None
        if base_name:
            base = AUDIT['textures'][props['BaseTexture']]['stem']
            node = nodes.new('ShaderNodeTexImage')
            node.image = textures[base_name]
            links.new(node.outputs['Color'], surface.inputs['Base Color'])
            if definition['shader'] == 'MeshAdditive.fx':
                links.new(node.outputs['Color'], surface.inputs['Emission'])
        if normal_name:
            normal = AUDIT['textures'][props['NormalTexture']]['stem']
            node, bump = nodes.new('ShaderNodeTexImage'), nodes.new('ShaderNodeNormalMap')
            node.image = textures[normal_name]
            links.new(node.outputs['Color'], bump.inputs['Color'])
            links.new(bump.outputs['Normal'], surface.inputs['Normal'])
        obj.data.materials.append(material)
        if len(record['materials']) == 1:
            for polygon in obj.data.polygons:
                polygon.material_index = 0
        else:
            for polygon in obj.data.polygons[offset:offset+definition['triangleCount']]:
                polygon.material_index = index
        offset += definition['triangleCount']
        materials.append(dict(name=material.name, shader=definition['shader'], base=base, normal=normal, mask=(base+'_TeamMask') if 'Colorize' in definition['shader'] else None, properties=props))
    assert len(obj.data.polygons) == sum(row['triangleCount'] for row in record['materials'])
    for constraint in obj.constraints:
        constraint.inverse_matrix = Matrix.Identity(4)

before = Snapshot(scene)
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    world = obj.matrix_world.copy()
    constraint = obj.constraints[0]
    assert len(obj.constraints) == 1 and constraint.type == 'CHILD_OF'
    obj.parent, obj.parent_type, obj.parent_bone = constraint.target, 'BONE', constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
after_parenting = Snapshot(scene)
assert max(abs(before[key][i]-after_parenting[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < .001
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/Centax.blend', copy=True)
fbx = OUTPUT_DIRECTORY + '/Centax.fbx'
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'MESH','ARMATURE','EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation = bpy.data.scenes.new('Centax FBX Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=fbx, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
for obj in validation.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj['EaWHidden'])
        obj.hide_set(obj.hide_render)
after = Snapshot(validation)
assert set(before['bones']) == set(after['bones'])
assert all(before['bones'][n]['parent'] == after['bones'][n]['parent'] for n in before['bones'])
bone_error = max((Vector(before['bones'][n]['head']) - Vector(after['bones'][n]['head'])).length for n in before['bones'])
assert bone_error < .001
assert set(before['meshes']) == set(after['meshes'])
assert all(before['meshes'][n]['triangles'] == after['meshes'][n]['triangles'] for n in before['meshes'])
assert all(before['meshes'][n]['slot_triangles'] == after['meshes'][n]['slot_triangles'] for n in before['meshes'])
report = dict(models={'Centax':dict(before=before,after=after)}, materials=materials, bone_error=bone_error, fbx=fbx, source_model=AUDIT['source_model'])
print('CENTAX_REPORT ' + json.dumps(report))
bpy.context.window.scene = scene
for obj in scene.objects:
    if obj.type == 'ARMATURE':
        obj.hide_set(True)
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.shading.type = 'MATERIAL'
        area.spaces.active.overlay.show_overlays = False
        area.spaces.active.region_3d.view_location = Vector((0,0,0))
        area.spaces.active.region_3d.view_distance = 330
        area.spaces.active.region_3d.view_rotation = Vector((1,-1,1)).to_track_quat('Z','Y')
        area.spaces.active.region_3d.view_perspective = 'ORTHO'