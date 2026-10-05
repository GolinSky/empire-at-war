"""Audit RV_XWING.ALO and stage its lossless DDS conversions for Blender MCP."""
from pathlib import Path
from PIL import Image
import json, struct, hashlib

source = Path('output/eaw-rebel-ships/DATA/ART')
output = Path('Temp/XWingImport/Output')
(output / 'Textures').mkdir(parents=True, exist_ok=True)
model = source / 'MODELS/RV_XWING.ALO'
with model.open('rb') as file:
    file.seek(16)
    count = struct.unpack('<I', file.read(4))[0]
    file.seek(124, 1)
    bones = []
    for index in range(count):
        file.seek(12, 1)
        length = struct.unpack('<I', file.read(4))[0] & 0x7fffffff
        name = file.read(length).rstrip(b'\0').decode('ascii')
        file.seek(8, 1)
        parent, visible, billboard = struct.unpack('<III', file.read(12))
        matrix = struct.unpack('<12f', file.read(48))
        bones.append(dict(name=name, parent_index=parent, visible=visible, matrix=matrix))
(output / 'BinaryBones.json').write_text(json.dumps(bones, indent=2))
textures = {'RV_XWING.DDS': 'XWing_Hull_Albedo', 'RV_XWING_GLOSS.DDS': 'XWing_Hull_GlossSource', 'W_LASER_SMALL.DDS': 'XWing_MuzzleFlash_Albedo'}
for original, target in textures.items():
    image = Image.open(source / 'TEXTURES' / original).convert('RGBA')
    image.save(output / 'Textures' / (target + '.png'))
    if original == 'RV_XWING.DDS':
        image.getchannel('A').save(output / 'Textures/XWing_Hull_TeamMask.png')
    print(original, image.size, image.getchannel('A').getextrema())
files = [model, *(source / 'TEXTURES' / name for name in textures), *(source / 'MODELS' / name for name in ['RV_XWING_DEPLOY_00.ALA','RV_XWING_UNDEPLOY_00.ALA'])]
(output / 'SourceHashes.json').write_text(json.dumps({str(path.resolve()): hashlib.sha256(path.read_bytes()).hexdigest() for path in files}, indent=2))
print('Bones', count, bones[0])
