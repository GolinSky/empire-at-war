"""Stage verified Tector attachments and share identical texture pixels."""
import json
import shutil
from pathlib import Path

ROOT = Path('Temp/TectorImport')
models = Path('Assets/Art/Models/EmpireShips/Tector')
textures = Path('Assets/Art/Textures/Models/EmpireShips/Tector')
materials = Path('Assets/Art/Materials/Models/EmpireShips/Tector')
for folder in (models, textures, materials):
    folder.mkdir(parents=True, exist_ok=True)
manifest = {}
for report_path in sorted(ROOT.glob('*/ConversionReport.json')):
    report = json.loads(report_path.read_text())
    name = report['variant']
    model = models / (name + '.fbx')
    shutil.copyfile(report_path.parent / (name + '.fbx'), model)
    for material in report['materials']:
        for field in ('base', 'normal'):
            stem = material[field]
            if stem is None:
                continue
            target = stem.replace(name + '_', 'Tector_', 1)
            source = report_path.parent / 'Textures' / (stem + '.png')
            destination = textures / (target + '.png')
            if destination.exists():
                assert source.read_bytes() == destination.read_bytes(), destination
            else:
                shutil.copyfile(source, destination)
            if field == 'base' and 'Colorize' in material['shader']:
                shutil.copyfile(report_path.parent / 'Textures' / (stem + '_TeamMask.png'), textures / (target + '_TeamMask.png'))
            material[field] = target
    report['model_path'] = model.as_posix()
    report['folder'] = 'Tector'
    manifest[name] = report
assert len(manifest) == 10
(ROOT / 'ArtManifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print('Staged', len(manifest), 'models')
