"""Share textures among the eleven ISD II attachments; retain conversion evidence."""
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

ROOT = Path('Temp/ISDIIImport')
models = Path('Assets/Art/Models/EmpireShips/ISDII')
textures = Path('Assets/Art/Textures/Models/EmpireShips/ISDII')
materials = Path('Assets/Art/Materials/Models/EmpireShips/ISDII')
for folder in (models, textures, materials):
    folder.mkdir(parents=True, exist_ok=True)
manifest = {}
for report_path in sorted(ROOT.glob('*/ConversionReport.json')):
    report = json.loads(report_path.read_text())
    name = report['variant']
    model = models/(name+'.fbx')
    shutil.copyfile(report_path.parent/(name+'.fbx'), model)
    for material in report['materials']:
        for field in ('base', 'normal'):
            stem = material[field]
            if stem is None:
                continue
            target = stem.replace(name+'_', 'ISDII_', 1)
            shutil.copyfile(report_path.parent/'Textures'/(stem+'.png'), textures/(target+'.png'))
            if field == 'base' and 'Colorize' in material['shader']:
                shutil.copyfile(report_path.parent/'Textures'/(stem+'_TeamMask.png'), textures/(target+'_TeamMask.png'))
            if field == 'base' and 'Additive' in material['shader']:
                image = Image.open(textures/(target+'.png')).convert('RGBA')
                r, g, b, _ = image.split()
                image.putalpha(ImageChops.lighter(ImageChops.lighter(r, g), b))
                image.save(textures/(target+'_Additive.png'))
            material[field] = target
    report['model_path'] = model.as_posix()
    report['folder'] = 'ISDII'
    manifest[name] = report
assert len(manifest) == 11
(ROOT/'ArtManifest.json').write_text(json.dumps(manifest, indent=2))
print('Staged', len(manifest), 'models and', len(list(textures.glob('*.png'))), 'shared textures')
