"""Audit the vanilla CR90 sources and stage lossless textures for Blender."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

ART_DIRECTORY = Path('F:/Private/empire-at-war/output/eaw-rebel-ships/DATA/ART')
OUTPUT_DIRECTORY = Path('F:/Private/empire-at-war/Temp/CorellianCorvetteImport/Output')
TEXTURES = {'RV_CORVETTE.DDS': 'CorellianCorvette_Hull_Albedo',
            'RV_CORVETTE_BUMP.DDS': 'CorellianCorvette_Hull_Normal',
            'W_LASER_SMALL.DDS': 'CorellianCorvette_Flash_Albedo'}

OUTPUT_DIRECTORY.joinpath('Textures').mkdir(parents=True, exist_ok=True)
reports = {}
hashes = {}
for filename in ('RV_CORVETTE.ALO', 'RV_CORVETTE_D.ALO'):
    path = ART_DIRECTORY / 'MODELS' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    with path.open('rb') as stream:
        assert stream.read(4) == struct.pack('<I', 0x200)
        stream.seek(16)
        count = struct.unpack('<I', stream.read(4))[0]
        stream.seek(124, 1)
        bones = []
        for index in range(count):
            stream.seek(12, 1)
            size = struct.unpack('<I', stream.read(4))[0] & 0x7fffffff
            name = stream.read(size).rstrip(b'\0').decode('ascii')
            stream.seek(8, 1)
            parent, visible, billboard = struct.unpack('<III', stream.read(12))
            matrix = struct.unpack('<12f', stream.read(48))
            bones.append(dict(name=name, parent_index=parent, visible=visible, matrix=matrix))
    assert bones[0]['name'] == 'Root'
    assert bones[0]['matrix'] == (1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0)
    reports[filename] = bones
textures = []
for filename, target in TEXTURES.items():
    path = ART_DIRECTORY / 'TEXTURES' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    with Image.open(path) as source:
        image = source.convert('RGBA')
        destination = OUTPUT_DIRECTORY / 'Textures' / (target + '.png')
        image.save(destination)
        with Image.open(destination) as converted:
            assert image.tobytes() == converted.convert('RGBA').tobytes()
        textures.append(dict(source=filename, size=image.size, alpha=image.getchannel('A').getextrema()))
        if filename == 'RV_CORVETTE.DDS':
            # Vanilla MeshBumpColorize uses inverse hull alpha as its ownership mask.
            mask = image.getchannel('A').point(lambda value: 255 - value)
            mask.save(OUTPUT_DIRECTORY / 'Textures/CorellianCorvette_Hull_TeamMask.png')
            textures[-1]['mask_coverage'] = sum(v > 0 for v in mask.tobytes()) / (image.width * image.height)
report = dict(bones=reports, textures=textures, source_hashes=hashes)
(OUTPUT_DIRECTORY / 'BinaryAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(dict(bones={name: len(bones) for name, bones in reports.items()}, textures=textures), indent=2))
