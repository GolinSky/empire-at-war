"""Stage audited FBXs while sharing identical source texture copies."""
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK = Path('Temp/TitanImport')
ART = Path('Assets/Art')
textures = ART/'Textures/Models/EmpireShips/Titan'
for kind in ('Models', 'Textures/Models', 'Materials/Models'):
    (ART/kind/'EmpireShips/Titan').mkdir(parents=True, exist_ok=True)
(ART/'Materials/Wrecks/Titan').mkdir(parents=True, exist_ok=True)
Image.new('L', (2, 2), 255).save(textures/'Titan_TeamStripes_TeamMask.png')
audit = json.loads((TASK/'SourceAudit.json').read_text())
models, materials = {}, []
for variant, source in audit['variants'].items():
    folder = TASK/variant
    report = json.loads((folder/'ConversionReport.json').read_text())
    shutil.copy2(folder/(variant+'.fbx'), ART/'Models/EmpireShips/Titan'/(variant+'.fbx'))
    remaps = {}
    for texture in source['textures'].values():
        stem = 'Titan_' + Path(texture['source']).stem
        remaps[texture['stem']] = stem
        destination = textures/(stem+'.png')
        if not destination.exists():
            shutil.copy2(folder/'Textures'/(texture['stem']+'.png'), destination)
            shutil.copy2(folder/'Textures'/(texture['stem']+'_TeamMask.png'), textures/(stem+'_TeamMask.png'))
    for material in report['materials']:
        for key in ('base', 'normal'):
            if material[key]:
                material[key] = remaps[material[key]]
        if ('Additive' in material['shader'] or 'Shield' in material['shader']) and material['base']:
            destination = textures/(material['base']+'_Additive.png')
            if not destination.exists():
                image = Image.open(textures/(material['base']+'.png')).convert('RGBA')
                red, green, blue, _ = image.split()
                image.putalpha(ImageChops.lighter(ImageChops.lighter(red, green), blue))
                image.save(destination)
    models[variant] = report
    materials.extend(report['materials'])
(TASK/'ConversionReport.json').write_text(json.dumps(dict(models=models, materials=materials), indent=2))
print('Staged', len(models), 'verified Titan FBXs')
