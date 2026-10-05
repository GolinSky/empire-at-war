"""Lossless Dispatcher DDS conversion, source hashes and livery audit."""
import colorsys
import hashlib
import json
from pathlib import Path
from PIL import Image

SOURCE = Path('C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Tecno Destroyer')
OUTPUT = Path('F:/Private/empire-at-war/Temp/DispatcherImport/Output')
TEXTURES = {
    SOURCE / 'CIS_Technouniondestroyer.dds': 'Dispatcher_Hull_Albedo',
    SOURCE / 'CIS_Technouniondestroyer_B.dds': 'Dispatcher_Hull_Normal',
    Path('F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Textures/bluethruster.dds'): 'Dispatcher_Thruster_Albedo',
}
(OUTPUT / 'Textures').mkdir(parents=True, exist_ok=True)
hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in SOURCE.iterdir() if p.is_file()}
textures = {}
for source, name in TEXTURES.items():
    hashes[str(source)] = hashlib.sha256(source.read_bytes()).hexdigest()
    with Image.open(source) as image:
        target = OUTPUT / 'Textures' / (name + '.png')
        image.save(target)
        with Image.open(target) as copy:
            assert image.convert('RGBA').tobytes() == copy.convert('RGBA').tobytes()
        textures[name] = dict(size=image.size, mode=image.mode, alpha_range=image.convert('RGBA').getchannel('A').getextrema())
        if name == 'Dispatcher_Hull_Albedo':
            histogram = [0] * 36
            for r,g,b,a in image.convert('RGBA').getdata():
                h,s,v = colorsys.rgb_to_hsv(r/255,g/255,b/255)
                if s >= .25 and v >= .08:
                    histogram[min(int(h*36),35)] += 1
            textures[name]['saturated_hue_bins'] = histogram
report = dict(source_hashes=hashes,textures=textures,external_thruster_source=str(next(p for p in TEXTURES if p.name=='bluethruster.dds')))
(OUTPUT / 'TextureAudit.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
