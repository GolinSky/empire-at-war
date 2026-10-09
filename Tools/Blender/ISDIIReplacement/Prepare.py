"""Resolve the requested XML and audit every original ALO/material/texture."""
import hashlib
import json
import re
import runpy
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageOps

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data')
OUTPUT = Path('Temp/ISDIIReplacement')
OUTPUT.mkdir(parents=True, exist_ok=True)
evidence_path = Path('Tools/Blender/ISDIIReplacement/ImportEvidence.json')
if evidence_path.exists():
    removal = json.loads(evidence_path.read_text())['obsolete']
    (OUTPUT / 'ObsoleteVisuals.json').write_text(json.dumps(removal, indent=2))
definitions = {}
for path in (SOURCE / 'XML').rglob('*.xml'):
    try:
        tree = ET.parse(path)
    except ET.ParseError:
        continue
    for element in tree.getroot():
        if element.get('Name'):
            definitions[element.get('Name')] = (element, path)
hashes = {}

def Resolve(name):
    element, path = definitions[name]
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    parent = element.findtext('Variant_Of_Existing_Type')
    values, chain = Resolve(parent.strip()) if parent else ({}, [])
    for child in element:
        # Repeated spawned-unit rows are additive within a definition.
        if child.tag.startswith(('Starting_Spawned_', 'Reserve_Spawned_')):
            values[child.tag] = [(c.text or '').strip() for c in element if c.tag == child.tag]
        else:
            values[child.tag] = (child.text or '').strip() if not len(child) else ET.tostring(child, encoding='unicode')
    return values, chain + [name]

unit, chain = Resolve('Star_Destroyer_II')
death, death_chain = Resolve(unit['Death_Clone'].split(',')[-1].strip())
hardpoints = []
for name in unit['HardPoints'].split(','):
    name = name.strip()
    data, parents = Resolve(name)
    record = dict(name=name, chain=parents, **data)
    if data.get('Fire_Projectile_Type'):
        record['projectile'], _ = Resolve(data['Fire_Projectile_Type'])
    hardpoints.append(record)
models = {'ISDIIHull': unit['Space_Model_Name'], 'ISDIIDeath': death['Space_Model_Name']}
attachments = []
for hp in hardpoints:
    filename = hp.get('Model_To_Attach')
    if not filename:
        continue
    variant = 'ISDIIParts' if hp['Type'] == 'HARD_POINT_DUMMY_ART' else 'ISDII_' + hp['name']
    models[variant] = filename
    attachments.append(dict(variant=variant, bone=hp['Attachment_Bone'], hardpoint=hp['name']))
audit_bones = runpy.run_path('Tools/Blender/prepare_isd_i.py')['Audit']
material_code = Path('Tools/Blender/MC80Independence/AuditMaterials.py').read_text().split('variants =')[0]
material_scope = {}
exec(material_code, material_scope)
audit_materials = material_scope['Audit']
textures = {p.name.lower(): p for p in (SOURCE / 'ART/TEXTURES').iterdir() if p.is_file()}
variants = {}
for variant, filename in models.items():
    path = SOURCE / 'ART/MODELS' / filename
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    audit = audit_bones(path)
    audit['binary'] = audit_materials(path)
    audit['file'] = str(path)
    audit['textures'] = {}
    target = OUTPUT / variant / 'Textures'
    target.mkdir(parents=True, exist_ok=True)
    for filename in sorted(set(n.decode('ascii') for n in re.findall(rb'[A-Za-z0-9_.-]+\.(?:dds|DDS|tga|TGA)', path.read_bytes()))):
        source = textures.get(filename.lower(), textures.get(str(Path(filename.lower()).with_suffix('.dds'))))
        if source is None:
            raise FileNotFoundError(filename)
        hashes[str(source)] = hashlib.sha256(source.read_bytes()).hexdigest()
        image = Image.open(source).convert('RGBA')
        stem = variant + '_' + source.stem
        image.save(target / (stem + '.png'))
        assert Image.open(target / (stem + '.png')).convert('RGBA').tobytes() == image.tobytes()
        ImageOps.invert(image.getchannel('A')).save(target / (stem + '_TeamMask.png'))
        audit['textures'][filename] = dict(source=str(source), stem=stem, size=list(image.size))
    variants[variant] = audit
    print(variant, filename, len(audit['bones']), 'bones', len(audit['binary']['meshes']), 'meshes')
result = dict(unit=unit, chain=chain, death=death, death_chain=death_chain, hardpoints=hardpoints,
              attachments=attachments, variants=variants, source_hashes=hashes)
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
print('Resolved', len(hardpoints), 'hardpoints;', len(attachments), 'attachments;', len(models), 'models')
