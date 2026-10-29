---
category: Refactoring
status: in-progress
created: 2026-10-29
---
# Hardpoint Health From Hull

## Goal
- Hardpoint HP derives from ship max hull → every hull change (data, research, level) scales hardpoints.
- Total hardpoint HP slightly > hull → a ship can never survive with all hardpoints destroyed.
- Per-class minimum hardpoint HP → corvettes/frigates keep weapons longer at low hull.

## Problem (pre-implementation snapshot, 2026-10-29)
- `HardPointHealth.health` is a flat per-type value in `ShipData.hardPointHealth`; `HealthModel.InitializeHardPoints` ignores `MaxHull`.
- `CombatModifiers.HullMultiplier` (research) and `RefreshStatModifiers` scale hull only, not hardpoints.
- Σ hardpoint HP / hull ranges `0.16×` (Bulwark) → `6.11×` (MC75); 13 ships < `0.5×`, 12 ships > `2.2×`. Full table: [[TODOs/Refactoring/Hardpoint_Health_From_Hull_Plan - Research|Research]].
- Ratio < 1 → ship stripped of all hardpoints keeps flying (Venator `0.37×`, Lucrehulk `0.21×`).
- Ratio ≫ 1 → hardpoints almost never die; sniping systems is pointless (VictoryII `4.38×`).

