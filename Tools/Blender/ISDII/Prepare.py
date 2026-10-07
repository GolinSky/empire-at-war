"""Audit the exact ISD II models and stage lossless source textures."""
import hashlib
import json
import runpy
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageOps

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data')
OUTPUT = Path('Temp/ISDIIImport')
MODELS = {'ISDIIParts': 'Empire_Imperial_SD_2.alo'}
MODELS.update({f'ISDIITLO{i:02}': f'Empire_Imperial_SD_2_TLO_{i:02}.ALO' for i in range(1, 9)})
MODELS.update({f'ISDIIICQ{i:02}': f'Empire_Imperial_SD_ICQ_{i:02}.ALO' for i in range(1, 3)})

audit_model = runpy.run_path('Tools/Blender/prepare_isd_i.py')['Audit']
textures = {p.name.lower(): p for p in (SOURCE/'ART/TEXTURES').iterdir() if p.is_file()}
import re
variants, hashes = {}, {}
for name, filename in MODELS.items():
    path = SOURCE/'ART/MODELS'/filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    audit = audit_model(path)
    folder = OUTPUT/name/'Textures'
    folder.mkdir(parents=True, exist_ok=True)
    audit['textures'] = {}
    for filename in sorted(set(n.decode('ascii') for n in re.findall(rb'[A-Za-z0-9_.-]+\.(?:dds|DDS|tga|TGA)', path.read_bytes()))):
        source = textures.get(filename.lower(), textures.get(str(Path(filename.lower()).with_suffix('.dds'))))
        if source is None:
            raise FileNotFoundError(filename)
        hashes[str(source)] = hashlib.sha256(source.read_bytes()).hexdigest()
        image = Image.open(source).convert('RGBA')
        stem = name+'_'+source.stem
        image.save(folder/(stem+'.png'))
        ImageOps.invert(image.getchannel('A')).save(folder/(stem+'_TeamMask.png'))
        audit['textures'][filename] = dict(source=str(source), stem=stem, size=list(image.size))
    audit['file'] = str(path)
    variants[name] = audit
    print(name, 'bones', len(audit['bones']), 'meshes', len(audit['objects']))
units = ET.parse(SOURCE/'XML/SpaceUnitsCapital.xml').getroot()
unit = next(e for e in units if e.get('Name') == 'T_Imperial_Star_Destroyer_2')
loadout = next(e for e in units if e.get('Name') == 'E_Imperial_Star_Destroyer_2_Fighters')
ids = {n.strip() for n in unit.findtext('HardPoints').split(',')}
hardpoints = [{ 'name': e.get('Name'), **{c.tag:(c.text or '').strip() for c in e}}
              for e in ET.parse(SOURCE/'XML/Hardpoints_Empire_Space.xml').getroot() if e.get('Name') in ids]
for filename in ['SpaceUnitsCapital.xml', 'Hardpoints_Empire_Space.xml']:
    path = SOURCE/'XML'/filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
hashes[str(SOURCE/'ART/MODELS/Empire_Imperial_SD.ALO')] = hashlib.sha256((SOURCE/'ART/MODELS/Empire_Imperial_SD.ALO').read_bytes()).hexdigest()
result = dict(variants=variants, source_hashes=hashes, hardpoints=hardpoints,
              unit={c.tag:(c.text or '').strip() for c in unit},
              loadout={c.tag:(c.text or '').strip() for c in loadout},
              abilities=ET.tostring(unit.find('Unit_Abilities_Data'), encoding='unicode'))
(OUTPUT/'BinaryAudit.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
