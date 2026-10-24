"""Audit the Charger c70 model shipped inside Republic at War 1129810972."""
import ast
import hashlib
import json
from pathlib import Path
import shutil
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[3]
MOD = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data')
OUTPUT = ROOT / 'Temp/CR70ChargerImport'


def Functions(path):
    tree = ast.parse(path.read_text(encoding='utf-8-sig'))
    tree.body = [node for node in tree.body if isinstance(node, (ast.Import, ast.ImportFrom, ast.FunctionDef))]
    # Extract only the self-contained binary readers, avoiding script entry points.
    tree.body = [node for node in tree.body if not isinstance(node, ast.ImportFrom) or node.module != 'Prepare']
    scope = {}
    exec(compile(tree, str(path), 'exec'), scope)
    return scope


def Main():
    source = OUTPUT / 'Source'
    textures = OUTPUT / 'Output/Textures'
    source.mkdir(parents=True, exist_ok=True)
    textures.mkdir(parents=True, exist_ok=True)
    model = MOD / 'Art/Models/ReV_light_frigate.ALO'
    shutil.copy2(model, source / model.name)
    binary = Functions(ROOT / 'Tools/Blender/prepare_isd_i.py')['Audit'](model)
    material = Functions(ROOT / 'Tools/Blender/MC80Independence/AuditMaterials.py')['Audit'](model)
    references = sorted({value for mesh in material['meshes'] for row in mesh['materials'] for value in row['properties'].values()
                         if isinstance(value, str) and value.lower().endswith(('.dds', '.tga'))})
    available = {path.name.lower(): path for path in (MOD / 'Art/Textures').iterdir() if path.is_file()}
    texture_sources = {}
    for name in references:
        key = name.lower()
        if key not in available:
            key = str(Path(key).with_suffix('.dds'))
        texture_sources[name] = available[key]
    targets = {'rev_light_frigate.dds': 'CR70_Charger_Hull_Albedo', 'rev_light_frigate_b.dds': 'CR70_Charger_Hull_Normal', 'rev_light_frigate_l.dds': 'CR70_Charger_Engine_Emission'}
    mapping = {name: targets[path.name.lower()] for name, path in texture_sources.items()}
    info = {}
    for name, target in mapping.items():
        original = texture_sources[name]
        shutil.copy2(original, source / original.name)
        image = Image.open(original).convert('RGBA')
        path = textures / (target + '.png')
        image.save(path)
        assert Image.open(path).convert('RGBA').tobytes() == image.tobytes()
        info[name] = dict(stem=target, size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
        if name.lower() == 'rev_light_frigate.dds':
            mask = ImageOps.invert(image.getchannel('A'))
            mask.save(textures / (target + '_TeamMask.png'))
            info[name]['team_mask_coverage'] = sum(value > 0 for value in mask.tobytes()) / (image.width * image.height)
    xml = MOD / 'XML/Units_Space_republic_light_frigate.xml'
    xml_copy = source / xml.name
    xml_copy.write_bytes(xml.read_bytes())
    report = dict(source_unit='Republic_light_frigate', source_model=str(model), requested_name='CR70 Corvette', model=binary, materials=material, textures=info,
                  source_hashes={str(path): hashlib.sha256(path.read_bytes()).hexdigest() for path in [model, xml, *texture_sources.values()]})
    (OUTPUT / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(dict(bones=[b['name'] for b in binary['bones']], meshes=[dict(name=m['name'], hidden=m['hidden'], materials=[(x['shader'],x['properties'].get('BaseTexture'),x['triangleCount']) for x in m['materials']]) for m in material['meshes']], textures=info), indent=2))


if __name__ == '__main__':
    Main()
