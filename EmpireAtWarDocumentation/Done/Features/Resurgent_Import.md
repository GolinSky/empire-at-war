---
type: plan
category: Features
status: done
updated: 2026-11-23
completed: 2026-11-23
acceptance: pending-user
---
# Resurgent Import (Empire battlecruiser: brawler + anti-capital + carrier)

## Goal

- Add the Resurgent-class Star Destroyer to the Empire roster, class `BattleCruiser`.
- Stronger than ISD II, on battlecruiser level. Direct ISD successor for sustained fleet combat.

## Outcome

- `ShipType.Resurgent = 221`. Saved-asset audit `Tools/Blender/Resurgent/Verify.cs` → ALL CHECKS PASSED; compile clean. Tests written/reviewed, not run (local override). Gates → [[TODOs/User_Acceptance_Queue]].
- Saved size `155.73 × 53.73 × 306.16` (2,915.81 m × 0.105), nav radius 156, ring ratio 0.920, pick hull 54 points, 64 weapons + 8 systems (ids 0..71), 44 turret models attached.

## Decision (user, 2026-11-23)

| Item | Value |
|---|---|
| Source | Remake Reforged 5.2 (Workshop `3372762649`): `First_Order_Resurgent.alo`, `Capital_Resurgent.xml`, `HP_TOR_Resurgent.xml` |
| Main battery | Option A: 8 `HeavyOctupleTurbolaser` (source sextuple heavy TL) |
| Heavy missiles | 2 `HeavyAssaultMissile` (`HeavyArtilleryRocket` is Executor-scoped and unused) |
| Price | `35,000` |
| Length | `2,915.81 m` → `306.16` units |
| Team color | No stripes. Source albedo has no hue (grey, mean saturation 0.02–0.13) and alpha 0 → no team colour yet. |

- Provisional (not confirmed by user): hull/shields `68,000 / 32,000`; Power to Main Batteries + Tractor Beam; hangar TIE Avenger 4/1 + TIE Defender 2/1 + TIE Interceptor 3/1; level 5, build 110 s, capacity 40, max 2; speed 20, rotation 6, turn accel 3, range 850, regen tier 4, height tier `Lowest`.
- Roles `Dps | Fragile | Carrier | AntiCapital`: at 35,000 the ship has 2.86 EHP/credit vs Empire BC median 4.48 → rule gives Fragile, not Brawler. Brawler would need EHP > ~157k at 35,000, or price < ~22,300.
- Side effect: third Empire BC → class medians become Empire-only; `Slow` removed from Allegiance and Titan (speed 20 = median).

## Revision (user, 2026-11-23)

- Roster edited by user: price `30,000`, capacity `25` (builder constants synced so `Register` keeps them).
- Light calibre → medium, same mount: 10 `LightDualTurbolaser` → `DualMediumTurboLaser` (5); 18 `Laser` → `MediumLaser` (23).
- DPS 2,756 → 2,880; vs Capital 2,259 → 2,351; vs shields 3,199 → 3,291; vs Fighter 344 → 311 (medium lasers lose the light-calibre strikecraft bias).
- Roles unchanged by rule at 30,000: 3.33 EHP/credit < Empire BC median 4.80 → Fragile. Brawler needs EHP > 144k at 30,000, or price < ~20,800.
- Weakness drivers (paper): capDPS per 1,000 cr 78 vs Allegiance 165 / ISD II 172; 72 hardpoints → 1,222 health per weapon vs Allegiance 2,840 / Titan 2,927; 18 lasers + 12 ion = 30 of 64 mounts deal ≤ 14 hull DPS each vs capitals.

## Revision 2 (user, 2026-11-23)

- User set hull/shields `70,000 / 35,000`, regen delay `0.8 s`, price `18,000` (builder constants synced).
- Roles → `Balanced | Dps | Carrier | AntiCapital`: 5.83 EHP/credit is the Empire BC median (Allegiance 6.20, Titan 4.80).
- Paper per 1,000 cr: EHP 5.83 (Allegiance 6.20, ISD II 6.75); vsCap 131 (Allegiance 165, ISD II 172). Per pop (25): EHP 4,200 (Allegiance 4,650), vsCap 94 (Allegiance 124). Regen 700/s, full in 50 s.
- Population then set to `20` (user): EHP 5,250 and vsCap 118 per pop.

