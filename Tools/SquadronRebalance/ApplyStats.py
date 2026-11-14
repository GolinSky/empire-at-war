"""Apply or inspect the 2026-11-14 target sheet. Requires PyYAML; default is read-only.

Run from any directory: python Tools/SquadronRebalance/ApplyStats.py [--apply]
Unity must refresh, explicitly reserialize changed assets and save after --apply.
Prefab resizing and weapon 72 -> 157 rebinding are separate Unity operations.
"""
import argparse
import copy
import json
from pathlib import Path
import re

import yaml


ROOT = Path(__file__).resolve().parents[2]
WEAPONS = ROOT / "Assets/Settings/Data/Models/Weapon/WeaponsData.asset"
# Members, hull, shields, regeneration HP/s, family -> whole-squadron raw DPS.
TARGETS = {
    "XWing": (5, 95, 15, .3, {20: 210, 23: 20}),
    "ARC170": (8, 60, 8, .2, {20: 200, 23: 20}),
    "TIEFighter": (10, 58, 0, 0, {20: 210}),
    "Vulture": (13, 44, 0, 0, {20: 200}),
    "AWing": (4, 100, 15, .3, {24: 200}),
    "VWing": (6, 64, 8, .2, {24: 190}),
    "TIEInterceptor": (8, 62, 0, 0, {24: 180}),
    "Belbullab22": (11, 44, 0, 0, {24: 170}),
    "YWingBomber": (3, 180, 10, .2, {20: 70, 21: 10, 23: 60}),
    "YWing": (4, 130, 8, .15, {20: 79, 23: 56}),
    "TIEBomber": (6, 100, 0, 0, {20: 78, 23: 52}),
    "DroidBomber": (10, 58, 0, 0, {20: 77, 23: 48}),
    "V19Torrent": (8, 58, 8, .2, {20: 210}),
    "NTB630": (4, 125, 8, .15, {20: 78, 23: 52}),
    "TIEBrute": (10, 64, 0, 0, {20: 220}),
    "TIEAvenger": (10, 54, 8, .1, {20: 205, 22: 20}),
    "TIEPunisher": (6, 110, 6, .1, {20: 77, 22: 12, 23: 56}),
}


def Read(path):
    text = path.read_text(encoding="utf-8-sig")
    document = "\n".join(line for line in text.splitlines()
                         if not line.startswith(("%", "---")))
    return text, yaml.safe_load(document)["MonoBehaviour"]


def SetField(text, name, value):
    result, count = re.subn(r"(?m)^(  " + re.escape(name) + r": ).*$",
                            lambda match: match[1] + format(value, ".10g"), text)
    if count != 1:
        raise ValueError(f"Expected one field: {name}; found {count}")
    return result


