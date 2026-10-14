"""Build the Blender MCP conversion request from the audited source manifest."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUTPUT = ROOT / 'Temp/MiningFacilitiesImport'
audit = json.loads((OUTPUT / 'SourceAudit.json').read_text())
code = 'AUDIT = ' + repr(audit) + '\n' + Path(__file__).with_name('Convert.py').read_text()
request = dict(tool='execute_blender_code', arguments=dict(code=code,
    user_prompt='replacing existing mining and battle mining facilities original vanilla ALO models from empire at war game on my pc'))
(OUTPUT / 'ConvertRequest.json').write_text(json.dumps(request))
