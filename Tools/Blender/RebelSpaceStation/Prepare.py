"""Audit the five vanilla ALOs and losslessly stage only referenced textures."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'output/aotr-space-stations/preview-selection/Vanilla/Data/ART'
OUTPUT = ROOT / 'Temp/RebelStationImport'


def Chunks(data):
    offset = 0
    while offset < len(data):
        kind, size = struct.unpack_from('<II', data, offset)
        size &= 0x7fffffff
        yield kind, data[offset + 8:offset + 8 + size]
        offset += 8 + size
    assert offset == len(data)


def MicroChunks(data):
    offset = 0
    while offset < len(data):
        kind, size = data[offset:offset + 2]
        yield kind, data[offset + 2:offset + 2 + size]
        offset += 2 + size
    assert offset == len(data)


def Bones(path):
    result = []
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
            result.append(dict(name=name, parent_index=parent, visible=visible,
                               matrix=list(struct.unpack('<12f', stream.read(48)))))
    return result


def Audit(path):
    meshes, connections = [], []
    object_index = 0
    for kind, payload in Chunks(path.read_bytes()):
        if kind == 0x1300:
            object_index += 1
        if kind == 0x600:
            for child, content in Chunks(payload):
                if child == 0x602:
                    connections.append(dict(mesh=struct.unpack_from('<I', content, 2)[0],
                                            bone=struct.unpack_from('<I', content, 8)[0]))
        if kind != 0x400:
            continue
        mesh = dict(materials=[], object_index=object_index)
        object_index += 1
        for child, content in Chunks(payload):
            if child == 0x401:
                mesh['name'] = content.rstrip(b'\0').decode('ascii')
            elif child == 0x402:
                mesh['hidden'] = bool(struct.unpack_from('<I', content, 32)[0])
            elif child == 0x10100:
                material = dict(properties={})
                for parameter, value in Chunks(content):
                    if parameter == 0x10101:
                        material['shader'] = value.rstrip(b'\0').decode('ascii')
                    elif parameter in (0x10102, 0x10103, 0x10104, 0x10105, 0x10106):
                        micros = list(MicroChunks(value))
                        name = micros[0][1].rstrip(b'\0').decode('ascii')
                        raw = micros[1][1]
                        result = (raw.rstrip(b'\0').decode('ascii') if parameter == 0x10105
                                  else list(struct.unpack('<' + ('I' if parameter == 0x10102 else 'f') * (len(raw) // 4), raw)))
                        material['properties'][name] = result
                mesh['materials'].append(material)
            elif child == 0x10000:
                header = next(v for k, v in Chunks(content) if k == 0x10001)
                mesh['materials'][-1]['vertexCount'], mesh['materials'][-1]['triangleCount'] = struct.unpack_from('<II', header)
        meshes.append(mesh)
    return dict(file=str(path), sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                bones=Bones(path), meshes=meshes, connections=connections)


def Main():
    (OUTPUT / 'Output/Textures').mkdir(parents=True, exist_ok=True)
    models, textures = {}, {}
    texture_files = {p.stem.lower(): p for p in (SOURCE / 'TEXTURES').iterdir()}
    for level in range(1, 6):
        model = Audit(SOURCE / f'MODELS/RB_STATION_{level:02}.ALO')
        model['level'] = level
        models[f'RebelSpaceStationLevel{level}'] = model
        for mesh in model['meshes']:
            for material in mesh['materials']:
                for key, ref in material['properties'].items():
                    if not isinstance(ref, str) or not ref.lower().endswith(('.dds', '.tga')):
                        continue
                    path = texture_files[Path(ref).stem.lower()]
                    name = 'RebelSpaceStation_' + path.stem
                    png = OUTPUT / 'Output/Textures' / (name + '.png')
                    pixels = Image.open(path).convert('RGBA')
                    pixels.save(png)
                    assert Image.open(png).convert('RGBA').tobytes() == pixels.tobytes()
                    textures[ref.lower()] = dict(source=str(path), png=str(png), name=name,
                                                sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                                                size=pixels.size, normal=key == 'NormalTexture')
    audit = dict(models=models, textures=textures)
    (OUTPUT / 'SourceAudit.json').write_text(json.dumps(audit, indent=2), encoding='utf-8')
    for name, model in models.items():
        print(name, 'bones', len(model['bones']), 'meshes', len(model['meshes']),
              'triangles', sum(m['triangleCount'] for mesh in model['meshes'] for m in mesh['materials']))
        print([(m['name'], m['hidden'], [(s['shader'], s['properties'].get('BaseTexture'), s['triangleCount']) for s in m['materials']]) for m in model['meshes']])
    print('Textures:', list(textures))


if __name__ == '__main__':
    Main()