def Run(apply):
    weapon_text, weapons = Read(WEAPONS)
    definitions = {entry["weaponType"]: entry for entry in weapons["weapons"]}
    families = {entry["family"]: entry for entry in weapons["families"]}
    calibers = {entry["caliber"]: entry for entry in weapons["calibers"]}
    blocks = {int(match[1]): match[0] for match in re.finditer(
        r"(?ms)^  - weaponType: (\d+)\n.*?(?=^  - weaponType: |\Z)", weapon_text)}
    if apply and 157 not in definitions:
        definition = copy.deepcopy(definitions[72])
        definition.update(weaponType=157, scope=2, faction=2, unit="YWingBomber")
        definitions[157] = definition
        block = blocks[72].replace("weaponType: 72\n", "weaponType: 157\n", 1)
        block = block.replace("    scope: 0\n", "    scope: 2\n", 1)
        block = block.replace("    faction: 0\n", "    faction: 2\n", 1)
        block = block.replace("    unit: \n", "    unit: YWingBomber\n", 1)
        block = re.sub(r"(?ms)^    reason:.*?(?=^    overrideVisual:)",
                       "    reason: YWingBomber squadron unit amount damage target.\n", block)
        blocks[157] = block
        weapon_text = weapon_text.rstrip() + "\n" + block

    def Dps(entry):
        definition = definitions[entry["weaponType"]]
        family = families[definition["family"]]
        damage = (family["damage"] * calibers[definition["caliber"]]["damageMultiplier"]
                  * definition["mount"] * definition["damageMultiplier"])
        cycle = max(family["reload"] * definition["reloadMultiplier"],
                    (family["shotsPerSalvo"] - 1) * family["shotInterval"])
        return entry["count"] * damage * family["shotsPerSalvo"] / cycle

    updates, report, tuned_types = {}, [], set()
    for unit, (members, hull, shields, regen, budgets) in TARGETS.items():
        path = ROOT / f"Assets/Settings/Data/Squadron/{unit}SquadronData.asset"
        text, data = Read(path)
        old_members = data["<MemberCount>k__BackingField"]
        entries = copy.deepcopy(data["weaponLoadout"])
        if apply:
            for entry in entries:
                if entry["count"] % old_members:
                    raise ValueError(f"Nonuniform craft loadout: {unit}")
                entry["count"] = entry["count"] // old_members * members
                if unit == "YWingBomber" and entry["weaponType"] == 72:
                    entry["weaponType"] = 157
            for family_id, target in budgets.items():
                group = [e for e in entries if definitions[e["weaponType"]]["family"] == family_id]
                factor = target / sum(Dps(entry) for entry in group)
                for entry in group:
                    definition = definitions[entry["weaponType"]]
                    if definition["scope"] != 2 or definition["unit"] != unit:
                        raise ValueError(f"Weapon is not scoped to {unit}: {definition}")
                    definition["damageMultiplier"] *= factor
                    tuned_types.add(entry["weaponType"])
            fields = {"MemberCount": members, "MemberHull": hull, "MemberShields": shields,
                      "ShieldRegenerateValue": regen * data["<ShieldRegenerateDelay>k__BackingField"],
                      "HullRepairPerSecond": 0, "LaserShieldDamageMultiplier": 1,
                      "WarheadDamageMultiplier": 1, "WarheadReloadMultiplier": 1}
            for field, value in fields.items():
                text = SetField(text, f"<{field}>k__BackingField", value)
            text = text[:text.index("  weaponLoadout:\n")] + "  weaponLoadout:\n" + "".join(
                f"  - weaponType: {e['weaponType']}\n    count: {e['count']}\n" for e in entries)
            updates[path] = text
        actual = {family_id: sum(Dps(e) for e in entries
                                if definitions[e["weaponType"]]["family"] == family_id)
                  for family_id in budgets}
        if not apply:
            expected_fields = {"MemberCount": members, "MemberHull": hull, "MemberShields": shields,
                               "ShieldRegenerateValue": regen * data["<ShieldRegenerateDelay>k__BackingField"],
                               "HullRepairPerSecond": 0, "LaserShieldDamageMultiplier": 1,
                               "WarheadDamageMultiplier": 1, "WarheadReloadMultiplier": 1}
            for field, target in expected_fields.items():
                if abs(data[f"<{field}>k__BackingField"] - target) > .0001:
                    raise ValueError(f"{unit} {field} differs from {target}")
            for family_id, target in budgets.items():
                if abs(actual[family_id] - target) > .0001:
                    raise ValueError(f"{unit} family {family_id}: {actual[family_id]} != {target}")
        report.append({"unit": unit, "members": members if apply else old_members,
                       "familyDps": actual, "dps": sum(actual.values())})

    if apply:
        for weapon_id, old_block in blocks.items():
            if weapon_id not in tuned_types:
                continue
            definition = definitions[weapon_id]
            new_block = re.sub(r"(?m)^    damageMultiplier: .*$",
                               f"    damageMultiplier: {definition['damageMultiplier']:.10g}", old_block)
            weapon_text = weapon_text.replace(old_block, new_block, 1)
        updates[WEAPONS] = weapon_text
        for path, text in updates.items():
            if path.read_text(encoding="utf-8-sig") != text:
                path.write_text(text, encoding="utf-8", newline="\n")
        print(json.dumps({"changedAssets": [str(p.relative_to(ROOT)).replace("\\", "/")
                                             for p in updates], "squadrons": report}, indent=2))
    else:
        print(json.dumps({"verifiedSquadrons": len(report), "squadrons": report}, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    Run(parser.parse_args().apply)
