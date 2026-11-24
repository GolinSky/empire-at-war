---
category: Bugs
status: done
created: 2026-11-24
updated: 2026-11-24
completed: 2026-11-24
acceptance: pending-user
---
# ISD I vs Venator Balance

## Goal

- ISD I stops losing 1v1 to Venator I; Venator-class beam stops deciding capital fights.

## Cause (telemetry `Battle_20261124_131935`, ~175 s)

- ISD I → Venator shields ≈632/s vs Venator regen 600 → net ≈32/s; Venator → ISD I ≈621/s vs regen 550 → net ≈71/s.
- Venator beam: 20,745 of ISD I's 31,140 hull loss (67%).
- Ions: 40,467 of ISD I's 110,559 shield damage (37%); hull only 488. Not useless: without them ISD I strips ≈400/s < 600 regen.
- ISD I's 8 `DualMediumTurboLaser` (range 518) never fired from its 585 hold → ~25% of its shield damage idle.

## Decision

- ISD I: 8 `DualMediumTurboLaser` → `DualHeavyTurboLaser` (option A). Why: same 585 range as its mains → all fire at hold. Avoid: quad mains (option B, Tector-level).
- ISD I price 7,500 → 7,000 (= Tector).
- `LaserBeam` weapon `damageMultiplier` 2 → 1 (halved): Venator I, Venator II, Resolute, Imperial Venator. Executor ability (id 33) and Xyston Superlaser (id 58) untouched.
- Imperator ability `LaserBeam` (id 10) → `BoostWeaponPower` (id 5); id 10 now unused, catalog entry kept.

## Results (source-verified, not in game)

| | Before | After |
|---|---|---|
| ISD I effective DPS vs shields | 779 | 1,000 |
| ISD I fires at hold vs Venator | 72% | 98% |
| ISD I DPS per 1,000 credits | 81 | 128 (Venator 128) |
| Venator beam | 3,661/shot, 122 DPS | 1,830/shot, 61 DPS |

- User-owned uncommitted edits in the same assets: ISD I hull 28,000 → 35,000, shields 11,000 → 12,000; Empire/Rebellion regen tiers.

## Files

- `Assets/Prefabs/Models/Ships/Empire/View/ISDIShipView.prefab` (8 mounts: `HP_ISD_1_New_TL_01..05`, `HP_ISD_Center_TL_01..03`; save also dropped dead `ionStunPrefab`/`ionFieldBounds`, no code uses them)
- `Assets/Settings/Data/Ship/Empire/ISDIShipData.asset` (`weaponLoadout` rebaked: DualHeavyTurboLaser ×14)
- `Assets/Settings/Data/Factions/Empire/EmpireFaction.asset` (price, description)
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset` (`LaserBeam`)
- `Assets/Settings/Data/Ship/Republic/ImperatorShipData.asset`, `RepublicFaction.asset` (ability, description)
- Test: `BeamColorDataTests` comment only. Reviewed, no edit: `WeaponTaxonomyTests` (loadout bake, variant reason), `EmpireHeroCapitalTests` (ISD II based), `XystonShipTests` (superlaser 46,800 ≥ ≈30,600 needed for ISD I). Not run.

## Guidance

- No reusable guidance beyond this note.
