"""Audit the installed AOTR Independence; never modify the original files."""
import hashlib
import json
import re
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data')
OUTPUT = Path('Temp/MC80IndependenceImport')


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


OUTPUT.mkdir(parents=True, exist_ok=True)
definitions = {}
for path in (SOURCE / 'XML').glob('*.xml'):
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError:
        continue
    for element in root:
        name = element.attrib.get('Name')
        if name:
            definitions[name.lower()] = element


def Values(name):
    element = definitions[name.lower()]
    parent = element.findtext('Variant_Of_Existing_Type')
    values = Values(parent.strip()) if parent else {}
    values.update({c.tag: (c.text or '').strip() for c in element})
    return values


unit = Values('R_MC80_Independence')
names = [n.strip() for n in unit['HardPoints'].split(',') if n.strip()]
hardpoints = [dict(name=n, **Values(n)) for n in names]
models = {'MC80Independence': unit['Space_Model_Name']}
for hp in hardpoints:
    if hp.get('Model_To_Attach'):
        models[hp['name']] = hp['Model_To_Attach']
hashes, audits = {}, {}
for key, filename in models.items():
    path = SOURCE / 'ART/MODELS' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    refs = sorted({m.decode('ascii') for m in re.findall(rb'(?i)[a-z0-9_.-]+\.(?:tga|dds)', path.read_bytes())})
    textures = {}
    for ref in refs:
        texture = SOURCE / 'ART/TEXTURES' / ref
        if not texture.exists():
            texture = texture.with_suffix('.dds')
        if not texture.exists():
            raise FileNotFoundError(texture)
        hashes[str(texture)] = hashlib.sha256(texture.read_bytes()).hexdigest()
        textures[ref] = str(texture)
    audits[key] = dict(file=str(path), bones=Bones(path), textures=textures)
projectiles = {hp['Fire_Projectile_Type']: Values(hp['Fire_Projectile_Type'])
               for hp in hardpoints if hp.get('Fire_Projectile_Type')}
audit = dict(unit=unit, hardpoints=hardpoints, models=audits,
             projectiles=projectiles, source_hashes=hashes)
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(audit, indent=2), encoding='utf-8')
print(json.dumps(dict(models={k: dict(file=v['file'], bones=len(v['bones']), textures=v['textures']) for k,v in audits.items()},
                     hardpoints=[{k: hp[k] for k in ('name','Type','Is_Targetable','Attachment_Bone','Fire_Bone_A','Model_To_Attach','Fire_Projectile_Type') if k in hp} for hp in hardpoints]), indent=2))
