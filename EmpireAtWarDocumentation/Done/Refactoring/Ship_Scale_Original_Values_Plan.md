---
category: Refactoring
status: done
created: 2026-10-26
completed: 2026-10-29
acceptance: pending-user
---
# Ship Scale Original Values Plan

> Closed 2026-10-29 by backlog triage: implementation complete; open user gates → [[TODOs/User_Acceptance_Queue]].

## Goal

- Standardize 56 roster ships against Venator = `1.00` = `1137 m`; squadrons excluded.
- Preserve the user's original ratios, lengths, EST labels and variant constraints.
- Preserve the measured pre-change baseline; apply no Unity changes during plan creation.

## Rules

- Execution authorized 2026-10-26, including a scoped commit. Home One and MC80 Independence must retain their exact pre-change sizes and dependencies.
- Formula: `ratio = shipLengthMeters / 1137`; `targetWorldLength = referenceWorldLength × ratio`.
- Original values below are user-supplied specifications. Non-EST entries were described as published lengths; lore sources were not independently verified.
- Listed EST values and standard variants accepted for this execution; Home One and MC80 Independence preserve current geometry by explicit override.
- Keep scaling separate from HP, shields, weapons, movement, economy and other combat tuning.
- Preserve hardpoint, engine, VFX, collider, shield and wreck alignment. Do not assign ratios directly to `Transform.localScale` unless source units are verified consistent.
- The `5%` threshold is audit triage only; it is not an implementation tolerance.

## Decision

- Chosen: separate geometry plan from [[TODOs/Refactoring/Original_Values_Rebalance|Original Values Rebalance]].
- Why: hull size normalization and combat rebalance have different scope and verification.
- Avoid: changing combat stats to compensate for geometry, replacing models, changing variants, or stretching one axis to force dimensions.
- Approved revision 2026-10-26: restore Captor to its exact pre-migration size (`140 Unity units`, approximately `1333.35 m`); Patrol Frigate = `400 m`. Historical target table retained.
- Approved targets: exact meter-derived lengths for all other ships; retain supplied ratios as historical display values. MC30c = 380 m; EST values accepted; C-9979 retains current proportions.
- Precision: `meters / 1137`; dimensional tolerance 0.01% relative after Unity float serialization. Venator reference remains fixed.

## Important Values

- Audit date: `2026-10-26`; live Unity prefab geometry, including existing uncommitted changes.
- Reference: `Assets/Prefabs/Models/Ships/Republic/VenatorShipView.prefab`; longitudinal extent `119.383362 Unity units`.
- Baseline: transformed active/enabled mesh vertices; shields, shadow meshes, lamps, engine glow, thrusters and muzzle flashes excluded.
- Read-only inspection used `AssetDatabase.LoadAssetAtPath`, mesh traversal and `MeshUtility.AcquireReadOnlyMeshData`; no scene instances or asset writes.
- Existing ratio = measured extent / reference extent. Difference = `100 × (existingRatio / suppliedRatio − 1)`.
- Audit result: `23/56` ships differ by more than `5%`; remaining `33/56` differ by at most `2.61%`.

### Original values and measured baseline

- Target columns reproduce the user's original values. Current units/ratios are rounded audit measurements, not serialized Transform scales or rollback data.
- Prefab ID normally maps to `<ID>ShipView.prefab`; exceptions appear in Notes.

### Empire

| Ship | Prefab ID | Original m | Original ratio | Current units | Current ratio | Difference | Notes |
|---|---|---:|---:|---:|---:|---:|---|
| AcclamatorAssault | `AcclamatorAssault` | 752 | 0.66 | 78.959 | 0.661 | +0.2% | — |
| Allegiance-class Battlecruiser | `Allegiance` | 2200 | 1.93 | 230.990 | 1.935 | +0.3% | — |
| ArquitensImperialCruiser | `ArquitensImperialCruiser` | 325 | 0.29 | 34.127 | 0.286 | -1.4% | — |
| Executor Star Dreadnought | `Executor` | 19000 | 16.71 | 1994.973 | 16.711 | +0.0% | — |
| Imperial Venator | `ImperialVenator` | 1137 | 1.00 | 119.321 | 0.999 | -0.1% | — |
| ISD I | `ISDI` | 1600 | 1.41 | 167.998 | 1.407 | -0.2% | — |
| ISD II | `ISDII` | 1600 | 1.41 | 180.000 | 1.508 | +6.9% | — |
| Procursator-class Star Destroyer | `Procursator` | 1200 | 1.06 | 126.546 | 1.060 | +0.0% | — |
| Raider Corvette | `RaiderCorvette` | 150 | 0.13 | 15.750 | 0.132 | +1.5% | — |
| Tartan-class Patrol Cruiser | `Tartan` | 250 | 0.22 | 26.385 | 0.221 | +0.5% | — |
| Tector Star Destroyer | `Tector` | 1600 | 1.41 | 180.000 | 1.508 | +6.9% | — |
| Titan-class Battlecruiser | `Titan` | 4500 | 3.96 | 472.494 | 3.958 | -0.1% | Mod |
| Victory I Star Destroyer — Advanced Loadout | `VictoryI` | 900 | 0.79 | 95.189 | 0.797 | +0.9% | — |
| Victory II Star Destroyer — Advanced Loadout | `VictoryII` | 900 | 0.79 | 94.499 | 0.792 | +0.2% | — |