## Key fact: the guarantee is already mathematical
- `DamageHardPoint`: hardpoint takes `d`, hull takes `d × hullDamageMultiplier`; overkill also reaches the hull. All 284 entries use multiplier `1`.
- → hull damage ≥ damage absorbed by hardpoints. Killing all hardpoints needs ≥ `ΣHP` absorbed → hull already ≥ `ΣHP` damaged.
- `ΣHP ≥ Hull` and multiplier `1` ⇒ hull reaches 0 first. No "disabled ship" state needed (the pasted proposal's "cannot eliminate" does not apply here).
- Only breakers: multiplier < 1, or hull repair beyond hardpoint repair (no ship hull repair found in source).

## Decision (implemented 2026-10-29)
- Formula per installed hardpoint `i`:
  `HP_i = max(Min[class], MaxHull × R × W[type_i] × w_i / Σ(W[type] × w))`
- `R = 1.15` global budget ratio → ~13% of hardpoint HP still alive at death in a pure hardpoint-fire case. `1.5` keeps most systems alive at death (weak sniping value).
- `MaxHull` = live value (`_data.Hull × _hullMultiplier × _levelHullScale × _constructionScale`); Σ over installed hardpoints only (`UnlockLevel ≤ level`).
- `hullDamageMultiplier` fixed at `1` → remove the field from ships (guarantee depends on it).
- Min floor raises the ratio above `R` only for small/many-hardpoint ships → this is the requested small-ship advantage.
- Why runtime, not baked data: research/levels change `MaxHull` mid-battle; one source of truth.
- Avoid: per-ship flat HP lists, per-ship ratio overrides (v1), disabled-ship state.

## Important Values (starting point)
| Type | Weight `W` |
|---|---|
| Weapon | 1.0 |
| PointDefense / MissileDefense | 0.7 |
| Engines | 1.5 |
| ShieldGenerator | 1.5 |
| Hangar / SupplyDock | 1.2 |
| TractorBeam / IonPulseCannon / SensorArray / CommandArray | 1.2 |

| Class | `Min` HP |
|---|---|
| Corvette | 100 |
| Frigate | 150 |
| Cruiser | 200 |
| Capital | 250 |
| HeavyCapital | 300 |

- Worked examples (`R = 1.15`):
  - CR90 hull `600`, 10 W + E + SG (ΣW 13): computed `53/80` → floor `100` each; Σ `1200` = `2.0×` (now `3.67×`).
  - Venator hull `7500`, 3 W + E + SG + Hangar (ΣW 7.2): W `1198`, E/SG `1797`, Hangar `1438`; Σ `8625` = `1.15×` (now `0.37×`).
  - ISD II hull `14700`, 28 W + E + SG + Hangar + Tractor (ΣW 33.2): W `509`, E/SG `764`; Σ `16905` = `1.15×` (now `1.80×`).
  - Executor hull `300000`, 182 hardpoints (ΣW ≈185.4): W `1861`; Σ `345000` = `1.15×` (now `1.56×`).

## Implementation
1. `HardPointHealthSettings` ScriptableObject in `Assets/Settings`: `budgetRatio`, type weights, per-class min. Injected via VContainer.
2. Pure calculator `HardPointHealthBudget` (no UnityEngine): `(maxHull, class, hardpoints) → HP[]`. Shared by `HealthModel`, `SimUnitFactory`, Balance Editor.
3. `HealthModel.InitializeHardPoints`: use calculator instead of `GetHardPointHealth`; multiplier `1`.
4. `HealthModel.RefreshStatModifiers`: hull multiplier change → `HardPointModel.ScaleHealth(ratio)` (keeps percentages).
5. `Upgrade` / `SetConstructionProgress`: recompute via calculator; keep current damage-preserving rules.
6. `HardPoint.healthOverride` → `healthWeight` (default `1`, multiplies `W`). Migrate 11 overrides (Peleset, Recalcitrant, Wavecrest) to equivalent weights.
7. Remove `hardPointHealth` from `ShipData`; update `ShipHardPointValidator`, `BalanceValidation`, `BalanceUnitAdapter`, `AloExistingShipBuilder`, `SimUnitDefinition`.
8. Balance Editor: show computed per-hardpoint HP and Σ/hull per ship; validator warns if Σ/hull < `1.0`.
9. Simulator before/after run against `Logs/Rebalance/2026-10-25-baseline/`; tune `R`, weights, mins.

## Edge Cases
- No hardpoints → hull only (unchanged path `HardPointModels.Length == 0`).
- Hit on destroyed hardpoint → split across live ones (unchanged, still 1:1 to hull).
- Level unlock adds hardpoints → existing hardpoints shrink unless level hull scale compensates; recompute restores full HP (current `Upgrade` behavior).
- Saves store percentages → compatible; layout check in `Restore` unchanged.
- Shield generator HP grows on low-ratio ships → shields harder to strip (Venator SG `600 → 1797`); check ion/shield-piercing balance.
- High-ratio ships (MC75, VictoryI/II, ISD III, MC80) lose hardpoint durability sharply → expect large balance shift.

## Execution Decisions
- Scope: ships only. Structures retain `EntityComponentData` flat health and `hardPointScale`.
- Weapon weight: one `Weapon` weight × per-hardpoint `healthWeight`.
- `R = 1.15`; class minimums use the table above.
- Retain starting values until user skirmish/final tuning acceptance.

## Files
- `Assets/Scripts/Components/Health/HealthModel.cs`
- `Assets/Scripts/Components/Health/HardPoints/HardPointModel.cs`
- `Assets/Scripts/Components/Health/Data/HardPointHealth.cs`, `IHealthData.cs`, `ResearchHealthData.cs`
- `Assets/Scripts/Components/ViewComponents/Health/HardPoint.cs`
- `Assets/Scripts/Entities/Ship/Data/ShipData.cs`
- `Assets/Scripts/Editor/BattleSimulator/SimUnitFactory.cs`, `SimUnitDefinition.cs`
- `Assets/Scripts/Editor/Balance/BalanceValidation.cs`, `BalanceUnitAdapter.cs`
- `Assets/Scripts/Editor/Ship/HardPoints/ShipHardPointValidator.cs`, `Editor/AloImport/AloExistingShipBuilder.cs`

## Verification
- Test categories touched: `Health`, `Ships`, `Balance`, `Simulation`. Tests asserting per-type HP need rewrite (`CorvetteLoadoutTests`, `ShipSystemHardpointTests`, `ShipAssetIntegrityTests`, `TitanShipTests`, `HealthModelTests`, `BattleSimulationTests`).
- New tests: Σ/hull ≥ `R` for every ship; min floor; hull-multiplier change scales hardpoints; "all hardpoints destroyed ⇒ hull ≤ 0".
- Local test execution disabled → run only on user request. User skirmish playtest.

## TODO
- [x] Execution authorized 2026-10-29: recommended ships-only scope, type weights × per-hardpoint weight, `R = 1.15`, class minimums. Structures retain flat health.
- [x] Steps 1–5 runtime: shared pure calculator, settings injection, modifier/upgrade/construction recalculation.
- [x] Steps 6–7 data migration: 62 ship data assets, 61 view prefabs, 11 overrides → relative weights; structures retain flat health.
- [x] Step 8 tooling: draft-aware computed per-mount HP, totals/ratio, editable weights, validators, simulator and ALO import checks.
- [x] Step 9 simulator comparison: 305 matchups × 10 seeds; 60 roster ships; zero definition errors. Retain proposed settings pending isolated balance acceptance.
- [ ] Step 9 user skirmish playtest and final tuning acceptance; plan remains active.

## Implementation Evidence — 2026-10-29
- Unity compilation/import/save clean; 123 migrated assets persisted; no console errors at verification.
- Simulator: 305 matchups × 10 seeds, 60 roster ships, zero definition errors. Export: `Logs/Rebalance/2026-10-29-hardpoint-health/`.
- Regression coverage added for weighting, class floor, hull modifier fractions, unlock rebudgeting, construction damage preservation, hull-death guarantee and saved fleet budget. Tests not run under local policy.
- Existing untracked `Tools/AloImport/Recipes/Rothana.json` contains old numeric HP baselines. Import verification correctly requires refreshing those baselines to computed HP; unrelated in-progress import work preserved.
- Remaining: user skirmish/final tuning acceptance. Keep plan active; do not archive as fully accepted.

## Verification Review — 2026-10-29
- Commit `08ca1d8f6` reviewed against plan steps 1–8: all present. Uncommitted ship prefab/data edits (`rangeMultiplier`, `Range`) are unrelated work.
- Runtime: `ProjectScope` registers `HardPointHealthSettings.CreateBudget()`; `ProjectContext.prefab` references the asset; `HealthModel` ctor receives it. Ships → budget, multiplier `1`; structures → flat path unchanged.
- Settings asset: `R = 1.15`, weights and class minimums match plan; arrays indexed by `HardPointType` (12) / `ShipClass` (9).
- Data: 0 ship assets keep `hardPointHealth`; 0 ship mounts keep `healthOverride > 0`; 1256 mounts have `healthWeight` (9 non-1 weights from the 11 old overrides).
- Independent recompute from assets: 61/61 ships Σ/hull ≥ `1.15` (range `1.15`–`2.00`; corvettes at floor: CR90/Raider `2.00`, Tartan `1.83`). Matches `Logs/Rebalance/2026-10-29-hardpoint-health/health.csv`.
- Unity: Editor ready, not compiling, no compile errors; 2 console exceptions unrelated (MusicService teardown, `DefendPlatformCatalog` `BattleAsteroid`).
- Tests: 11 new tests authored; not run (local policy).
- Minor findings:
  - Fixed 2026-10-29: removed `HealthModel` 3-arg code-default ctor (and now-unneeded `[Inject]`); 13 test constructions pass `new HardPointHealthBudget()` (same values).
  - Fixed 2026-10-29: `ShipHardPointValidator` reports ship mounts with `healthOverride > 0` (ignored by ships → use `healthWeight`).
  - Fixes verified: Unity recompile clean, 0 console errors. Tests not run (local policy).
  - Open: mid-battle hull research increase: floor-bound mounts scale by less than the hull factor; guarantee holds in practice but is not strictly proven for that case.
