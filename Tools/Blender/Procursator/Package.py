import hashlib,json,shutil
from pathlib import Path
from PIL import Image,ImageChops
TASK=Path('Temp/ProcursatorImport'); OUTPUT=Path('output/remake-empire-units/Procursator-Converted')
audit=json.loads((TASK/'SourceAudit.json').read_text())
for path,expected in audit['source_hashes'].items():
 assert hashlib.sha256(Path(path).read_bytes()).hexdigest()==expected,path
textureCount=0
for name,variant in audit['variants'].items():
 for texture in variant['textures'].values():
  original=Image.open(texture['source']).convert('RGBA')
  for path in (TASK/name/'Textures'/(texture['stem']+'.png'),Path('Assets/Art/Textures/Models/EmpireShips/Procursator')/(texture['stem']+'.png')):
   assert Image.open(path).convert('RGBA').tobytes()==original.tobytes(),path
  textureCount+=1
icon=Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/ProcursatorIcon.png').convert('RGBA');box=icon.getchannel('A').getbbox()
assert icon.size==(512,512) and icon.getchannel('A').getextrema()==(0,255)
assert box and min(box)>0 and max(box)<512
for kind in ('Team','WreckTeam'):
 images=[Image.open(TASK/'Previews'/(kind+str(i)+'.png')).convert('RGBA') for i in range(8)]
 assert all(ImageChops.difference(images[0],other).convert('RGB').getbbox() for other in images[1:]),kind
OUTPUT.mkdir(parents=True,exist_ok=True)
for name in audit['variants']:
 shutil.copytree(TASK/name,OUTPUT/'SourceConversion'/name,dirs_exist_ok=True,ignore=shutil.ignore_patterns('*.blend1'))
shutil.copytree(TASK/'Previews',OUTPUT/'Previews',dirs_exist_ok=True)
evidence=Path('Tools/Blender/Procursator/Evidence');evidence.mkdir(exist_ok=True)
for name in ('SourceAudit.json','ConversionReport.json','ArtReport.json','HardpointMapping.json','UnityVerification.json'):
 shutil.copy2(TASK/name,evidence/name);shutil.copy2(TASK/name,OUTPUT/name)
verification=dict(sourceFiles=len(audit['source_hashes']),losslessTextureReferences=textureCount,iconAlphaBox=box,palettes=8)
(evidence/'PackageVerification.json').write_text(json.dumps(verification,indent=2));print(verification)
