"""Compare source and FBX scene triangle corners through Blender MCP."""
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
            for index in triangle.loops:
                vertex = obj.data.vertices[obj.data.loops[index].vertex_index]
                corners.append(tuple(obj.matrix_world @ vertex.co) + tuple(uv[index].uv))
        result[obj['EaWName']] = corners
    return result


source = bpy.context.scene
before = Geometry(bpy.data.scenes['CR70 Charger Source'])
after = Geometry(bpy.data.scenes['CR70 Charger FBX Validation'])
assert set(before) == set(after)
reports = []
for name, corners in before.items():
    assert len(corners) == len(after[name]), name
    by_uv = {}
    for corner in after[name]:
        by_uv.setdefault(tuple(round(v, 5) for v in corner[3:]), []).append(corner)
    position_error = uv_error = 0
    for a in corners:
        candidates = by_uv[tuple(round(v, 5) for v in a[3:])]
        b = min((sum((a[i] - value[i]) ** 2 for i in range(3)), value) for value in candidates)[1]
        position_error = max(position_error, max(abs(a[i] - b[i]) for i in range(3)))
        uv_error = max(uv_error, max(abs(a[i] - b[i]) for i in range(3, 5)))
    assert position_error < .001, (name, position_error)
    assert uv_error < .00001, (name, uv_error)
    reports.append(dict(mesh=name, corners=len(corners), position_error=position_error, uv_error=uv_error))
bpy.context.window.scene = source
print('GEOMETRY_JSON ' + json.dumps(reports))
