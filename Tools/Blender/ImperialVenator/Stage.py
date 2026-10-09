"""Stage only verified FBXs/textures and check binary material assignments."""
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

ROOT = Path('Temp/ImperialVenatorImport')
source = json.loads((ROOT / 'SourceAudit.json').read_text())
models = Path('Assets/Art/Models/EmpireShips/ImperialVenator')
textures = Path('Assets/Art/Textures/Models/EmpireShips/ImperialVenator')
materials = Path('Assets/Art/Materials/Models/EmpireShips/ImperialVenator')
for folder in (models, textures, materials):
    folder.mkdir(parents=True, exist_ok=True)
manifest = {}
for name, audit in source['variants'].items():
    folder = ROOT / name
    report = json.loads((folder / 'ConversionReport.json').read_text())
    definitions = {m['name']: m for m in report['materials']}
    for record in audit['binary']['meshes']:
        mesh = next(m for n, m in report['before']['meshes'].items() if n.split('.')[0] == record['name'])
        assert len(mesh['materials']) == len(record['materials'])
        shadow = all(m['shader'] in ('MeshShadowVolume.fx', 'RSkinShadowVolume.fx') for m in record['materials'])
        if not shadow:
            assert mesh['triangles'] == sum(m['triangleCount'] for m in record['materials'])
        assert mesh['hidden'] == (record['hidden'] or shadow)
        for actual, expected in zip(mesh['materials'], record['materials']):
            assert definitions[actual]['shader'] == expected['shader']
            for field, prop in [('base', 'BaseTexture'), ('normal', 'NormalTexture')]:
                ref = expected['properties'].get(prop)
                if ref:
                    assert definitions[actual][field] == audit['textures'][ref]['stem']
    model = models / (name + '.fbx')
    shutil.copyfile(folder / (name + '.fbx'), model)
    for material in report['materials']:
        for field in ('base', 'normal'):
            stem = material[field]
            if stem is None:
                continue
            target = 'ImperialVenator_' + Path(audit['textures'][next(ref for ref, t in audit['textures'].items() if t['stem'] == stem)]['source']).stem
            shutil.copyfile(folder / 'Textures' / (stem + '.png'), textures / (target + '.png'))
            if field == 'base' and 'Colorize' in material['shader']:
                shutil.copyfile(folder / 'Textures' / (stem + '_TeamMask.png'), textures / (target + '_TeamMask.png'))
            if field == 'base' and ('Additive' in material['shader'] or material['shader'] == 'MeshShield.fx'):
                image = Image.open(textures / (target + '.png')).convert('RGBA')
                r, g, b, _ = image.split()
                image.putalpha(ImageChops.lighter(ImageChops.lighter(r, g), b))
                image.save(textures / (target + '_Additive.png'))
            material[field] = target
    report['model_path'], report['folder'] = model.as_posix(), 'ImperialVenator'
    manifest[name] = report
(ROOT / 'ArtManifest.json').write_text(json.dumps(manifest, indent=2))
print('Binary material/texture/visibility checks passed;', len(manifest), 'models and', len(list(textures.glob('*.png'))), 'shared texture files staged.')
