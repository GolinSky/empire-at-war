"""Stage only audited Remake ship art, retaining lossless source texture copies."""
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK = Path('Temp/ISDIRemakeImport')
manifest = {}
for path in TASK.glob('*/ConversionReport.json'):
    report = json.loads(path.read_text())
    variant = report['variant']
    model = Path('Assets/Art/Models/EmpireShips/ISDI')/(variant+'.fbx')
    model.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(path.parent/(variant+'.fbx'), model)
    textures = Path('Assets/Art/Textures/Models/EmpireShips/ISDI')
    textures.mkdir(parents=True, exist_ok=True)
    for source_texture in (path.parent/'Textures').glob('*.png'):
        shutil.copyfile(source_texture, textures/source_texture.name)
    for material in report['materials']:
        for key in ('base', 'normal'):
            stem = material[key]
            if stem is None:
                continue
            destination = textures/(stem+'.png')
            shutil.copyfile(path.parent/'Textures'/(stem+'.png'), destination)
            if key == 'base' and ('Additive' in material['shader'] or 'Shield' in material['shader']) and not stem.endswith('NONE'):
                image = Image.open(destination).convert('RGBA')
                red, green, blue, _ = image.split()
                image.putalpha(ImageChops.lighter(ImageChops.lighter(red,green),blue))
                image.save(textures/(stem+'_Additive.png'))
            if key == 'base' and 'Colorize' in material['shader']:
                shutil.copyfile(path.parent/'Textures'/(stem+'_TeamMask.png'), textures/(stem+'_TeamMask.png'))
    report['model_path'] = model.as_posix()
    report['folder'] = 'ISDI'
    manifest[variant] = report
Image.new('L', (32,32),255).save(textures/'ISDI_TeamStripes_TeamMask.png')
(TASK/'ArtManifest.json').write_text(json.dumps(manifest, indent=2))
print('Staged', len(manifest), 'Remake FBXs')
