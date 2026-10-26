"""Verify original source hashes, metadata, icon crop and all eight team palettes."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

NAME = 'BothanAssaultCruiser'
TASK = Path('Temp/BothanAssaultCruiserImport')
OUTPUT = Path('output/remake-rebel-units/BothanAssaultCruiser-Converted')
audit = json.loads((TASK/'SourceAudit.json').read_text())
for path, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == expected, path
assets = [p for p in Path('Assets').rglob('*') if NAME in p.name and p.is_file() and p.suffix != '.meta']
for path in assets:
    assert Path(str(path)+'.meta').exists(), path
icon = Image.open(f'Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/{NAME}Icon.png').convert('RGBA')
alpha = icon.getchannel('A')
assert icon.size == (512,512) and alpha.getextrema() == (0,255)
crop = alpha.getbbox()
assert crop[0] > 10 and crop[1] > 10 and crop[2] < 502 and crop[3] < 502
differences = {}
for kind in ('Team','WreckTeam'):
    images = [Image.open(TASK/'Previews'/(kind+str(i)+'.png')).convert('RGB') for i in range(8)]
    differences[kind] = []
    for index, image in enumerate(images[1:],1):
        pixels = ImageChops.difference(images[0],image).tobytes()
        changed = sum(max(pixels[i:i+3]) > 2 for i in range(0,len(pixels),3))
        assert changed > 100, (kind,index,changed)
        differences[kind].append(changed)
report = dict(source_hashes_verified=len(audit['source_hashes']),assets_with_metadata=len(assets),icon_alpha_bounds=crop,palette_pixel_differences=differences)
(TASK/'PackageInspection.json').write_text(json.dumps(report,indent=2))
OUTPUT.mkdir(parents=True,exist_ok=True)
for source in (TASK/NAME).glob('*'):
    if source.is_file() and source.suffix in ('.fbx','.blend','.json'):
        shutil.copy2(source,OUTPUT/source.name)
shutil.copytree(TASK/NAME/'Textures',OUTPUT/'Textures',dirs_exist_ok=True)
shutil.copytree(TASK/'Previews',OUTPUT/'Previews',dirs_exist_ok=True)
for source in TASK.glob('*.json'):
    shutil.copy2(source,OUTPUT/source.name)
print(json.dumps(report,indent=2))