### Rebellion

| Ship | Prefab ID | Original m | Original ratio | Current units | Current ratio | Difference | Notes |
|---|---|---:|---:|---:|---:|---:|---|
| Assault Frigate Mk II | `AssaultFrigateMkII` | 700 | 0.62 | 100.000 | 0.838 | +35.1% | — |
| Bothan Assault Cruiser | `BothanAssaultCruiser` | 850 | 0.75 | 160.000 | 1.340 | +78.7% | — |
| CD-273 Corellian Destroyer | `CD273` | 500 | 0.44 | 59.516 | 0.499 | +13.3% | EST |
| Corellian Battlecruiser | `CorellianBattlecruiser` | 800 | 0.70 | 140.000 | 1.173 | +67.5% | EST |
| Corellian Corvette / CR90 | `CorellianCorvette` | 150 | 0.13 | 15.750 | 0.132 | +1.5% | — |
| Dauntless Heavy Cruiser | `Dauntless` | 1250 | 1.10 | 160.000 | 1.340 | +21.8% | EST; >1200 m |
| Endurance Fleet Carrier | `Endurance` | 1040 | 0.91 | 180.000 | 1.508 | +65.7% | — |
| Home One / Admiral Ackbar | `HomeOne` | 1300 | 1.14 | 398.979 | 3.342 | +193.2% | Legends; conflicting sizes |
| Liberator Cruiser | `Liberator` | 600 | 0.53 | 160.000 | 1.340 | +152.9% | EST |
| MC30c Frigate | `MC30c` | 380 | 0.33 | 80.000 | 0.670 | +103.1% | Alternative 580 m |
| MC50 Medium Cruiser | `MC50` | 650 | 0.57 | 109.348 | 0.916 | +60.7% | EST |
| MC75 Profundity | `MC75Profundity` | 1204.44 | 1.06 | 126.565 | 1.060 | +0.0% | — |
| MC80 Independence | `MC80Independence` | 1300 | 1.14 | 399.102 | 3.343 | +193.2% | — |
| Mon Calamari Star Cruiser / MC80 | `MonCalCruiser` | 1200 | 1.06 | 125.998 | 1.055 | -0.4% | — |
| Nebula Star Destroyer | `Nebula` | 1040 | 0.91 | 160.000 | 1.340 | +47.3% | — |
| Nebulon-B Frigate | `NebulonB` | 300 | 0.26 | 31.500 | 0.264 | +1.5% | — |
| Quasar Fire | `QuasarFire` | 350 | 0.31 | 70.000 | 0.586 | +89.1% | Legends |

### Republic

| Ship | Prefab ID | Original m | Original ratio | Current units | Current ratio | Difference | Notes |
|---|---|---:|---:|---:|---:|---:|---|
| Acclamator | `Acclamator` | 752 | 0.66 | 78.959 | 0.661 | +0.2% | — |
| Arquitens | `Arquitens` | 325 | 0.29 | 34.125 | 0.286 | -1.4% | — |
| Carrack-class Cruiser | `Carrack` | 350 | 0.31 | 37.009 | 0.310 | +0.0% | — |
| Centax-class Heavy Frigate | `Centax` | 500 | 0.44 | 41.784 | 0.350 | -20.5% | EST |
| CR70 Corvette | `CR70` | 130 | 0.11 | 15.750 | 0.132 | +19.9% | — |
| Heavy Dreadnought | `HeavyDreadnought` | 600 | 0.53 | 73.007 | 0.612 | +15.4% | Dreadnaught-class cruiser |
| Imperator-class Star Destroyer | `Imperator` | 1600 | 1.41 | 167.998 | 1.407 | -0.2% | — |
| IPV-2C Stealth Corvette | `StealthCorvette` | 99.7 | 0.09 | 10.469 | 0.088 | -2.6% | — |
| Pelta-class Frigate | `Pelta` | 282 | 0.25 | 29.846 | 0.250 | -0.0% | — |
| Pride of the Core | `Mandator` | 8000 | 7.04 | 839.989 | 7.036 | -0.1% | Mandator II |
| Resolute — Admiral Yularen | `Resolute` | 1137 | 1.00 | 119.383 | 1.000 | +0.0% | — |
| Rothana Star Destroyer | `Rothana` | 2274 | 2.00 | 160.000 | 1.340 | -33.0% | EST; fan design |
| Venator I | `Venator` | 1137 | 1.00 | 119.383 | 1.000 | +0.0% | Reference |
| Venator II | `VenatorII` | 1137 | 1.00 | 119.383 | 1.000 | +0.0% | Uses ResoluteShipView |
| Victory-class Star Destroyer | `Victory` | 900 | 0.79 | 94.499 | 0.792 | +0.2% | Uses Empire/VictoryShipView |

