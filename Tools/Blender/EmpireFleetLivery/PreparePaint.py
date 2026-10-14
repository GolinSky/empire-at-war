"""Derive scuffed red paint from source metal detail; original albedos remain untouched."""
import hashlib,json,colorsys
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter
HUE=.968
inputs=json.loads(Path('Tools/Blender/EmpireFleetLivery/Evidence/PaintInputs.json').read_text())
report=[]
for record in inputs:
 source=Path(record['source']);image=Image.open(source).convert('RGB');rgb=np.asarray(image,dtype=np.float32)/255
 h,w=rgb.shape[:2];rng=np.random.default_rng(int(hashlib.sha256(record['material'].encode()).hexdigest()[:8],16))
 def Noise(size):
  a=Image.fromarray(rng.integers(0,256,(size,size),dtype=np.uint8)).resize((w,h),Image.Resampling.BICUBIC)
  return np.asarray(a,dtype=np.float32)/255
 wear=.45*Noise(64)+.35*Noise(256)+.2*Noise(1024)
 brightness=rgb.max(axis=2)
 edges=np.asarray(image.convert('L').filter(ImageFilter.FIND_EDGES),dtype=np.float32)/255
 # Broad erosion, fine chips and seam wear expose the original metal through the paint.
 paint=np.clip((wear-.35)*5.5-edges*.65,0,1)
 paint[wear<.34]=0
 value=brightness*(.63+.18*wear)
 red=np.array(colorsys.hsv_to_rgb(HUE,.68,1),dtype=np.float32)
 dyed=value[:,:,None]*red
 result=rgb*(1-paint[:,:,None])+dyed*paint[:,:,None]
 Image.fromarray(np.clip(result*255,0,255).astype(np.uint8)).save(record['derived'])
 report.append(dict(material=record['material'],sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),hue=HUE,unpaintedFraction=float((paint<.2).mean()),derived=record['derived']))
Path('Tools/Blender/EmpireFleetLivery/Evidence/PaintTextures.json').write_text(json.dumps(report,indent=2))
print('Derived',len(report),'worn red albedos; hue',HUE,'original pixels preserved in exposed metal.')
