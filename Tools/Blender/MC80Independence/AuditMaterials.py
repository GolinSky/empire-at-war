"""Read original ALO material assignments independently of Blender's material reuse."""
import hashlib
import json
import re
import struct
from pathlib import Path

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS')
OUTPUT = Path('Temp/MC80IndependenceImport')


def Chunks(data):
    offset = 0
    while offset < len(data):
        kind, size = struct.unpack_from('<II', data, offset)
        size &= 0x7fffffff
        yield kind, data[offset+8:offset+8+size]
        offset += 8+size
    assert offset == len(data)


def MicroChunks(data):
    offset = 0
    while offset < len(data):
        kind, size = data[offset:offset+2]
        yield kind, data[offset+2:offset+2+size]
        offset += 2+size
    assert offset == len(data)


def Audit(path):
    meshes = []
    connections = []
    proxies = []
    for kind, payload in Chunks(path.read_bytes()):
        if kind == 0x600:
            for child, content in Chunks(payload):
                if child == 0x602:
                    connections.append(dict(mesh=struct.unpack_from('<I',content,2)[0],bone=struct.unpack_from('<I',content,8)[0]))
                elif child == 0x603:
                    fields = list(MicroChunks(content))
                    proxies.append(dict(name=fields[0][1].rstrip(b'\0').decode('ascii'),bone=struct.unpack('<I',fields[1][1])[0]))
        if kind != 0x400:
            continue
        mesh = dict(materials=[])
        for child, content in Chunks(payload):
            if child == 0x401:
                mesh['name'] = content.rstrip(b'\0').decode('ascii')
            elif child == 0x402:
                mesh['hidden'] = bool(struct.unpack_from('<I', content, 32)[0])
                mesh['collision'] = bool(struct.unpack_from('<I', content, 36)[0])
            elif child == 0x10100:
                material = dict(properties={})
                for parameter, value in Chunks(content):
                    if parameter == 0x10101:
                        material['shader'] = value.rstrip(b'\0').decode('ascii')
                    elif parameter in (0x10102, 0x10103, 0x10104, 0x10105, 0x10106):
                        micros = list(MicroChunks(value))
                        name = micros[0][1].rstrip(b'\0').decode('ascii')
                        raw = micros[1][1]
                        if parameter == 0x10105:
                            result = raw.rstrip(b'\0').decode('ascii')
                        elif parameter == 0x10102:
                            result = struct.unpack('<I', raw)[0]
                        else:
                            result = list(struct.unpack('<'+'f'*(len(raw)//4), raw))
                        material['properties'][name] = result
                mesh['materials'].append(material)
            elif child == 0x10000:
                header = next(value for kind, value in Chunks(content) if kind == 0x10001)
                mesh['materials'][-1]['vertexCount'], mesh['materials'][-1]['triangleCount'] = struct.unpack_from('<II', header)
        meshes.append(mesh)
    return dict(file=str(path), sha256=hashlib.sha256(path.read_bytes()).hexdigest(), meshes=meshes,connections=connections,proxies=proxies)


variants = {path.name: Audit(path) for path in SOURCE.glob('*.ALO') if re.search('mc80|mon.cal.home.one', path.name, re.I)}
(OUTPUT/'BinaryMaterialAudit.json').write_text(json.dumps(variants, indent=2))
for name, model in variants.items():
    print(name, len(model['meshes']), 'meshes')
for mesh in variants['RV_MC80_Independence.ALO']['meshes']:
    print(mesh['name'], mesh['hidden'], [(m['shader'], m['properties'].get('BaseTexture')) for m in mesh['materials']])
