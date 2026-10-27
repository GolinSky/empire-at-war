"""Resolve audited data into the safe-mode Blender payload and collect its report."""
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/BulwarkImport'
audit = json.loads((TASK / 'SourceAudit.json').read_text())
template = (ROOT / 'Tools/Blender/Bulwark/Convert.py').read_text()
payload = TASK / 'ConvertResolved.py'
payload.write_text(template.replace('__SOURCE_AUDIT__', repr(audit)), encoding='utf-8')
request = TASK / 'ConvertRequest.json'
request.write_text(json.dumps(dict(tool='execute_blender_code', code_file=str(payload))), encoding='utf-8')
result = subprocess.run(['uvx', '--from', 'mcp-for-blender==2.1.3', 'python', str(ROOT / 'Tools/Blender/Bulwark/Call.py'), str(request)],
                        capture_output=True, text=True, encoding='utf-8')
(TASK / 'ConversionLog.txt').write_text(result.stdout + result.stderr, encoding='utf-8')
if result.returncode:
    raise RuntimeError('Blender conversion failed; inspect Temp/BulwarkImport/ConversionLog.txt')
line = next(line for line in result.stdout.splitlines() if line.startswith('BULWARK_REPORT '))
report = json.loads(line[len('BULWARK_REPORT '):])
(TASK / 'ConversionReport.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print({name: dict(meshes=len(row['before']['meshes']), bones=len(row['before']['bones'])) for name, row in report['models'].items()})
