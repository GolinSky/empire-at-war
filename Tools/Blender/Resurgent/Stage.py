"""Stage the converted Resurgent hull and turret FBX files and their textures into Assets/, and write per-material build rows.

Run from the repository root after Convert.py: python Tools/Blender/Resurgent/Stage.py
Writes Tools/Blender/Resurgent/<Variant>Materials.json: Blender material -> source shader, textures and tints.
Effect shaders (additive, engine-glow "shield") also get *_Additive.png with alpha = max(R, G, B), as the ISD I import.
"""
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK = Path('Temp/ResurgentImport')
TOOLS = Path('Tools/Blender/Resurgent')
HELPER_SHADERS = ('MeshShadowVolume.fx', 'MeshCollision.fx')
VARIANTS = ('Resurgent', 'ResurgentSextupleTurret', 'ResurgentLightTurret', 'ResurgentQuadIonTurret',
            'ResurgentMissileTurret', 'ResurgentQuadTurret')
audit = json.loads((TASK / 'SourceAudit.json').read_text(encoding='utf-8'))['variants']

for variant in VARIANTS:
    report = json.loads((TASK / variant / 'Report.json').read_text(encoding='utf-8'))
    model_dir = Path(f'Assets/Art/Models/EmpireShips/{variant}')
    texture_dir = Path(f'Assets/Art/Textures/Models/EmpireShips/{variant}')
    model_dir.mkdir(parents=True, exist_ok=True)
    texture_dir.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(TASK / variant / f'{variant}.fbx', model_dir / f'{variant}.fbx')

    # Blender mesh names follow audit order with .001 suffixes for repeats; each mesh has one material slot here.
    source_by_mesh, counts = {}, {}
    for mesh in audit[variant]['material_audit']['meshes']:
        index = counts.get(mesh['name'], 0)
        counts[mesh['name']] = index + 1
        assert len(mesh['materials']) == 1, mesh['name']
        source_by_mesh[mesh['name'] if index == 0 else f"{mesh['name']}.{index:03}"] = mesh['materials'][0]
    by_name = {m['name']: m for m in report['materials']}
    rows = []
    for mesh_name, mesh in report['before']['meshes'].items():
        (material_name,) = mesh['materials']
        record, source = by_name[material_name], source_by_mesh[mesh_name]
        assert record['shader'] == source['shader'], (mesh_name, record['shader'], source['shader'])
        props = source['properties']
        rows.append(dict(name=material_name, mesh=mesh_name, shader=record['shader'], base=record['base'],
                         normal=record['normal'], hidden=mesh['hidden'],
                         diffuse=props.get('Diffuse'), color=props.get('Color')))
    (TOOLS / f'{variant}Materials.json').write_text(json.dumps(rows, indent=1), encoding='utf-8')

    stems = set()
    for row in rows:
        if row['shader'] in HELPER_SHADERS or row['base'] is None:
            continue
        stems.add(row['base'])
        if row['normal']:
            stems.add(row['normal'])
        if 'Additive' in row['shader'] or 'Shield' in row['shader']:
            image = Image.open(TASK / variant / 'Textures' / f"{row['base']}.png").convert('RGBA')
            red, green, blue, _ = image.split()
            image.putalpha(ImageChops.lighter(ImageChops.lighter(red, green), blue))
            image.save(texture_dir / f"{row['base']}_Additive.png")
    for stem in sorted(stems):
        shutil.copyfile(TASK / variant / 'Textures' / f'{stem}.png', texture_dir / f'{stem}.png')
    print(variant, 'materials', len(rows), 'textures', len(stems),
          'tints', sorted({tuple(round(c, 2) for c in r['diffuse'][:3]) for r in rows if r['diffuse']}))
