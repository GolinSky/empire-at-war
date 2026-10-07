from pathlib import Path
import json,runpy
from PIL import Image,ImageOps
r=json.loads(Path('Temp/TectorImport/SourceAudit.json').read_text())
audit=runpy.run_path('Tools/Blender/prepare_isd_i.py')['Audit']
old=json.loads(Path('Tools/Blender/ISDI/ImportEvidence.json').read_text())['source_hashes']
shared={'Tector':'ISDI','TectorTurret19':'ISDIParts'}
for i in range(1,7):shared[f'TectorTurret{i:02}']=f'ISDITLD{i:02}'
for i in range(9,12):shared[f'TectorTurret{i:02}']=f'ISDITLT{i-8:02}'
for name in shared:
 path=r['models'][name]['file'];assert r['source_hashes'][path]==old[path],path
r['shared']=shared;r['variants']={}
for name,m in r['models'].items():
 if name in shared:continue
 a=audit(Path(m['file']));a['file']=m['file'];a['textures']={}
 target=Path('Temp/TectorImport')/name/'Textures';target.mkdir(parents=True,exist_ok=True)
 for ref,source in m['textures'].items():
  image=Image.open(source).convert('RGBA');stem=name+'_'+Path(source).stem
  image.save(target/(stem+'.png'));ImageOps.invert(image.getchannel('A')).save(target/(stem+'_TeamMask.png'))
  a['textures'][ref]={'source':source,'stem':stem}
 r['variants'][name]=a
Path('Temp/TectorImport/SourceAudit.json').write_text(json.dumps(r,indent=2))
print('Reusing',len(shared),'hash-matched models; converting',len(r['variants']))
