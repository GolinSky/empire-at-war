"""Convert in an isolated compatible Blender process; preserve the live MCP session."""
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/RecalcitrantImport'
audit = json.loads((TASK / 'SourceAudit.json').read_text())
code = (ROOT / 'Tools/Blender/Recalcitrant/Convert.py').read_text().replace('__SOURCE_AUDIT__', repr(audit))
payload = TASK / 'ConvertResolved.py'
payload.write_text("import bpy\nimport addon_utils\naddon_utils.enable('io_alamo_tools', default_set=False)\n" + code)
blender = Path(os.environ['LOCALAPPDATA']) / 'AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe'
with (TASK / 'ConversionLog.txt').open('w', encoding='utf-8') as log:
    subprocess.run([str(blender), '--background', '--python-exit-code', '1', '--factory-startup', '--python', str(payload)], stdout=log, stderr=subprocess.STDOUT, check=True)
report = json.loads((TASK / 'ConversionReport.json').read_text())
assert set(report['models']) == set(audit['models'])
print('Converted Recalcitrant hull, eight attached turrets with source/FBX geometry checks.')
