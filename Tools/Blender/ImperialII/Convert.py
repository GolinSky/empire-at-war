"""Host-side MCP driver; keeps Blender safe mode enabled on isolated port 9884."""
import json
import re
import subprocess
from pathlib import Path

ROOT = Path('Temp/ImperialIIImport')
audits = json.loads((ROOT/'BinaryAudit.json').read_text())['variants']
exporter = Path('Tools/Blender/export_imperial_i_advanced.py').read_text()
for name, audit in audits.items():
    output = str((ROOT/name).resolve()).replace('\\', '/')
    code = 'import bpy\nassert bpy.types.blendermcp_server.port == 9884\n'
    code += f'VARIANT={name!r}\nSOURCE={audit["file"]!r}\nAUDIT={audit!r}\nOUTPUT={output!r}\n'
    code += exporter
    request = ROOT/(name+'.json')
    request.write_text(json.dumps(dict(tool='execute_blender_code', arguments=dict(
        code=code, user_prompt='Add Imperial II Star Destroyer to empire'))))
    result = subprocess.run(['uvx', '--from', 'mcp-for-blender==2.1.3', 'python',
                             str(ROOT/'mcp_call.py'), str(request)], capture_output=True, text=True)
    (ROOT/(name+'.log')).write_text(result.stdout+'\n'+result.stderr, encoding='utf-8')
    if result.returncode:
        raise RuntimeError(result.stdout)
    match = re.search(r'REPORT_JSON (.+)', result.stdout)
    if match is None:
        raise RuntimeError(result.stdout)
    report = json.loads(match.group(1))
    (ROOT/name/'ConversionReport.json').write_text(json.dumps(report, indent=2))
    print(name, 'geometry error', report['geometry_error'], 'bone error', report['bone_error'], flush=True)
