"""Audit the Y-Wing skeleton and stage lossless source textures."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

SOURCE_DIRECTORY = Path('F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art')
OUTPUT_DIRECTORY = Path('F:/Private/empire-at-war/Temp/YWingImport/Output')
TEXTURES = {
    'ReV_ywing.dds': 'YWing_Hull_Albedo',
    'ReV_ywing_b.dds': 'YWing_Hull_Normal',
    'flash_red.dds': 'YWing_MuzzleFlash_Albedo',
}

OUTPUT_DIRECTORY.joinpath('Textures').mkdir(parents=True, exist_ok=True)
source = SOURCE_DIRECTORY / 'Models/ReV_ywing.ALO'
with source.open('rb') as stream:
    assert stream.read(4) == struct.pack('<I', 0x200)
    stream.seek(12)
    stream.read(4)
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
assert count == 13 and bones[0]['name'] == 'Root'
assert bones[0]['matrix'] == (1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0)
hashes = {str(source): hashlib.sha256(source.read_bytes()).hexdigest()}
for source_name, target_name in TEXTURES.items():
    texture = SOURCE_DIRECTORY / 'Textures' / source_name
    hashes[str(texture)] = hashlib.sha256(texture.read_bytes()).hexdigest()
    with Image.open(texture) as image:
        target = OUTPUT_DIRECTORY / 'Textures' / (target_name + '.png')
        image.save(target)
        with Image.open(target) as copy:
            assert image.convert('RGBA').tobytes() == copy.convert('RGBA').tobytes()
        if source_name == 'ReV_ywing.dds':
            image.getchannel('A').save(OUTPUT_DIRECTORY / 'Textures/YWing_Hull_TeamMask.png')
report = {'source_hashes': hashes, 'source_bones': bones}
(OUTPUT_DIRECTORY / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps({'bones': count, 'root': bones[0], 'textures': list(TEXTURES)}))
