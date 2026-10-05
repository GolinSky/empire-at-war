"""Audit vanilla MC80 sources and stage lossless textures for Blender MCP."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

SOURCE = Path('output/eaw-rebel-ships/DATA/ART')
OUTPUT = Path('Temp/MonCalCruiserImport/Output')
TEXTURES = {
    'RV_MONCALCRUISER.DDS': 'MonCalCruiser_Hull_Albedo',
    'RV_MONCALCRUISER_BC.DDS': 'MonCalCruiser_Hull_Normal',
    'EV_GIRDERS00.DDS': 'MonCalCruiser_Girders_Albedo',
    'W_BUILDINGLIGHTS.DDS': 'MonCalCruiser_Lights_Emissive',
    'W_BLAST00.DDS': 'MonCalCruiser_Damage_Albedo',
    'SHIELD_COLOR.DDS': 'MonCalCruiser_Shield_Albedo',
    'NB_SHIELDRIPPLE.DDS': 'MonCalCruiser_Shield_Distortion',
    'NB_SHIELDWAVE.DDS': 'MonCalCruiser_Shield_Wave',
}


def Chunks(data):
    offset = 0
    while offset < len(data):
        kind, size = struct.unpack_from('<II', data, offset)
        size &= 0x7fffffff
        yield kind, data[offset + 8:offset + 8 + size]
        offset += 8 + size
    assert offset == len(data)


def Audit(source):
    bones = []
    with source.open('rb') as stream:
        stream.seek(16)
        count = struct.unpack('<I', stream.read(4))[0]
        stream.seek(124, 1)
        for i in range(count):
            stream.seek(12, 1)
            size = struct.unpack('<I', stream.read(4))[0] & 0x7fffffff
            name = stream.read(size).rstrip(b'\0').decode('ascii')
            stream.seek(8, 1)
            parent, visible, billboard = struct.unpack('<III', stream.read(12))
            matrix = list(struct.unpack('<12f', stream.read(48)))
            bones.append(dict(name=name, parent_index=parent, visible=visible, matrix=matrix))
    objects, connections = [], []
    for kind, payload in Chunks(source.read_bytes()):
        if kind in (0x400, 0x1300):
            _, name = next(Chunks(payload))
            objects.append(dict(kind=hex(kind), name=name.rstrip(b'\0').decode('ascii')))
        elif kind == 0x600:
            for child, content in Chunks(payload):
                if child == 0x602:
                    mesh = struct.unpack_from('<I', content, 2)[0]
                    bone = struct.unpack_from('<I', content, 8)[0]
                    connections.append(dict(object_index=mesh, bone_index=bone, bone=bones[bone]['name']))
    return dict(bones=bones, objects=objects, connections=connections)


(OUTPUT / 'Textures').mkdir(parents=True, exist_ok=True)
hashes, variants = {}, {}
models = [('MonCalCruiser', 'RV_MONCALCRUISER.ALO'), ('MonCalCruiserWreck', 'RV_MONCALCRUISER_D.ALO')]
models += [('MonCalCruiserPod_' + suffix, 'RV_MONCALCRUISER_HP00_' + suffix + '.ALO') for suffix in ('F-L', 'F-R', 'M-L', 'M-R', 'B-L', 'B-R', 'E')]
for name, source_name in models:
    source = SOURCE / 'MODELS' / source_name
    hashes[str(source)] = hashlib.sha256(source.read_bytes()).hexdigest()
    variants[name] = Audit(source)
    print(name, len(variants[name]['bones']), variants[name]['objects'])
for source_name, target in TEXTURES.items():
    source = SOURCE / 'TEXTURES' / source_name
    hashes[str(source)] = hashlib.sha256(source.read_bytes()).hexdigest()
    image = Image.open(source).convert('RGBA')
    target_path = OUTPUT / 'Textures' / (target + '.png')
    image.save(target_path)
    assert Image.open(target_path).convert('RGBA').tobytes() == image.tobytes()
    if target == 'MonCalCruiser_Hull_Albedo':
        mask = image.getchannel('A')
        mask.save(OUTPUT / 'Textures/MonCalCruiser_Hull_TeamMask.png')
        print('Team mask coverage', sum(value > 0 for value in mask.tobytes()) / (mask.width * mask.height))
    print(source_name, image.size, image.getchannel('A').getextrema())
(OUTPUT / 'BinaryAudit.json').write_text(json.dumps(dict(variants=variants, source_hashes=hashes), indent=2))
