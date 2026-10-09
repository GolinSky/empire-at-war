"""Resolve the installed Imperial Venator XML and audit unchanged source art."""
import ast
import hashlib
import json
import runpy
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image, ImageOps

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data')
OUTPUT = Path('Temp/ImperialVenatorImport')
OUTPUT.mkdir(parents=True, exist_ok=True)
bone_audit = runpy.run_path('Tools/Blender/prepare_isd_i.py')['Audit']
module = ast.parse(Path('Tools/Blender/Tector/AuditMaterials.py').read_text())
namespace = {}
exec(compile(ast.Module(body=[n for n in module.body if isinstance(n, (ast.Import, ast.ImportFrom, ast.FunctionDef))], type_ignores=[]), 'BinaryAudit', 'exec'), namespace)
binary_audit = namespace['Audit']
definitions, elements, files = {}, {}, {}
for path in sorted((SOURCE / 'XML').rglob('*.xml')):
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError:
        continue
    for element in root:
        name = element.get('Name')
        if name:
            definitions[name] = {c.tag: (c.text or '').strip() for c in element}
            elements[name], files[name] = element, path


def Resolve(name):
    chain, current = [], name
    while current:
        if current in chain:
            raise ValueError('Cyclic inheritance: ' + current)
        if current not in definitions:
            raise KeyError(current)
        chain.append(current)
        current = definitions[current].get('Variant_Of_Existing_Type')
    values = {}
    for ancestor in reversed(chain):
        values.update(definitions[ancestor])
    return dict(inheritance=chain, resolved=values)


unit = Resolve('Venator_Empire')
assert unit['resolved']['Affiliation'] == 'Empire'
assert unit['resolved']['Space_Model_Name'] == 'Empire_Venator_Star_Destroyer.ALO'
registered = ET.parse(SOURCE / 'XML/GameObjectFiles.xml').getroot()
assert any((e.text or '').strip().replace('\\', '/').lower() == 'units/space/units_space_empire_venator.xml' for e in registered.iter('File'))
hardpoints = [dict(name=n.strip(), **Resolve(n.strip())) for n in unit['resolved']['HardPoints'].split(',') if n.strip()]
models = {'ImperialVenator': unit['resolved']['Space_Model_Name']}
for hp in hardpoints:
    attached = hp['resolved'].get('Model_To_Attach')
    if attached:
        models['ImperialVenator_' + hp['name'].removeprefix('HP_Venator_')] = attached
death = Resolve(unit['resolved']['Death_Clone'].split(',')[-1].strip())
models['ImperialVenatorDeath'] = death['resolved']['Space_Model_Name']
texture_files = {p.name.lower(): p for p in (SOURCE / 'ART/TEXTURES').iterdir() if p.is_file()}
variants, binaries, hashes = {}, {}, {}
for name, filename in models.items():
    path = SOURCE / 'ART/MODELS' / filename
    audit, binary = bone_audit(path), binary_audit(path)
    audit['file'], audit['binary'], audit['textures'] = str(path), binary, {}
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    target = OUTPUT / name / 'Textures'
    target.mkdir(parents=True, exist_ok=True)
    refs = {v for m in binary['meshes'] for slot in m['materials'] for k, v in slot['properties'].items() if k.endswith('Texture') and isinstance(v, str) and v.lower().endswith(('.tga', '.dds'))}
    for ref in sorted(refs):
        key = ref.lower()
        if key not in texture_files:
            key = str(Path(key).with_suffix('.dds'))
        texture = texture_files[key]
        hashes[str(texture)] = hashlib.sha256(texture.read_bytes()).hexdigest()
        image = Image.open(texture).convert('RGBA')
        stem = name + '_' + texture.stem
        image.save(target / (stem + '.png'))
        ImageOps.invert(image.getchannel('A')).save(target / (stem + '_TeamMask.png'))
        audit['textures'][ref] = dict(source=str(texture), stem=stem, size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
    variants[name], binaries[name] = audit, binary
    print(name, filename, len(audit['bones']), 'bones', len(binary['meshes']), 'meshes', flush=True)
for name in unit['inheritance'] + death['inheritance'] + [a for hp in hardpoints for a in hp['inheritance']]:
    path = files[name]
    hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
abilities = next(elements[n].find('Unit_Abilities_Data') for n in unit['inheritance'] if elements[n].find('Unit_Abilities_Data') is not None)
garrison = [{c.tag: (c.text or '').strip()} for c in elements['Venator_Empire'] if 'Spawned_Units' in c.tag]
report = dict(unit=unit, hardpoints=hardpoints, death_clone=death, garrison=garrison, abilities=ET.tostring(abilities, encoding='unicode'), variants=variants, source_hashes=hashes)
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
(OUTPUT / 'BinaryMaterialAudit.json').write_text(json.dumps(binaries, indent=2), encoding='utf-8')
print('Inheritance:', unit['inheritance'])
print('Source stats:', {k: v for k, v in unit['resolved'].items() if any(s in k for s in ('Health', 'Shield', 'Speed', 'Cost', 'Build', 'Population'))})
print('Garrison:', garrison)
