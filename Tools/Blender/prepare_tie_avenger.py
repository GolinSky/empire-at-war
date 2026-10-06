"""Audit the AOTR TIE Avenger and stage lossless DDS conversions."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

SOURCE_DIRECTORY = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART')
OUTPUT_DIRECTORY = Path('F:/Private/empire-at-war/Temp/TIEAvengerImport/Output')
TEXTURES = {
    'Tie_Fighter_Adv_Bomber.dds': 'TIEAvenger_Hull_Albedo',
    'Tie_Fighter_Adv_Bomber_NM.dds': 'TIEAvenger_Hull_Normal',
}

OUTPUT_DIRECTORY.joinpath('Textures').mkdir(parents=True, exist_ok=True)
source = SOURCE_DIRECTORY / 'MODELS/TIE_AVENGER.ALO'
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
assert count == 16 and bones[0]['name'] == 'Root'
hashes = {str(source): hashlib.sha256(source.read_bytes()).hexdigest()}
for source_name, target_name in TEXTURES.items():
    texture = SOURCE_DIRECTORY / 'TEXTURES' / source_name
    hashes[str(texture)] = hashlib.sha256(texture.read_bytes()).hexdigest()
    with Image.open(texture) as image:
        target = OUTPUT_DIRECTORY / 'Textures' / (target_name + '.png')
        image.save(target)
        with Image.open(target) as copy:
            assert image.convert('RGBA').tobytes() == copy.convert('RGBA').tobytes()
report = {'source_hashes': hashes, 'source_bones': bones}
(OUTPUT_DIRECTORY / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps({'bones': bones, 'textures': list(TEXTURES)}))