### Separatists

| Ship | Prefab ID | Original m | Original ratio | Current units | Current ratio | Difference | Notes |
|---|---|---:|---:|---:|---:|---:|---|
| C-9979 Lander | `C9979` | 210 | 0.18 | 22.050 | 0.185 | +2.6% | Length; wingspan up to 370 m reported |
| Captor-class Carrier | `Captor` | 500 | 0.44 | 140.000 | 1.173 | +166.5% | EST |
| Dispatcher-class Frigate | `Dispatcher` | 600 | 0.53 | 82.568 | 0.692 | +30.5% | EST |
| Invincible | `Invincible` | 2011 | 1.77 | 318.236 | 2.666 | +50.6% | — |
| Lucrehulk | `Lucrehulk` | 3170 | 2.79 | 332.845 | 2.788 | -0.1% | Diameter |
| Malevolence | `Malevolence` | 4845 | 4.26 | 508.718 | 4.261 | +0.0% | — |
| Munificent | `Munificent` | 825 | 0.73 | 86.624 | 0.726 | -0.6% | — |
| Patrol Frigate | `PatrolFrigate` | 210 | 0.18 | 81.097 | 0.679 | +277.4% | EST |
| Providence | `Providence` | 1088 | 0.96 | 114.238 | 0.957 | -0.3% | Standard; large variant 2177 m |
| Recusant | `Recusant` | 1187 | 1.04 | 124.633 | 1.044 | +0.4% | Standard; large variant 2542 m |

## Edge Cases

- Providence standard `1088 m / 0.96`; large dreadnought `2177 m / 1.91`. Existing ratio `0.957` matches standard.
- Recusant standard `1187 m / 1.04`; larger variant `2542 m / 2.24`. Existing ratio `1.044` matches standard.
- MC30c original `380 m / 0.33`; alternative `580 m / 0.51`. Existing ratio `0.670` exceeds both; user approved 380 m.
- Lucrehulk uses diameter: maximum horizontal mesh extent `332.846 units / 2.788`; do not compare hull thickness or assume conventional length.
- C-9979 length `22.050 units / 0.185` matches closely; width corresponds to approximately `556 m` against the reference. Supplied note reports wingspan up to `370 m` → review model proportions; uniform scaling cannot independently fix width and length.
- Home One has conflicting Canon/Legends sizes; original target was `1300 m / 1.14`; user override retains current size. MC80 Independence likewise retains current size.
- Republic Victory is `ShipType.Victory` → `Empire/VictoryShipView.prefab`; `ThrantaShipView` is not its roster model.
- `VenatorIIShipView` mapping → `Republic/ResoluteShipView.prefab`; editing the shared asset affects Venator II and Resolute together.
- Roster totals: Empire `14`, Rebellion `17`, Republic `15`, Separatists `10`. Disabled/off-roster ISD III and Thranta excluded.

## Implementation

1. Re-read this plan and backlog; remeasure live prefabs and confirm intended variants. Resolve ratio precision and EST decisions before mutations.
2. Snapshot affected asset paths/GUIDs, original serialized root/model scales, hierarchy transforms and dependent geometry/settings. Preserve unrelated uncommitted work.
3. Keep Venator's reference length fixed. Compute `multiplier = targetWorldLength / measuredCurrentLength`; apply uniform scaling at the appropriate hierarchy level.
4. Scale ship geometry and aligned dependencies together; inspect separately rooted wrecks, colliders, shields, engine/muzzle effects and selection anchors.
5. Review geometry-derived `NavigationRadius`, `HullBottom`, `HullTop` and height/selection bounds. Recompute only when required by geometry; document gameplay effects without tuning combat balance.
6. Persist every changed Unity asset with Unity-aware tooling. Disk edits require `AssetDatabase.Refresh()`, path-scoped `ForceReserializeAssets(...)`, `SaveAssets()` and Console verification.
7. Remeasure all 56 roster entries against the fixed reference; verify shared aliases and capture before/after results.
8. Verify hardpoint/engine/VFX/collider/shield/wreck alignment and relevant prefab contracts. Before Unity tests inspect open scenes, resolve dirty named scenes, block dirty untitled scenes; run asynchronous tests and poll to completion.
9. Record actual results and remaining acceptance; move plan/backlog to Done only after the agreed scope and verification are complete.

