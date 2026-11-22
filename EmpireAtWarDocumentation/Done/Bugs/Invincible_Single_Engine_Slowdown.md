---
category: Bugs
status: done
created: 2026-11-22
completed: 2026-11-22
acceptance: pending-user
---
# Engine Hardpoints Match Visual Engines (Invincible, Recusant Dreadnought, Procurator, Secutor)

Related: [[Done/Features/Invincible_Import]], [[Done/Features/Recusant_Dreadnought_Import]], [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]], [[AssetPipeline/Guides/SHIP_ENGINE_TRAIL_GUIDE]]

## Outcome (2026-11-22)

- Invincible 1 → 3 engine hardpoints; Secutor 2 → 3. Recusant Dreadnought (3) and Procurator (2) already matched → unchanged.
- First engine loss: Invincible 25 → 20 speed (was → 10). Secutor → 20.
- Verification: `ShipHardPointValidator.Validate` 0 issues on all 4 (wiring, Ids = index, hull health budget); Unity compile 0 errors; preview-scene renders confirm hardpoint + plume on each main engine. Tests not run (local override). In-game check → [[TODOs/User_Acceptance_Queue]].

## Decision

- Chosen (user, 2026-11-22): **main engines only** — one hardpoint per main engine / engine pod; small auxiliary nozzles get none.
- Why: speed loss per destroyed engine = `0.6 / engineCount`; small engine share of the hardpoint health budget; matches plume counts.
- Avoid: one per nozzle (Secutor 9, Recusant ~12, Procurator ~13) → engines dominate the health budget. Raising `MinMoveCoefficient` / `healthWeight` per ship.

## Engine count (step 1, preview renders `Logs/EngineRender/`)

| Ship | Main engines | Small nozzles | Engine hardpoints after |
|---|---|---|---|
| Invincible | 3 (center + 2 side) | 0 | 3 (`Id` 27, 30, 31) |
| Secutor | 3 (center + 2 side) | 6 | 3 (`Id` 2, 3, 46) |
| Recusant Dreadnought | 3 pods | ~12 lights on pods | 3 (`Id` 30–32), unchanged |
| Procurator | 2 | ~11 | 2 (`Id` 29, 30), unchanged |

## Implementation

1. [x] Count visible engines (preview-scene renders, 4 ships). Table above.
2. [x] Invincible: `Id` 27 stays center; added `Engines_Port` (`Id` 30) and `Engines_Starboard` (`Id` 31) at side nozzle glow-disc centers `(±12.68, -21.87, -149.79)` root space; clones of `Engines` (weight, `destroyedVfxScale` 0.6635086, own `DestroyedVfxAnchor` on nearest hull vertex).
3. [x] Secutor: `HP_Secutor_E_01/02` (`Id` 2, 3) moved from `(±36.94, -9.39, -99.45)` (inside hull between engines) onto the side main engines `(±19.49, -9.57, -117.33)`; added `HP_Secutor_E_03` (`Id` 46) on the center engine `(0, -5.46, -117.33)`. Hardpoints sit 6 units ahead of the plume exit.
4. [x] Recusant Dreadnought, Procurator: counts match; hardpoints already on their pods/engines → no change.
5. [x] New hardpoints appended to `HealthComponent.ShipUnits` and `EntityBindings.Injectables`; existing Ids unchanged.
6. [x] Invincible plumes moved onto the nozzles: they were saved at 0.6635 × (root scale) of their intended positions, inside the hull. Now `(0, -19.29, -153.75)`, `(±12.81, -22.00, -150.86)`.
7. [x] Validator: 0 issues on all 4. `ShipData` coverage holds (`Engines` type already present on every ship).
8. [x] Tests reviewed: `ShipEngineHardpointTests` (Executor, synthetic), `ShipSystemHardpointTests` (other ships), `ForwardMainBatteryTests` (weapons only), `BattleSaveServiceTests` Secutor test (reads count live) → no change. Added `Validator_MigratesAppendedHardpointsOnceAndPreservesExistingDamage` cases Invincible 30 + 2, Secutor 46 + 1.
9. [x] `Tools/Blender/Invincible/BuildInvincibleShip.cs` creates the two side engines on rebuild. Recusant builder: count unchanged → no edit.
10. [ ] In-game check (user gate): destroy one engine → Invincible 25 → 20; Secutor 25 → 20; old saves with Invincible/Secutor load.

## Save migration

- `BattleSaveValidator`: Invincible 30 → 32, Secutor 46 → 47 hardpoints. Old saves get full-health appended engines; existing damage kept.

## Edge Cases

- More engines → larger engine share of the hull health budget → other hardpoints on Invincible/Secutor lose some health. Budget check still ≥ 1.0 × hull.
- Saves: hardpoint `Id`s persist → appended only, never renumbered.

## Follow-ups (not done)

- Recusant Dreadnought plumes show the same 0.6635 × offset: `(±9.33, -9.74, -66.49)`, `(-0.07, -7.39, -62.47)` vs pod nozzle lights near `(±15, -15, -92)` and top pod `(0, 8, -77)`. Top pod has no plume.
- `ShipHardPointValidator.FindShipData` returns null for views under `Ships/<Faction>/View/` (data lives in `Settings/Data/Ship/<Faction>/`) → budget check silently skipped in the placement window.
- Lucrehulk, HomeOne, Mediator Battlecruiser also have 1 engine hardpoint. Out of scope.

## Reusable guidance

- Plume offset on scaled roots → added to [[AssetPipeline/Guides/SHIP_ENGINE_TRAIL_GUIDE]] Edge Cases.
