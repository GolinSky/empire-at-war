"""Collect conversion reports and stage type-first Unity source assets."""
import json
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/RebelStationImport'
reports = []
for level in range(1, 6):
    log = (TASK / f'Conversion{level}.log').read_text(encoding='utf-8-sig')
    reports.extend(json.loads(log.split('CONVERSION_JSON ', 1)[1]))
    name = f'RebelSpaceStationLevel{level}'
    assert reports[-1]['name'] == name and reports[-1]['level'] == level
    folder = ROOT / f'Assets/Art/Models/SpaceStations/RebelSpaceStation/Level{level}'
    folder.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(TASK / f'Output/{name}.fbx', folder / f'{name}.fbx')
    print(name, 'meshes', len(reports[-1]['source']['meshes']),
          'triangles', sum(m['triangles'] for m in reports[-1]['source']['meshes'].values()),
          'bones', len(reports[-1]['source']['bones']),
          'geometry error', reports[-1]['geometry_error'], 'UV error', reports[-1]['uv_error'])
(TASK / 'ConversionReport.json').write_text(json.dumps(reports, indent=2))
texture_folder = ROOT / 'Assets/Art/Textures/Models/SpaceStations/RebelSpaceStation'
texture_folder.mkdir(parents=True, exist_ok=True)
for path in (TASK / 'Output/Textures').glob('*.png'):
    shutil.copyfile(path, texture_folder / path.name)
albedo = Image.open(texture_folder / 'RebelSpaceStation_RB_Station.png').convert('RGBA')
mask = albedo.getchannel('A')
mask.save(texture_folder / 'RebelSpaceStation_Hull_TeamMask.png')
print('Team mask coverage', sum(v > 0 for v in mask.tobytes()) / (mask.width * mask.height))
(ROOT / 'Assets/Art/Materials/Models/SpaceStations/RebelSpaceStation').mkdir(parents=True, exist_ok=True)