## Files

- `Assets/Prefabs/Models/Ships/{Empire,Rebellion,Republic,Separatist}/*ShipView.prefab` — measured ship hierarchy.
- `Assets/Prefabs/Models/Wrecks/` — linked wrecks/death visuals; inspect actual references before editing.
- `Assets/Settings/AssetMappingData.asset` — runtime aliases, including Venator II.
- `Assets/Settings/Data/Factions/*/*Faction.asset` — roster membership and display names.
- `Assets/Settings/Data/Ship/*/*ShipData.asset` — geometry-derived settings only if required.
- `Assets/Scripts/Entities/Faction/Model/ShipType.cs`; `Assets/Scripts/Entities/Ship/ShipFactory.cs` — identity/runtime mapping.
- [[TODOs/TODOs|Backlog]]; [[TODOs/Refactoring/Original_Values_Rebalance|Related combat rebalance]].

## TODO

- [x] Preserve all 56 original target ratios, lengths and EST/variant notes.
- [x] Record read-only current geometry baseline and 23 differences above 5%.
- [x] Verify Republic Victory and Venator II prefab mappings.
- [x] Exact meter targets approved; tolerance 0.01% relative.
- [x] EST values, MC30c 380 m and unchanged C-9979 proportions approved; Home One/MC80 Independence explicitly preserved.
- [x] Snapshot paths/GUIDs, serialized assets, all hierarchy matrices and geometry fields in `Logs/ShipScale/before.json` and `before-files/`.
- [x] Applied 2026-10-26: 28 ship roots + 28 linked wreck roots; 28 ShipData hull/navigation fields; world hardpoint VFX scales and Local engine-particle scales.
- [x] Saved through PrefabUtility/AssetDatabase; re-read all 56 entries. Dimensions, aliases and 253324 numeric hierarchy/dependency checks passed. Preserved ships/wrecks/data byte-identical.
- [ ] Run relevant Unity EditMode suites and in-game visual acceptance. User explicitly requested commit with Unity tests pending on 2026-10-26; keep plan active.

## Verification

- `Tools/ShipScale/results.csv` — all 56 measured before/after dimensions and applied multipliers; 28 entries already at target or explicitly preserved required no write.
- `Tools/ShipScale/ShipScaleAudit.cs`, `ApplyShipScale.cs`, `verify.py` — reproducible measurement/migration/check tools; before/after snapshots and rollback copies remain in ignored `Logs/ShipScale/`.
- `Tools/ShipScale/ParticleScale.cs` — resolved 61 systems: 49 Local, 12 Hierarchy, 0 Shape. Seeded simulation/rendered-mesh comparisons at ×0.3 and ×1.5: 98/98 passed, maximum vertex error 0.00005481 units (<0.001).
- Uniform root scaling preserves every descendant alignment; Local leaf particle transforms also scale to preserve rendered exhaust dimensions. Hardpoint destruction effects use world scale and were adjusted separately.
- Wrecks retain authored death geometry; no regeneration or Addressables modifications. HullBottom/HullTop/NavigationRadius scale; movement lanes, combat stats, projectile sizes and weapon-derived muzzle flashes remain unchanged by this task.
- All 28 data diffs contain only three geometry fields; all 56 changed prefabs retain the same external GUID sets. Seven existing prefab tests updated for world-space geometry and positive uniform roots.
- Fresh solid-hull measurement excludes unlit lens/engine-glow meshes; six historical audit extents differ slightly. Historical table retained; current execution measurements in CSV are authoritative. Home One and MC80 Independence preserve exact assets regardless of measurement differences.
- Unity migration and audit scripts compiled without diagnostics; saved assets reloaded for after-capture. No import/serialization errors identified; shared Editor emitted unrelated test errors.
- Pending: Unity suites covering Ship, Wreck, Shield, HardPoint, RaiderCorvette, Tartan, MC30c and selection reuse; in-game hardpoint/shield/destruction/engine visual acceptance. No test pass is claimed for updated NUnit tests.

### Captor and Patrol Frigate revision — 2026-10-26

- User override: Captor restored to original `140 Unity units` (approximately `1333.35 m`); Patrol Frigate changed from `210 m` to `400 m` (`41.99942 Unity units`).
- Ship roots, linked wrecks, hardpoint destruction effects and HullBottom/HullTop/NavigationRadius updated together. Captor ship/wreck/data are byte-identical to the pre-migration snapshot.
- Re-read all 56 assets: 253324 numeric dimension/alignment checks passed again. No new Unity console errors. Unity tests and in-game acceptance remain pending under the prior user instruction.
- Final net migration affects 27 ship/wreck/data sets; Home One, MC80 Independence and Captor preserve pre-migration sizes. `Tools/ShipScale/targets.json` and `results.csv` reflect the overrides.
