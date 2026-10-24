"""Audit both Centax models from Republic at War workshop 1129810972."""
import hashlib
import json
from pathlib import Path
import runpy
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
MOD = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data')
TASK = ROOT / 'Temp/CentaxImport'


def Main():
    functions = runpy.run_path(str(ROOT / 'Tools/Blender/Carrack/Prepare.py'))['Functions']
    audit_model = functions(ROOT / 'Tools/Blender/prepare_isd_i.py')['Audit']
    audit_materials = functions(ROOT / 'Tools/Blender/MC80Independence/AuditMaterials.py')['Audit']
    source, textures = TASK / 'Source', TASK / 'Output/Textures'
    source.mkdir(parents=True, exist_ok=True)
    textures.mkdir(parents=True, exist_ok=True)
    models = {}
    files = [MOD / 'XML/Units_Space_republic_centax.xml', MOD / 'XML/Hardpoints_Republic_Space.xml', MOD / 'Art/Models/ReV_centax_frigate_d_die_00.ala', MOD.parent / 'credits.md']
    for name, filename in [('Centax', 'ReV_centax_frigate.ALO'), ('CentaxWreck', 'ReV_centax_frigate_d.ALO')]:
        path = MOD / 'Art/Models' / filename
        models[name] = dict(source_model=str(path), model=audit_model(path), materials=audit_materials(path))
        files.append(path)
    available = {p.name.lower(): p for p in (MOD / 'Art/Textures').iterdir() if p.is_file()}
    extract = functions(ROOT / 'Tools/Blender/RebelSpaceStation/PrepareAttachments.py')['Extract']
    references = sorted({v for model in models.values() for mesh in model['materials']['meshes'] for row in mesh['materials'] for v in row['properties'].values() if isinstance(v, str) and v.lower().endswith(('.dds', '.tga'))})
    info = {}
    for name in references:
        if name.lower() == 'shield_color.dds' and name.lower() not in available:
            extract(Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data/textures.meg'), [name], source)
            available[name.lower()] = source / name
        path = available[name.lower()] if name.lower() in available else available[str(Path(name.lower()).with_suffix('.dds'))]
        files.append(path)
        stem = 'Centax_' + path.stem
        image = Image.open(path).convert('RGBA')
        target = textures / (stem + '.png')
        image.save(target)
        assert Image.open(target).convert('RGBA').tobytes() == image.tobytes()
        info[name] = dict(stem=stem, size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
        if any('Colorize' in row['shader'] and row['properties'].get('BaseTexture', '').lower() == name.lower() for model in models.values() for mesh in model['materials']['meshes'] for row in mesh['materials']):
            mask = image.getchannel('A')
            mask.save(textures / (stem + '_TeamMask.png'))
            info[name]['team_mask_coverage'] = sum(value > 0 for value in mask.tobytes()) / (image.width * image.height)
    for path in files:
        if path.parent != source:
            shutil.copy2(path, source / path.name)
    report = dict(models=models, textures=info, shield_texture_archive='D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data/textures.meg', source_hashes={str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in files})
    (TASK / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(dict(models={k: dict(bones=[b['name'] for b in v['model']['bones']], meshes=[dict(name=m['name'], hidden=m['hidden'], materials=[(d['shader'], d['properties'].get('BaseTexture'), d['triangleCount']) for d in m['materials']]) for m in v['materials']['meshes']]) for k,v in models.items()}, textures=info), indent=2))


if __name__ == '__main__':
    Main()
