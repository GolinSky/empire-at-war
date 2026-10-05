"""Run through Blender MCP on the fresh audited Imperator source scene.
The binary has an identity Root omitted by ALAMO. All meshes and bones remain.
Blender 3.6.23; prepared lossless PNGs are staged by Prepare.py.
"""
import bpy
import json
import math
import itertools
from mathutils import Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/ImperatorImport/Output'
VARIANT = 'Imperator'
TEXTURES = {
    'EV_ISD_HULL.dds': ('Imperator_Hull_Albedo', False),
    'EV_ISD_HULL_BC.dds': ('Imperator_Hull_Normal', True),
    'EV_ISDMKI_Bridge.dds': ('Imperator_Bridge_Albedo', False),
    'EV_ISDMKI_Bridge_bc.dds': ('Imperator_Bridge_Normal', True),
    'EV_ISDMKI_Bridge_Lights.dds': ('Imperator_BridgeLights_Emissive', False),
    'EV_ISDMKI_shiplights.dds': ('Imperator_ShipLights_Emissive', False),
    'EV_ISDMKI_THRUSTER.dds': ('Imperator_Thrusters_Emissive', False),
    'W_BLAST00.dds': ('Imperator_Damage_Albedo', False),
}


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [obj for obj in scene.objects if obj.type == 'MESH']
    points = [obj.matrix_world @ vertex.co for obj in meshes if not obj.hide_render for vertex in obj.data.vertices]
    return {
        'meshes': {obj.name: {'vertices': len(obj.data.vertices), 'triangles': sum(len(poly.vertices)-2 for poly in obj.data.polygons), 'uv_layers': len(obj.data.uv_layers), 'hidden': obj.hide_render, 'materials': [s.material.name for s in obj.material_slots]} for obj in meshes},
        'bones': {bone.name: {'parent': bone.parent.name if bone.parent else None, 'head': list(obj.matrix_world @ bone.head_local)} for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.data.bones},
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
        other = next(o for o in validation.objects if o.type == 'MESH' and o.get('EaWName') == obj.name)
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


source_scene = bpy.context.scene
assert source_scene.name == VARIANT + ' Source'
rig = next(o for o in source_scene.objects if o.type == 'ARMATURE')
assert len(rig.data.bones) == (167 if VARIANT == 'Imperator' else 74)
assert 'Root' not in rig.data.bones
rig.name = VARIANT + 'Rig'
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
root = rig.data.edit_bones.new('Root')
root.head = (0,0,0)
root.tail = (0,1,0)
for bone in rig.data.edit_bones:
    if bone != root and bone.parent is None:
        bone.parent = root
bpy.ops.object.mode_set(mode='OBJECT')
before = Snapshot(source_scene)
for obj in source_scene.objects:
    if obj.type != 'MESH':
        continue
    obj['EaWHidden'] = obj.hide_render
    obj['EaWName'] = obj.name
    world = obj.matrix_world.copy()
    assert len(obj.constraints) == 1 and obj.constraints[0].type == 'CHILD_OF'
    constraint = obj.constraints[0]
    obj.parent = constraint.target
    assert 'BONE' in [i.identifier for i in obj.bl_rna.properties['parent_type'].enum_items]
    obj.parent_type = 'BONE'
    obj.parent_bone = constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
after_parenting = Snapshot(source_scene)
assert max(abs(before[key][i]-after_parenting[key][i]) for key in ('bounds_min','bounds_max') for i in range(3)) < .001
images = {}
for source_name,(target_name,is_data) in TEXTURES.items():
    image = bpy.data.images.load(OUTPUT_DIRECTORY + '/Textures/' + target_name + '.png',check_existing=False)
    assert image.size[0] > 0
    assert 'CHANNEL_PACKED' in [i.identifier for i in image.bl_rna.properties['alpha_mode'].enum_items]
    image.alpha_mode = 'CHANNEL_PACKED'
    image.colorspace_settings.is_data = is_data
    image.pack()
    images[source_name] = image
materials = []
for material in {s.material for o in source_scene.objects if o.type == 'MESH' for s in o.material_slots}:
    shader = material.shaderList.shaderList
    base = material.BaseTexture
    normal = material.NormalTexture if 'Bump' in shader else None
    materials.append({'name':material.name,'shader':shader,'base':TEXTURES[base][0] if base in TEXTURES else None,'normal':TEXTURES[normal][0] if normal in TEXTURES else None,'missing':base == 'ISDI_shiplights.dds'})
    material['EaWShader'] = shader
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new('ShaderNodeOutputMaterial')
    surface = nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value = .65
    material.node_tree.links.new(surface.outputs['BSDF'],output.inputs['Surface'])
    if base in images:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images[base]
        material.node_tree.links.new(texture.outputs['Color'],surface.inputs['Base Color'])
        if 'Additive' in shader:
            material.node_tree.links.new(texture.outputs['Color'],surface.inputs['Emission'])
    if normal in images:
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = images[normal]
        normal_map = nodes.new('ShaderNodeNormalMap')
        material.node_tree.links.new(texture.outputs['Color'],normal_map.inputs['Color'])
        material.node_tree.links.new(normal_map.outputs['Normal'],surface.inputs['Normal'])
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY+'/'+VARIANT+'.blend')
fbx_path = OUTPUT_DIRECTORY+'/'+VARIANT+'.fbx'
bpy.ops.export_scene.fbx(filepath=fbx_path,use_selection=False,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=False,use_custom_props=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='RELATIVE')
validation_scene = bpy.data.scenes.new(VARIANT+'_FBX_Validation')
bpy.context.window.scene = validation_scene
bpy.ops.import_scene.fbx(filepath=fbx_path,use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
for obj in validation_scene.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj.get('EaWHidden',False))
        obj.hide_set(obj.hide_render)
after = Snapshot(validation_scene)
assert set(before['bones']) == set(after['bones'])
bone_error = max((Vector(before['bones'][name]['head'])-Vector(after['bones'][name]['head'])).length for name in before['bones'])
assert bone_error < .001
assert all(before['bones'][name]['parent'] == after['bones'][name]['parent'] for name in before['bones'])
assert len(before['meshes']) == len(after['meshes'])
geometry_error = VerifyGeometry(source_scene,validation_scene)
assert all(mesh['uv_layers'] == 1 for mesh in after['meshes'].values())
assert max(abs(before[key][i]-after[key][i]) for key in ('bounds_min','bounds_max') for i in range(3)) < .001
print('IMPERATOR_REPORT',json.dumps({'before':before,'after':after,'materials':materials,'textures':TEXTURES,'bone_error':bone_error,'geometry_error':geometry_error,'fbx':fbx_path}))
bpy.context.window.scene = source_scene
