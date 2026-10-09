"""Convert the resolved ISD II hull, structure, turrets and death clone."""
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path('Temp/ISDIIReplacement')
variants = json.loads((ROOT / 'SourceAudit.json').read_text())['variants']
exporter = Path('Tools/Blender/export_isd_i.py').read_text()
exporter = exporter.replace('scene = bpy.context.scene', "for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.armatures, bpy.data.images):\n    for item in list(collection):\n        if item.users == 0: collection.remove(item)\nscene = bpy.context.scene", 1)
repair = Path('Tools/Blender/ISDIIReplacement/RepairMaterials.py').read_text()
exporter = exporter.replace("assert len(rig.data.bones) == len(AUDIT['bones'])", "assert len(rig.data.bones) == len(AUDIT['bones'])\n" + repair)

for name, audit in variants.items():
    if sys.argv[1:] and name not in sys.argv[1:]:
        continue

    output = (ROOT / name).resolve().as_posix()
    code = f"assert bpy.types.blendermcp_server.port == 9890\nVARIANT={name!r}\nSOURCE={audit['file']!r}\nAUDIT={audit!r}\nOUTPUT={output!r}\n" + exporter
    request = ROOT / (name + '.json')
    request.write_text(json.dumps(dict(tool='execute_blender_code', arguments=dict(
        code=code, user_prompt='Use Blender for conversion, preserving source geometry, UVs, textures, hierarchy, and attachment positions.'))), encoding='utf-8')
    result = subprocess.run(['uvx', '--from', 'mcp-for-blender==2.1.3', 'python',
                             'Tools/Blender/ISDIIReplacement/Call.py', str(request)], capture_output=True, text=True)
    (ROOT / (name + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    match = re.search(r'REPORT_JSON (.+)', result.stdout)
    if result.returncode or match is None:
        raise RuntimeError(result.stdout[:3000])
    report = json.loads(match.group(1))
    (ROOT / name / 'ConversionReport.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(name, 'geometry error', report['geometry_error'], 'bone error', report['bone_error'], flush=True)
