"""Stage lossless CD-273 art and record original material parameters."""
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK = Path('Temp/CD273Import')
report = json.loads((TASK/'CD273/ConversionReport.json').read_text())
audit = json.loads((TASK/'SourceAudit.json').read_text())['variants']['CD273']['material_audit']
materials = {name: definition for mesh in audit['meshes'] for name, definition in zip(report['before']['meshes'][mesh['name']]['materials'], mesh['materials'])}
for row in report['materials']:
    row['source_properties'] = materials[row['name']]['properties']
combined = dict(models={'CD273':report}, materials=report['materials'], mesh_materials={'CD273':audit})
(TASK/'ConversionReport.json').write_text(json.dumps(combined, indent=2))
for kind in ('Models','Textures','Materials'):
    folder = Path('Assets/Art')/kind
    if kind != 'Models': folder /= 'Models'
    (folder/'RebellionShips/CD273').mkdir(parents=True, exist_ok=True)
shutil.copy2(TASK/'CD273/CD273.fbx', 'Assets/Art/Models/RebellionShips/CD273/CD273.fbx')
destination = Path('Assets/Art/Textures/Models/RebellionShips/CD273')
for source in (TASK/'CD273/Textures').glob('*.png'):
    shutil.copy2(source, destination/source.name)
for row in report['materials']:
    stem = row['base']
    if stem and ('Additive' in row['shader'] or 'Shield' in row['shader']):
        image = Image.open(destination/(stem+'.png')).convert('RGBA')
        r,g,b,_ = image.split()
        image.putalpha(ImageChops.lighter(ImageChops.lighter(r,g),b))
        image.save(destination/(stem+'_Additive.png'))
