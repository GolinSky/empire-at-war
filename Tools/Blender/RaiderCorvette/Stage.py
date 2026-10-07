"""Stage converted art, retaining exact DDS pixels and authored alpha team masks."""
import json
import shutil
from pathlib import Path
from PIL import Image

TASK = Path('Temp/RaiderCorvetteImport')
SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/TEXTURES')
reports = json.loads((TASK / 'ConversionReport.json').read_text(encoding='utf-8-sig'))
for report in reports:
    name = report['name']
    folders = {kind: Path(f'Assets/Art/{kind}/Models/EmpireShips/{name}')
               for kind in ('Materials', 'Textures')}
    model_folder = Path(f'Assets/Art/Models/EmpireShips/{name}')
    model_folder.mkdir(parents=True, exist_ok=True)
    for folder in folders.values():
        folder.mkdir(parents=True, exist_ok=True)
    shutil.copy2(TASK / f'Output/{name}.fbx', model_folder / f'{name}.fbx')
    for material in report['materials']:
        for key, token in [('base', 'Albedo'), ('normal', 'Normal')]:
            filename = material.get(key)
            if not filename or filename == 'None':
                continue
            image = Image.open(SOURCE / filename).convert('RGBA')
            path = folders['Textures'] / f'{name}_{Path(filename).stem}_{token}.png'
            image.save(path)
            assert Image.open(path).convert('RGBA').tobytes() == image.tobytes()
            material[key + 'Map'] = path.as_posix()
            if key == 'base' and 'Colorize' in material['shader']:
                mask = image.getchannel('A')
                path = folders['Textures'] / f'{name}_{Path(filename).stem}_TeamMask.png'
                mask.save(path)
                material['maskMap'] = path.as_posix()
                material['maskCoverage'] = 1 - mask.histogram()[0] / (mask.width * mask.height)
(TASK / 'ConversionReport.json').write_text(json.dumps(reports, indent=2), encoding='utf-8')
for report in reports:
    print(report['name'], [(m['name'], m.get('maskCoverage')) for m in report['materials']])
