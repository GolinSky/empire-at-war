"""Audit vanilla Home One sources and stage lossless textures for Blender MCP."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

SOURCE = Path('output/eaw-rebel-ships/DATA/ART')
OUTPUT = Path('Temp/HomeOneImport/Output')
TEXTURES = {
    'RV_HOMEONE.DDS': 'HomeOne_Hull_Albedo',
    'RV_HOMEONE_BC.DDS': 'HomeOne_Hull_Normal',
    'EV_GIRDERS00.DDS': 'HomeOne_Girders_Albedo',
    'W_BUILDINGLIGHTS.DDS': 'HomeOne_Lights_Emissive',
    'W_BLAST00.DDS': 'HomeOne_Damage_Albedo',
    'SHIELD_COLOR.DDS': 'HomeOne_Shield_Albedo',
    'NB_SHIELDRIPPLE.DDS': 'HomeOne_Shield_Distortion',
    'NB_SHIELDWAVE.DDS': 'HomeOne_Shield_Wave',
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
models = [('HomeOne', 'RV_HOMEONE.ALO'), ('HomeOneWreck', 'RV_HOMEONE_D.ALO')]
models += [('HomeOnePod_' + suffix, 'RV_HOMEONE_HP_' + suffix + '.ALO') for suffix in ('TBL_FL','TBL_FR','TBL_BL','TBL_BR','IC_FL','IC_FR','IC_BL','IC_BR','SHG','E')]
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
    if target == 'HomeOne_Hull_Albedo':
        mask = image.getchannel('A')
        mask.save(OUTPUT / 'Textures/HomeOne_Hull_TeamMask.png')
        print('Team mask coverage', sum(value > 0 for value in mask.tobytes()) / (mask.width * mask.height))
    print(source_name, image.size, image.getchannel('A').getextrema())
(OUTPUT / 'BinaryAudit.json').write_text(json.dumps(dict(variants=variants, source_hashes=hashes), indent=2))
