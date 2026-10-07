import json
from pathlib import Path
from PIL import Image, ImageChops

manifest = json.loads(Path('Temp/ISDIImport/ArtManifest.json').read_text())
for folder in {v['folder'] for v in manifest.values()}:
    stems = {m['base'] for v in manifest.values() if v['folder'] == folder for m in v['materials'] if 'Additive' in m['shader'] and m['base']}
    for stem in stems:
        path = Path('Assets/Art/Textures/Models/EmpireShips') / folder / (stem + '.png')
        image = Image.open(path).convert('RGBA')
        red, green, blue, alpha = image.split()
        image.putalpha(ImageChops.lighter(ImageChops.lighter(red, green), blue))
        image.save(path.with_name(stem + '_Additive.png'))
