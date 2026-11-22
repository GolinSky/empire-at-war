---
type: plan
category: Features
status: done
updated: 2026-11-22
completed: 2026-11-22
acceptance: pending-user
---
# Viscount Import (Prototype + Dreadnought)

## Goal

- Add two Rebellion ships from one holder blend: **Viscount Prototype** (3 km battlecruiser) and **Viscount Dreadnought** (17 km dreadnought, Executor counterpart).
- Keep every source weapon bone position; reuse existing shared weapon types (user decision 2026-11-22).

## Outcome

- Both ships imported, built and registered for the Rebellion roster (`ShipType` 331 / 332). Saved-asset audit `Tools/Blender/Viscount/Verify.cs` → ALL CHECKS PASSED; compile 0 errors. Tests not run (local override); in-game gates → [[TODOs/User_Acceptance_Queue]].

## Decision

| | Prototype | Dreadnought |
|---|---|---|
| `ShipType` | `ViscountPrototype = 331` | `ViscountDreadnought = 332` |
| Source ALO | WS `1382582782` `RV_Viscount.alo` | WS `1770851727` `Rebel_Mon_Calamari_Viscount.ALO` |
| Class | `BattleCruiser` | `Dreadnought` |
| Length (units) | 315 (3 km × 0.105 u/m) | 1,785 (17 km × 0.105 u/m) |
| Hull / shields | 55,000 / 60,000 | 180,000 / 225,000 (Executor 120% / 150%) |
| Regen | Rebellion `BattleCruiser` tier 900/s ×1 | Rebellion `Dreadnought` tier 1,500/s ×1.2 = 1,800/s (Executor 150%) |
| Speed | 26 (Independence) | 13 (Executor 80%); Executor turn/hyperspace/radar/height values |
| Donor | `MC80IndependenceShipView` + data/wreck/preview/matchups | same donor |
| Abilities | `BoostShieldPower` | `BoostShieldPower` |
| Hangars | 300 (8, 1), 301 (8, 1); 2 hangar hardpoints | 300, 301, 1 (8, 2 each); 5 hangar hardpoints |
| Roster (provisional) | lvl 5, 22,000, 85 s, cap 25, max 3 | lvl 5, 80,500 (Executor ×1.15), 150 s, cap 50, max 1 |
| Audio | engine/explosion row cloned from Mon Cal Cruiser 302; voices from 305 | same |

- Scale anchor: Executor 19 km ≈ 1,995 units; Venator 1,137 m = 119.38 units → 0.105 u/m.
- Point defense: none on the Dreadnought (user). Prototype `HP_LC` bones carry lasers.
- Fire arcs: port mounts `-180..15`, starboard `-15..180`, centreline (|x| < 12% half-width) `-180..180`.
- Avoid: new Viscount-specific weapon types; unmounted weapon bones.

## Weapon Mapping

| Prototype bones | Type | Count |
|---|---|---:|
| `HP_TL01–05`, `HP_TL16–20` (dorsal + mirrors) | `HeavyDualTurbolaser` (3) | 10 |
| `HP_TL06–15` (lower flanks) | `MediumQuadTurbolaser` (90) | 10 |
| `HP_IC01–12` | `HeavyDualIonCannon` (91) | 12 |
| `HP_LC01–14` | `MediumDualLaser` (94) | 14 |
| `HP_MIS01–04` | `MediumMissileLauncher` (93) | 4 |
| `HP_TORP01–04` | `HeavyProtonTorpedo` (66) | 4 |
| Systems | 2 `HP_SHIELD`, 5 `HP_E`, 2 `HP_SPAWN` | 9 |

| Dreadnought bones (source projectile) | Type | Count |
|---|---|---:|
| `HP_HTL` heavy + `HP_TL` quad | `HeavyDualTurbolaser` (3) | 24 |
| `HP_TL` dual | `MediumDualTurbolaser` (5) | 16 |
| `HP_TL` single | `MediumTurbolaser` (4) | 20 |
| `HP_IC` quad | `HeavyTripleIonCannon` (68) | 7 |
| `HP_IC` dual | `HeavyDualIonCannon` (91) | 11 |
| `HP_IC` single | `MediumIonCannon` (32) | 12 |
| `HP_PTL` | `MediumProtonTorpedo` (48) | 12 |
| Systems | 4 `HP_Shield_Gen`, 4 `HP_Engine`, 5 `HP_Hangar_01–05` | 13 |

