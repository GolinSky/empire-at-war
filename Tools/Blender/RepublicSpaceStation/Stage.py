"""Stage the five validated Republic FBXs and source-identical texture pixels."""
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/RepublicStationImport'
reports = []
for level in range(1, 6):
    name = f'RepublicSpaceStationLevel{level}'
    log = (TASK / (name + '.log')).read_text(encoding='utf-8-sig')
    report = json.loads(log.split('CONVERSION_JSON ', 1)[1])[0]
    assert report['name'] == name and report['level'] == level
    assert max(report['bone_error'], report['geometry_error']) < .002 and report['uv_error'] < .00001
    folder = ROOT / 'Assets/Art/Models/SpaceStations/RepublicSpaceStation' / f'Level{level}'
    folder.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(TASK / f'Output/{name}.fbx', folder / (name + '.fbx'))
    reports.append(report)
    print(name, 'bones', len(report['source']['bones']), 'geometry error', report['geometry_error'])
(TASK / 'ConversionReport.json').write_text(json.dumps(reports, indent=2))
(TASK / 'AttachmentConversionReport.json').write_text('[]')
folder = ROOT / 'Assets/Art/Textures/Models/SpaceStations/RepublicSpaceStation'
folder.mkdir(parents=True, exist_ok=True)
for path in (TASK / 'Output/Textures').glob('*.png'):
    shutil.copyfile(path, folder / path.name)
source = json.loads((TASK / 'SourceAudit.json').read_text())
name = source['textures']['reb_shipyards.tga']['name']
mask = Image.open(folder / (name + '.png')).convert('RGBA').getchannel('A')
mask.save(folder / 'RepublicSpaceStation_Hull_TeamMask.png')
print('Team mask coverage:', sum(v > 0 for v in mask.tobytes()) / (mask.width * mask.height))
(ROOT / 'Assets/Art/Materials/Models/SpaceStations/RepublicSpaceStation').mkdir(parents=True, exist_ok=True)
