"""Stage five validated original CIS FBXs and losslessly decoded referenced textures."""
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/CisStationImport'
reports = []
for level in range(1, 6):
    name = f'CisSpaceStationLevel{level}'
    log = (TASK / (name + '.log')).read_text(encoding='utf-8-sig')
    report = json.loads(log.split('CONVERSION_JSON ', 1)[1])[0]
    assert report['name'] == name and report['level'] == level
    assert max(report['bone_error'], report['geometry_error']) < .002 and report['uv_error'] < .00001
    folder = ROOT / f'Assets/Art/Models/SpaceStations/CisSpaceStation/Level{level}'
    folder.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(TASK / f'Output/{name}.fbx', folder / (name + '.fbx'))
    reports.append(report)
    print(name, 'bones', len(report['source']['bones']), 'geometry error', report['geometry_error'])
(TASK / 'ConversionReport.json').write_text(json.dumps(reports, indent=2))
folder = ROOT / 'Assets/Art/Textures/Models/SpaceStations/CisSpaceStation'
folder.mkdir(parents=True, exist_ok=True)
for path in (TASK / 'Output/Textures').glob('*.png'):
    shutil.copyfile(path, folder / path.name)
audit = json.loads((TASK / 'SourceAudit.json').read_text())
albedo = audit['textures']['seb_shipyards.dds']['name']
mask = Image.open(folder / (albedo + '.png')).convert('RGBA').getchannel('A')
mask.save(folder / 'CisSpaceStation_Hull_TeamMask.png')
print('Team mask coverage:', sum(v > 0 for v in mask.tobytes()) / (mask.width * mask.height))
(ROOT / 'Assets/Art/Materials/Models/SpaceStations/CisSpaceStation').mkdir(parents=True, exist_ok=True)
