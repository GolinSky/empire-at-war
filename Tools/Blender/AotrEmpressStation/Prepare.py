"""Audit Empress III source, XML inheritance and separate artwork; decode referenced DDS only."""
import hashlib
import json
import struct
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Tools/Blender/RebelSpaceStation'))
from Prepare import Audit, Chunks

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data')
OUTPUT = ROOT / 'Temp/AotrEmpressStationImport'
definitions = {}
xml_paths = {}
for path in (SOURCE / 'XML').glob('*.xml'):
    try:
        tree = ET.parse(path)
    except ET.ParseError:
        continue
    for row in tree.getroot():
        if row.get('Name'):
            definitions[row.get('Name').lower()] = row
            xml_paths[row.get('Name').lower()] = path

resolved = set()

def Values(name):
    resolved.add(name.lower())
    row = definitions[name.lower()]
    parent = row.findtext('Variant_Of_Existing_Type')
    values = Values(parent.strip()) if parent else {}
    values.update({child.tag: (child.text or '').strip() for child in row})
    return values

def Unique(names):
    result = []
    for name in names:
        key = name
        number = 1
        while key in result:
            key = f'{name}.{number:03}'
            number += 1
        result.append(key)
    return result

unit = Values('R_Defense_Empress')
hardpoints = [dict(name=name.strip(), **Values(name.strip())) for name in unit['HardPoints'].split(',') if name.strip()]
projectiles = {hp['Fire_Projectile_Type']: Values(hp['Fire_Projectile_Type']) for hp in hardpoints if hp.get('Fire_Projectile_Type')}
files = {p.name.lower(): p for p in (SOURCE / 'ART/MODELS').iterdir()}
models = {'AotrEmpressStation': Audit(files[unit['Space_Model_Name'].lower()])}
attachments = {}
for hp in hardpoints:
    ref = hp.get('Model_To_Attach')
    if not ref:
        continue
    if ref.lower() not in attachments:
        key = 'AotrEmpressStation_' + Path(ref).stem
        attachments[ref.lower()] = key
        models[key] = Audit(files[ref.lower()])
    hp['artModel'] = attachments[ref.lower()]

textures = {}
texture_files = {p.stem.lower(): p for p in (SOURCE / 'ART/TEXTURES').glob('*.dds')}
(OUTPUT / 'Output/Textures').mkdir(parents=True, exist_ok=True)
archive = Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data/textures.meg')
archive_index = {}
with archive.open('rb') as stream:
    names_count, files_count = struct.unpack('<II', stream.read(8))
    names = [stream.read(struct.unpack('<H',stream.read(2))[0]).decode('ascii') for _ in range(names_count)]
    for _ in range(files_count):
        _, _, size, offset, name_index = struct.unpack('<5I',stream.read(20))
        archive_index[Path(names[name_index]).stem.lower()] = (offset,size)
for key, model in models.items():
    model['level'] = 0
    for row, name in zip(model['meshes'], Unique([r['name'] for r in model['meshes']])):
        row['key'] = name
    model['normals'] = {}
    for row, (kind, payload) in zip(model['meshes'], [(k,v) for k,v in Chunks(Path(model['file']).read_bytes()) if k == 0x400]):
        normals = []
        for kind, payload in Chunks(payload):
            if kind != 0x10000:
                continue
            fields = dict(Chunks(payload))
            count = struct.unpack_from('<I', fields[0x10001])[0]
            stride = 144 if 0x10007 in fields else 128
            vertices = fields[0x10007 if stride == 144 else 0x10005]
            normals.extend(struct.unpack_from('<3f', vertices, i * stride + 12) for i in range(count))
        model['normals'][row['key']] = normals
    for mesh in model['meshes']:
        for material in mesh['materials']:
            for prop, ref in material['properties'].items():
                if not isinstance(ref, str) or not ref.lower().endswith(('.dds','.tga')):
                    continue
                stem = Path(ref).stem.lower()
                if stem not in texture_files:
                    offset, size = archive_index[stem]
                    path = OUTPUT / (stem + '.dds')
                    with archive.open('rb') as stream:
                        stream.seek(offset)
                        path.write_bytes(stream.read(size))
                    texture_files[stem] = path
                path = texture_files[stem]
                name = 'AotrEmpressStation_' + path.stem
                png = OUTPUT / 'Output/Textures' / (name + '.png')
                image = Image.open(path).convert('RGBA')
                image.save(png)
                assert Image.open(png).convert('RGBA').tobytes() == image.tobytes()
                textures[ref.lower()] = dict(source=str(path), png=str(png), name=name,
                    sha256=hashlib.sha256(path.read_bytes()).hexdigest(), size=image.size, normal=prop == 'NormalTexture')
                if prop == 'BaseTexture' and 'Colorize' in material['shader']:
                    image.getchannel('A').save(OUTPUT / 'Output/Textures' / (name + '_TeamMask.png'))

audit = dict(models=models, textures=textures, unit=unit, hardpoints=hardpoints, projectiles=projectiles,
    xml_hashes={str(path):hashlib.sha256(path.read_bytes()).hexdigest() for path in
        {xml_paths[name] for name in resolved}})
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(audit, indent=2))
print(json.dumps(dict(models={k:dict(bones=len(v['bones']),meshes=len(v['meshes']),triangles=sum(s['triangleCount'] for m in v['meshes'] for s in m['materials'])) for k,v in models.items()},
    textures=len(textures),hardpoints=len(hardpoints),attachedPieces=sum('artModel' in h for h in hardpoints),
    attachmentFiles=attachments),indent=2))
