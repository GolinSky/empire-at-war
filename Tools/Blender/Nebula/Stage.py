"""Stage Nebula art and retain the audited conversion manifest."""
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK = Path('Temp/NebulaImport')
NAME = 'Nebula'
models, materials = {}, []
for kind in ('Models', 'Textures/Models', 'Materials/Models'):
    Path(f'Assets/Art/{kind}/RebellionShips/{NAME}').mkdir(parents=True, exist_ok=True)
Path(f'Assets/Art/Materials/Wrecks/{NAME}').mkdir(parents=True, exist_ok=True)
textures = Path(f'Assets/Art/Textures/Models/RebellionShips/{NAME}')
for path in TASK.glob('*/ConversionReport.json'):
    report = json.loads(path.read_text())
    variant = report['variant']
    shutil.copy2(path.parent/(variant+'.fbx'), f'Assets/Art/Models/RebellionShips/{NAME}/{variant}.fbx')
    for texture in (path.parent/'Textures').glob('*.png'):
        shutil.copy2(texture, textures/texture.name)
    for material in report['materials']:
        if ('Additive' in material['shader'] or 'Shield' in material['shader']) and material['base']:
            image = Image.open(textures/(material['base']+'.png')).convert('RGBA')
            red, green, blue, _ = image.split()
            image.putalpha(ImageChops.lighter(ImageChops.lighter(red, green), blue))
            image.save(textures/(material['base']+'_Additive.png'))
    models[variant] = report
    materials.extend(report['materials'])
(TASK/'ConversionReport.json').write_text(json.dumps(dict(models=models, materials=materials), indent=2))
print('Staged', len(models), 'verified Nebula FBXs')
