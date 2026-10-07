"""Audit the AOTR advanced ISD and its exact three fighter dependencies.

Run with Pillow from the repository root. Source files remain unchanged.
"""
import hashlib
import json
import re
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image, ImageOps

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART')
OUTPUT = Path('Temp/ISDIImport')
MODELS = {
    'ISDI': 'Empire_Imperial_SD.ALO',
    'TIEInterceptor': 'EV_TIE_INTERCEPTOR_E.ALO',
    'TIEBrute': 'EV_TIE_Brute.ALO',
    'TIEPunisher': 'EV_TIE_Punisher.ALO',
}
MODELS['ISDIParts'] = 'Empire_Imperial_SD_1.alo'
for _index in range(1, 7):
    MODELS[f'ISDITLD{_index:02}'] = f'Empire_Imperial_SD_1_TLD_{_index:02}.ALO'
for _index in range(1, 3):
    MODELS[f'ISDIICD{_index:02}'] = f'Empire_Imperial_SD_1_ICD_{_index:02}.ALO'
for _index in range(1, 4):
    MODELS[f'ISDITLT{_index:02}'] = f'Empire_Imperial_Star_Destroyer_Triple_Turbolaser_{_index:02}.ALO'


def Chunks(data):
    offset = 0
    while offset < len(data):
        kind, size = struct.unpack_from('<II', data, offset)
        size &= 0x7fffffff
        yield kind, data[offset + 8:offset + 8 + size]
        offset += 8 + size
    assert offset == len(data)


def Audit(path):
    bones = []
    with path.open('rb') as stream:
        stream.seek(16)
        count = struct.unpack('<I', stream.read(4))[0]
        stream.seek(124, 1)
        for _ in range(count):
            stream.seek(12, 1)
            size = struct.unpack('<I', stream.read(4))[0] & 0x7fffffff
            name = stream.read(size).rstrip(b'\0').decode('ascii')
            stream.seek(8, 1)
            parent, visible, billboard = struct.unpack('<III', stream.read(12))
            matrix = list(struct.unpack('<12f', stream.read(48)))
            bones.append(dict(name=name, parent_index=parent, visible=visible, matrix=matrix))
    objects, connections = [], []
    for kind, payload in Chunks(path.read_bytes()):
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


def Main():
    textures = {p.name.lower(): p for p in (SOURCE / 'TEXTURES').iterdir() if p.is_file()}
    hashes, variants = {}, {}
    for variant, filename in MODELS.items():
        model = SOURCE / 'MODELS' / filename
        hashes[str(model)] = hashlib.sha256(model.read_bytes()).hexdigest()
        audit = Audit(model)
        target = OUTPUT / variant / 'Textures'
        target.mkdir(parents=True, exist_ok=True)
        audit['textures'] = {}
        names = sorted(set(n.decode('ascii') for n in re.findall(rb'[A-Za-z0-9_.-]+\.(?:dds|DDS|tga|TGA)', model.read_bytes())))
        for name in names:
            key = name.lower()
            if key not in textures:
                key = str(Path(key).with_suffix('.dds'))
            source = textures[key]
            hashes[str(source)] = hashlib.sha256(source.read_bytes()).hexdigest()
            image = Image.open(source).convert('RGBA')
            stem = variant + '_' + source.stem
            destination = target / (stem + '.png')
            image.save(destination)
            assert Image.open(destination).convert('RGBA').tobytes() == image.tobytes()
            ImageOps.invert(image.getchannel('A')).save(target / (stem + '_TeamMask.png'))
            audit['textures'][name] = dict(source=str(source), stem=stem, size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
        variants[variant] = audit
        print(variant, 'bones', len(audit['bones']), 'objects', len(audit['objects']), 'textures', len(names))
    xml = SOURCE.parent / 'XML/Hardpoints_Empire_Space.xml'
    hardpoints = {hp.get('Name'): {child.tag: child.text for child in hp} for hp in ET.parse(xml).getroot() if hp.get('Name', '').startswith(('HP_ISD1_', 'HP_ISD_Triple_Turret_'))}
    (OUTPUT / 'BinaryAudit.json').write_text(json.dumps(dict(variants=variants, source_hashes=hashes, hardpoints=hardpoints), indent=2))


if __name__ == '__main__':
    Main()
