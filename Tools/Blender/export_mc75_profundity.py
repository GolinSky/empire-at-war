"""Run once on the fresh MC75Profundity Source scene through Blender MCP."""
import bpy
import json
import math
import itertools
from mathutils import Matrix, Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/MC75ProfundityImport'
TEXTURES = {
    'PROFUNDITY.DDS': ('MC75Profundity_Engines_Albedo', False),
    'PROFUNDITY_B.DDS': ('MC75Profundity_Engines_Normal', True),
    'RV_PROFUNDITY_DIFF.DDS': ('MC75Profundity_Hull_Albedo', False),
    'RV_PROFUNDITY_B.DDS': ('MC75Profundity_Hull_Normal', True),
    'LIGHTMESH_WINDOWS_FOR_BIG_STUFF.DDS': ('MC75Profundity_Windows_Emissive', False),
    'P_ISD_ENG.DDS': ('MC75Profundity_EngineGlow_Emissive', False),
    'PROFUNDITY_AMBIENT_LIGHT.DDS': ('MC75Profundity_Ambient_Emissive', False),
}


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [o for o in scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ v.co for o in meshes if not o.hide_render for v in o.data.vertices]
    return dict(meshes={o.get('EaWName', o.name): dict(vertices=len(o.data.vertices), triangles=sum(len(p.vertices)-2 for p in o.data.polygons), uv_layers=len(o.data.uv_layers), hidden=o.hide_render) for o in meshes}, bones={b.name: dict(parent=b.parent.name if b.parent else None, head=list(o.matrix_world @ b.head_local)) for o in scene.objects if o.type == 'ARMATURE' for b in o.data.bones}, bounds_min=[min(p[i] for p in points) for i in range(3)], bounds_max=[max(p[i] for p in points) for i in range(3)])


def Triangles(obj):
    obj.data.calc_loop_triangles()
    return [[(obj.matrix_world @ obj.data.vertices[v].co, obj.data.uv_layers.active.data[l].uv.copy()) for v,l in zip(t.vertices,t.loops)] for t in obj.data.loop_triangles]


def VerifyGeometry(source, validation):
    maximum_error = 0
    for obj in source.objects:
        if obj.type != 'MESH':
            continue
        other = next(o for o in validation.objects if o.type == 'MESH' and o.get('EaWName') == obj.get('EaWName'))
        original, copied = Triangles(obj), Triangles(other)
        assert len(original) == len(copied), obj.name
        buckets = {}
        def Key(triangle):
            return tuple(math.floor(sum(p[0][i] for p in triangle)/3/.01) for i in range(3))
        for index, triangle in enumerate(original):
            buckets.setdefault(Key(triangle), []).append(index)
        for triangle in copied:
            center = Key(triangle)
            candidates = [i for offset in itertools.product((-1,0,1),repeat=3) for i in buckets.get(tuple(center[j]+offset[j] for j in range(3)),[])]
            error = min(max(min((p-q).length if (uv-old_uv).length < .00001 else 1000 for q,old_uv in original[i]) for p,uv in triangle) for i in candidates)
            assert error < .001, (obj.name,error)
            maximum_error = max(maximum_error,error)
    return maximum_error


bpy.context.window.scene = bpy.data.scenes['MC75Profundity Source']
source = bpy.context.scene
rig = next(o for o in source.objects if o.type == 'ARMATURE')
assert len(rig.data.bones) == 146
assert 'Root' not in rig.data.bones
# The source binary contains an identity Root; upstream drops it during import.
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
root = rig.data.edit_bones.new('Root')
root.head, root.tail = (0,0,0), (0,1,0)
for bone in rig.data.edit_bones:
    if bone != root and bone.parent is None:
        bone.parent = root
