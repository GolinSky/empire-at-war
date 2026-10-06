"""Audit AOTR BTL-A4 sources and stage lossless textures. Originals stay untouched."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART')
OUTPUT = Path('F:/Private/empire-at-war/Temp/YWingBomberImport/Output')
TEXTURES = {
    'REB_YWing_C.dds': 'YWingBomber_Hull_Albedo',
    'REB_YWing_C_normal.dds': 'YWingBomber_Hull_Normal',
    'Y_Wing_Engine_Glow.dds': 'YWingBomber_Engine_Albedo',
}

OUTPUT.joinpath('Textures').mkdir(parents=True, exist_ok=True)
report = {'source_hashes': {}, 'models': {}, 'textures': {}}
for model in ['RV_Y_WING.ALO', 'RV_Y_WING_TURRET.ALO']:
    path = SOURCE / 'MODELS' / model
    report['source_hashes'][str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    with path.open('rb') as stream:
        assert stream.read(4) == struct.pack('<I', 0x200)
        stream.seek(16)
        count = struct.unpack('<I', stream.read(4))[0]
        stream.seek(124, 1)
        bones = []
        for index in range(count):
            stream.seek(12, 1)
            length = struct.unpack('<I', stream.read(4))[0] & 0x7fffffff
            name = stream.read(length).rstrip(b'\0').decode('ascii')
            stream.seek(8, 1)
            parent, visible, billboard = struct.unpack('<III', stream.read(12))
            matrix = struct.unpack('<12f', stream.read(48))
            bones.append({'name': name, 'parent_index': parent, 'visible': visible, 'matrix': matrix})
        report['models'][model] = bones
for source_name, target_name in TEXTURES.items():
    path = SOURCE / 'TEXTURES' / source_name
    report['source_hashes'][str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    with Image.open(path) as image:
        target = OUTPUT / 'Textures' / (target_name + '.png')
        image.save(target)
        with Image.open(target) as copied:
            assert image.convert('RGBA').tobytes() == copied.convert('RGBA').tobytes()
        report['textures'][source_name] = {'size': image.size, 'mode': image.mode}
        if source_name == 'REB_YWing_C.dds':
            alpha = image.convert('RGBA').getchannel('A')
            alpha.save(OUTPUT / 'Textures/YWingBomber_Hull_TeamMask.png')
            report['textures'][source_name]['alpha_extrema'] = alpha.getextrema()
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
