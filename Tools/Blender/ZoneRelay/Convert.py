"""Run in Blender 3.6 on a fresh scene; reuse the audited ALO conversion core."""
import json
import sys
from pathlib import Path
import bpy

PROJECT = Path('F:/Private/empire-at-war')
sys.path.insert(0, str(PROJECT / 'Tools/AloImport'))
from blender_worker import matrices, unique_names, make_material, expected_corners, actual_corners, compare, verify_bones
from source_audit import audit_alo

output = PROJECT / 'Temp/ZoneRelayImport'
spec = json.loads((output / 'Spec.json').read_text())
assert bpy.app.version[:2] == (3, 6), bpy.app.version_string
bpy.ops.preferences.addon_enable(module='io_alamo_tools')
bpy.ops.preferences.addon_enable(module='io_scene_fbx')
audit = audit_alo(spec['piece']['file'], 'SpaceSensorNode', include_geometry=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.import_mesh.alo(filepath=audit['file'], importAnimations=False)
rig = next(obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE')
rig.name = 'SpaceSensorNodeRig'
world = matrices(audit['bones'])
names = unique_names(audit['bones'])
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')
if 'Root' not in rig.data.edit_bones:
    root = rig.data.edit_bones.new('Root')
    root.head, root.tail = (0,0,0), (0,1,0)
for index, row in enumerate(audit['bones']):
    bone = rig.data.edit_bones[names[index]]
    bone.parent = None if row['parent_index'] == 0xffffffff else rig.data.edit_bones[names[row['parent_index']]]
    bone.matrix = world[index]
bpy.ops.object.mode_set(mode='OBJECT')
for index, name in enumerate(names):
    rig.data.bones[name]['EaWSourceBoneIndex'] = index
    rig.data.bones[name]['EaWPieceId'] = 'SpaceSensorNode'
    rig.data.bones[name]['EaWSourceMatrix'] = json.dumps(audit['bones'][index]['matrix'])
for obj in list(bpy.context.scene.objects):
    if obj.type == 'MESH':
        bpy.data.objects.remove(obj, do_unlink=True)
cache = {}
source_checks = []
for mesh in audit['meshes']:
    mesh['pieceId'] = 'SpaceSensorNode'
    slot = mesh['materials'][0]
    assert len(mesh['materials']) == 1
    # Preserve ALO vertex order and isolate normal spaces at every triangle edge.
    data = bpy.data.meshes.new(mesh['name'])
    data.from_pydata(slot['positions'], [], [slot['triangles'][i:i+3] for i in range(0,len(slot['triangles']),3)])
    data.update()
    uv = data.uv_layers.new(name='UVMap')
    for loop in data.loops:
        u,v = slot['uvs'][loop.vertex_index]
        uv.data[loop.index].uv = (u,-v)
    for poly in data.polygons:
        poly.use_smooth = True
    data.use_auto_smooth = True
    for edge in data.edges:
        edge.use_edge_sharp = True
    data.normals_split_custom_set_from_vertices(slot['normals'])
    obj = bpy.data.objects.new(mesh['name'],data)
    bpy.context.scene.collection.objects.link(obj)
    connection = next(c for c in audit['connections'] if c['object_index'] == mesh['object_index'])
    bone_index = connection['bone_index']
    obj.parent, obj.parent_type, obj.parent_bone = rig, 'BONE', names[bone_index]
    bpy.context.view_layer.update()
    obj.matrix_world = world[bone_index]
    obj['EaWSourceMeshIndex'] = mesh['index']
    obj['EaWHidden'] = mesh['hidden']
    obj.hide_render = mesh['hidden']
    data.materials.append(make_material(spec,'SpaceSensorNode',mesh['index'],slot,cache))
    source_checks.append(compare(expected_corners(mesh,world[bone_index]),actual_corners(obj),mesh,spec))
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(output/'SpaceSensorNode.blend'),copy=True)
bpy.ops.export_scene.fbx(filepath=str(output/'SpaceSensorNode.fbx'),object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=False,use_custom_props=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='RELATIVE',mesh_smooth_type='EDGE')
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj,do_unlink=True)
bpy.ops.import_scene.fbx(filepath=str(output/'SpaceSensorNode.fbx'),use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
roundtrip = []
for mesh in audit['meshes']:
    obj = next(o for o in bpy.context.scene.objects if o.type=='MESH' and int(o['EaWSourceMeshIndex'])==mesh['index'])
    connection = next(c for c in audit['connections'] if c['object_index']==mesh['object_index'])
    roundtrip.append(compare(expected_corners(mesh,world[connection['bone_index']]),actual_corners(obj),mesh,spec))
rig = next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
result = {'source':source_checks,'fbx':roundtrip,'skeletonFbx':verify_bones(rig,audit,names,world,roundtrip=True),'triangles':sum(m['materials'][0]['triangleCount'] for m in audit['meshes'])}
(output / 'ConversionReport.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result))