bpy.ops.object.mode_set(mode='OBJECT')
rig.name = 'MC75ProfundityRig'
copies = {}
for obj in source.objects:
    if obj.type != 'MESH':
        continue
    obj['EaWName'] = obj.name
    obj['EaWHidden'] = obj.hide_render
    for slot in obj.material_slots:
        old = slot.material
        if old.name not in copies:
            copies[old.name] = old.copy()
            copies[old.name].name = 'MC75Profundity_' + old.name.replace(' Material','').replace(' ','_')
        slot.material = copies[old.name]
    obj['EaWShader'] = obj.material_slots[0].material.shaderList.shaderList
before = Snapshot(source)
for obj in source.objects:
    if obj.type != 'MESH' or not obj.constraints:
        continue
    world = obj.matrix_world.copy()
    assert len(obj.constraints) == 1 and obj.constraints[0].type == 'CHILD_OF'
    constraint = obj.constraints[0]
    obj.parent, obj.parent_type, obj.parent_bone = constraint.target, 'BONE', constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
after_parenting = Snapshot(source)
assert max(abs(before[k][i]-after_parenting[k][i]) for k in ('bounds_min','bounds_max') for i in range(3)) < .001
images = {}
for name,(target,is_data) in TEXTURES.items():
    image = bpy.data.images.load(OUTPUT_DIRECTORY+'/Textures/'+target+'.png',check_existing=False)
    image.alpha_mode = 'CHANNEL_PACKED'
    image.colorspace_settings.is_data = is_data
    image.pack()
    images[name] = image
materials = []
for material in copies.values():
    shader, base = material.shaderList.shaderList, material.BaseTexture.upper()
    normal = material.NormalTexture.upper() if 'Bump' in shader else ''
    materials.append(dict(name=material.name,shader=shader,base=TEXTURES[base][0] if base in TEXTURES else None,normal=TEXTURES[normal][0] if normal in TEXTURES else None))
    material['EaWShader'] = shader
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output, surface = nodes.new('ShaderNodeOutputMaterial'), nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value = .65
    material.node_tree.links.new(surface.outputs['BSDF'],output.inputs['Surface'])
    if base in images:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images[base]
        material.node_tree.links.new(texture.outputs['Color'],surface.inputs['Base Color'])
        if 'Additive' in shader:
            material.node_tree.links.new(texture.outputs['Color'],surface.inputs['Emission'])
    if normal in images:
        texture, normal_map = nodes.new('ShaderNodeTexImage'), nodes.new('ShaderNodeNormalMap')
        texture.image = images[normal]
        material.node_tree.links.new(texture.outputs['Color'],normal_map.inputs['Color'])
        material.node_tree.links.new(normal_map.outputs['Normal'],surface.inputs['Normal'])
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_location = tuple((before['bounds_min'][i]+before['bounds_max'][i])/2 for i in range(3))
        area.spaces.active.region_3d.view_distance = 900
        area.spaces.active.clip_end = 20000
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY+'/MC75Profundity.blend',copy=True)
fbx = OUTPUT_DIRECTORY+'/MC75Profundity.fbx'
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=False,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=False,use_custom_props=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='RELATIVE')
validation = bpy.data.scenes.new('MC75Profundity FBX Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=fbx,use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
for obj in validation.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj['EaWHidden'])
        obj.hide_set(obj.hide_render)
after = Snapshot(validation)
assert set(before['bones']) == set(after['bones'])
bone_error = max((Vector(before['bones'][n]['head'])-Vector(after['bones'][n]['head'])).length for n in before['bones'])
assert bone_error < .001
assert all(before['bones'][n]['parent']==after['bones'][n]['parent'] for n in before['bones'])
assert set(before['meshes']) == set(after['meshes'])
assert all(m['uv_layers']==1 for m in after['meshes'].values())
geometry_error = VerifyGeometry(source,validation)
print('MC75_REPORT',json.dumps(dict(before=before,after=after,materials=materials,bone_error=bone_error,geometry_error=geometry_error)))
bpy.context.window.scene = source
