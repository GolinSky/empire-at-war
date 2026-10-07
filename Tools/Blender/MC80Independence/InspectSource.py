"""Inspect source mesh/material/UV intent through the isolated Blender MCP."""
import bpy
import json

assert bpy.types.blendermcp_server.port == 9883
scene = bpy.data.scenes['MC80Independence Source']
meshes = []
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    points = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    uvs = [loop.uv for loop in obj.data.uv_layers.active.data]
    meshes.append(dict(name=obj.name, hidden=obj.hide_render, parent_bone=obj.parent_bone,
        triangles=sum(len(p.vertices)-2 for p in obj.data.polygons),
        material_index_counts={str(index): sum(p.material_index == index for p in obj.data.polygons) for index in set(p.material_index for p in obj.data.polygons)},
        bounds_min=[min(p[i] for p in points) for i in range(3)],
        bounds_max=[max(p[i] for p in points) for i in range(3)],
        uv_min=[min(uv[i] for uv in uvs) for i in range(2)],
        uv_max=[max(uv[i] for uv in uvs) for i in range(2)],
        materials=[dict(name=slot.material.name, shader=slot.material['EaWShader'],
                        base=slot.material.BaseTexture,
                        diffuse=list(slot.material.diffuse_color)) for slot in obj.material_slots]))
report = dict(blender=bpy.app.version_string, scene=scene.name, meshes=meshes)
print('SURFACE_JSON', json.dumps(report))
