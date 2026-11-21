"""Blender 3.6 (background): Repair Satellite.blend -> FBX for Unity.

Usage: blender.exe -b "<Repair Satellite.blend>" --python Convert.py -- <output dir>
Bakes CHILD_OF constraints into world transforms, tags hidden helpers with EaWHidden, exports FBX and a JSON report.
The source .blend is never saved.
"""
import json
import sys
from pathlib import Path

import bpy

output = Path(sys.argv[sys.argv.index('--') + 1])
output.mkdir(parents=True, exist_ok=True)
scene = bpy.context.scene

for obj in [o for o in bpy.data.objects if o.type in {'LIGHT', 'CAMERA'}]:
    bpy.data.objects.remove(obj, do_unlink=True)

report = {'source': scene.get('Source ALO'), 'meshes': {}}
meshes = [o for o in scene.objects if o.type == 'MESH']
world = {o.name: o.matrix_world.copy() for o in meshes}
for obj in meshes:
    hidden = obj.hide_render or obj.hide_get()
    for constraint in list(obj.constraints):
        obj.constraints.remove(constraint)
    obj.parent = None
    obj.matrix_world = world[obj.name]
    obj['EaWHidden'] = int(hidden)
    material = obj.data.materials[0]
    obj['EaWShader'] = material.shaderList.shaderList
    report['meshes'][obj.name] = {
        'hidden': hidden, 'triangles': sum(len(p.vertices) - 2 for p in obj.data.polygons),
        'material': material.name, 'uv': len(obj.data.uv_layers) > 0,
    }
    obj.hide_set(False)
    obj.hide_render = False

for obj in [o for o in scene.objects if o.type == 'ARMATURE']:
    bpy.data.objects.remove(obj, do_unlink=True)

bpy.context.view_layer.update()
fbx = output / 'RepairSatellite.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=False, object_types={'MESH'},
    add_leaf_bones=False, bake_anim=False, use_custom_props=True,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='STRIP', embed_textures=False)
(output / 'ConversionReport.json').write_text(json.dumps(report, indent=2))
print('CONVERSION_JSON', json.dumps(report))
