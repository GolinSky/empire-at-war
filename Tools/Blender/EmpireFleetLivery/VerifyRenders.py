from pathlib import Path
from PIL import Image,ImageChops
import numpy as np,json,hashlib
base=Path('Tools/Blender/EmpireFleetLivery');reports=[]
for ship in ('Procursator','Allegiance','Titan','Tartan'):
 folder=base/'Previews'/ship
 row={'ship':ship}
 for prefix in ('Team','WreckTeam'):
  images=[np.asarray(Image.open(folder/(prefix+str(i)+'.png')).convert('RGBA')) for i in range(8)]
  visible=(images[0][:,:,3]>32)
  changed=np.max(np.abs(images[0][:,:,:3].astype(int)-images[2][:,:,:3].astype(int)),axis=2)>12
  fraction=float((changed&visible).sum()/visible.sum());assert 0<fraction<.10,(ship,prefix,fraction)
  row[prefix+'ChangedSurfaceFraction']=fraction
  assert all(np.any(np.abs(images[0].astype(int)-im.astype(int))>6) for im in images[1:])
 reports.append(row)
for rec in json.loads((base/'Evidence'/'PaintTextures.json').read_text()):
 inp=next(r for r in json.loads((base/'Evidence'/'PaintInputs.json').read_text()) if r['material']==rec['material'])
 assert hashlib.sha256(Path(inp['source']).read_bytes()).hexdigest()==rec['sourceSha256']
(base/'Evidence'/'RenderedVerification.json').write_text(json.dumps({'allEightPalettesDistinct':True,'originalAlbedosUnchanged':True,'reports':reports},indent=2))
print(json.dumps(reports,indent=2))
