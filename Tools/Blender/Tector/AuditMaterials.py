"""Read original ALO material assignments independently of Blender's material reuse."""
import hashlib
import json
import re
import struct
import sys
from pathlib import Path


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


ROOT=Path('Temp/TectorImport')
report=json.loads((ROOT/'SourceAudit.json').read_text())
binary={name:Audit(Path(model['file'])) for name,model in report['models'].items()}
(ROOT/'BinaryMaterialAudit.json').write_text(json.dumps(binary,indent=2))
if '--source-only' in sys.argv:
 print('Audited',len(binary),'source models')
 raise SystemExit(0)
for name in report['variants']:
 converted=json.loads((ROOT/name/'ConversionReport.json').read_text())
 meshes=converted['before']['meshes']
 definitions={m['name']:m for m in converted['materials']}
 for source in binary[name]['meshes']:
  candidates=[(n,m) for n,m in meshes.items() if n.split('.')[0]==source['name'].split('.')[0]]
  assert len(candidates)==1,(name,source['name'],candidates)
  n,mesh=candidates[0]
  assert len(mesh['materials'])==len(source['materials']),(name,n,'slots')
  shadow=all(m['shader']=='MeshShadowVolume.fx' for m in source['materials'])
  if not shadow:
   assert mesh['triangles']==sum(m['triangleCount'] for m in source['materials']),(name,n,'triangles')
  assert mesh['hidden']==(source['hidden'] or shadow),(name,n,'visibility')
  for slot,expected in zip(mesh['materials'],source['materials']):
   actual=definitions[slot]
   assert actual['shader']==expected['shader'],(name,n,'shader')
   base=expected['properties'].get('BaseTexture')
   if base is not None:
    assert actual['base']==name+'_'+Path(base).stem,(name,n,'base texture',actual,expected)
(ROOT/'BinaryMaterialAudit.json').write_text(json.dumps(binary,indent=2))
print('Binary material slots/shaders/textures match all new attachment meshes; visible/collision triangles match. ALAMO decodes shadow volumes to disabled helper surfaces.')
