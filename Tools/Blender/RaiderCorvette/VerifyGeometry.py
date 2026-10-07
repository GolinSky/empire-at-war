"""Compare the prepared source scene to its FBX validation scene via Blender MCP."""
import bpy
import json

def Geometry(scene):
    bpy.context.window.scene = scene
    bpy.context.view_layer.update()
    result = {}
    for obj in scene.objects:
        if obj.type != 'MESH':
            continue
        obj.data.calc_loop_triangles()
        uv = obj.data.uv_layers.active.data
        corners = []
        for triangle in obj.data.loop_triangles:
            for loop in triangle.loops:
                vertex = obj.data.loops[loop].vertex_index
                position = obj.matrix_world @ obj.data.vertices[vertex].co
                corners.append(tuple(position) + tuple(uv[loop].uv))
        result[obj['EaWName']] = corners
    return result

source = bpy.data.scenes['RaiderCorvette Source']
before = Geometry(source)
after = Geometry(bpy.data.scenes['RaiderCorvette Validation'])
assert set(before) == set(after)
position_error = uv_error = 0
for key in before:
    assert len(before[key]) == len(after[key]), key
    by_uv = {}
    for point in after[key]:
        uv_key = tuple(round(v, 5) for v in point[3:])
        by_uv.setdefault(uv_key, []).append(point)
    for point in before[key]:
        candidates = by_uv[tuple(round(v, 5) for v in point[3:])]
        best = min((sum((point[i]-p[i])**2 for i in range(3)), p) for p in candidates)[1]
        position_error = max(position_error, max(abs(point[i]-best[i]) for i in range(3)))
        uv_error = max(uv_error, max(abs(point[i]-best[i]) for i in range(3, 5)))
assert position_error < .001 and uv_error < .00001, (position_error, uv_error)
bpy.context.window.scene = source
print('GEOMETRY_JSON ' + json.dumps(dict(meshes=len(before), triangle_corners=sum(len(v) for v in before.values()), position_error=position_error, uv_error=uv_error)))
