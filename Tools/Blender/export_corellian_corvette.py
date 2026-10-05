"""Export CR90 scene geometry through Blender MCP; keep other open scenes intact.

Run after importing RV_CORVETTE.ALO (or RV_CORVETTE_D.ALO for the wreck).
ALAMO may stop during global helper cleanup in a multi-scene session. This
script completes that cleanup only for this model and preserves its verified Root.
"""
import bpy
import bmesh
import json
from mathutils import Matrix, Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/CorellianCorvetteImport/Output'
VARIANT = 'CorellianCorvette'
TEXTURES = {
    'RV_CORVETTE.DDS': ('CorellianCorvette_Hull_Albedo', False),
    'RV_CORVETTE_BUMP.DDS': ('CorellianCorvette_Hull_Normal', True),
    'W_LASER_SMALL.DDS': ('CorellianCorvette_Flash_Albedo', False),
}


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [obj for obj in scene.objects if obj.type == 'MESH']
    visible = [obj for obj in meshes if obj.get('EaWName') in ('Corvette', 'engines')]
    if not visible:
        visible = [obj for obj in meshes if not obj.hide_render]
    points = [obj.matrix_world @ vertex.co for obj in visible for vertex in obj.data.vertices]
    return {
        'meshes': {obj.get('EaWName', obj.name): dict(vertices=len(obj.data.vertices), triangles=sum(len(p.vertices)-2 for p in obj.data.polygons), uv_layers=len(obj.data.uv_layers), hidden=obj.hide_render, materials=[s.material.name for s in obj.material_slots]) for obj in meshes},
        'bones': {bone.name: dict(parent=bone.parent.name if bone.parent else None, head=list(obj.matrix_world @ bone.head_local)) for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.data.bones},
        'bounds_min': [min(p[i] for p in points) for i in range(3)],
        'bounds_max': [max(p[i] for p in points) for i in range(3)],
    }


source_scene = bpy.context.scene
assert source_scene.name == VARIANT + ' Source'
rig = next(obj for obj in source_scene.objects if obj.type == 'ARMATURE')
expected_bones = 17 if VARIANT == 'CorellianCorvette' else 12
assert len(rig.data.bones) in (expected_bones, expected_bones - 1)
rig.name = VARIANT + 'Rig'
# Source binary verifies identity Root. ALAMO creates its child as a second root.
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
root = rig.data.edit_bones.get('Root')
if root is None:
    root = rig.data.edit_bones.new('Root')
    root.head = (0, 0, 0)
    root.tail = (0, 1, 0)
for bone in rig.data.edit_bones:
    if bone != root and bone.parent is None:
        bone.parent = root
bpy.ops.object.mode_set(mode='OBJECT')

materials = []
copies = {}
for obj in source_scene.objects:
    if obj.type != 'MESH':
        continue
    name = obj.name.rsplit('.', 1)[0] if obj.name[-4:-3] == '.' else obj.name
    obj['EaWName'] = name
    shader = obj.material_slots[0].material.shaderList.shaderList
    obj['EaWShader'] = shader
    # Complete the importer's cleanup without touching another scene's helpers.
    if shader in ('MeshCollision.fx', 'MeshShadowVolume.fx', 'RSkinShadowVolume.fx'):
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=0.0001)
        mesh.to_mesh(obj.data)
        mesh.free()
    obj.hide_render = bool(obj.Hidden) or shader in ('MeshCollision.fx', 'MeshShadowVolume.fx', 'RSkinShadowVolume.fx')
    obj.hide_set(obj.hide_render)
    obj['EaWHidden'] = obj.hide_render
    for constraint in obj.constraints:
        constraint.inverse_matrix = Matrix.Identity(4)
    for slot in obj.material_slots:
        original = slot.material
        if original.name not in copies:
            copies[original.name] = original.copy()
        slot.material = copies[original.name]
bpy.context.view_layer.update()
before = Snapshot(source_scene)
for obj in source_scene.objects:
    if obj.type != 'MESH':
        continue
    world = obj.matrix_world.copy()
    assert len(obj.constraints) == 1 and obj.constraints[0].type == 'CHILD_OF'
    constraint = obj.constraints[0]
    obj.parent = constraint.target
    obj.parent_type = 'BONE'
    obj.parent_bone = constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
after_parenting = Snapshot(source_scene)
assert max(abs(before[k][i] - after_parenting[k][i]) for k in ('bounds_min', 'bounds_max') for i in range(3)) < 0.001

images = {}
for name, (target, is_data) in TEXTURES.items():
    image = bpy.data.images.load(OUTPUT_DIRECTORY + '/Textures/' + target + '.png', check_existing=False)
    image.alpha_mode = 'CHANNEL_PACKED'
    image.colorspace_settings.is_data = is_data
    image.pack()
    images[name] = image
for material in copies.values():
    shader = material.shaderList.shaderList
    base = material.BaseTexture.upper()
    normal = material.NormalTexture.upper()
    materials.append(dict(name=material.name, shader=shader, base=TEXTURES[base][0] if base in TEXTURES else None, normal=TEXTURES[normal][0] if normal in TEXTURES else None))
    material['EaWShader'] = shader
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new('ShaderNodeOutputMaterial')
    surface = nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value = 0.65
    material.node_tree.links.new(surface.outputs['BSDF'], output.inputs['Surface'])
    if base in images:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images[base]
        material.node_tree.links.new(texture.outputs['Color'], surface.inputs['Base Color'])
    if 'Bump' in shader and normal in images:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images[normal]
        normal_map = nodes.new('ShaderNodeNormalMap')
        material.node_tree.links.new(texture.outputs['Color'], normal_map.inputs['Color'])
        material.node_tree.links.new(normal_map.outputs['Normal'], surface.inputs['Normal'])

for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_location = tuple((before['bounds_min'][i] + before['bounds_max'][i]) / 2 for i in range(3))
        area.spaces.active.region_3d.view_distance = 230
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/' + VARIANT + '.blend', copy=True)
fbx = OUTPUT_DIRECTORY + '/' + VARIANT + '.fbx'
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation = bpy.data.scenes.new(VARIANT + '_FBX_Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=fbx, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
for obj in validation.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj.get('EaWHidden', False))
        obj.hide_set(obj.hide_render)
after = Snapshot(validation)
assert set(before['bones']) == set(after['bones'])
bone_error = max((Vector(before['bones'][n]['head']) - Vector(after['bones'][n]['head'])).length for n in before['bones'])
assert bone_error < 0.001
assert all(before['bones'][n]['parent'] == after['bones'][n]['parent'] for n in before['bones'])
assert set(before['meshes']) == set(after['meshes'])
assert all(m['uv_layers'] == 1 for m in after['meshes'].values())
assert max(abs(before[k][i]-after[k][i]) for k in ('bounds_min','bounds_max') for i in range(3)) < 0.001
print('CR90_REPORT', json.dumps(dict(before=before, after=after, materials=materials, bone_error=bone_error, fbx=fbx)))
bpy.context.window.scene = source_scene
