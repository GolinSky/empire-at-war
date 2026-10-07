"""Audit the installed Tector model, attachments, weapons and source hashes."""
import hashlib
import json
import re
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data')
OUTPUT = Path('Temp/TectorImport')


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


def Values(element):
    return {c.tag: (c.text or '').strip() for c in element}


OUTPUT.mkdir(parents=True, exist_ok=True)
(OUTPUT / 'Output/Textures').mkdir(parents=True, exist_ok=True)
definitions, elements, files = {}, {}, {}
for path in (SOURCE / 'XML').glob('*'):
    if path.suffix.lower() != '.xml':
        continue
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError:
        continue
    for element in root:
        name = element.attrib.get('Name')
        if name:
            definitions[name] = Values(element)
            elements[name] = element
            files[name] = path


def Resolve(name):
    chain, current = [], name
    while current in definitions:
        if current in chain:
            raise ValueError('Cyclic inheritance: ' + current)
        chain.append(current)
        current = definitions[current].get('Variant_Of_Existing_Type')
    resolved = {}
    for ancestor in reversed(chain):
        resolved.update(definitions[ancestor])
    return dict(inheritance=chain, resolved=resolved)


unit_name = 'E_Tector_Star_Destroyer'
unit = Resolve(unit_name)['resolved']
names = [name.strip() for name in unit['HardPoints'].split(',') if name.strip()]
hardpoints = [dict(name=name, **Resolve(name)['resolved']) for name in names]
models = {'Tector': unit['Space_Model_Name']}
for hp in hardpoints:
    filename = hp.get('Model_To_Attach')
    if filename and filename not in models.values():
        models['TectorTurret' + str(len(models)).zfill(2)] = filename
hashes, audits = {}, {}
for key, filename in models.items():
    path = SOURCE / 'ART/MODELS' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    refs = sorted({m.decode('ascii') for m in re.findall(
        rb'(?i)\x02.([a-z0-9_.-]+\.(?:tga|dds))\x00', path.read_bytes())})
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
projectiles = {hp['Fire_Projectile_Type']: Resolve(hp['Fire_Projectile_Type'])
               for hp in hardpoints if hp.get('Fire_Projectile_Type')}
for name in [unit_name] + names + [a for p in projectiles.values() for a in p['inheritance']]:
    path = files[name]
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
report = dict(models=audits, hardpoints=hardpoints, projectiles=projectiles,
              unit=unit, abilities=ET.tostring(next(elements[n].find('Unit_Abilities_Data') for n in Resolve(unit_name)['inheritance'] if elements[n].find('Unit_Abilities_Data') is not None), encoding='unicode'),
              source_hashes=hashes)
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(dict(models={key: dict(file=data['file'], bones=len(data['bones'])) for key, data in audits.items()},
                     unit={k: v for k, v in unit.items() if any(s in k.lower() for s in ('spawn', 'health', 'shield', 'cost', 'speed', 'build', 'population'))},
                     abilities=report['abilities'],
                     hardpoints=[{k: v for k, v in hp.items() if k in ('name', 'Attachment_Bone', 'Model_To_Attach', 'Fire_Bone_A', 'Fire_Projectile_Type', 'Is_Targetable', 'Type', 'Damage', 'Fire_Pulse_Count', 'Fire_Cone_Width')} for hp in hardpoints]), indent=2))
