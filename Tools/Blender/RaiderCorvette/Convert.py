"""Run through Blender MCP in a fresh scene; preserve every existing scene."""
import bpy
import json
from mathutils import Vector

TASK = 'F:/Private/empire-at-war/Temp/RaiderCorvetteImport'

assert bpy.app.version_string == '3.6.23'
assert 'RaiderCorvette Source' not in bpy.data.scenes
scene = bpy.data.scenes.new('RaiderCorvette Source')
bpy.context.window.scene = scene
bpy.ops.import_mesh.alo(filepath='D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/Raider_Corvette.ALO', importAnimations=False)
rig = next(o for o in scene.objects if o.type == 'ARMATURE')
# Prepare.py verifies the source has 31 bones and an identity Root.
assert len(rig.data.bones) == 30 and 'Root' not in rig.data.bones
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
root = rig.data.edit_bones.new('Root')
root.head, root.tail = (0, 0, 0), (0, 1, 0)
for bone in rig.data.edit_bones:
    if bone != root and bone.parent is None:
        bone.parent = root
bpy.ops.object.mode_set(mode='OBJECT')

def Snapshot():
    bpy.context.view_layer.update()
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ v.co for o in meshes if not o.hide_render for v in o.data.vertices]
    return dict(meshes={o.get('EaWName', o.name):dict(triangles=sum(len(p.vertices)-2 for p in o.data.polygons), uv_layers=len(o.data.uv_layers), hidden=o.hide_render) for o in meshes},
                bones={b.name:dict(parent=b.parent.name if b.parent else None, head=list(rig.matrix_world @ b.head_local)) for b in rig.data.bones},
                bounds_min=[min(p[i] for p in points) for i in range(3)], bounds_max=[max(p[i] for p in points) for i in range(3)])

before = Snapshot()
for obj in scene.objects:
    obj['EaWName'] = obj.name
    if obj.type != 'MESH':
        continue
    obj['EaWHidden'] = obj.hide_render
    world = obj.matrix_world.copy()
    if obj.constraints:
        constraint = obj.constraints[0]
        assert len(obj.constraints) == 1 and constraint.type == 'CHILD_OF'
        obj.parent, obj.parent_type, obj.parent_bone = constraint.target, 'BONE', constraint.subtarget
        obj.constraints.remove(constraint)
        bpy.context.view_layer.update()
        obj.matrix_world = world
materials = []
for mat in {s.material for o in scene.objects if o.type == 'MESH' for s in o.material_slots}:
    shader, base = mat.shaderList.shaderList, mat.BaseTexture
    normal = mat.NormalTexture if 'Bump' in shader else None
    materials.append(dict(name=mat.name, shader=shader, base=base, normal=normal))
    mat['EaWShader'] = shader
    mat.use_nodes = True
    mat.node_tree.nodes.clear()
    output = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
    surface = mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    mat.node_tree.links.new(surface.outputs['BSDF'], output.inputs['Surface'])
    for filename, socket in [(base, 'Base Color'), (normal, 'Normal')]:
        if not filename or filename == 'None':
            continue
        image = next(i for i in bpy.data.images if i.name.lower() == filename.lower())
        assert image.size[0] > 0, filename
        image.filepath_raw = TASK + '/Output/Textures/' + filename[:-4] + '.png'
        image.file_format = 'PNG'
        image.save()
        image.pack()
        node = mat.node_tree.nodes.new('ShaderNodeTexImage')
        node.image = image
        if socket == 'Normal':
            image.colorspace_settings.is_data = True
            bump = mat.node_tree.nodes.new('ShaderNodeNormalMap')
            mat.node_tree.links.new(node.outputs['Color'], bump.inputs['Color'])
            mat.node_tree.links.new(bump.outputs['Normal'], surface.inputs['Normal'])
        else:
            mat.node_tree.links.new(node.outputs['Color'], surface.inputs['Base Color'])
bpy.ops.wm.save_as_mainfile(filepath=TASK + '/Output/RaiderCorvette.blend', copy=True)
bpy.ops.export_scene.fbx(filepath=TASK + '/Output/RaiderCorvette.fbx', use_selection=False, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation = bpy.data.scenes.new('RaiderCorvette Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=TASK + '/Output/RaiderCorvette.fbx', use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
rig = next(o for o in validation.objects if o.type == 'ARMATURE')
for obj in validation.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj['EaWHidden'])
after = Snapshot()
assert before['meshes'] == after['meshes']
assert set(before['bones']) == set(after['bones'])
assert all(before['bones'][k]['parent'] == after['bones'][k]['parent'] for k in before['bones'])
bone_error = max((Vector(before['bones'][k]['head']) - Vector(after['bones'][k]['head'])).length for k in before['bones'])
bounds_error = max(abs(before[k][i] - after[k][i]) for k in ('bounds_min', 'bounds_max') for i in range(3))
assert bone_error < .001 and bounds_error < .001, (bone_error, bounds_error)
report = dict(name='RaiderCorvette', source=before, roundtrip=after, materials=materials, bone_error=bone_error, bounds_error=bounds_error)
print('CONVERSION_JSON ' + json.dumps([report]))
bpy.context.window.scene = scene
rig = next(o for o in scene.objects if o.type == 'ARMATURE')
rig.hide_set(True)
for obj in scene.objects:
    if obj.type == 'MESH':
        obj.hide_set(obj.hide_render)
for area in bpy.context.window.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.shading.type = 'MATERIAL'
        area.spaces.active.overlay.show_overlays = False
        region = area.spaces.active.region_3d
        region.view_location = (Vector(before['bounds_min']) + Vector(before['bounds_max'])) * .5
        region.view_distance = 300
        region.view_rotation = Vector((1, -1, 1)).to_track_quat('Z', 'Y')
        region.view_perspective = 'ORTHO'
print(json.dumps(dict(meshes=len(before['meshes']), triangles=sum(m['triangles'] for m in before['meshes'].values()), bones=len(before['bones']), bone_error=bone_error, bounds_error=bounds_error, materials=materials)))
