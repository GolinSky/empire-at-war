"""Verify unchanged sources, asset metadata, icon crop and all team palettes."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK=Path('Temp/CD273Import')
OUTPUT=Path('output/remake-rebel-units/CD273-Converted')
audit=json.loads((TASK/'SourceAudit.json').read_text())
for source,expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(source).read_bytes()).hexdigest()==expected,source
assets=[p for p in Path('Assets').rglob('*') if 'CD273' in p.name and p.is_file() and p.suffix!='.meta']
for path in assets:
    assert Path(str(path)+'.meta').exists(),path
icon=Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/CD273Icon.png').convert('RGBA')
assert icon.size==(512,512) and icon.getchannel('A').getextrema()==(0,255)
crop=icon.getchannel('A').getbbox()
assert crop[0]>10 and crop[1]>10 and crop[2]<502 and crop[3]<502
differences={}
for kind in ('Team','WreckTeam'):
    images=[Image.open(TASK/'Previews'/(kind+str(i)+'.png')).convert('RGB') for i in range(8)]
    differences[kind]=[]
    for index,image in enumerate(images[1:],1):
        pixels=ImageChops.difference(images[0],image).tobytes()
        changed=sum(max(pixels[i:i+3])>2 for i in range(0,len(pixels),3))
        assert changed>100,(kind,index,changed)
        differences[kind].append(changed)
report=dict(source_hashes_verified=len(audit['source_hashes']),assets_with_metadata=len(assets),icon_alpha_bounds=crop,palette_pixel_differences=differences)
(TASK/'PackageInspection.json').write_text(json.dumps(report,indent=2))
OUTPUT.mkdir(parents=True,exist_ok=True)
for filename in ('SourceAudit.json','ConversionReport.json','HardpointMapping.json','ArtReport.json','UnityInspection.json','PackageInspection.json'):
    shutil.copy2(TASK/filename,OUTPUT/filename)
for filename in ('CD273.fbx','CD273.blend'):
    shutil.copy2(TASK/'CD273'/filename,OUTPUT/filename)
for source,name in ((TASK/'CD273/Textures','Textures'),(TASK/'Previews','Previews')):
    shutil.copytree(source,OUTPUT/name,dirs_exist_ok=True)
shutil.copy2('Tools/Blender/CD273/README.md',OUTPUT/'README.md')
print(json.dumps(report,indent=2))
