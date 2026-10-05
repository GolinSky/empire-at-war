"""Audit the stealth corvette source skeleton and stage lossless texture conversions."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

ART_DIRECTORY = Path('F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art')
OUTPUT_DIRECTORY = Path('F:/Private/empire-at-war/Temp/StealthCorvetteImport/Output')
TEXTURES = {
    'ReV_Stealthship.dds': 'StealthCorvette_Hull_Albedo',
    'ReV_Stealthship_B.dds': 'StealthCorvette_Hull_Normal',
}

OUTPUT_DIRECTORY.joinpath('Textures').mkdir(parents=True, exist_ok=True)
source = ART_DIRECTORY / 'Models/ReV_Stealthship.ALO'
with source.open('rb') as stream:
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
assert count == 15 and bones[0]['name'] == 'Root'
assert bones[0]['matrix'] == (1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0)
paths = [source]
textures = []
for source_name, target_name in TEXTURES.items():
    path = ART_DIRECTORY / 'Textures' / source_name
    paths.append(path)
    with Image.open(path) as image:
        destination = OUTPUT_DIRECTORY / 'Textures' / (target_name + '.png')
        image.save(destination)
        with Image.open(destination) as converted:
            assert image.convert('RGBA').tobytes() == converted.convert('RGBA').tobytes()
        textures.append({'source': source_name, 'output': destination.name, 'size': image.size, 'mode': image.mode})
        if source_name == 'ReV_Stealthship.dds':
            alpha = image.getchannel('A')
            alpha.save(OUTPUT_DIRECTORY / 'Textures/StealthCorvette_Hull_TeamMask.png')
            textures[-1]['alpha_extrema'] = alpha.getextrema()
            textures[-1]['alpha_coverage'] = sum(value > 0 for value in alpha.tobytes()) / (image.width * image.height)
report = {'bones': bones, 'textures': textures, 'source_hashes': {str(path): hashlib.sha256(path.read_bytes()).hexdigest() for path in paths}}
(OUTPUT_DIRECTORY / 'BinaryAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps({'bone_count': count, 'root': bones[0], 'textures': textures}, indent=2))
