"""Stage validated Imperial FBXs and lossless textures in type-first folders."""
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/EmpireStationImport'
audit = json.loads((TASK / 'AttachmentAudit.json').read_text())
reports = []
attachments = []
for name in [f'EmpireSpaceStationLevel{level}' for level in range(1, 6)] + list(audit['models']):
    log = (TASK / (name + '.log')).read_text(encoding='utf-8-sig')
    report = json.loads(log.split('CONVERSION_JSON ', 1)[1])[0]
    assert report['name'] == name
    assert max(report['bone_error'], report['geometry_error']) < .002 and report['uv_error'] < .00001
    base = name.startswith('EmpireSpaceStationLevel')
    folder = ROOT / 'Assets/Art/Models/SpaceStations/EmpireSpaceStation' / (f"Level{report['level']}" if base else 'Attachments')
    folder.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(TASK / f'Output/{name}.fbx', folder / (name + '.fbx'))
    (reports if base else attachments).append(report)
    print(name, 'bones', len(report['source']['bones']), 'geometry error', report['geometry_error'])
(TASK / 'ConversionReport.json').write_text(json.dumps(reports, indent=2))
(TASK / 'AttachmentConversionReport.json').write_text(json.dumps(attachments, indent=2))
folder = ROOT / 'Assets/Art/Textures/Models/SpaceStations/EmpireSpaceStation'
folder.mkdir(parents=True, exist_ok=True)
for path in (TASK / 'Output/Textures').glob('*.png'):
    shutil.copyfile(path, folder / path.name)
source = json.loads((TASK / 'SourceAudit.json').read_text())
name = source['textures']['eb_station.tga']['name']
mask = Image.open(folder / (name + '.png')).convert('RGBA').getchannel('A')
mask.save(folder / 'EmpireSpaceStation_Hull_TeamMask.png')
print('Team mask coverage:', sum(v > 0 for v in mask.tobytes()) / (mask.width * mask.height))
(ROOT / 'Assets/Art/Materials/Models/SpaceStations/EmpireSpaceStation').mkdir(parents=True, exist_ok=True)