## Hardpoints (72, ids in this order)

| IDs | Source bones | Type | Weapon (`WeaponType`) | Turret model |
|---|---|---|---|---|
| 0–7 | `HSTL_01–08` | Weapon, main | `HeavyOctupleTurbolaser` (85) | `_STL` |
| 8–15 | `LQTL_01–08` | Weapon, main | `HeavyQuadTurbolaser` (89) | `Neutral_Republic_Secutor_QTL` |
| 16–25 | `LDTL_01–10` | Weapon | `LightDualTurbolaser` (36) | `_LC` |
| 26–33 | `MIC_01–08` | Weapon | `MediumIonCannon` (32) | `_LC` |
| 34–37 | `QHIC_01–04` | Weapon | `HeavyDualIonCannon` (91) | `_QTL` |
| 38–43 | `ML_01–06` | Weapon | `MediumMissileLauncher` (93) | `_ML` |
| 44–45 | `DB_RKT_01–02` | Weapon | `HeavyAssaultMissile` (65) | none |
| 46–63 | `QL_01–18` | Weapon | `Laser` (8) | none |
| 64–65 | `S_01–02` | ShieldGenerator | — | — |
| 66–68 | `HP_E1–3` | Engines | — | — |
| 69 | `HP_TRAC_BONE_00` | TractorBeam | — | — |
| 70 | `SPAWN_01` | Hangar | — | — |
| 71 | `HP_SG` | CommandArray | — | — |

- Arcs (ISD II rule): side main −10…135 / −135…10; other side guns −15…160 / −160…15; within 8% of half-width of keel −142…142.

## Important Values

| Ship | Class | Hull / Shields | Base DPS | vs Capital |
|---|---|---|---:|---:|
| ISD II | Capital | 40,000 / 14,000 | 1,514 | 1,373 |
| Allegiance | BattleCruiser | 65,000 / 28,000 | 2,501 | 2,473 |
| Titan | BattleCruiser | 72,450 / 39,600 | 1,290 | 1,223 |
| Resurgent | BattleCruiser | 68,000 / 32,000 | 2,757 | 2,259 |

- Hardpoint health: 72 points split `hull × 1.35` → ~1,222 per weapon (ISD II 1,617, Allegiance 2,840).

## Implementation

- [x] `Prepare.py` audit + PNGs (all bones present; `W_PLANETCLOUDS_NONE_B.dds` missing → no normal on 2 hangar-light panels).
- [x] `Convert.py`: hull bone err 2.4e-4 / geo 2.7e-4 (tol 0.002); 5 turrets ≤ 2.3e-5.
- [x] `Stage.py`; `BuildResurgent.cs` `Build` → `Register`; ISD II view donor (hangar), Allegiance command-array template, ISD II data/wreck/preview/audio donors, Allegiance matchups.
- [x] Code: `ShipType.Resurgent = 221`, `ShipIconGenerator` mapping.
- [x] Registrations: ShipsData, AssetMappingData, EmpireUnitViews/Data/SharedIcons, EmpireFaction, ShipUiData, TooltipIconData, ReinforcementData (synced), ShipSfxData (ISD II rows).
- [x] Tests: `ResurgentShipTests` (Ships), `ShipSystemHardpointTests` + Resurgent case. Reviewed (no change needed): `ShipRoleValidationTests` (roles fixed in data), `HazardHeightTests`/`AsteroidFieldHeightTests` (tier Lowest span −271.7…−212.0 inside bands), `ShipSfxDataCoverageTests`, `ShipReinforcementPreviewSizeTests`, `ShipAssetIntegrityTests`, `UnitWreckAssetDependencyTests`.

## Edge Cases

- `ISDIIShipView` root scale 0.933 → reset to 1 or the ship spawns 6.7% short.
- First save logs a `ShipReinforcementPreviewSync` NullReference (view saved before registration); `Register` syncs afterwards (in sync).
- Reusable rules extracted → [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] (Resurgent import evidence).

## Files

- Tooling: `Tools/Blender/Resurgent/` (`Prepare.py`, `Convert.py`, `Stage.py`, `ResurgentMounts.json`, `*Materials.json`, `BuildResurgent.cs`, `Render.cs`, `Verify.cs`).
- Previews: `Temp/ResurgentImport/Previews/` (ignored; rerun `Render.cs`).
