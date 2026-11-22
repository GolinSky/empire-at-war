"""Effective ship DPS vs a target class: raw weapon DPS x matrix accuracy x caliber biases, split into shield and hull DPS.
Shield-piercing damage types skip shields and count as hull DPS. Approximation: ignores fire arcs, range, travel time."""
import re, os, sys, glob, json
sys.path.insert(0, os.path.dirname(__file__))
import dps as D

ROOT = D.ROOT
CLASSES = ['fighter', 'bomber', 'interceptor', 'corvette', 'frigate', 'cruiser', 'heavyCruiser', 'capital', 'battleCruiser',
           'dreadnought', 'structure', 'heroStrikecraft']

def classvals(block, key):
    m = re.search(r'\n\s+%s:\n((?:\s+\w+: [-\d.]+\n)+)' % key, block)
    return {k: float(v) for k, v in re.findall(r'(\w+): ([-\d.]+)', m.group(1))} if m else {}

mt = open(os.path.join(ROOT, r'Assets\Settings\Data\Models\Weapon\DamageMatrixData.asset'), encoding='utf-8').read()
matrix = {}
for b in re.split(r'^  - ', mt, flags=re.M)[1:]:
    dt = int(D.num(b, 'damageType'))
    matrix[dt] = dict(damage=classvals(b, 'damage'), accuracy=classvals(b, 'accuracy'), vsShield=D.num(b, 'vsShield'),
                      ship=D.num(b, 'shipShieldMultiplier'), pierce=D.num(b, 'shieldPiercing') == 1)

wt_text = D.text
fam_dt = {int(D.num(b, 'family')): int(D.num(b, 'damageType')) for b in D.blocks(wt_text, 'families')}
fam_bias = {int(D.num(b, 'family')): D.num(b, 'targetSizeBias') == 1 for b in D.blocks(wt_text, 'families')}
cal_bias = {int(D.num(b, 'caliber')): (classvals(b, 'damageBias'), classvals(b, 'accuracyBias')) for b in D.blocks(wt_text, 'calibers')}
fam_of = {int(D.num(b, 'weaponType')): int(D.num(b, 'family')) for b in D.blocks(wt_text, 'weapons')}

def eff(loadout, cls):
    sh = hu = 0.0
    for wt, n in loadout:
        w = D.weapons[wt]; fam = fam_of[wt]; m = matrix[fam_dt[fam]]
        db, ab = cal_bias[w['caliber']] if (fam != 7 and fam_bias[fam]) else ({}, {})
        base = w['dps'] * n * m['accuracy'][cls] * ab.get(cls, 1) * db.get(cls, 1)
        if m['pierce']:
            hu += base * m['damage'][cls]
        else:
            sh += base * m['vsShield'] * m['ship']
            hu += base * m['damage'][cls]
    return sh, hu

def report(name, hull, shields, loadout):
    n = sum(c for _, c in loadout)
    raw = sum(D.weapons[wt]['dps'] * c for wt, c in loadout)
    out = [f'{name:26} hull={hull:7.0f} sh={shields:7.0f} mounts={n:3} raw={raw:6.0f}']
    for cls in ('capital', 'battleCruiser', 'dreadnought'):
        s, h = eff(loadout, cls)
        out.append(f'{cls[:5]}: sh {s:5.0f} hull {h:5.0f}')
    print('  '.join(out))

if __name__ == '__main__':
    refs = ['Empire/Executor', 'Empire/Praetor', 'Rebellion/HomeOne', 'Rebellion/MC80Independence', 'Rebellion/MediatorBattlecruiser',
            'Empire/Titan', 'Empire/Secutor', 'Empire/ISDII']
    for r in refs:
        hits = glob.glob(os.path.join(ROOT, r'Assets\Settings\Data\Ship', '*', os.path.basename(r) + 'ShipData.asset'))
        s = D.ship(hits[0])
        report(s['name'], s['hull'], s['shields'], s['loadout'])
    if len(sys.argv) > 1:
        for name, spec in json.load(open(sys.argv[1])).items():
            report(name, spec['hull'], spec['shields'], [(int(k), v) for k, v in spec['loadout'].items()])
