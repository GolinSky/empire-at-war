"""Write per-variant mount tables (socket bone -> project weapon/system) from Evidence/SourceHardpoints.json.

Run from the repository root: python Tools/Blender/Viscount/GenerateMounts.py
Every source weapon bone gets one mount. Weapon types are existing shared WeaponType values
chosen by the 2026-11-22 DPS balance (Evidence/viscount_draft.json, Evidence/eff.py).
"""
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path('Tools/Blender/Viscount')
SOURCE = json.loads((ROOT / 'Evidence/SourceHardpoints.json').read_text(encoding='utf-8'))

HEAVY_DUAL_TURBOLASER, MEDIUM_QUAD_TURBOLASER, MEDIUM_DUAL_TURBOLASER, MEDIUM_TURBOLASER = 3, 90, 5, 4
HEAVY_TRIPLE_ION, HEAVY_DUAL_ION, MEDIUM_ION = 68, 91, 32
MEDIUM_DUAL_LASER, MEDIUM_MISSILE, HEAVY_TORPEDO, MEDIUM_TORPEDO = 94, 93, 66, 48


def Prototype(bone):
    prefix, number = re.match(r'HP_([A-Z]+)(\d+)$', bone).groups()
    n = int(number)
    if prefix == 'TL':
        # TL01-05 are dorsal port mounts mirrored by TL16-20; TL06-15 sit lower on the flanks.
        return HEAVY_DUAL_TURBOLASER if n <= 5 or n >= 16 else MEDIUM_QUAD_TURBOLASER
    return {'IC': HEAVY_DUAL_ION, 'LC': MEDIUM_DUAL_LASER, 'MIS': MEDIUM_MISSILE, 'TORP': HEAVY_TORPEDO}[prefix]


def Dreadnought(row):
    projectile = row['projectile']
    if row['type'] == 'WEAPON_TORPEDO':
        return MEDIUM_TORPEDO
    if row['type'] == 'WEAPON_ION_CANNON':
        return HEAVY_TRIPLE_ION if 'Quad' in projectile else HEAVY_DUAL_ION if 'Dual' in projectile else MEDIUM_ION
    if 'Heavy' in projectile or 'Quad' in projectile:
        return HEAVY_DUAL_TURBOLASER
    return MEDIUM_DUAL_TURBOLASER if 'Dual' in projectile else MEDIUM_TURBOLASER


def Build(label, variant):
    rows = SOURCE[label]['hardpoints']
    weapons, systems = [], []
    for row in sorted(rows, key=lambda r: r['from_bow_pct']):
        bone, kind = row['bone'], row['type']
        if label == 'Prototype':
            if re.match(r'HP_(TL|IC|LC|MIS|TORP)\d+$', bone):
                weapons.append(dict(socket=bone, weaponType=Prototype(bone)))
            elif bone.startswith('HP_SHIELD'):
                systems.append(dict(socket=bone, system='ShieldGenerator'))
            elif bone.startswith('HP_E'):
                systems.append(dict(socket=bone, system='Engines'))
            elif bone.startswith('HP_SPAWN'):
                systems.append(dict(socket=bone, system='Hangar'))
        else:
            if kind.startswith('WEAPON_'):
                weapons.append(dict(socket=bone, weaponType=Dreadnought(row), sourceProjectile=row['projectile']))
            elif bone.startswith('HP_Shield_Gen'):
                systems.append(dict(socket=bone, system='ShieldGenerator'))
            elif kind == 'ENGINE':
                systems.append(dict(socket=bone, system='Engines'))
            elif kind == 'FIGHTER_BAY':
                systems.append(dict(socket=bone, system='Hangar'))
    for index, mount in enumerate(weapons):
        mount['id'] = index
    for index, mount in enumerate(systems):
        mount['id'] = len(weapons) + index
    (ROOT / f'{variant}Mounts.json').write_text(json.dumps(dict(weapons=weapons, systems=systems), indent=1), encoding='utf-8')
    print(variant, len(weapons), 'weapons', dict(Counter(m['weaponType'] for m in weapons)), dict(Counter(m['system'] for m in systems)))


Build('Prototype', 'ViscountPrototype')
Build('Dreadnought', 'ViscountDreadnought')
