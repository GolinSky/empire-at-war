"""Stage lossless NTB-630 textures and record unchanged source hashes."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

SOURCE_DIRECTORY = Path('F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art')
OUTPUT_DIRECTORY = Path('F:/Private/empire-at-war/Temp/NTB630Import/Output')
TEXTURES = {
    'ReV_ntb.dds': 'NTB630_Hull_Albedo',
    'ReV_ntb_gloss.dds': 'NTB630_Hull_GlossSource',
    'W_laser_small.dds': 'NTB630_MuzzleFlash_Albedo',
    'black.dds': 'NTB630_EngineHelper_Albedo',
}

OUTPUT_DIRECTORY.joinpath('Textures').mkdir(parents=True, exist_ok=True)
source = SOURCE_DIRECTORY / 'Models/ReV_ntb630.ALO'
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
        matrix = list(struct.unpack('<12f', stream.read(48)))
        bones.append({'name': name, 'parent_index': parent, 'visible': visible, 'matrix': matrix})
assert count == 14 and bones[0]['name'] == 'Root'
assert bones[0]['matrix'] == [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0]
audit = {'source_bones': bones}
hashes = {str(source): hashlib.sha256(source.read_bytes()).hexdigest()}
for source_name, target_name in TEXTURES.items():
    source = SOURCE_DIRECTORY / 'Textures' / source_name
    hashes[str(source)] = hashlib.sha256(source.read_bytes()).hexdigest()
    with Image.open(source) as image:
        target = OUTPUT_DIRECTORY / 'Textures' / (target_name + '.png')
        image.save(target)
        with Image.open(target) as copy:
            assert image.convert('RGBA').tobytes() == copy.convert('RGBA').tobytes()
        if source_name == 'ReV_ntb.dds':
            image.getchannel('A').save(OUTPUT_DIRECTORY / 'Textures/NTB630_Hull_TeamMask.png')
audit['source_hashes'] = hashes
audit['missing_textures'] = ['ReV_ntb2.dds', 'ReV_ntb2_gloss.dds']
(OUTPUT_DIRECTORY / 'SourceAudit.json').write_text(json.dumps(audit, indent=2))
print(json.dumps({'bones': 14, 'textures': list(TEXTURES), 'missing': audit['missing_textures']}))
