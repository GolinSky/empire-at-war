"""Audit and stage the Pelta shipped in Republic at War 1129810972."""
import hashlib
import json
from pathlib import Path
import runpy
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
MOD = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data')
TASK = ROOT / 'Temp/PeltaImport'


def Main():
    functions = runpy.run_path(str(ROOT / 'Tools/Blender/CR70/Prepare.py'))['Functions']
    audit_model = functions(ROOT / 'Tools/Blender/prepare_isd_i.py')['Audit']
    audit_materials = functions(ROOT / 'Tools/Blender/MC80Independence/AuditMaterials.py')['Audit']
    source, textures = TASK / 'Source', TASK / 'Output/Textures'
    source.mkdir(parents=True, exist_ok=True)
    textures.mkdir(parents=True, exist_ok=True)
    model = MOD / 'Art/Models/ReV_Pelta.ALO'
    material = audit_materials(model)
    binary = audit_model(model)
    files = [model, MOD / 'Art/Models/ReV_Pelta_D.ALO', MOD / 'XML/Units_Space_Republic_Pelta.xml', MOD / 'XML/Hardpoints_Republic_Space.xml']
    available = {p.name.lower(): p for p in (MOD / 'Art/Textures').iterdir() if p.is_file()}
    references = sorted({v for mesh in material['meshes'] for row in mesh['materials'] for v in row['properties'].values() if isinstance(v, str) and v.lower().endswith(('.dds', '.tga'))})
    targets = {'rev_pelta.dds': 'Pelta_Hull_Albedo', 'rev_pelta_n.dds': 'Pelta_Hull_Normal', 'rev_pelta_lights.dds': 'Pelta_Lights_Emission', 'nb_shieldwave.dds': 'Pelta_SourceShield_Emission'}
    info = {}
    for name in references:
        path = available[name.lower()] if name.lower() in available else available[str(Path(name.lower()).with_suffix('.dds'))]
        files.append(path)
        stem = targets[path.name.lower()]
        image = Image.open(path).convert('RGBA')
        target = textures / (stem + '.png')
        image.save(target)
        assert Image.open(target).convert('RGBA').tobytes() == image.tobytes()
        info[name] = dict(stem=stem, size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
    for path in files:
        shutil.copy2(path, source / path.name)
    report = dict(source_unit='Pelta_Frigate', source_model=str(model), model=binary, materials=material, textures=info,
                  death_model=dict(model=audit_model(files[1]), materials=audit_materials(files[1])),
                  source_hashes={str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in files})
    (TASK / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(dict(bones=[b['name'] for b in binary['bones']], meshes=material['meshes'], textures=info), indent=2))


if __name__ == '__main__':
    Main()
