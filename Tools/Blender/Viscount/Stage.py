"""Copy converted FBX files and the textures their visible materials use into Assets/.

Run from the repository root after Convert.py: python Tools/Blender/Viscount/Stage.py
Helper materials (shadow volume, collision) take no textures. Bump shaders also stage their *_TeamMask.png.
"""
import json
import shutil
from pathlib import Path

TASK = Path('Temp/ViscountImport')
HELPER_SHADERS = ('MeshShadowVolume.fx', 'MeshCollision.fx')

for variant in ('ViscountPrototype', 'ViscountDreadnought'):
    materials = json.loads(Path(f'Tools/Blender/Viscount/{variant}Materials.json').read_text(encoding='utf-8'))
    model_dir = Path(f'Assets/Art/Models/RebellionShips/{variant}')
    texture_dir = Path(f'Assets/Art/Textures/Models/RebellionShips/{variant}')
    model_dir.mkdir(parents=True, exist_ok=True)
    texture_dir.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(TASK / variant / f'{variant}.fbx', model_dir / f'{variant}.fbx')
    stems = set()
    for material in materials:
        if material['shader'] in HELPER_SHADERS:
            continue
        for key in ('base', 'normal'):
            if material[key]:
                stems.add(material[key])
        if 'Bump' in material['shader'] and material['base']:
            stems.add(material['base'] + '_TeamMask')
    for stem in sorted(stems):
        shutil.copyfile(TASK / variant / 'Textures' / f'{stem}.png', texture_dir / f'{stem}.png')
    print(variant, 'textures', len(stems))
