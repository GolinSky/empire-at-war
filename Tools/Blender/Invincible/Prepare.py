"""Audit Trench's Invincible and losslessly stage its referenced source textures."""
import ast
import hashlib
import json
from pathlib import Path
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
MOD = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data')
TASK = ROOT / 'Temp/InvincibleImport'


def Functions(path):
    tree = ast.parse(path.read_text(encoding='utf-8-sig'))
    tree.body = [node for node in tree.body if isinstance(node, (ast.Import, ast.ImportFrom, ast.FunctionDef))]
    scope = {}
    exec(compile(tree, str(path), 'exec'), scope)
    return scope


def Main():
    source, textures = TASK / 'Source', TASK / 'Output/Textures'
    source.mkdir(parents=True, exist_ok=True)
    textures.mkdir(parents=True, exist_ok=True)
    model = MOD / 'Art/Models/SeV_Invincible.ALO'
    binary = Functions(ROOT / 'Tools/Blender/prepare_isd_i.py')['Audit'](model)
    materials = Functions(ROOT / 'Tools/Blender/MC80Independence/AuditMaterials.py')['Audit'](model)
    references = sorted({value for mesh in materials['meshes'] for row in mesh['materials'] for value in row['properties'].values()
                         if isinstance(value, str) and value.lower().endswith(('.dds', '.tga'))})
    available = {p.stem.lower(): p for p in (MOD / 'Art/Textures').iterdir() if p.is_file()}
    names = {'sev_invincible': 'Invincible_Hull_Albedo', 'sev_providence_b': 'Invincible_Hull_Normal',
             'sev_providence_l': 'Invincible_Lights_Emissive', 'w_blast00': 'Invincible_WeaponFlash_Albedo'}
    files = [model, MOD / 'XML/Units_hero_cis_trench.xml', MOD / 'XML/Units_space_cis_providence.xml']
    info = {}
    for reference in references:
        path = available[Path(reference).stem.lower()]
        files.append(path)
        stem = names[path.stem.lower()]
        image = Image.open(path).convert('RGBA')
        target = textures / (stem + '.png')
        image.save(target)
        assert Image.open(target).convert('RGBA').tobytes() == image.tobytes()
        record = dict(source=str(path), stem=stem, size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
        if stem == 'Invincible_Hull_Albedo':
            mask = image.getchannel('A')
            mask.save(textures / 'Invincible_Hull_TeamMask.png')
            record['team_mask'] = 'Invincible_Hull_TeamMask'
            record['team_mask_coverage'] = sum(value > 0 for value in mask.tobytes()) / (image.width * image.height)
        info[reference] = record
    for path in files:
        shutil.copy2(path, source / path.name)
    report = dict(source_unit='Admiral_Trench', source_model=str(model), model=binary, materials=materials, textures=info,
                  scale_ratio=.78 / .28, source_hashes={str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in files})
    (TASK / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(dict(bones=[b['name'] for b in binary['bones']], meshes=[dict(name=m['name'], hidden=m['hidden'],
          slots=[(d['shader'], d['triangleCount']) for d in m['materials']]) for m in materials['meshes']], textures=info), indent=2))


if __name__ == '__main__':
    Main()
