"""Audit RaW's Dreadnaught and twelve XML-attached turrets; preserve source files."""
import hashlib
import json
from pathlib import Path
import runpy
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
MOD = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972')
TASK = ROOT / 'Temp/HeavyDreadnoughtImport'


def Main():
    functions = runpy.run_path(str(ROOT / 'Tools/Blender/Carrack/Prepare.py'))['Functions']
    audit = functions(ROOT / 'Tools/Blender/prepare_isd_i.py')['Audit']
    materials = functions(ROOT / 'Tools/Blender/MC80Independence/AuditMaterials.py')['Audit']
    source, textures = TASK / 'Source', TASK / 'Output/Textures'
    source.mkdir(parents=True, exist_ok=True)
    textures.mkdir(parents=True, exist_ok=True)
    names = [('Hull', 'ReV_dreadnought_heavy_cruiser.alo')]
    names += [('Turret%02d' % i, 'ReV_dreadnought_turret_%02d.alo' % i) for i in range(12)]
    names += [('DeathClone', 'ReV_dreadnought_heavy_cruiser_DC.alo')]
    models, files = {}, []
    for name, filename in names:
        path = MOD / 'Data/Art/Models' / filename
        models[name] = dict(source_model=str(path), model=audit(path), materials=materials(path))
        files.append(path)
    references = sorted({v for model in models.values() for mesh in model['materials']['meshes']
                         for row in mesh['materials'] for v in row['properties'].values()
                         if isinstance(v, str) and v.lower().endswith(('.dds', '.tga'))})
    available = {p.name.lower(): p for p in (MOD / 'Data/Art/Textures').iterdir() if p.is_file()}
    info = {}
    for name in references:
        path = available[name.lower()]
        files.append(path)
        image = Image.open(path).convert('RGBA')
        stem = 'HeavyDreadnought_' + path.stem
        image.save(textures / (stem + '.png'))
        assert Image.open(textures / (stem + '.png')).convert('RGBA').tobytes() == image.tobytes()
        info[name] = dict(stem=stem, size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
        if any('Colorize' in row['shader'] and row['properties'].get('BaseTexture', '').lower() == name.lower()
               for model in models.values() for mesh in model['materials']['meshes'] for row in mesh['materials']):
            mask = image.getchannel('A')
            mask.save(textures / (stem + '_TeamMask.png'))
            info[name]['mask_coverage'] = sum(v > 0 for v in mask.tobytes()) / (image.width * image.height)
    files += [MOD / 'Data/XML/Units_space_republic_dreadnaught.xml', MOD / 'Data/XML/Hardpoints_Republic_Space.xml', MOD / 'credits.md']
    for path in files:
        shutil.copy2(path, source / path.name)
    report = dict(models=models, textures=info, source_hashes={str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in files})
    (TASK / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(dict(models={k: dict(bones=[b['name'] for b in v['model']['bones']], meshes=[dict(name=m['name'], hidden=m['hidden'], materials=[(d['shader'], d['properties'].get('BaseTexture'), d['triangleCount']) for d in m['materials']]) for m in v['materials']['meshes']]) for k,v in models.items()}, textures=info), indent=2))


if __name__ == '__main__':
    Main()
