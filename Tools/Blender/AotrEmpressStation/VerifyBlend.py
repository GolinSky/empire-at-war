"""Reopen every editable source blend in an isolated Blender process."""
import bpy
import json
from pathlib import Path

ROOT = Path('F:/Private/empire-at-war')
TASK = ROOT / 'Temp/AotrEmpressStationImport'
reports = json.loads((TASK / 'ConversionReport.json').read_text())
results = []
for report in reports:
    path = TASK / 'Output' / (report['name'] + '.blend')
    bpy.ops.wm.open_mainfile(filepath=str(path))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    assert len(meshes) == len(report['source']['meshes'])
    assert len(rig.data.bones) == len(report['source']['bones'])
    for obj in meshes:
        expected = report['source']['meshes'][obj['EaWName']]
        assert obj.parent_bone == expected['parent']
        assert obj.hide_render == expected['hidden']
        assert obj.data.uv_layers.active is not None
        assert len(obj.data.materials) == len(expected['materials'])
    for image in bpy.data.images:
        if image.filepath:
            assert image.packed_file is not None, (report['name'], image.name, image.filepath)
    results.append(dict(name=report['name'], meshes=len(meshes), bones=len(rig.data.bones), packedImages=len(bpy.data.images)))
(TASK / 'VerifiedEditableBlends.json').write_text(json.dumps(results, indent=2))
print('PASS',len(results),'editable source blends; packed textures, meshes, UV layers and hierarchy')
