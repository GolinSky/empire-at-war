"""Audit the original Tibanna ALO and losslessly decode its referenced textures."""
import hashlib
import json
import struct
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Tools/Blender/RebelSpaceStation'))
from Prepare import Audit, Chunks

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART')
TASK = ROOT / 'output/TibannaGasStationImport'
(TASK / 'Output/Textures').mkdir(parents=True, exist_ok=True)
model = Audit(SOURCE / 'MODELS/NB_Tibanna.ALO')
model['level'] = 0
model['normals'] = {}
for row, (_, payload) in zip(model['meshes'], [(k, v) for k, v in Chunks(Path(model['file']).read_bytes()) if k == 0x400]):
    normals = []
    for kind, data in Chunks(payload):
        if kind != 0x10000:
            continue
        fields = dict(Chunks(data))
        count = struct.unpack_from('<I', fields[0x10001])[0]
        stride = 144 if 0x10007 in fields else 128
        vertices = fields[0x10007 if stride == 144 else 0x10005]
        normals.extend(struct.unpack_from('<3f', vertices, i * stride + 12) for i in range(count))
    model['normals'][row['name']] = normals
textures = {}
files = {p.stem.lower(): p for p in (SOURCE / 'TEXTURES').glob('*.dds')}
for row in model['meshes']:
    for material in row['materials']:
        for key, ref in material['properties'].items():
            if not isinstance(ref, str) or not ref.lower().endswith(('.dds', '.tga')):
                continue
            path = files[Path(ref).stem.lower()]
            name = 'TibannaGasStation_' + path.stem
            png = TASK / 'Output/Textures' / (name + '.png')
            pixels = Image.open(path).convert('RGBA')
            pixels.save(png)
            if key == 'BaseTexture' and 'Colorize' in material['shader']:
                mask = pixels.getchannel('A')
                mask.save(png.with_name(name + '_TeamMask.png'))
                histogram = mask.histogram()
                print('Direct alpha team mask coverage', 100 * (sum(histogram) - histogram[0]) / sum(histogram), 'percent')
            assert Image.open(png).convert('RGBA').tobytes() == pixels.tobytes()
            textures[ref.lower()] = dict(source=str(path), png=str(png), name=name, sha256=hashlib.sha256(path.read_bytes()).hexdigest(), size=pixels.size, normal=key == 'NormalTexture')
audit = dict(models={'TibannaGasStation': model}, textures=textures)
(TASK / 'SourceAudit.json').write_text(json.dumps(audit, indent=2))
print([(r['name'], r['hidden'], [(m['shader'], m['triangleCount']) for m in r['materials']]) for r in model['meshes']])
print('Bones', len(model['bones']), 'Root', model['bones'][0], 'Textures', list(textures))
