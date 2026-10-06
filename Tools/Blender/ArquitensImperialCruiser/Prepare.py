"""Read-only AOTR source audit for the Empire Arquitens import."""
import hashlib
import json
import re
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data')
OUTPUT = Path('Temp/ArquitensImperialCruiserImport')


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
unit = next(e for e in ET.parse(SOURCE / 'XML/SpaceUnitsFrigates.xml').getroot()
            if e.attrib.get('Name') == 'E_Arquitens_Light_Cruiser')
names = {name.strip() for name in unit.findtext('Hardpoints').split(',')}
hardpoints = [dict(name=e.attrib['Name'], **Values(e))
              for e in ET.parse(SOURCE / 'XML/Hardpoints_Empire_Space.xml').getroot()
              if e.attrib.get('Name') in names]
models = {'ArquitensImperialCruiser': unit.findtext('Space_Model_Name')}
for index, hp in enumerate(hardpoints):
    if hp.get('Model_To_Attach'):
        models['ArquitensImperialCruiserTurret' + str(index + 1).zfill(2)] = hp['Model_To_Attach']
hashes, audits = {}, {}
for key, filename in models.items():
    path = SOURCE / 'ART/MODELS' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    refs = sorted({m.decode('ascii') for m in re.findall(
        rb'(?i)[a-z0-9_.-]+\.(?:tga|dds)', path.read_bytes())})
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
definitions = {}
for path in (SOURCE / 'XML').glob('*'):
    if path.suffix.lower() != '.xml':
        continue
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError:
        continue
    for element in root:
        if element.attrib.get('Name'):
            definitions[element.attrib['Name']] = Values(element)
projectiles = {}
for name in {hp['Fire_Projectile_Type'] for hp in hardpoints if hp.get('Fire_Projectile_Type')}:
    chain, current = [], name
    while current in definitions:
        if current in chain:
            raise ValueError('Cyclic projectile inheritance: ' + current)
        chain.append(current)
        current = definitions[current].get('Variant_Of_Existing_Type')
    resolved = {}
    for ancestor in reversed(chain):
        resolved.update(definitions[ancestor])
    projectiles[name] = dict(inheritance=chain, resolved=resolved)
for filename in ('SpaceUnitsFrigates.xml', 'Hardpoints_Empire_Space.xml', 'PROJECTILES_SPACE.XML'):
    path = SOURCE / 'XML' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
report = dict(models=audits, hardpoints=hardpoints, projectiles=projectiles,
              unit=Values(unit), abilities=ET.tostring(unit.find('Unit_Abilities_Data'), encoding='unicode'),
              source_hashes=hashes)
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps({name: dict(bones=len(data['bones']), hash=hashes[data['file']],
                            root=data['bones'][0]) for name, data in audits.items()}, indent=2))
print(json.dumps(projectiles, indent=2))
