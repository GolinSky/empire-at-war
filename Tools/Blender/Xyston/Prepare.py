"""Audit the Xyston hull and superlaser cannon ALOs and stage their textures as lossless PNGs.

Run from the repository root: python Tools/Blender/Xyston/Prepare.py
Writes Temp/XystonImport/SourceAudit.json and Temp/XystonImport/<Variant>/Textures/*.png.
ISD I superstructure and turret attachments are reused from the ISD I Remake import (same source mod).
"""
import ast
import hashlib
import json
import re
from pathlib import Path
from PIL import Image, ImageOps

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data/Art')
OUTPUT = Path('Temp/XystonImport')
VARIANTS = {
    'Xyston': SOURCE / 'Models/Empire_Xyston_SD.ALO',
    'XystonCannon': SOURCE / 'Models/Empire_Xyston_SD_Cannon.ALO',
}
# Bones the unit XML attaches art or hardpoints to (Units_Space_Empire_Star_Destroyer_Xyston.xml).
REQUIRED_BONES = {
    'Xyston': ['HP_Cannon_Attach', 'Decal_Attach_04', 'TA_ISD1_ICD_01', 'TA_ISD1_ICD_02'] +
              [f'TA_ISD1_TLD_{i:02}' for i in range(1, 7)] + ['Ta_ISD_ICQ_01', 'Ta_ISD_ICQ_02'] +
              [f'IC_{i:02}' for i in range(1, 7)] + [f'TL_{i:02}' for i in range(1, 6)] +
              ['HP_SG', 'HP_trac_bone_00'],
    'XystonCannon': ['Turret', 'Barrel', 'FP_01'],
}


def LoadFunctions(path):
    tree = ast.parse(Path(path).read_text(encoding='utf-8-sig'))
    module = ast.Module(body=[n for n in tree.body if isinstance(n, (ast.Import, ast.ImportFrom, ast.FunctionDef))], type_ignores=[])
    scope = {}
    exec(compile(module, path, 'exec'), scope)
    return scope


def Main():
    binary = LoadFunctions('Tools/Blender/prepare_isd_i.py')['Audit']
    materials = LoadFunctions('Tools/Blender/MC80Independence/AuditMaterials.py')['Audit']
    files = {p.name.lower(): p for p in (SOURCE / 'Textures').iterdir() if p.is_file()}
    audits, missing, absent_bones = {}, {}, {}
    for variant, path in VARIANTS.items():
        audit = binary(path)
        audit['file'] = str(path)
        audit['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
        audit['material_audit'] = materials(path)
        audit['textures'] = {}
        names = {b['name'].lower() for b in audit['bones']}
        absent_bones[variant] = [b for b in REQUIRED_BONES[variant] if b.lower() not in names]
        target = OUTPUT / variant / 'Textures'
        target.mkdir(parents=True, exist_ok=True)
        refs = sorted({value for mesh in audit['material_audit']['meshes'] for material in mesh['materials']
                       for value in material['properties'].values()
                       if isinstance(value, str) and re.search(r'(?i)\.(dds|tga)$', value)})
        for name in refs:
            key = name.lower()
            alternates = [key, str(Path(key).with_suffix('.dds')), str(Path(key).with_suffix('.tga'))]
            found = next((files[k] for k in alternates if k in files), None)
            if found is None:
                missing.setdefault(variant, []).append(name)
                continue
            image = Image.open(found).convert('RGBA')
            stem = variant + '_' + found.stem
            image.save(target / (stem + '.png'))
            assert Image.open(target / (stem + '.png')).convert('RGBA').tobytes() == image.tobytes()
            ImageOps.invert(image.getchannel('A')).save(target / (stem + '_TeamMask.png'))
            audit['textures'][name] = dict(source=str(found), sha256=hashlib.sha256(found.read_bytes()).hexdigest(), stem=stem,
                                           size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
        audits[variant] = audit
    (OUTPUT / 'SourceAudit.json').write_text(json.dumps(dict(variants=audits, missing=missing, absent_bones=absent_bones), indent=2), encoding='utf-8')
    print(json.dumps({v: dict(bones=len(a['bones']), meshes=len(a['material_audit']['meshes']),
                              shaders=sorted({m['shader'] for mesh in a['material_audit']['meshes'] for m in mesh['materials']}),
                              multiSlot=[m['name'] for m in a['material_audit']['meshes'] if len(m['materials']) > 1],
                              textures=len(a['textures'])) for v, a in audits.items()}
                     | dict(missing=missing, absent_bones=absent_bones), indent=1))


if __name__ == '__main__':
    Main()
