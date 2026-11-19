"""Read-only squadron duel model. Requires PyYAML.

Run: python Tools/SquadronRebalance/DuelSim.py [--trials N] [--seed S]

Reads live WeaponsData, DamageMatrixData and Squadron/*SquadronData assets.
Prints laser hits-to-kill breakpoints (margin = hull left before the killing hit,
as a share of hull; also at x1.1 hull and x1.1 damage; thin = below 5% when
any variant kills in 6 hits or fewer) and "destroyed in a row"
tables: one squadron fights fresh enemy squadrons one after another until it dies.
Model: random alive target per shot, continuous fire, accuracy rolls, per-craft shields
as in SquadronHealthModel. Ignores torpedoes/ion, attack runs, arcs, speed,
regeneration, abilities and research.
"""
import argparse
import heapq
import math
import random
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
WEAPONS = ROOT / "Assets/Settings/Data/Models/Weapon/WeaponsData.asset"
MATRIX = ROOT / "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset"
SQUADRONS = ROOT / "Assets/Settings/Data/Squadron"
CLASS_KEYS = {0: "fighter", 1: "bomber", 7: "interceptor"}
# Families that fire at strikecraft: fighter laser, missile, interceptor laser.
ANTI_CRAFT_FAMILIES = (20, 22, 24)
UNITS = ["XWing", "ARC170", "V19Torrent", "TIEAvenger", "TIEBrute", "TIEFighter", "Vulture",
         "AWing", "VWing", "TIEInterceptor", "Belbullab22",
         "YWingBomber", "YWing", "TIEPunisher", "NTB630", "TIEBomber", "HyenaBomber", "DroidBomber"]
PRICES = {"XWing": 800, "ARC170": 600, "V19Torrent": 500, "TIEAvenger": 550, "TIEBrute": 450,
          "TIEFighter": 300, "Vulture": 200, "AWing": 850, "VWing": 650, "TIEInterceptor": 450,
          "Belbullab22": 300}
MIN_MARGIN = .05
# Breakpoints matter when few hits kill: one extra hit costs at most ~1/6 of the duel.
MAX_BREAKPOINT_HITS = 6


def Read(path):
    text = path.read_text(encoding="utf-8-sig")
    return yaml.safe_load("\n".join(line for line in text.splitlines()
                                    if not line.startswith(("%", "---"))))["MonoBehaviour"]


class Unit:
    def __init__(self, name, weapons, matrix):
        data = Read(SQUADRONS / f"{name}SquadronData.asset")
        field = lambda key: data[f"<{key}>k__BackingField"]
        self.name = name
        self.count = field("MemberCount")
        self.hull = field("MemberHull")
        self.shields = field("MemberShields")
        self.shipClass = CLASS_KEYS[field("ShipClass")]
        self.laserShield = field("LaserShieldDamageMultiplier")
        definitions = {entry["weaponType"]: entry for entry in weapons["weapons"]}
        families = {entry["family"]: entry for entry in weapons["families"]}
        calibers = {entry["caliber"]: entry for entry in weapons["calibers"]}
        # Per craft: (bolt damage, damage type, seconds between shots).
        self.guns = []
        for entry in data["weaponLoadout"]:
            definition = definitions[entry["weaponType"]]
            family = families[definition["family"]]
            if definition["family"] not in ANTI_CRAFT_FAMILIES:
                continue
            perCraft = entry["count"] / self.count
            bolt = (family["damage"] * calibers[definition["caliber"]]["damageMultiplier"]
                    * definition["mount"] * definition["damageMultiplier"])
            cycle = max(family["reload"] * definition["reloadMultiplier"],
                        (family["shotsPerSalvo"] - 1) * family["shotInterval"])
            for _ in range(round(perCraft)):
                self.guns.append((bolt, family["damageType"], cycle / family["shotsPerSalvo"]))
        self.matrix = matrix

    def Laser(self):
        return max((gun for gun in self.guns if gun[1] in (13, 14)), key=lambda gun: gun[0])


def LoadMatrix():
    rows = {}
    for entry in Read(MATRIX)["damageTypes"]:
        rows[entry["damageType"]] = entry
    return rows


def Hit(matrix, target, craft, bolt, damageType, damageScale=1.0):
    """Applies one hit to craft [hull, shields]; mirrors SquadronHealthModel.ApplyDamage."""
    row = matrix[damageType]
    damage = bolt * damageScale
    if craft[1] > 0 and not row["shieldPiercing"]:
        shieldMultiplier = row["squadronShieldMultiplier"] * (
            target.laserShield if damageType in (0, 12, 13, 14) else 1)
        shieldDamage = damage * shieldMultiplier
        if shieldDamage <= craft[1]:
            craft[1] -= shieldDamage
            return
        damage = (shieldDamage - craft[1]) / shieldMultiplier
        craft[1] = 0
    craft[0] -= damage * row["damage"][target.shipClass]


def Breakpoint(matrix, attacker, target, hullScale=1.0, damageScale=1.0):
    bolt, damageType, _ = attacker.Laser()
    craft = [target.hull * hullScale, target.shields]
    hits = 0
    while True:
        before = craft[0]
        Hit(matrix, target, craft, bolt, damageType, damageScale)
        hits += 1
        if craft[0] <= 0:
            return hits, before / (target.hull * hullScale)


