"""Convert RaW's Charger c70 through Blender MCP; preserve all other scenes."""
import bpy
import bmesh
import json
from mathutils import Matrix, Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/CR70ChargerImport/Output'
AUDIT = json.loads("{\"source_unit\":\"Republic_light_frigate\",\"source_model\":\"D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Models\\\\ReV_light_frigate.ALO\",\"requested_name\":\"CR70 Corvette\",\"model\":{\"bones\":[{\"name\":\"Root\",\"parent_index\":4294967295,\"visible\":1,\"matrix\":[1,0,0,0,0,1,0,0,0,0,1,0]},{\"name\":\"Engine\",\"parent_index\":0,\"visible\":1,\"matrix\":[-1,0,1.5099581673894136e-7,-44.21373748779297,1.5099581673894136e-7,0,1,75.55125427246094,0,1,0,-0.01850029267370701]},{\"name\":\"Engines\",\"parent_index\":0,\"visible\":1,\"matrix\":[-1,0,1.5099581673894136e-7,-7.371759414672852,1.5099581673894136e-7,0,1,75.63228607177734,0,1,0,-31.75396728515625]},{\"name\":\"PE_Rep_Corvette\",\"parent_index\":2,\"visible\":1,\"matrix\":[-1,1.4210854715202004e-14,8.742277657347586e-8,9.935750961303711,8.742277657347586e-8,0,1,31.75396728515625,1.4210854715202004e-14,1,-8.470329472543003e-22,5.135833740234375]},{\"name\":\"PTE_Rep_Corvette\",\"parent_index\":2,\"visible\":1,\"matrix\":[-1,2.842170943040401e-14,3.8941436741879443e-7,-5.35455322265625,3.8941436741879443e-7,-3.3881317890172014e-21,1,31.75396728515625,2.842170943040401e-14,1,-1.3552527156068805e-20,5.135833740234375]},{\"name\":\"Collision\",\"parent_index\":0,\"visible\":1,\"matrix\":[1,0,0,-0.5455780029296875,0,1,0,-15.487434387207031,0,0,1,0]},{\"name\":\"Hull\",\"parent_index\":0,\"visible\":1,\"matrix\":[1,0,0,7.289857708690581e-25,0,1,0,-4.696199417114258,0,0,1,-0.46181318163871765]},{\"name\":\"Object02\",\"parent_index\":0,\"visible\":1,\"matrix\":[1,0,0,0.002862590132281184,0,1,0,3.0002245903015137,0,0,1,0.05904402211308479]},{\"name\":\"HP01\",\"parent_index\":0,\"visible\":1,\"matrix\":[1,0,0,20.547622680664062,0,1,0,-26.112564086914062,0,0,1,10.682567596435547]},{\"name\":\"HP02\",\"parent_index\":0,\"visible\":1,\"matrix\":[-1,8.742277657347586e-8,7.64274186065882e-15,-20.744983673095703,8.742277657347586e-8,1,8.742277657347586e-8,-26.112560272216797,0,8.742277657347586e-8,-1,10.682567596435547]},{\"name\":\"HP03\",\"parent_index\":0,\"visible\":1,\"matrix\":[-1,8.742277657347586e-8,7.64274186065882e-15,-20.744983673095703,8.742277657347586e-8,1,8.742277657347586e-8,-26.112560272216797,0,8.742277657347586e-8,-1,-11.070388793945312]},{\"name\":\"HP04\",\"parent_index\":0,\"visible\":1,\"matrix\":[1,0,0,20.547622680664062,0,1,0,-26.112564086914062,0,0,1,-11.070388793945312]},{\"name\":\"HP05\",\"parent_index\":0,\"visible\":1,\"matrix\":[-3.5762786865234375e-7,0.9999999403953552,8.742274815176643e-8,0.04352552071213722,0.9999999403953552,3.5762786865234375e-7,1.7763568394002505e-14,41.3343505859375,-1.0658141036401503e-14,8.742274815176643e-8,-1,19.402986526489258]},{\"name\":\"HP06\",\"parent_index\":0,\"visible\":1,\"matrix\":[-1.1920928955078125e-7,0.9999999403953552,0,0.04352569580078125,-0.9999999403953552,-1.1920928955078125e-7,0,-111.26988983154297,0,0,1,16.048648834228516]}],\"objects\":[{\"kind\":\"0x400\",\"name\":\"Engine\"},{\"kind\":\"0x400\",\"name\":\"Engines\"},{\"kind\":\"0x400\",\"name\":\"Collision\"},{\"kind\":\"0x400\",\"name\":\"Hull\"},{\"kind\":\"0x400\",\"name\":\"Object02\"}],\"connections\":[{\"object_index\":0,\"bone_index\":1,\"bone\":\"Engine\"},{\"object_index\":1,\"bone_index\":2,\"bone\":\"Engines\"},{\"object_index\":2,\"bone_index\":5,\"bone\":\"Collision\"},{\"object_index\":3,\"bone_index\":6,\"bone\":\"Hull\"},{\"object_index\":4,\"bone_index\":7,\"bone\":\"Object02\"}]},\"materials\":{\"file\":\"D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Models\\\\ReV_light_frigate.ALO\",\"sha256\":\"1b541d67bb332c91d03e86d8ee1e1f33dfba63c63571becef55dd0214fde129c\",\"meshes\":[{\"materials\":[{\"properties\":{\"BaseTexture\":\"ReV_light_frigate_L.dds\",\"UVScrollRate\":[0,0,0,0],\"Color\":[1,0.9960784912109375,0.5490196347236633,0]},\"shader\":\"MeshAdditive.fx\",\"vertexCount\":51,\"triangleCount\":48}],\"name\":\"Engine\",\"hidden\":false,\"collision\":false},{\"materials\":[{\"properties\":{},\"shader\":\"alDefault.fx\",\"vertexCount\":3,\"triangleCount\":1}],\"name\":\"Engines\",\"hidden\":true,\"collision\":false},{\"materials\":[{\"properties\":{\"Color\":[0,0,1,0.5]},\"shader\":\"MeshCollision.fx\",\"vertexCount\":24,\"triangleCount\":12}],\"name\":\"Collision\",\"hidden\":true,\"collision\":true},{\"materials\":[{\"properties\":{\"Emissive\":[0,0,0,0],\"Diffuse\":[1,1,1,0],\"Specular\":[0.21568629145622253,0.21568629145622253,0.21568629145622253,0],\"Shininess\":[32],\"Colorization\":[1,1,1,1],\"UVOffset\":[0,0,0,0],\"BaseTexture\":\"ReV_light_frigate.dds\",\"NormalTexture\":\"ReV_light_frigate_b.dds\"},\"shader\":\"MeshBumpColorize.fx\",\"vertexCount\":7468,\"triangleCount\":3921}],\"name\":\"Hull\",\"hidden\":false,\"collision\":false},{\"materials\":[{\"properties\":{\"Emissive\":[0,0,0,0],\"Diffuse\":[1,1,1,0],\"Specular\":[0.21568629145622253,0.21568629145622253,0.21568629145622253,0],\"Shininess\":[32],\"Colorization\":[1,1,1,1],\"UVOffset\":[0,0,0,0],\"BaseTexture\":\"ReV_light_frigate.dds\",\"NormalTexture\":\"ReV_light_frigate_b.dds\"},\"shader\":\"MeshBumpColorize.fx\",\"vertexCount\":424,\"triangleCount\":240}],\"name\":\"Object02\",\"hidden\":false,\"collision\":false}],\"connections\":[{\"mesh\":0,\"bone\":1},{\"mesh\":1,\"bone\":2},{\"mesh\":2,\"bone\":5},{\"mesh\":3,\"bone\":6},{\"mesh\":4,\"bone\":7}],\"proxies\":[{\"name\":\"PE_Rep_Corvette\",\"bone\":3},{\"name\":\"PTE_Rep_Corvette\",\"bone\":4}]},\"textures\":{\"ReV_light_frigate.dds\":{\"stem\":\"CR70_Charger_Hull_Albedo\",\"size\":[1024,1024],\"alpha\":[0,255],\"team_mask_coverage\":0.8316450119018555},\"ReV_light_frigate_L.dds\":{\"stem\":\"CR70_Charger_Engine_Emission\",\"size\":[64,64],\"alpha\":[255,255]},\"ReV_light_frigate_b.dds\":{\"stem\":\"CR70_Charger_Hull_Normal\",\"size\":[1024,1024],\"alpha\":[0,0]}},\"source_hashes\":{\"D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Models\\\\ReV_light_frigate.ALO\":\"1b541d67bb332c91d03e86d8ee1e1f33dfba63c63571becef55dd0214fde129c\",\"D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\XML\\\\Units_Space_republic_light_frigate.xml\":\"e305f3bef81b110c0a6ed713a2f3258d0748a71be8672112ab9362cb4e6f79e6\",\"D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Textures\\\\ReV_light_frigate.dds\":\"27587d85337b41aad20f38b513b452454efa1bb9b6814bc5505021e235c1afd3\",\"D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Textures\\\\ReV_light_frigate_L.dds\":\"df8f92bfee8ee210e90f021ecb9e7e85f74d7196500ffbed0ce8daa062d9180e\",\"D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Textures\\\\ReV_light_frigate_b.dds\":\"2c0ad833bf818a9dbdfea668badcd7d5b3b1fcf155eff6972cbf0caf62a33786\"}}")


