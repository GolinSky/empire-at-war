"""Verify immutable source inputs and package editable conversion evidence."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

task=Path('Temp/DauntlessImport')
audit=json.loads((task/'SourceAudit.json').read_text())
for name, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(name).read_bytes()).hexdigest()==expected, name
for texture in audit['variants']['Dauntless']['textures'].values():
    original=Image.open(texture['source']).convert('RGBA')
    copied=Image.open(task/'Dauntless/Textures'/(texture['stem']+'.png')).convert('RGBA')
    assert original.tobytes()==copied.tobytes(),texture['source']
icon=Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/DauntlessIcon.png').convert('RGBA')
assert icon.size==(512,512)
box=icon.getchannel('A').getbbox()
assert box and box[0]>0 and box[1]>0 and box[2]<512 and box[3]<512,box
for prefix in ('Team','WreckTeam'):
    images=[Image.open(task/'Previews'/(prefix+str(i)+'.png')).convert('RGBA') for i in range(8)]
    assert all(ImageChops.difference(images[0],im).convert('RGB').getbbox() for im in images[1:]), prefix
output=Path('output/remake-rebel-units/Dauntless-Converted')
output.mkdir(parents=True,exist_ok=True)
for name in ('Dauntless.blend','Dauntless.fbx','ConversionReport.json'):
    shutil.copy2(task/'Dauntless'/name,output/name)
for directory in ('Textures','Previews'):
    shutil.copytree(task/('Dauntless/'+directory if directory=='Textures' else directory),output/directory,dirs_exist_ok=True)
evidence=Path('Tools/Blender/Dauntless/Evidence')
evidence.mkdir(parents=True,exist_ok=True)
for name in ('SourceAudit.json','ConversionReport.json','ArtReport.json','HardpointMapping.json','UnityInspection.json'):
    shutil.copy2(task/name,output/name)
    shutil.copy2(task/name,evidence/name)
report=dict(sourceHashesVerified=len(audit['source_hashes']),texturesVerified=len(audit['variants']['Dauntless']['textures']),iconAlphaBounds=box,teamPalettesVerified=8,wreckPalettesVerified=8)
(output/'PackageVerification.json').write_text(json.dumps(report,indent=2))
shutil.copy2(output/'PackageVerification.json',evidence/'PackageVerification.json')
print(json.dumps(report))
