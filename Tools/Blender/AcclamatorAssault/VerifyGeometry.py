"""Compare retained source and validation scenes without modifying other work."""
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


reports = []
names = ['AcclamatorAssault'] + ['AcclamatorAssaultTurret'+str(i).zfill(2) for i in range(1,7)]
for name in names:
    before = Geometry(bpy.data.scenes[name+' Source'])
    after = Geometry(bpy.data.scenes[name+' Validation'])
    assert set(before) == set(after)
    position_error, uv_error = 0, 0
    for key in before:
        assert len(before[key]) == len(after[key]), key
        by_uv = {}
        for corner in after[key]:
            uv_key = tuple(round(x,5) for x in corner[3:])
            by_uv.setdefault(uv_key,[]).append(corner)
        for a in before[key]:
            candidates = by_uv[tuple(round(x,5) for x in a[3:])]
            best = min((sum((a[i]-b[i])**2 for i in range(3)),b) for b in candidates)[1]
            position_error = max(position_error,max(abs(a[i]-best[i]) for i in range(3)))
            uv_error = max(uv_error,max(abs(a[i]-best[i]) for i in range(3,5)))
    assert position_error < .001 and uv_error < .00001, (name,position_error,uv_error)
    reports.append(dict(name=name,position_error=position_error,uv_error=uv_error,
                        triangle_corners=sum(len(v) for v in before.values())))
print('GEOMETRY_JSON ' + json.dumps(reports))
bpy.context.window.scene = bpy.data.scenes['AcclamatorAssault Source']
