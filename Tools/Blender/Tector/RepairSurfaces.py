"""Blender snippet: restore binary material slots without changing geometry or UVs."""
for record in AUDIT['binary']['meshes']:
    if len(record['materials']) < 2:
        continue
    obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH' and o.name.split('.')[0] == record['name'])
    geometry = [tuple(v.co) for v in obj.data.vertices]
    uvs = [tuple(uv.uv) for uv in obj.data.uv_layers.active.data]
    assert len(obj.data.polygons) == sum(m['triangleCount'] for m in record['materials'])
    obj.data.materials.clear()
    offset = 0
    for slot, definition in enumerate(record['materials']):
        material = bpy.data.materials.new(VARIANT + '_' + record['name'] + '_Slot' + str(slot).zfill(2))
        material.shaderList.shaderList = definition['shader']
        material.BaseTexture = definition['properties']['BaseTexture']
        material.NormalTexture = definition['properties']['NormalTexture']
        for key, value in definition['properties'].items():
            material[key] = value
        obj.data.materials.append(material)
        for polygon in obj.data.polygons[offset:offset + definition['triangleCount']]:
            polygon.material_index = slot
        offset += definition['triangleCount']
    assert geometry == [tuple(v.co) for v in obj.data.vertices]
    assert uvs == [tuple(uv.uv) for uv in obj.data.uv_layers.active.data]
