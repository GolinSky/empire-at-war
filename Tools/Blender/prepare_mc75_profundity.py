"""Audit the installed AOTR MC75 and stage lossless textures; leave inputs intact."""
import hashlib
import json
import struct
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image
import colorsys

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data')
OUTPUT = Path('Temp/MC75ProfundityImport')
TEXTURES = {
    'Profundity.dds': 'MC75Profundity_Engines_Albedo',
    'Profundity_B.dds': 'MC75Profundity_Engines_Normal',
    'rv_Profundity_diff.dds': 'MC75Profundity_Hull_Albedo',
    'rv_Profundity_B.dds': 'MC75Profundity_Hull_Normal',
    'Lightmesh_Windows_For_Big_Stuff.dds': 'MC75Profundity_Windows_Emissive',
    'P_ISD_ENG.dds': 'MC75Profundity_EngineGlow_Emissive',
    'Profundity_Ambient_Light.dds': 'MC75Profundity_Ambient_Emissive',
}


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
    objects = []
    for kind, payload in Chunks(path.read_bytes()):
        if kind in (0x400, 0x1300):
            _, name = next(Chunks(payload))
            objects.append(dict(kind=hex(kind), name=name.rstrip(b'\0').decode('ascii')))
    return dict(bones=bones, objects=objects)


(OUTPUT / 'Textures').mkdir(parents=True, exist_ok=True)
model = SOURCE / 'ART/MODELS/RV_Profundity.ALO'
hashes = {str(model): hashlib.sha256(model.read_bytes()).hexdigest()}
audit = Audit(model)
for source_name, target in TEXTURES.items():
    path = SOURCE / 'ART/TEXTURES' / source_name
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    image = Image.open(path).convert('RGBA')
    output = OUTPUT / 'Textures' / (target + '.png')
    image.save(output)
    assert Image.open(output).convert('RGBA').tobytes() == image.tobytes()
    if target.endswith(('Hull_Albedo', 'Engines_Albedo')):
        # This model's alpha never exceeds 115; inverted alpha would recolor all
        # neutral plating. Select its authored cyan and red paint explicitly.
        mask = Image.new('L', image.size)
        values = []
        for red, green, blue, _ in image.getdata():
            hue, saturation, value = colorsys.rgb_to_hsv(red / 255, green / 255, blue / 255)
            values.append(255 if saturation > .25 and value > .12 and (.45 < hue < .65 or hue < .06 or hue > .94) else 0)
        mask.putdata(values)
        mask.save(OUTPUT / 'Textures' / (target.replace('Albedo', 'TeamMask') + '.png'))
        print(target, image.size, 'alpha', image.getchannel('A').getextrema(), 'mask coverage', sum(v > 0 for v in mask.tobytes()) / len(mask.tobytes()))
root = ET.parse(SOURCE / 'XML/Hardpoints_Rebel_Space.xml').getroot()
hardpoints = [{**{'name': element.attrib['Name']}, **{child.tag: child.text.strip() if child.text else '' for child in element}} for element in root if element.attrib.get('Name', '').startswith('HP_MC75_Profundity')]
projectiles = {}
for path in (SOURCE / 'XML').glob('*.xml'):
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError:
        continue
    for element in root:
        if element.attrib.get('Name') in {hp['Fire_Projectile_Type'] for hp in hardpoints if hp.get('Fire_Projectile_Type')}:
            projectiles[element.attrib['Name']] = {c.tag: c.text.strip() if c.text else '' for c in element}
report = dict(source_hashes=hashes, audit=audit, hardpoints=hardpoints, projectiles=projectiles)
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('SOURCE', len(audit['bones']), 'bones', len(audit['objects']), 'objects; root', audit['bones'][0])
for hp in hardpoints:
    print(hp['name'], hp.get('Attachment_Bone'), hp.get('Fire_Bone_A'), hp.get('Fire_Projectile_Type'), hp.get('Health'), hp.get('Full_Salvo_Weapon_Delay_Multiplier'))
