"""Stage verified original hardpoint FBXs; base models and textures stay unchanged."""
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/RebelStationImport'
audit = json.loads((TASK / 'AttachmentAudit.json').read_text())
destination = ROOT / 'Assets/Art/Models/SpaceStations/RebelSpaceStation/Attachments'
destination.mkdir(parents=True, exist_ok=True)
reports = []
for name in audit['models']:
    log = (TASK / 'Attachments' / (name + '.log')).read_text(encoding='utf-8-sig')
    report = json.loads(log.split('CONVERSION_JSON ', 1)[1])[0]
    assert report['name'] == name
    reports.append(report)
    shutil.copyfile(TASK / 'Output' / (name + '.fbx'), destination / (name + '.fbx'))
(TASK / 'AttachmentConversionReport.json').write_text(json.dumps(reports, indent=2))
print('Staged', len(reports), 'original hardpoint models.')
