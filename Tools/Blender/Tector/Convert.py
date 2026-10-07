"""Convert Tector-only attachments through the configured Blender MCP session."""
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path('Temp/TectorImport')
variants = json.loads((ROOT / 'SourceAudit.json').read_text())['variants']
exporter = Path('Tools/Blender/export_isd_i.py').read_text()
repair = Path('Tools/Blender/Tector/RepairSurfaces.py').read_text()
exporter = exporter.replace("assert len(rig.data.bones) == len(AUDIT['bones'])", "assert len(rig.data.bones) == len(AUDIT['bones'])\n" + repair)
binary = json.loads((ROOT / 'BinaryMaterialAudit.json').read_text())
for name, audit in variants.items():
    if sys.argv[1:] and name not in sys.argv[1:]:
        continue
    audit['binary'] = binary[name]
    output = (ROOT / name).resolve().as_posix()
    code = f"VARIANT={name!r}\nSOURCE={audit['file']!r}\nAUDIT={audit!r}\nOUTPUT={output!r}\n" + exporter
    request = ROOT / (name + '.json')
    request.write_text(json.dumps(dict(tool='execute_blender_code', arguments=dict(
        code=code, user_prompt='Add this unit to empire'))), encoding='utf-8')
    result = subprocess.run(['uvx', '--from', 'mcp-for-blender==2.1.3', 'python',
                             'Tools/Blender/mcp_call.py', str(request)], capture_output=True, text=True)
    (ROOT / (name + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    match = re.search(r'REPORT_JSON (.+)', result.stdout)
    if result.returncode or match is None:
        raise RuntimeError(result.stdout + result.stderr)
    report = json.loads(match.group(1))
    (ROOT / name / 'ConversionReport.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(name, 'geometry error', report['geometry_error'], 'bone error', report['bone_error'], flush=True)
