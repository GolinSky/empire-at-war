"""Export a fresh vanilla MC80 source, wreck or hardpoint-pod scene through MCP.

Import each variant into <VARIANT> Source. Keep original files and other scenes intact.
The binary audit supplies the exact mesh names and bone count; no root repair is needed.
"""
import bpy
import bmesh
import json
import math
import itertools
from mathutils import Matrix, Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/MonCalCruiserImport/Output'
VARIANT = 'MonCalCruiser'
TEXTURES = {
    'RV_MONCALCRUISER.DDS': ('MonCalCruiser_Hull_Albedo', False),
    'RV_MONCALCRUISER_BC.DDS': ('MonCalCruiser_Hull_Normal', True),
    'EV_GIRDERS00.DDS': ('MonCalCruiser_Girders_Albedo', False),
    'W_BUILDINGLIGHTS.DDS': ('MonCalCruiser_Lights_Emissive', False),
    'W_BLAST00.DDS': ('MonCalCruiser_Damage_Albedo', False),
    'SHIELD_COLOR.DDS': ('MonCalCruiser_Shield_Albedo', False),
    'NB_SHIELDRIPPLE.DDS': ('MonCalCruiser_Shield_Distortion', True),
    'NB_SHIELDWAVE.DDS': ('MonCalCruiser_Shield_Wave', True),
}


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [obj for obj in scene.objects if obj.type == 'MESH']
    visible = [obj for obj in meshes if not obj.hide_render]
    points = [obj.matrix_world @ vertex.co for obj in visible for vertex in obj.data.vertices]
    return {
        'meshes': {obj.get('EaWName', obj.name): dict(vertices=len(obj.data.vertices), triangles=sum(len(p.vertices)-2 for p in obj.data.polygons), uv_layers=len(obj.data.uv_layers), hidden=obj.hide_render, materials=[s.material.name for s in obj.material_slots]) for obj in meshes},
        'bones': {bone.name: dict(parent=bone.parent.name if bone.parent else None, head=list(obj.matrix_world @ bone.head_local)) for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.data.bones},
        'bounds_min': [min(p[i] for p in points) for i in range(3)],
        'bounds_max': [max(p[i] for p in points) for i in range(3)],
    }


def Triangles(obj):
    obj.data.calc_loop_triangles()
    return [[(obj.matrix_world @ obj.data.vertices[v].co, obj.data.uv_layers.active.data[l].uv.copy()) for v,l in zip(t.vertices,t.loops)] for t in obj.data.loop_triangles]


def VerifyGeometry(source, validation):
    maximum_error = 0
    for obj in source.objects:
        if obj.type != 'MESH':
            continue
        other = next(o for o in validation.objects if o.type == 'MESH' and o.get('EaWName') == obj.get('EaWName'))
        original = Triangles(obj)
        copied = Triangles(other)
        buckets = {}
        def Key(triangle):
            return tuple(math.floor(sum(p[0][i] for p in triangle)/3/.01) for i in range(3))
        for index, triangle in enumerate(original):
            buckets.setdefault(Key(triangle), []).append(index)
        used = set()
        for triangle in copied:
            center = Key(triangle)
            candidates = [i for offset in itertools.product((-1,0,1),repeat=3) for i in buckets.get(tuple(center[j]+offset[j] for j in range(3)),[])]
            error, index = min((max(min((p-q).length if (uv-old_uv).length < .00001 else 1000 for q,old_uv in original[i]) for p,uv in triangle),i) for i in candidates)
            assert error < .001, (obj.name,error)
            maximum_error = max(maximum_error,error)
            used.add(index)
        for index, triangle in enumerate(original):
            if index in used:
                continue
            # FBX can weld duplicate faces. Every distinct position/UV face must survive.
            center = Key(triangle)
            candidates = [i for i in used if Key(original[i]) == center]
            a,b,c = [p[0] for p in triangle]
            if (b-a).cross(c-a).length < .0000001:
                continue
            assert any(max(min((p-q).length if (uv-old_uv).length < .00001 else 1000 for q,old_uv in original[i]) for p,uv in triangle) < .001 for i in candidates), obj.name
    return maximum_error


