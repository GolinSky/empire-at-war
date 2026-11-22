"""Audit both Viscount ALOs and stage their textures as lossless PNGs.

Run from the repository root: python Tools/Blender/Viscount/Prepare.py
Writes Temp/ViscountImport/SourceAudit.json and Temp/ViscountImport/<Variant>/Textures/*.png.
"""
import ast
import hashlib
import json
import re
from pathlib import Path
from PIL import Image, ImageOps

WORKSHOP = Path('D:/SteamLibrary/steamapps/workshop/content/32470')
OUTPUT = Path('Temp/ViscountImport')
VARIANTS = {
    'ViscountPrototype': WORKSHOP / '1382582782/Data/Art/Models/RV_Viscount.alo',
    'ViscountDreadnought': WORKSHOP / '1770851727/Data/Art/Models/Rebel_Mon_Calamari_Viscount.ALO',
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
    audits, missing = {}, {}
    for variant, path in VARIANTS.items():
        textures_dir = path.parents[1] / 'Textures'
        files = {p.name.lower(): p for p in textures_dir.iterdir() if p.is_file()}
        audit = binary(path)
        audit['file'] = str(path)
        audit['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
        audit['material_audit'] = materials(path)
        audit['textures'] = {}
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
    (OUTPUT / 'SourceAudit.json').write_text(json.dumps(dict(variants=audits, missing=missing), indent=2), encoding='utf-8')
    print(json.dumps({v: dict(bones=len(a['bones']), meshes=len(a['material_audit']['meshes']),
                              multiSlot=[m['name'] for m in a['material_audit']['meshes'] if len(m['materials']) > 1],
                              textures=len(a['textures'])) for v, a in audits.items()} | dict(missing=missing), indent=1))


if __name__ == '__main__':
    Main()