## Important Values

- Effective DPS (accuracy, damage matrix, caliber bias; no arcs/range): Prototype vs battlecruiser = 3,430 shield / 1,571 hull. ISD II 2,376 / 1,279; Home One 2,265 / 1,236; Praetor 4,912 / 3,274.
- Dreadnought vs dreadnought = 6,941 shield (Executor 125%) / 2,594 hull (87%). Target 120% / 90%.
- Saved sizes: Prototype 95.9 × 70.8 × 315.0, nav radius 160; Dreadnought 504.6 × 374.1 × 1,785.0, nav radius 895. Selection ring ratio 0.920 / 0.919 (test range 0.88–0.96).
- Conversion error: Prototype bones 0.00012 / geometry 0.00021; Dreadnought bones 0.00087 / geometry 0.00123 (size-scaled limit 0.005).

## Implementation

- [x] 1. Source audit + PNG textures → `Temp/ViscountImport/<Variant>/` (`Prepare.py`; texture names from material chunks).
- [x] 2. ALO → FBX in isolated windowed Blender 3.6.23 (`Convert.py` + `export_isd_i.py`); per-mesh material rebuild; FBX round-trip verified.
- [x] 3. Unity art (`Stage.py`, `BuildViscount.cs`): FBX scale 0.02, Ship Lit hull + team mask, URP additive lights; helpers stripped from visual prefabs.
- [x] 4. Gameplay views from Independence donor; 54 / 102 weapon mounts + 9 / 13 systems on bone sockets (`*Mounts.json`, `GenerateMounts.py`); shield bake, collider, selection ring, ion bounds, engine plumes (`ShipEnginePlumeSetup`).
- [x] 5. Ship data, wrecks (`ShipWreckBuilder`), placement previews (synced), icons.
- [x] 6. Registrations: `ShipType`, `ShipsData`, `AssetMappingData`, `RebellionUnitViews`/`Data`/`SharedIcons`, `RebellionFaction`, `ShipUiData`, `TooltipIconData`, `ReinforcementData`, `ShipSfxData` (ships + voices), matchups, `ShipIconGenerator.MAPPINGS`.
- [x] 7. Verify saved assets + console; review tests; commit.

## Verification

- `Verify.cs`: ShipUnits/WeaponComponent/Injectables cover every hardpoint; unique IDs; per-hardpoint anchors; loadout == mounts; engines aft (bow +Z); plumes listed + fogged; ring fit; no helper meshes; Addressables groups; icon sprites.
- Console since build: only build-time `ShipReinforcementPreviewPostprocessor` KeyNotFound (fires before registration) and icon render-texture release warning (fixed in script). Final preview sync → "in sync".
- Tests reviewed, not run: `Ships` (plume, system hardpoint — Viscounts added to `ShipSystemHardpointTests`, asset integrity, preview size, helper mesh), `Health` (`HealthOverlayAnchorTests`), `Combat` (`WeaponTaxonomyTests` loadout match), `Audio` (`ShipSfxDataCoverageTests`), `Balance` (`BalanceInventoryTests`), `EnemyAi` (ShipType enumeration).

## Edge Cases

- Prototype mod XML used 14 of 54 weapon bones and listed HIC/CCM entries several times → source counts not authoritative.
- Dreadnought `HP_Shield_Gen` bones unused by mod (phases put shields on `HP_PTL`); we mount shields on `HP_Shield_Gen`.
- `Pe_*` / `Pte_*` ALOs are particle systems (chunk `0x900`); never import them with ALAMO.
- Dreadnought `roles` copied from Executor's working-copy value (65) of the uncommitted ship-role work; HEAD Executor = 102. Recheck when that work lands.
- Reusable rules extracted → [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] (Viscount evidence 2026-11-22).

## Files

- Tooling: `Tools/Blender/Viscount/` (`Prepare.py`, `Convert.py`, `GenerateMounts.py`, `Stage.py`, `BuildViscount.cs`, `Verify.cs`, `*Mounts.json`, `*Materials.json`, `Evidence/`).
- Holder: `C:/Users/golin/Downloads/Viscount-Converted/Viscount.blend`; hardpoint tables `Viscount_*_Hardpoints.csv`, map `Viscount_Hardpoints_Map.svg`.
