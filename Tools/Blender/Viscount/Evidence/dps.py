"""Raw ship DPS from WeaponsData.asset using WeaponProfileFactory's formula (before damage matrix and target biases)."""
import re, glob, os, sys, json

ROOT = r'F:\Private\empire-at-war'
FAM = {0: 'Laser', 1: 'Turbolaser', 2: 'Ion', 3: 'MassDriver', 4: 'Missile', 5: 'Torpedo', 6: 'IonTorpedo', 7: 'PointDefense', 8: 'Beam'}

def blocks(text, key):
    sec = re.search(r'^  %s:\n(.*?)(?=^  \w+:|\Z)' % key, text, re.S | re.M).group(1)
    return [b for b in re.split(r'^  - ', sec, flags=re.M) if b.strip()]

def num(b, k):
    m = re.search(r'(?:^|\n)\s*%s: ([-\d.]+)' % k, b)
    return float(m.group(1)) if m else None

text = open(os.path.join(ROOT, r'Assets\Settings\Data\Models\Weapon\WeaponsData.asset'), encoding='utf-8').read()
families = {int(num(b, 'family')): dict(dmg=num(b, 'damage'), shots=num(b, 'shotsPerSalvo'), interval=num(b, 'shotInterval'),
                                         reload=num(b, 'reload'), range=num(b, 'range'), platform=num(b, 'platform'))
            for b in blocks(text, 'families')}
calibers = {int(num(b, 'caliber')): dict(dmg=num(b, 'damageMultiplier'), rng=num(b, 'rangeMultiplier')) for b in blocks(text, 'calibers')}
mounts = {int(num(b, 'mount')): num(b, 'damageMultiplier') for b in blocks(text, 'mounts')}
weapons = {}
for b in blocks(text, 'weapons'):
    wt = int(num(b, 'weaponType'))
    name = re.search(r'displayName: (.*)', b).group(1).strip()
    fam, cal, mnt = int(num(b, 'family')), int(num(b, 'caliber')), int(num(b, 'mount'))
    f = families[fam]
    hascal = fam != 7
    cd = calibers[cal]['dmg'] if hascal else 1
    cr = calibers[cal]['rng'] if hascal else 1
    mm = mnt if f['platform'] == 1 else mounts[mnt]
    dmg = f['dmg'] * cd * mm * num(b, 'damageMultiplier')
    reload = f['reload'] * num(b, 'reloadMultiplier')
    cycle = reload + (f['shots'] - 1) * f['interval']
    weapons[wt] = dict(name=name, family=FAM.get(fam, str(fam)), dps=dmg * f['shots'] / cycle, range=f['range'] * cr * num(b, 'rangeMultiplier'),
                       caliber=cal, mount=mnt)

def ship(path):
    t = open(path, encoding='utf-8').read()
    lo = re.findall(r'weaponType: (\d+)\n\s+count: (\d+)', t)
    g = lambda k: re.search(r'<%s>k__BackingField: ([\d.]+)' % k, t)
    return dict(name=os.path.basename(path)[:-len('ShipData.asset')], cls=int(g('ShipClass').group(1)), hull=float(g('Hull').group(1)),
                shields=float(g('Shields').group(1)), speed=float(g('Speed').group(1)), loadout=[(int(a), int(c)) for a, c in lo])

def summarize(s):
    by = {}
    mounts_n = 0
    for wt, c in s['loadout']:
        w = weapons[wt]
        by[w['family']] = by.get(w['family'], 0) + w['dps'] * c
        mounts_n += c
    return mounts_n, by

if __name__ == '__main__':
    mode = sys.argv[1] if len(sys.argv) > 1 else 'ships'
    if mode == 'weapons':
        for wt, w in sorted(weapons.items(), key=lambda kv: (kv[1]['family'], -kv[1]['dps'])):
            print(f"{wt:4} {w['name']:34} {w['family']:12} cal={w['caliber']} mount={w['mount']} dps={w['dps']:7.1f} range={w['range']:6.0f}")
    else:
        files = glob.glob(os.path.join(ROOT, r'Assets\Settings\Data\Ship\*\*ShipData.asset'))
        rows = []
        for f in files:
            try:
                s = ship(f)
            except AttributeError:
                continue
            if s['cls'] not in (5, 11):
                continue
            n, by = summarize(s)
            rows.append((s, n, by))
        for s, n, by in sorted(rows, key=lambda r: -sum(r[2].values())):
            tot = sum(by.values())
            fams = ' '.join(f"{k}={v:.0f}" for k, v in sorted(by.items(), key=lambda kv: -kv[1]))
            print(f"{s['name']:28} cls={s['cls']:2} hull={s['hull']:7.0f} sh={s['shields']:7.0f} spd={s['speed']:4.0f} mounts={n:3} DPS={tot:7.0f}  {fams}")
            print('      ' + ', '.join(f"{c}x {weapons[wt]['name']}" for wt, c in s['loadout']))
