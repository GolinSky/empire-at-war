"""Lossless DDS conversion for the verified Independence source textures."""
import json
from pathlib import Path
from PIL import Image

TASK = Path('Temp/MC80IndependenceImport')
audit = json.loads((TASK / 'SourceAudit.json').read_text())
output = TASK / 'Textures'
output.mkdir(exist_ok=True)
mapping = {}
for name, source in audit['models']['MC80Independence']['textures'].items():
    stem = 'MC80Independence_' + Path(name).stem
    normal = name.upper().endswith('_B.DDS')
    image = Image.open(source).convert('RGBA')
    image.save(output / (stem + '.png'))
    assert Image.open(output / (stem + '.png')).convert('RGBA').tobytes() == image.tobytes()
    mapping[name.upper()] = (stem, normal)
    if name.startswith('Rebel_Mon_Cal_Home_One_') and not normal and not name.endswith(('_S.dds','Ambient.dds')):
        mask = image.getchannel('A')
        mask.save(output / (stem + '_TeamMask.png'))
        print(name, image.size, 'alpha', mask.getextrema(), 'nonzero', 1 - mask.histogram()[0]/(mask.width*mask.height))
(TASK / 'TextureMapping.json').write_text(json.dumps(mapping, indent=2))