bpy.context.window.scene = bpy.data.scenes[VARIANT + ' Source']
source_scene = bpy.context.scene
assert source_scene.name == VARIANT + ' Source'
rig = next(obj for obj in source_scene.objects if obj.type == 'ARMATURE')
AUDITS = {'MonCalCruiser': {'bone_count': 71, 'objects': [{'kind': '0x400', 'name': 'HP_F-R_Blast'}, {'kind': '0x400', 'name': 'HP_M-R_Blast'}, {'kind': '0x400', 'name': 'HP_B-R_Blast'}, {'kind': '0x400', 'name': 'HP_B-L_Blast'}, {'kind': '0x400', 'name': 'HP_M-L_Blast'}, {'kind': '0x400', 'name': 'HP_F-L_Blast'}, {'kind': '0x400', 'name': 'SHADOW'}, {'kind': '0x400', 'name': 'MonCalCruiser'}, {'kind': '0x400', 'name': 'COLLISION'}, {'kind': '0x400', 'name': 'shield'}, {'kind': '0x400', 'name': 'engines_big'}, {'kind': '0x400', 'name': 'engines_small'}, {'kind': '0x400', 'name': 'girders'}, {'kind': '0x400', 'name': 'Lighting'}]}, 'MonCalCruiserWreck': {'bone_count': 79, 'objects': [{'kind': '0x400', 'name': 'SHADOW'}, {'kind': '0x400', 'name': 'MonCalCruiser'}, {'kind': '0x400', 'name': 'Girders00'}, {'kind': '0x400', 'name': 'gird02'}, {'kind': '0x400', 'name': 'HP_B-R_Blast'}, {'kind': '0x400', 'name': 'HP_F-L_Blast'}, {'kind': '0x400', 'name': 'HP_M-L_Blast'}, {'kind': '0x400', 'name': 'blasty'}, {'kind': '0x400', 'name': 'engines_big'}, {'kind': '0x400', 'name': 'engines_small'}, {'kind': '0x400', 'name': 'gird01'}, {'kind': '0x400', 'name': 'HP_B-L_Blast'}, {'kind': '0x400', 'name': 'Girders01'}, {'kind': '0x400', 'name': 'HP_F-R_Blast'}, {'kind': '0x400', 'name': 'HP_M-R_Blast'}]}, 'MonCalCruiserPod_F-L': {'bone_count': 77, 'objects': [{'kind': '0x400', 'name': 'HP00_F-L'}, {'kind': '0x400', 'name': 'HP_F-L_Coll'}, {'kind': '0x400', 'name': 'Lighting'}]}, 'MonCalCruiserPod_F-R': {'bone_count': 77, 'objects': [{'kind': '0x400', 'name': 'HP00_F-R'}, {'kind': '0x400', 'name': 'HP_F-R_Coll'}, {'kind': '0x400', 'name': 'Lighting'}]}, 'MonCalCruiserPod_M-L': {'bone_count': 77, 'objects': [{'kind': '0x400', 'name': 'HP00_M-L'}, {'kind': '0x400', 'name': 'HP_M-L_Coll'}, {'kind': '0x400', 'name': 'Lighting'}]}, 'MonCalCruiserPod_M-R': {'bone_count': 77, 'objects': [{'kind': '0x400', 'name': 'HP00_M-R'}, {'kind': '0x400', 'name': 'HP_M-R_Coll'}, {'kind': '0x400', 'name': 'Lighting'}]}, 'MonCalCruiserPod_B-L': {'bone_count': 77, 'objects': [{'kind': '0x400', 'name': 'HP00_B-L'}, {'kind': '0x400', 'name': 'HP_B-L_Coll'}, {'kind': '0x400', 'name': 'Lighting'}]}, 'MonCalCruiserPod_B-R': {'bone_count': 77, 'objects': [{'kind': '0x400', 'name': 'HP00_B-R'}, {'kind': '0x400', 'name': 'HP_B-R_Coll'}, {'kind': '0x400', 'name': 'Lighting'}]}, 'MonCalCruiserPod_E': {'bone_count': 76, 'objects': [{'kind': '0x400', 'name': 'HP_E_Coll'}]}}
audit = AUDITS[VARIANT]
assert len(rig.data.bones) == audit['bone_count']
assert 'Root' in rig.data.bones
rig.name = VARIANT + 'Rig'
mesh_objects = [obj for obj in source_scene.objects if obj.type == 'MESH']
assert len(mesh_objects) == len(audit['objects'])
for obj, description in zip(mesh_objects, audit['objects']):
    obj['EaWName'] = description['name']

materials = []
copies = {}
for obj in source_scene.objects:
    if obj.type != 'MESH':
        continue
    name = obj['EaWName']
    shader = obj.material_slots[0].material.shaderList.shaderList
    obj['EaWShader'] = shader
    # Complete the importer's cleanup without touching another scene's helpers.
    if shader in ('MeshCollision.fx', 'MeshShadowVolume.fx', 'RSkinShadowVolume.fx'):
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=0.0001)
        mesh.to_mesh(obj.data)
        mesh.free()
    obj.hide_render = bool(obj.Hidden) or shader in ('MeshCollision.fx', 'MeshShadowVolume.fx', 'RSkinShadowVolume.fx', 'MeshShield.fx') or name.startswith(('HP_', 'SHADOW')) or name in ('girders', 'blasty', 'engines_big', 'engines_small')
    obj.hide_set(obj.hide_render)
    obj['EaWHidden'] = obj.hide_render
    for constraint in obj.constraints:
        constraint.inverse_matrix = Matrix.Identity(4)
    for slot in obj.material_slots:
        original = slot.material
        if original.name not in copies:
            copies[original.name] = original.copy()
            copies[original.name].name = VARIANT + '_' + original.name.replace(' Material', '').replace(' ', '_')
        slot.material = copies[original.name]
bpy.context.view_layer.update()
before = Snapshot(source_scene)
for obj in source_scene.objects:
    if obj.type != 'MESH':
        continue
    world = obj.matrix_world.copy()
    if not obj.constraints:
        continue
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
    normal = material.NormalTexture.upper() if 'Bump' in shader else ''
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
        area.spaces.active.region_3d.view_distance = 650 if VARIANT == 'MonCalCruiser' else 400
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
geometry_error = VerifyGeometry(source_scene, validation)
report = dict(before=before, after=after, materials=materials, bone_error=bone_error, geometry_error=geometry_error, fbx=fbx)

print('MC80_REPORT', json.dumps(report))
bpy.context.window.scene = source_scene
