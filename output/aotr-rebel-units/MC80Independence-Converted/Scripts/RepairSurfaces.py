"""Restore original slots/shaders without changing geometry or UVs; execute via generated MCP request."""
assert bpy.types.blendermcp_server.port == 9883
source = bpy.data.scenes['MC80Independence Source']
bpy.context.window.scene = source
before = Snapshot(source)
unchanged = {obj.name: ([tuple(v.co) for v in obj.data.vertices], [tuple(uv.uv) for uv in obj.data.uv_layers.active.data]) for obj in source.objects if obj.type == 'MESH'}
materials = {}
for definition in MATERIAL_AUDIT['materialDefinitions']:
    name, shader = definition['name'], definition['shader']
    material = bpy.data.materials.get(name)
    if material is None:
        material = bpy.data.materials.new(name)
    material.shaderList.shaderList = shader
    material['EaWShader'] = shader
    for property_name, value in definition['properties'].items():
        material[property_name] = value
    material.use_nodes = True
    material.node_tree.nodes.clear()
    nodes, links = material.node_tree.nodes, material.node_tree.links
    output, surface = nodes.new('ShaderNodeOutputMaterial'), nodes.new('ShaderNodeBsdfPrincipled')
    surface.inputs['Roughness'].default_value = .65
    surface.inputs['Alpha'].default_value = 1
    links.new(surface.outputs['BSDF'], output.inputs['Surface'])
    if definition['base']:
        image = bpy.data.images.load(OUTPUT_DIRECTORY+'/Textures/'+definition['base']+'.png',check_existing=True)
        image.alpha_mode = 'CHANNEL_PACKED'
        image.pack()
        texture = nodes.new('ShaderNodeTexImage')
        texture.image = image
        links.new(texture.outputs['Color'],surface.inputs['Base Color'])
        if 'Additive' in shader:
            links.new(texture.outputs['Color'],surface.inputs['Emission'])
    if definition['normal']:
        image = bpy.data.images.load(OUTPUT_DIRECTORY+'/Textures/'+definition['normal']+'.png',check_existing=True)
        image.colorspace_settings.is_data = True
        image.pack()
        texture, normal = nodes.new('ShaderNodeTexImage'), nodes.new('ShaderNodeNormalMap')
        texture.image = image
        links.new(texture.outputs['Color'],normal.inputs['Color'])
        links.new(normal.outputs['Normal'],surface.inputs['Normal'])
    materials[name] = material
for record in MATERIAL_AUDIT['meshes']:
    obj = next(obj for obj in source.objects if obj.type == 'MESH' and obj.get('EaWName') == record['name'])
    obj.data.materials.clear()
    for slot, definition in enumerate(record['materials']):
        obj.data.materials.append(materials[definition['materialName']])
    if len(record['materials']) == 1:
        for polygon in obj.data.polygons:
            polygon.material_index = 0
    else:
        assert len(obj.data.polygons) == sum(definition['triangleCount'] for definition in record['materials'])
        offset = 0
        for slot, definition in enumerate(record['materials']):
            for polygon in obj.data.polygons[offset:offset+definition['triangleCount']]:
                polygon.material_index = slot
            offset += definition['triangleCount']
    assert unchanged[obj.name] == ([tuple(v.co) for v in obj.data.vertices], [tuple(uv.uv) for uv in obj.data.uv_layers.active.data])
after = Snapshot(source)
assert before == after
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY+'/MC80Independence.blend',copy=True)
fbx = OUTPUT_DIRECTORY+'/MC80Independence.fbx'
bpy.ops.export_scene.fbx(filepath=fbx,use_selection=False,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=False,use_custom_props=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='RELATIVE')
validation = bpy.data.scenes['MC80Independence FBX Validation']
for obj in list(validation.objects):
    bpy.data.objects.remove(obj,do_unlink=True)
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=fbx,use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
for obj in validation.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj['EaWHidden'])
        obj.hide_set(obj.hide_render)
round_trip = Snapshot(validation)
assert set(after['bones']) == set(round_trip['bones'])
bone_error = max((Vector(after['bones'][name]['head'])-Vector(round_trip['bones'][name]['head'])).length for name in after['bones'])
assert bone_error < .001
geometry_error = VerifyGeometry(source, validation)
for record in MATERIAL_AUDIT['meshes']:
    obj = next(obj for obj in validation.objects if obj.type == 'MESH' and obj.get('EaWName') == record['name'])
    assert [slot.material.name.split('.')[0] for slot in obj.material_slots] == [definition['materialName'] for definition in record['materials']], record['name']
bpy.context.window.scene = source
print('SURFACE_REPAIR_JSON',json.dumps(dict(before=after,after=round_trip,materials=MATERIAL_AUDIT['materialDefinitions'],mesh_materials=MATERIAL_AUDIT['meshes'],bone_error=bone_error,geometry_error=geometry_error,geometry_and_uvs_unchanged=True)))
