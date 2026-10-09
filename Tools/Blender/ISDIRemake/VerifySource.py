"""Verify source files, lossless textures, archived GUIDs and visible triangle fidelity."""
import hashlib,json,re,subprocess
from pathlib import Path
from PIL import Image,ImageChops

TASK=Path('Temp/ISDIRemakeImport')
audit=json.loads((TASK/'SourceAudit.json').read_text())
for path,digest in audit['source_hashes'].items():
 assert hashlib.sha256(Path(path).read_bytes()).hexdigest()==digest,path
textures=0
for name,variant in audit['variants'].items():
 for definition in variant['textures'].values():
  source=Image.open(definition['source']).convert('RGBA')
  target=Image.open(Path('Assets/Art/Textures/Models/EmpireShips/ISDI')/(definition['stem']+'.png')).convert('RGBA')
  assert source.size==target.size and source.tobytes()==target.tobytes(),definition['stem']
  textures+=1
 counts={};report=json.loads((TASK/name/'ConversionReport.json').read_text())
 for mesh in variant['material_audit']['meshes']:
  index=counts.get(mesh['name'],0);counts[mesh['name']]=index+1
  key=mesh['name'] if index==0 else mesh['name']+'.'+str(index).zfill(3)
  if not report['before']['meshes'][key]['hidden']:
   assert sum(m['triangleCount'] for m in mesh['materials'])==report['before']['meshes'][key]['triangles'],key
archive_guids=archive_binaries=0
for folder in ('Assets/Art/Models/EmpireShips','Assets/Art/Materials/Models/EmpireShips','Assets/Art/Textures/Models/EmpireShips','Assets/Art/Materials/Wrecks'):
 paths=subprocess.check_output(['git','ls-files',folder+'/ISDI',folder+'/ISDI.meta'],text=True).splitlines()
 for original in paths:
  target=Path(original.replace('/ISDI','/ISDIObsoleteAOTR',1))
  old=subprocess.check_output(['git','show','HEAD:'+original])
  assert target.exists(),str(target)
  if original.endswith('.meta'):
   guid=re.search(rb'^guid: (\w+)',old,re.M).group(1)
   assert guid==re.search(rb'^guid: (\w+)',target.read_bytes(),re.M).group(1),str(target)
   archive_guids+=1
  elif target.suffix.lower() in ('.png','.fbx'):
   assert old==target.read_bytes(),str(target);archive_binaries+=1
palette=[]
base=Image.open(TASK/'Previews/Team0.png').convert('RGB')
for i in range(1,8):
 difference=ImageChops.difference(base,Image.open(TASK/f'Previews/Team{i}.png').convert('RGB'))
 assert difference.getbbox() is not None,'Live team '+str(i);palette.append(difference.getbbox())
base=Image.open(TASK/'Previews/WreckTeam0.png').convert('RGB')
for i in range(1,8):assert ImageChops.difference(base,Image.open(TASK/f'Previews/WreckTeam{i}.png').convert('RGB')).getbbox() is not None,'Wreck team '+str(i)
icon=Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIcon.png').convert('RGBA')
assert icon.size==(512,512) and icon.getchannel('A').getextrema()==(0,255)
bounds=icon.getchannel('A').getbbox();assert bounds[0]>0 and bounds[1]>0 and bounds[2]<512 and bounds[3]<512
silhouette=Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/ISDISilhouette.png').convert('RGBA')
assert silhouette.getchannel('A').tobytes()==icon.getchannel('A').tobytes()
result=dict(source_hashes_unchanged=len(audit['source_hashes']),lossless_texture_copies=textures,original_archive_guids_retained=archive_guids,original_archive_binary_files_unchanged=archive_binaries,visible_binary_triangle_counts=True,live_and_wreck_palettes=8,transparent_icon_size=list(icon.size),icon_crop=list(bounds))
(TASK/'SourceVerification.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
