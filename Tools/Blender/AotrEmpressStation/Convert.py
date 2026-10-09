"""Run in an isolated Blender 3.6 process; adapt the audited station converter."""
import bpy
import json
from pathlib import Path

ROOT = Path('F:/Private/empire-at-war')
TASK = ROOT / 'Temp/AotrEmpressStationImport'
AUDIT = json.loads((TASK / 'SourceAudit.json').read_text())
assert bpy.app.version[:2] == (3, 6)
print('BLENDER_VERSION', bpy.app.version_string, flush=True)
converter = (ROOT / 'Tools/Blender/RebelSpaceStation/Convert.py').read_text()
converter = converter.replace("assert bpy.types.blendermcp_server.port == 9885", '')
converter = converter.replace('RebelStationImport', 'AotrEmpressStationImport').replace('RebelStationSlot', 'AotrEmpressStationSlot')
converter = converter.replace("    for row in audit['meshes']:\n        obj = scene.objects[row['name']]", "    imported_meshes = [o for o in scene.objects if o.type == 'MESH']\n    assert len(imported_meshes) == len(audit['meshes'])\n    for row, obj in zip(audit['meshes'], imported_meshes):\n        assert obj.name == row['key'], (obj.name, row['key'])")
converter = converter.replace("obj['EaWName'] = row['name']", "obj['EaWName'] = row['key']")
converter = converter.replace("        constraint = obj.constraints[0]\n        assert constraint.type == 'CHILD_OF' and len(obj.constraints) == 1", "        for constraint in obj.constraints:\n            assert constraint.type == 'CHILD_OF'\n        obj.constraints.clear()")
converter = converter.replace('        obj.constraints.remove(constraint)\n', '')
converter = converter.replace('    bpy.ops.wm.save_as_mainfile', "    for material in list(bpy.data.materials):\n        if material.users == 0:\n            bpy.data.materials.remove(material)\n    for image in list(bpy.data.images):\n        if image.users == 0:\n            bpy.data.images.remove(image)\n        elif image.filepath and image.packed_file is None:\n            image.pack()\n    bpy.ops.wm.save_as_mainfile")
converter = converter.replace("'Shadow' in shader or row['name'].endswith('_Blast')", "'Shadow' in shader or 'Collision' in shader or row['name'].endswith('_Blast')")
converter = converter.replace("if 'Shadow' in shader:", "if 'Shadow' in shader or 'Collision' in shader:")
converter = converter.replace('import json\n', 'import json\nimport itertools\nfrom pathlib import Path\n')
converter = converter.replace("corners = {o['EaWName']: Corners(o) for o in scene.objects if o.type == 'MESH'}", "corners = {o['EaWName']: Corners(o) for o in scene.objects if o.type == 'MESH' and not any(t in o['EaWShader'] for t in ('Shadow','Collision'))}")
converter = converter.replace("        actual = Corners(obj)", "        if obj['EaWName'] not in corners:\n            continue\n        actual = Corners(obj)")
converter = converter.replace('candidates = buckets.get(key, expected)', 'candidates = [p for delta in itertools.product((-1,0,1),repeat=3) for p in buckets.get(tuple(k+d*.001 for k,d in zip(key,delta)), ())]\n            if not candidates:\n                candidates = buckets.get(key, expected)')
converter = converter.replace('tuple(k+d*.001 for k,d in zip(key,delta))', 'tuple(round(k+d*.001,3) for k,d in zip(key,delta))')
converter = converter.replace('    before = Snapshot(scene)', "    for row, obj in zip(audit['meshes'], imported_meshes):\n        if any(token in row['materials'][0]['shader'] for token in ('Shadow','Collision')):\n            continue\n        normals = audit['normals'][row['key']]\n        assert len(obj.data.vertices) == len(normals), row['key']\n        obj.data.use_auto_smooth = True\n        obj.data.normals_split_custom_set_from_vertices(normals)\n    before = Snapshot(scene)")
converter = converter.replace("reports = [Convert(name, model) for name, model in AUDIT['models'].items()]\nprint('CONVERSION_JSON ' + json.dumps(reports))", "reports = []\nfor name, model in AUDIT['models'].items():\n    print('CONVERTING',name,flush=True)\n    reports.append(Convert(name, model))\n    Path(OUTPUT + '/../ConversionReport.json').write_text(json.dumps(reports,indent=2))\n    print('CONVERTED',name,flush=True)")
exec(compile(converter, str(TASK / 'StationConverter.py'), 'exec'))
