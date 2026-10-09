"""Resolve the installed Remake ISD I and audit its original ALO materials."""
import ast
import hashlib
import json
import re
import struct
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageOps

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data')
OUTPUT = Path('Temp/ISDIRemakeImport')


def LoadFunctions(path):
    tree = ast.parse(Path(path).read_text(encoding='utf-8-sig'))
    functions = ast.Module(body=[node for node in tree.body if isinstance(node, (ast.Import, ast.ImportFrom, ast.FunctionDef))], type_ignores=[])
    scope = {}
    exec(compile(functions, path, 'exec'), scope)
    return scope


def Main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    definitions, files = {}, {}
    for path in (SOURCE / 'Xml').rglob('*'):
        if path.suffix.lower() != '.xml':
            continue
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            continue
        for element in root:
            name = element.get('Name')
            if name:
                definitions[name] = element
                files[name] = path

    def Resolve(name):
        chain, current = [], name
        while current:
            if current in chain:
                raise ValueError('Cyclic inheritance: ' + current)
            element = definitions[current]
            chain.append(current)
            current = element.findtext('Variant_Of_Existing_Type')
            current = current.strip() if current else None
        values = {}
        for ancestor in reversed(chain):
            element = definitions[ancestor]
            for tag in {child.tag for child in element}:
                values[tag] = [ET.tostring(child, encoding='unicode') if len(child) else (child.text or '').strip() for child in element if child.tag == tag]
        return dict(inheritance=chain, values=values)

    unit = Resolve('Star_Destroyer')
    hardpoints = [dict(name=name.strip(), **Resolve(name.strip())) for name in unit['values']['HardPoints'][0].split(',') if name.strip()]
    models = {'ISDI': unit['values']['Space_Model_Name'][0]}
    attachments = []
    for hp in hardpoints:
        value = hp['values']
        if value.get('Model_To_Attach', [''])[0]:
            key = 'ISDIPart' + str(len(attachments)).zfill(2)
            models[key] = value['Model_To_Attach'][0]
            attachments.append(dict(variant=key, hardpoint=hp['name'], bone=value['Attachment_Bone'][0]))
    death = Resolve(unit['values']['Death_Clone'][0].split(',')[1].strip())
    models['ISDIWreck'] = death['values']['Space_Model_Name'][0]
    binary = LoadFunctions('Tools/Blender/prepare_isd_i.py')['Audit']
    materials = LoadFunctions('Tools/Blender/MC80Independence/AuditMaterials.py')['Audit']
    texture_files = {path.name.lower(): path for path in (SOURCE/'Art/Textures').iterdir() if path.is_file()}
    hashes, audits = {}, {}
    for variant, filename in models.items():
        path = SOURCE / 'Art/Models' / filename
        audit = binary(path)
        audit['file'] = str(path)
        audit['material_audit'] = materials(path)
        audit['textures'] = {}
        hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
        target = OUTPUT / variant / 'Textures'
        target.mkdir(parents=True, exist_ok=True)
        refs = sorted(set(name.decode('ascii') for name in re.findall(rb'(?i)[a-z0-9_.-]+\.(?:dds|tga)', path.read_bytes())))
        for name in refs:
            key = name.lower()
            if key not in texture_files:
                key = str(Path(key).with_suffix('.dds'))
            texture = texture_files[key]
            hashes[str(texture)] = hashlib.sha256(texture.read_bytes()).hexdigest()
            image = Image.open(texture).convert('RGBA')
            stem = variant + '_' + texture.stem
            image.save(target / (stem + '.png'))
            assert Image.open(target/(stem+'.png')).convert('RGBA').tobytes() == image.tobytes()
            ImageOps.invert(image.getchannel('A')).save(target/(stem+'_TeamMask.png'))
            audit['textures'][name] = dict(source=str(texture), stem=stem, size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
        audits[variant] = audit
    projectiles = {hp['values']['Fire_Projectile_Type'][0]: Resolve(hp['values']['Fire_Projectile_Type'][0]) for hp in hardpoints if 'Fire_Projectile_Type' in hp['values']}
    for item in [unit, death, *hardpoints, *projectiles.values()]:
        for name in item['inheritance']:
            path = files[name]
            hashes[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    report = dict(unit=unit, death=death, hardpoints=hardpoints, attachments=attachments, variants=audits, projectiles=projectiles, source_hashes=hashes)
    (OUTPUT/'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(dict(inheritance=unit['inheritance'], attachments=attachments, stats={k:v for k,v in unit['values'].items() if any(s in k for s in ('Health','Shield','Speed','Cost','Spawned','Population'))}, hardpoints=[dict(name=hp['name'], **{k:v for k,v in hp['values'].items() if k in ('Type','Attachment_Bone','Damage_Bone','Fire_Bone_A','Fire_Projectile_Type','Model_To_Attach','Health','Fire_Pulse_Count')}) for hp in hardpoints], meshes={key:len(value['material_audit']['meshes']) for key,value in audits.items()}), indent=2))


if __name__ == '__main__':
    Main()