def Fight(matrix, a, aCraft, b, rng):
    """Fights until one side has no craft; returns surviving craft of a and b."""
    bCraft = [[b.hull, b.shields] for _ in range(b.count)]
    sides = ((a, aCraft, b, bCraft), (b, bCraft, a, aCraft))
    events = []
    for sideIndex, (unit, crafts, _, _) in enumerate(sides):
        for craftIndex, craft in enumerate(crafts):
            if craft[0] <= 0:
                continue
            for gun in unit.guns:
                heapq.heappush(events, (rng.random() * gun[2], rng.random(), sideIndex, craftIndex, gun))
    while events:
        time, _, sideIndex, craftIndex, gun = heapq.heappop(events)
        unit, crafts, enemy, enemyCrafts = sides[sideIndex]
        if crafts[craftIndex][0] <= 0:
            continue
        alive = [craft for craft in enemyCrafts if craft[0] > 0]
        if not alive:
            break
        bolt, damageType, interval = gun
        if rng.random() < matrix[damageType]["accuracy"][enemy.shipClass]:
            Hit(matrix, enemy, rng.choice(alive), bolt, damageType)
        heapq.heappush(events, (time + interval, rng.random(), sideIndex, craftIndex, gun))
    return aCraft, bCraft


def InARow(matrix, a, b, trials, rng, limit=30):
    total = 0.0
    for _ in range(trials):
        aCraft = [[a.hull, a.shields] for _ in range(a.count)]
        destroyed = 0.0
        while destroyed < limit:
            aCraft, bCraft = Fight(matrix, a, aCraft, b, rng)
            lost = sum(1 for craft in bCraft if craft[0] <= 0)
            if lost == b.count:
                destroyed += 1
                continue
            destroyed += lost / b.count
            break
        total += destroyed
    return total / trials


def Main(trials, seed):
    weapons, matrix = Read(WEAPONS), LoadMatrix()
    units = {name: Unit(name, weapons, matrix) for name in UNITS}
    fighters = [u for u in units.values() if any(g[1] in (13, 14) for g in u.guns)
                and u.shipClass != "bomber"]

    print("Laser breakpoints (hits to kill a fresh craft / worst margin; * = thin)")
    thin = []
    for attacker in fighters:
        cells = []
        for target in units.values():
            results = [Breakpoint(matrix, attacker, target),
                       Breakpoint(matrix, attacker, target, hullScale=1.1),
                       Breakpoint(matrix, attacker, target, damageScale=1.1)]
            worst = min(margin for _, margin in results)
            isThin = worst < MIN_MARGIN and min(h for h, _ in results) <= MAX_BREAKPOINT_HITS
            if isThin:
                thin.append(f"{attacker.name} -> {target.name}: "
                            + ", ".join(f"{h} hits {m:.1%}" for h, m in results))
            cells.append(f"{target.name}:{results[0][0]}/{worst:.0%}{'*' if isThin else ''}")
        print(f"  {attacker.name} bolt {attacker.Laser()[0]:.2f}: " + " ".join(cells))
    print("Thin breakpoints:" if thin else "Thin breakpoints: none")
    for line in thin:
        print("  " + line)

    rng = random.Random(seed)
    print(f"\nDestroyed in a row ({trials} trials; target = price / anchor price)")
    pairs = [(n, "TIEFighter") for n in ("XWing", "ARC170", "V19Torrent", "TIEAvenger",
                                          "TIEBrute", "Vulture", "AWing")]
    pairs += [("TIEFighter", "Vulture"), ("TIEFighter", "XWing")]
    pairs += [(n, "TIEInterceptor") for n in ("AWing", "VWing", "Belbullab22")]
    pairs += [("TIEInterceptor", "Belbullab22")]
    pairs += [("XWing", n) for n in ("ARC170", "TIEAvenger", "TIEBrute", "AWing")]
    pairs += [("TIEAvenger", "XWing")]
    for a, b in pairs:
        value = InARow(matrix, units[a], units[b], trials, rng)
        target = PRICES[a] / PRICES[b]
        print(f"  {a:>15} vs {b:<15} {value:5.2f}  (price ratio {target:.2f})")

    print("\nTorpedo volley (family 23, whole squadron, before damage matrix)")
    definitions = {entry["weaponType"]: entry for entry in weapons["weapons"]}
    calibers = {entry["caliber"]: entry for entry in weapons["calibers"]}
    family = {entry["family"]: entry for entry in weapons["families"]}[23]
    for name in ("YWingBomber", "YWing", "TIEPunisher", "NTB630", "TIEBomber", "HyenaBomber",
                 "DroidBomber"):
        data = Read(SQUADRONS / f"{name}SquadronData.asset")
        warhead = data["<WarheadDamageMultiplier>k__BackingField"]
        volley = sum(family["damage"] * calibers[definitions[e["weaponType"]]["caliber"]]["damageMultiplier"]
                     * definitions[e["weaponType"]]["mount"] * definitions[e["weaponType"]]["damageMultiplier"]
                     * e["count"] * warhead
                     for e in data["weaponLoadout"] if definitions[e["weaponType"]]["family"] == 23)
        print(f"  {name:>12} {volley:6.0f}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--trials", type=int, default=300)
    parser.add_argument("--seed", type=int, default=1)
    arguments = parser.parse_args()
    Main(arguments.trials, arguments.seed)
