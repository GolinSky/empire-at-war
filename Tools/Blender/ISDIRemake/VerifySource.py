"""Verify source files, lossless textures, old-visual cleanup and visible triangle fidelity."""
import hashlib,json,re
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
cleanup=json.loads((TASK/'Cleanup.json').read_text())
for original in cleanup['originalArchiveRecords']:
 assert not Path(original).exists() and not Path(original+'.meta').exists(),original
for moved in cleanup['movedAssets']:
 target=Path(moved['newPath']);content=target.read_bytes()
 if target.suffix=='.png':assert content.startswith(b'\x89PNG\r\n\x1a\n'),str(target)
 if target.suffix=='.mat':content=re.sub(rb'(?m)^  m_Name:.*\r?\n',b'',content)
 assert hashlib.sha256(content).hexdigest()==moved['contentSha256'],str(target)
 assert re.search(r'^guid: (\w+)',Path(str(target)+'.meta').read_text(),re.M)[1]==moved['guid'],str(target)
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
result=dict(source_hashes_unchanged=len(audit['source_hashes']),lossless_texture_copies=textures,old_visual_records_removed=len(cleanup['originalArchiveRecords']),shared_tector_assets_preserved=len(cleanup['movedAssets']),unused_records_deleted=len(cleanup['deletedAssets']),visible_binary_triangle_counts=True,live_and_wreck_palettes=8,transparent_icon_size=list(icon.size),icon_crop=list(bounds))
(TASK/'SourceVerification.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
