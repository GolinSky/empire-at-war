objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
assert len(objects) == len(AUDIT['binary']['meshes'])
for obj, record in zip(objects, AUDIT['binary']['meshes']):
    assert obj.name.split('.')[0] == record['name'].split('.')[0], (obj.name, record['name'])
    if len(record['materials']) > 1:
        assert len(obj.data.polygons) == sum(m['triangleCount'] for m in record['materials']), obj.name
    obj.data.materials.clear()
    offset = 0
    for slot, definition in enumerate(record['materials']):
        material = bpy.data.materials.new(VARIANT + '_' + record['name'] + '_Slot' + str(slot).zfill(2))
        material.shaderList.shaderList = definition['shader']
        material.BaseTexture = definition['properties'].get('BaseTexture', 'None')
        material.NormalTexture = definition['properties'].get('NormalTexture', 'None')
        for key, value in definition['properties'].items():
            material[key] = value
        obj.data.materials.append(material)
        polygons = obj.data.polygons if len(record['materials']) == 1 else obj.data.polygons[offset:offset + definition['triangleCount']]
        for polygon in polygons:
            polygon.material_index = slot
        offset += definition['triangleCount']
    obj.hide_render = obj.hide_render or record['hidden'] or record['collision'] or all('Shadow' in m['shader'] or 'Collision' in m['shader'] for m in record['materials'])
    obj.hide_set(record['hidden'])
