"""Stage converted art, retaining source pixels and authored alpha team masks."""
import json
import shutil
from pathlib import Path
from PIL import Image, ImageOps

TASK = Path('Temp/AllegianceImport')
SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data/ART/TEXTURES')
reports = json.loads((TASK / 'ConversionReport.json').read_text(encoding='utf-8-sig'))
for report in reports:
    name = report['name']
    folders = {kind: Path(f'Assets/Art/{kind}/Models/EmpireShips/Allegiance')
               for kind in ('Materials', 'Textures')}
    model_folder = Path(f'Assets/Art/Models/EmpireShips/Allegiance')
    model_folder.mkdir(parents=True, exist_ok=True)
    for folder in folders.values():
        folder.mkdir(parents=True, exist_ok=True)
    shutil.copy2(TASK / f'Output/{name}.fbx', model_folder / f'{name}.fbx')
    for material in report['materials']:
        for key, token in [('base', 'Albedo'), ('normal', 'Normal')]:
            filename = material.get(key)
            if not filename or filename == 'None':
                continue
            source = next(p for p in SOURCE.rglob('*') if p.name.lower() in (filename.lower(), Path(filename).with_suffix('.dds').name.lower()))
            image = Image.open(source).convert('RGBA')
            path = folders['Textures'] / f'Allegiance_{Path(filename).stem}_{token}.png'
            image.save(path)
            assert Image.open(path).convert('RGBA').tobytes() == image.tobytes()
            material[key + 'Map'] = path.as_posix()
            if key == 'base' and 'Colorize' in material['shader']:
                mask = ImageOps.invert(image.getchannel('A'))
                path = folders['Textures'] / f'Allegiance_{Path(filename).stem}_TeamMask.png'
                mask.save(path)
                material['maskMap'] = path.as_posix()
                material['maskCoverage'] = 1 - mask.histogram()[0] / (mask.width * mask.height)
(TASK / 'ConversionReport.json').write_text(json.dumps(reports, indent=2), encoding='utf-8')
for report in reports:
    print(report['name'], [(m['name'], m.get('maskCoverage')) for m in report['materials']])
