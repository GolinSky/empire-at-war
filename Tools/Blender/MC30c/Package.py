"""Verify original inputs, lossless textures and rendered output; archive editable conversion."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK = Path('Temp/MC30cImport')
OUTPUT = Path('output/remake-rebel-units/MC30c-Converted')
audit = json.loads((TASK/'SourceAudit.json').read_text())
for path, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == expected, path
for texture in audit['variants']['MC30c']['textures'].values():
    original = Image.open(texture['source']).convert('RGBA')
    for path in (TASK/'MC30c/Textures'/(texture['stem']+'.png'), Path('Assets/Art/Textures/Models/RebellionShips/MC30c')/(texture['stem']+'.png')):
        assert Image.open(path).convert('RGBA').tobytes() == original.tobytes(), path
icon = Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/MC30cIcon.png').convert('RGBA')
assert icon.size == (512,512)
box = icon.getchannel('A').getbbox()
assert box and box[0]>0 and box[1]>0 and box[2]<512 and box[3]<512, box
assert icon.getchannel('A').getextrema() == (0,255)
palettes = []
for kind in ('Team', 'WreckTeam'):
    images = [Image.open(TASK/'Previews'/(kind+str(i)+'.png')).convert('RGBA') for i in range(8)]
    assert all(ImageChops.difference(images[0], other).convert('RGB').getbbox() for other in images[1:]), kind
    palettes.append(kind)
for root in ('Assets/Art/Models/RebellionShips/MC30c', 'Assets/Art/Materials/Models/RebellionShips/MC30c', 'Assets/Art/Textures/Models/RebellionShips/MC30c', 'Assets/Art/Materials/Wrecks/Rebellion/MC30c'):
    for path in Path(root).rglob('*'):
        if path.is_file() and path.suffix != '.meta':
            assert Path(str(path)+'.meta').exists(), path
OUTPUT.mkdir(parents=True, exist_ok=True)
shutil.copytree(TASK/'MC30c', OUTPUT/'SourceConversion', dirs_exist_ok=True, ignore=shutil.ignore_patterns('*.blend1'))
shutil.copytree(TASK/'Previews', OUTPUT/'Previews', dirs_exist_ok=True)
for path in TASK.glob('*.json'):
    if '.request.' not in path.name:
        shutil.copy2(path, OUTPUT/path.name)
verification = dict(sourceFiles=len(audit['source_hashes']),losslessTextures=len(audit['variants']['MC30c']['textures']),iconAlphaBox=box,palettes=palettes)
(OUTPUT/'PackageVerification.json').write_text(json.dumps(verification, indent=2))
evidence = Path('Tools/Blender/MC30c/Evidence')
evidence.mkdir(exist_ok=True)
for name in ('SourceAudit.json', 'ConversionReport.json', 'ArtReport.json', 'HardpointMapping.json', 'UnityVerification.json'):
    shutil.copy2(TASK/name, evidence/name)
shutil.copy2(OUTPUT/'PackageVerification.json', evidence/'PackageVerification.json')
print(json.dumps(verification))
