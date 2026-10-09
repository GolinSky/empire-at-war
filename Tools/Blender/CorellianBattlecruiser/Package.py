"""Check original hashes, Unity metadata and rendered palette/crop evidence."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

NAME = 'CorellianBattlecruiser'
TASK = Path('Temp/CorellianBattlecruiserImport')
OUTPUT = Path('output/aotr-rebel-units/CorellianBattlecruiser-Converted')
audit = json.loads((TASK/'SourceAudit.json').read_text())
for path, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest()==expected, path
assets = [p for p in Path('Assets').rglob('*') if NAME in p.name and p.is_file() and p.suffix!='.meta']
for path in assets:
    assert Path(str(path)+'.meta').exists(), path
icon = Image.open(f'Assets/Art/Textures/Ui/Icons/ShipIcon/{NAME}Icon.png').convert('RGBA')
alpha = icon.getchannel('A')
assert icon.size==(512,512) and alpha.getextrema()==(0,255)
crop = alpha.getbbox()
assert crop[0]>10 and crop[1]>10 and crop[2]<502 and crop[3]<502
differences = {}
for kind in ('Team','WreckTeam'):
    images = [Image.open(TASK/'Previews'/(kind+str(i)+'.png')).convert('RGBA') for i in range(8)]
    differences[kind] = []
    for index,image in enumerate(images[1:],1):
        diff = ImageChops.difference(images[0].convert('RGB'),image.convert('RGB'))
        pixels = diff.tobytes()
        changed = sum(max(pixels[i:i+3])>2 for i in range(0,len(pixels),3))
        assert changed>100, (kind,index,changed)
        differences[kind].append(changed)
report = dict(source_hashes_verified=len(audit['source_hashes']), assets_with_metadata=len(assets), icon_alpha_bounds=crop, palette_pixel_differences=differences)
(TASK/'PackageInspection.json').write_text(json.dumps(report,indent=2))
OUTPUT.mkdir(parents=True,exist_ok=True)
for source in TASK.glob('*'):
    if source.is_file() and source.suffix in ('.fbx','.blend','.json'):
        shutil.copy2(source,OUTPUT/source.name)
for name in ('Textures','Previews'):
    shutil.copytree(TASK/name,OUTPUT/name,dirs_exist_ok=True)
print(json.dumps(report,indent=2))
