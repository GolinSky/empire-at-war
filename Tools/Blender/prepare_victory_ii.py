"""Audit AOTR Victory II model, loadout and source provenance."""
import hashlib
import json
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data')
OUTPUT = Path('Temp/VictoryIIImport')


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
            bones.append(dict(name=name, parent_index=parent, visible=visible,
                              matrix=list(struct.unpack('<12f', stream.read(48)))))
    return bones


def Definition(path, name):
    return next(e for e in ET.parse(path).getroot() if e.attrib.get('Name') == name)


def Values(element):
    return {c.tag: (c.text or '').strip() for c in element}


OUTPUT.mkdir(parents=True, exist_ok=True)
unit = Definition(SOURCE / 'XML/SpaceUnitsCruisers.xml', 'T_Victory_Star_Destroyer_2')
advanced = Definition(SOURCE / 'XML/SpaceUnitsCruisers.xml', 'E_Victory_Star_Destroyer_2_Fighters')
hardpoints = ET.parse(SOURCE / 'XML/Hardpoints_Empire_Space.xml').getroot()
names = {n.strip() for n in unit.findtext('HardPoints').split(',')}
selected = [{'name': e.attrib['Name'], **Values(e)} for e in hardpoints if e.attrib.get('Name') in names]
models = {'VictoryII': 'EV_VSD_II.ALO', 'TIEInterceptor': 'EV_TIE_INTERCEPTOR_E.ALO'}
for hp in selected:
    if hp.get('Model_To_Attach'):
        models[hp['Attachment_Bone']] = hp['Model_To_Attach']
hashes = {}
audits = {}
for key, filename in models.items():
    path = SOURCE / 'ART/MODELS' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    audits[key] = {'file': str(path), 'bones': Audit(path)}
projectiles = {}
projectile_names = {hp['Fire_Projectile_Type'] for hp in selected if hp.get('Fire_Projectile_Type')}
definitions = {}
for path in (SOURCE / 'XML').glob('*.xml'):
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError:
        continue
    for element in root:
        if element.attrib.get('Name'):
            definitions[element.attrib['Name']] = Values(element)
for name in projectile_names:
    chain = []
    current = name
    while current in definitions:
        if current in chain:
            raise ValueError('Cyclic projectile inheritance: ' + current)
        chain.append(current)
        current = definitions[current].get('Variant_Of_Existing_Type')
    resolved = {}
    for ancestor in reversed(chain):
        resolved.update(definitions[ancestor])
    projectiles[name] = dict(raw=definitions[name], inheritance=chain, resolved=resolved)
squadron = Values(Definition(SOURCE / 'XML/Squadrons.xml', 'E_TIE_Interceptor_S'))
conversion = OUTPUT / 'ConversionReport.json'
if conversion.exists():
    for model in json.loads(conversion.read_text(encoding='utf-8')):
        for material in model['materials']:
            for key in ('base', 'normal'):
                filename = material.get(key)
                if filename and filename != 'None':
                    path = SOURCE / 'ART/TEXTURES' / filename
                    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
for filename in ('SpaceUnitsCruisers.xml', 'Hardpoints_Empire_Space.xml', 'PROJECTILES_SPACE.XML', 'Squadrons.xml'):
    path = SOURCE / 'XML' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
report = dict(models=audits, hardpoints=selected, projectiles=projectiles,
              unit=Values(unit), advanced_unit=Values(advanced),
              abilities=ET.tostring(unit.find('Unit_Abilities_Data'), encoding='unicode'),
              squadron=squadron, source_hashes=hashes)
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('Audited', {name:len(data['bones']) for name,data in audits.items()})
for hp in selected:
    print(hp['name'], hp.get('Attachment_Bone'), hp.get('Is_Targetable'), hp.get('Health'))
