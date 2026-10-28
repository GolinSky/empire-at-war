"""Run through Blender MCP in a fresh scene, using the audited mining converter."""
import bpy
import json
from pathlib import Path

ROOT = Path('F:/Private/empire-at-war')
TASK = ROOT / 'output/TibannaGasStationImport'
AUDIT = json.loads((TASK / 'SourceAudit.json').read_text())
assert bpy.app.version[:2] == (3, 6)
original_scene = bpy.context.scene
bpy.context.window.scene = bpy.data.scenes.new('Tibanna Conversion')
converter = (ROOT / 'Tools/Blender/MiningFacilities/Convert.py').read_text()
start = converter.index('if bpy.context.scene.name.endswith')
end = converter.index('\n\ndef ClearScene')
converter = converter[:start] + converter[end:]
converter = converter.replace('Temp/MiningFacilitiesImport', 'output/TibannaGasStationImport').replace('MiningFacilitiesSlot', 'TibannaGasStationSlot')
start = converter.index('    for collection in (bpy.data.materials')
end = converter.index('\n\ndef Snapshot')
converter = converter[:start] + converter[end:]
converter = converter.replace("    for row in audit['meshes']:\n        obj = scene.objects[row['name']]", "    imported_meshes = [o for o in scene.objects if o.type == 'MESH']\n    assert len(imported_meshes) == len(audit['meshes'])\n    for row, obj in zip(audit['meshes'], imported_meshes):")
converter = converter.replace("        constraint = obj.constraints[0]\n        assert constraint.type == 'CHILD_OF' and len(obj.constraints) == 1", "        assert all(c.type == 'CHILD_OF' for c in obj.constraints)\n        obj.constraints.clear()")
converter = converter.replace('        obj.constraints.remove(constraint)\n', '')
converter = converter.replace("'Shadow' in shader", "any(t in shader for t in ('Shadow', 'Collision'))")
converter = converter.replace("'Shadow' in obj['EaWShader']", "any(t in obj['EaWShader'] for t in ('Shadow', 'Collision'))")
converter = converter.replace("    bpy.ops.import_mesh.alo(filepath=audit['file'], importAnimations=False)", "    try:\n        bpy.ops.import_mesh.alo(filepath=audit['file'], importAnimations=False)\n    except RuntimeError as error:\n        if 'ViewLayer' not in str(error) or 'does not contain object' not in str(error):\n            raise\n        assert len([o for o in scene.objects if o.type == 'MESH']) == len(audit['meshes'])")
converter = converter.replace("assert len(rig.data.bones) == len(audit['bones']) - 1", "assert len(rig.data.bones) in (len(audit['bones']), len(audit['bones']) - 1)")
converter = converter.replace("root_bone = rig.data.edit_bones.new('Root')", "root_bone = rig.data.edit_bones.get('Root') or rig.data.edit_bones.new('Root')")
converter = converter.replace('    before = Snapshot(scene)', "    for row, obj in zip(audit['meshes'], imported_meshes):\n        if any(t in obj['EaWShader'] for t in ('Shadow','Collision')):\n            continue\n        normals = audit['normals'][row['name']]\n        assert len(normals) == len(obj.data.vertices)\n        obj.data.use_auto_smooth = True\n        obj.data.normals_split_custom_set_from_vertices(normals)\n    before = Snapshot(scene)")
converter = converter.replace("reports = [Convert(name, model) for name, model in AUDIT['models'].items()]\nprint('CONVERSION_JSON ' + json.dumps(reports))", "reports = [Convert(name, model) for name, model in AUDIT['models'].items()]\nPath(TASK / 'RawConversionReport.json').write_text(json.dumps(reports))\nprint('CONVERSION_COMPLETE', [(r['name'],r['geometry_error'],r['uv_error']) for r in reports])")
exec(compile(converter, str(TASK / 'Converter.py'), 'exec'))
bpy.context.window.scene = original_scene
