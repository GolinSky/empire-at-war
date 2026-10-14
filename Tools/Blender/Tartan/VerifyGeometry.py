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

name = 'TartanTurret' if 'TartanTurret' in bpy.data.filepath else 'Tartan'
source = next(s for s in bpy.data.scenes if s.name.startswith(name + ' Source') and len(s.objects) > 0)
validation = bpy.data.scenes.new(name + ' Check')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=bpy.data.filepath.replace('.blend', '.fbx'), use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
before = Geometry(source)
after = Geometry(validation)
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
report = dict(meshes=len(before), triangle_corners=sum(len(v) for v in before.values()), position_error=position_error, uv_error=uv_error)
from pathlib import Path
Path('F:/Private/empire-at-war/Temp/TartanImport/' + name + 'GeometryVerification.json').write_text(json.dumps(report, indent=2))
print(report)
objects = set(source.objects)
for other in list(bpy.data.scenes):
    if other != source:
        bpy.data.scenes.remove(other)
for obj in list(bpy.data.objects):
    if obj not in objects:
        bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.outliner.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