def Snapshot(scene):
    bpy.context.window.scene = scene
    bpy.context.view_layer.update()
    meshes = [obj for obj in scene.objects if obj.type == 'MESH']
    points = [obj.matrix_world @ vertex.co for obj in meshes if not obj.hide_render and obj['EaWShader'] != 'MeshAdditive.fx' for vertex in obj.data.vertices]
    return dict(
        meshes={obj['EaWName']: dict(exported_name=obj.name, vertices=len(obj.data.vertices), triangles=sum(len(p.vertices)-2 for p in obj.data.polygons), uv_layers=len(obj.data.uv_layers), hidden=obj.hide_render, materials=[slot.material.name for slot in obj.material_slots], slot_triangles=[sum(len(p.vertices)-2 for p in obj.data.polygons if p.material_index == slot) for slot in range(len(obj.material_slots))]) for obj in meshes},
        bones={bone.name: dict(parent=bone.parent.name if bone.parent else None, head=list(obj.matrix_world @ bone.head_local)) for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.data.bones},
        bounds_min=[min(p[i] for p in points) for i in range(3)],
        bounds_max=[max(p[i] for p in points) for i in range(3)])


scene = bpy.data.scenes['CR70 Charger Source']
bpy.context.window.scene = scene
rig = next(obj for obj in scene.objects if obj.type == 'ARMATURE')
assert len(rig.data.bones) == len(AUDIT['model']['bones']) == 14
rig.name = 'CR70ChargerRig'
# The importer stopped before its final hierarchy cleanup; restore audited parents.
bpy.context.view_layer.objects.active = rig
rig.hide_set(False)
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for index, record in enumerate(AUDIT['model']['bones']):
    bone = rig.data.edit_bones[record['name']]
    parent = record['parent_index']
    bone.parent = rig.data.edit_bones[AUDIT['model']['bones'][parent]['name']] if parent != 4294967295 else None
bpy.ops.object.mode_set(mode='OBJECT')
textures = {}
for filename, record in AUDIT['textures'].items():
    image = bpy.data.images.load(OUTPUT_DIRECTORY + '/Textures/' + record['stem'] + '.png', check_existing=False)
    image.alpha_mode = 'CHANNEL_PACKED'
    image.colorspace_settings.is_data = 'Normal' in record['stem']
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
        material = bpy.data.materials.new('CR70_Charger_' + name + '_Slot' + str(index))
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
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/CR70.blend', copy=True)
fbx = OUTPUT_DIRECTORY + '/CR70.fbx'
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'MESH','ARMATURE','EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation = bpy.data.scenes.new('CR70 Charger FBX Validation')
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
report = dict(models={'CR70':dict(before=before,after=after)}, materials=materials, bone_error=bone_error, fbx=fbx, source_model=AUDIT['source_model'])
print('CHARGER_REPORT ' + json.dumps(report))
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
