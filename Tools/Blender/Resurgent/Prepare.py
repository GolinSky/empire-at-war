"""Audit the Resurgent hull and its turret ALOs and stage their textures as lossless PNGs.

Run from the repository root: python Tools/Blender/Resurgent/Prepare.py
Writes Temp/ResurgentImport/SourceAudit.json and Temp/ResurgentImport/<Variant>/Textures/*.png.
Source: Remake Reforged 5.2 (Workshop 3372762649), Capital_Resurgent.xml + HP_TOR_Resurgent.xml.
Turret copies (_01.._18 etc.) are byte-identical, so one variant per turret model is converted.
"""
import ast
import hashlib
import json
import re
from pathlib import Path
from PIL import Image, ImageOps

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/3372762649/Data/Art')
OUTPUT = Path('Temp/ResurgentImport')
VARIANTS = {
    'Resurgent': SOURCE / 'Models/First_Order_Resurgent.alo',
    'ResurgentSextupleTurret': SOURCE / 'Models/First_Order_Resurgent_STL_01.alo',
    'ResurgentLightTurret': SOURCE / 'Models/First_Order_Resurgent_LC_01.alo',
    'ResurgentQuadIonTurret': SOURCE / 'Models/First_Order_Resurgent_QTL_01.alo',
    'ResurgentMissileTurret': SOURCE / 'Models/First_Order_Resurgent_ML_01.alo',
    'ResurgentQuadTurret': SOURCE / 'Models/Neutral_Republic_Secutor_QTL_01.ALO',
}
# Bones HP_TOR_Resurgent.xml attaches turrets or hardpoints to.
REQUIRED_BONES = {
    'Resurgent': [f'HSTL_{i:02}' for i in range(1, 9)] + [f'LQTL_{i:02}' for i in range(1, 9)] +
                 [f'LDTL_{i:02}' for i in range(1, 11)] + [f'MIC_{i:02}' for i in range(1, 9)] +
                 [f'QHIC_{i:02}' for i in range(1, 5)] + [f'ML_{i:02}' for i in range(1, 7)] +
                 ['DB_RKT_01', 'DB_RKT_02', 'DB_RKT_01_Fire', 'DB_RKT_02_Fire'] + [f'QL_{i:02}' for i in range(1, 19)] +
                 ['S_01', 'S_02', 'HP_E1', 'HP_E2', 'HP_E3', 'HP_TRAC_BONE_00', 'SPAWN_01', 'HP_SG'],
    'ResurgentSextupleTurret': ['FP_01'],
    'ResurgentLightTurret': ['FP_01', 'FP_03'],
    'ResurgentQuadIonTurret': ['FP_01'],
    'ResurgentMissileTurret': ['FP_01'],
    'ResurgentQuadTurret': ['FP_01'],
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
