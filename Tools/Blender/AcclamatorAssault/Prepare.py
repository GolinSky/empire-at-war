"""Read-only AOTR audit for the Imperial Acclamator Assault Ship."""
import hashlib
import json
import re
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data')
OUTPUT = Path('Temp/AcclamatorAssaultImport')


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
            definitions[element.attrib['Name']] = element
unit = definitions['E_Acclamator_Assault_Ship_Fighters']
chain, current = [], unit
while current is not None:
    chain.append(current.attrib['Name'])
    current = definitions.get(current.findtext('Variant_Of_Existing_Type'))
resolved = {}
for name in reversed(chain):
    resolved.update(Values(definitions[name]))
hardpoints = [dict(name=name.strip(), **Values(definitions[name.strip()]))
              for name in resolved['HardPoints'].split(',') if name.strip()]
models = {'AcclamatorAssault': resolved['Space_Model_Name']}
for hp in hardpoints:
    if hp.get('Model_To_Attach'):
        models['AcclamatorAssaultTurret' + hp['name'][-2:]] = hp['Model_To_Attach']
hashes, audits = {}, {}
for name, filename in models.items():
    path = SOURCE / 'ART/MODELS' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    textures = {}
    for ref in sorted({m.decode('ascii') for m in re.findall(rb'(?i)[a-z0-9_.-]+\.(?:tga|dds)', path.read_bytes())}):
        texture = SOURCE / 'ART/TEXTURES' / ref
        if not texture.exists():
            texture = texture.with_suffix('.dds')
        if not texture.exists():
            raise FileNotFoundError(texture)
        textures[ref] = str(texture)
        hashes[str(texture)] = hashlib.sha256(texture.read_bytes()).hexdigest()
    audits[name] = dict(file=str(path), bones=Bones(path), textures=textures)
projectiles = {}
for name in {hp['Fire_Projectile_Type'] for hp in hardpoints if hp.get('Fire_Projectile_Type')}:
    inherited, current = [], name
    while current in definitions:
        inherited.append(current)
        current = definitions[current].findtext('Variant_Of_Existing_Type')
    projectile = {}
    for ancestor in reversed(inherited):
        projectile.update(Values(definitions[ancestor]))
    projectiles[name] = dict(inheritance=inherited, resolved=projectile)
report = dict(models=audits,
              unit=resolved, inheritance=chain, hardpoints=hardpoints,
              projectiles=projectiles, source_hashes=hashes,
              abilities=[ET.tostring(definitions[n].find('Unit_Abilities_Data'), encoding='unicode')
                         for n in chain if definitions[n].find('Unit_Abilities_Data') is not None],
              complement={n:[dict(tag=c.tag,value=c.text.strip()) for c in definitions[n]
                             if 'Spawned_Units' in c.tag] for n in chain})
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
template = Path('Tools/Blender/AcclamatorAssault/Convert.py').read_text()
code = template.replace('__AUDIT__', 'json.loads(' + repr(json.dumps({'models': audits})) + ')')
(OUTPUT / 'ConvertRequest.json').write_text(json.dumps(dict(tool='execute_blender_code',
    arguments=dict(code=code, user_prompt='Add this unit to empire'))), encoding='utf-8')
print(json.dumps(dict(models={n:dict(file=a['file'],bones=len(a['bones']),root=a['bones'][0]) for n,a in audits.items()},
                      unit={k:v for k,v in resolved.items()
                      if k in ('Build_Cost_Credits','Build_Time_Seconds','Population_Value','Tech_Level','Unit_Abilities_Data')},
                      hardpoints=[{k:v for k,v in h.items() if k in ('name','Attachment_Bone','Fire_Bone_A','Health','Fire_Pulse_Count','Fire_Pulse_Delay_Seconds','Fire_Min_Recharge_Seconds','Fire_Max_Recharge_Seconds','Fire_Range_Distance')} for h in hardpoints],
                      abilities=report['abilities'], complement=report['complement']), indent=2))
