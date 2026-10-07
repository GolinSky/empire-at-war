"""Build a safe-mode MCP request without disk reads inside Blender."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/RebelStationImport'
audit = json.loads((TASK / 'SourceAudit.json').read_text())
level = int(sys.argv[1])
name = f'RebelSpaceStationLevel{level}'
audit['models'] = {name: audit['models'][name]}
code = 'AUDIT = ' + repr(audit) + '\n' + (Path(__file__).with_name('Convert.py')).read_text()
request = dict(tool='execute_blender_code', arguments=dict(code=code,
    user_prompt='Import the original Rebel space station models into the Unity project at: F:\\Private\\empire-at-war\\'))
(TASK / f'ConvertRequest{level}.json').write_text(json.dumps(request), encoding='utf-8')
