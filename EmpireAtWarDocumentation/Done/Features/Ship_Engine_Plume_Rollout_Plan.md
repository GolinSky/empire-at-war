---
category: Features
status: done
created: 2026-11-20
system:
  - ship-vfx
  - battle-rendering
tags:
  - vfx
  - optimization
  - unity
completed: 2026-11-20
acceptance: pending-user
---
# Ship Engine Plume Rollout

## Goal

- One shared, scalable engine exhaust for all 73 ship view prefabs (Empire 17, Rebellion 21, Republic 16, Separatist 19).
- Per ship: nozzle position + one uniform scale per nozzle. No per-ship curves, colors or lifetimes.
- Replace the CR70-only `TrailRenderer` ([[AssetPipeline/Guides/SHIP_ENGINE_TRAIL_GUIDE]]) with the same look.

## Decision

- **Chosen (user, 2026-11-20):** local-space `LineRenderer` plume, length driven by current speed.
- **Why:** trail length = speed × `Time`. CR70 ratio (length ≈ 14 × width) needs `Time` 0.26 s at 50 u/s, ≈4.8 s for ISD II (20 u/s), ≈35 s for Executor (16 u/s) → streak hangs behind while a capital turns in place.
- **Why:** local-space `LineRenderer` width and positions scale with the Transform. Editor probe 2026-11-20: scale 10 → world bounds 10× in width and length.
- **Avoid:** `TrailRenderer` on any ship; a second exhaust prefab; per-ship width curves or gradients.
- **Cost:** plume loses the bend in turns that the trail had.

## Validation of the current CR70 setup (2026-11-20)

- Look, material, texture, fog list, hyperspace gating → correct, user-approved.
- `ShipMoveComponent.engineTrail` is one field → multi-engine ships impossible.
- Four `engineTrail != null` guards → violates the no-silent-null rule; an array removes them.
- Trail sits inside the model prefab instance under FBX node `Engine` (lossy scale ≈347) as an added-GameObject override → local position `(0.0088, 0.0001, -0.0013)`; breaks if the model node is renamed or reimported.
- Fixed `Time` 0.26 s → does not transfer to other speeds or sizes (see Decision).
- Cost per moving ship: ≈22 vertices rebuilt every frame from point history. Plume: 6 vertices, no history.
- 52 legacy `Engines` particle systems on 18 ship prefabs are inactive; positions are stale (Executor emitters at `z ≈ -58`, hull aft at `z = -997`). Not usable as nozzle markers.
- 15 ship prefabs have no renderer or material named engine/glow/thrust/exhaust → nozzles must be located from a rendered aft view.

## Design

### Shared prefab

- `Assets/Prefabs/Vfx/Ships/ShipEnginePlume.prefab`: one GameObject, one `LineRenderer`.
- Authored at unit width, so **instance uniform scale = plume width in ship units**.

| Setting | Value |
|---|---|
| Use World Space | off |
| Positions | `(0,0,0)`, `(0,0,-1.96)`, `(0,0,-14)` |
| Width multiplier / curve | 1 / `(0, 1)`, `(0.14, 1)`, `(1, 0.05)` |
| Color | `(1, 0.99, 0.96)` → `(1, 0.91, 0.80)` |
| Alpha | `(0, 0.85)`, `(0.14, 0.64)`, `(1, 0)` |
| Alignment / Texture mode | View / Stretch |
| Material | `ShipEngineTrail.mat` (unchanged, shared) |
| Corner / cap vertices | 0 / 0 |
| Shadows, probes, motion vectors, lighting data | off |

- CR70 check: scale 0.95 → width 0.95, length 13.3 (approved trail: 0.95 / ≈13).

### Runtime

- `ShipMoveComponent`: replace `TrailRenderer engineTrail` with `LineRenderer[] enginePlumes`.
- New plain class `ShipEnginePlumes(LineRenderer[] plumes)` beside `ShipMovementTweenPlayer`; one method `SetThrottle(float fraction)`.
- `Tick()` → `SetThrottle(Model.Phase == MovementPhase.Arriving ? 0f : _motion.CurrentSpeed / Model.UndamagedSpeed)`.
- `SetThrottle`: skip when the fraction changed < 0.01; else `SetPosition(1, z = -1.96 × f)`, `SetPosition(2, z = -14 × f)`; `enabled = f > 0`.
- Stopped, ion-disabled, anchored, hyperspace → fraction 0 → plume off. Engine damage → shorter plume. No `Clear()` or emission state.
- Fog keeps using `Renderer.forceRenderingOff`; it stays independent of `enabled`.
- Ships not yet set up → empty array → no guards.

### Placement tool

- `Assets/Scripts/Editor/Ship/ShipEnginePlumeSetup.cs`, static `Apply(string prefabPath, Vector3[] rootSpaceNozzles, float[] widths)`.
- Steps inside `PrefabUtility.LoadPrefabContents`:
  1. Remove existing plume instances and any `TrailRenderer` engine trail.
  2. `PrefabUtility.InstantiatePrefab` the shared plume under `ShipMoveComponent.bodyTransform` (follows bank, near-unit scale).
  3. Set position from root space; rotation identity; `localScale = width / bodyTransform.lossyScale.x`; layer = body layer.
  4. Write `ShipMoveComponent.enginePlumes`; replace plume entries in `FogVisibilityComponent.renderers`.
  5. `SaveAsPrefabAsset`, `UnloadPrefabContents`, `AssetDatabase.SaveAssets()`.
- Idempotent: rerun with new numbers to rescale or move.

## Per-ship flow

1. **Measure.** Read-only `script eval` lists engine renderers with root-space bounds. Nozzle = bounds center `x, y`; `z` = aft face (`bounds.min.z`). Width = nozzle short side.
2. **Split merged meshes.** One glow mesh covering several nozzles (ISD `Engine_Glow_Big` 55 × 12) → cluster vertices on X, one nozzle per cluster.
3. **No named engine mesh** → render the prefab from behind in a preview scene, read nozzle centers from the image.
4. **Choose count.** Main nozzles only. Budget: ≤3 plumes per ship, ≤5 for hulls over 400 units. Wide flat banks (MC80 20 × 3.4) → 2–3 plumes of width = bank height.
5. **Apply** with `ShipEnginePlumeSetup.Apply`.
6. **Verify.** Console clean; prefab reloaded; preview-scene render from above and from the side at throttle 1 → plume starts at the nozzle exit, no gap, no overlap with hull.
7. Record nozzle count in the batch checklist below.

## Phases

- [x] **1. System.** Shared prefab, `ShipEnginePlumes`, `ShipMoveComponent` change, `ShipEnginePlumeSetup`. Replaced the `engineTrail` case in `ShipNavigationServiceTests` with throttle cases (0 → disabled, 0.5/1/2 → length, drop to 0 → disabled, arriving → disabled).
- [x] **2. Reference ships.** CR70 `(0.005, -0.251, -6.830)` w 0.95, ISD II, Executor. User skipped the pre-batch gate (2026-11-20: "all ships"); in-game look moved to the acceptance queue.
- [x] **3. Contract test.** `ShipEnginePlumePrefabTests` (`Ships`): shared instance, overrides limited to Transform/name/layer/`widthMultiplier`, listed in `enginePlumes` + fog `renderers`, width = scale, no `TrailRenderer`, ≥1 plume.
- [x] **4. Empire** (17).
- [x] **5. Republic** (16).
- [x] **6. Rebellion** (21).
- [x] **7. Separatist** (19).
- [x] **8. Close.** Guide rewritten → [[AssetPipeline/Guides/SHIP_ENGINE_TRAIL_GUIDE]]. Battle draw/overdraw capture → user (agents do not enter Play Mode).

## Outcome (2026-11-20)

- 73/73 ship views have plumes (1–5 each). Chimaera and Devastator excluded by user (new, uncommitted hero ships, not edited). Both are `ISDIIShipView` variants → inherit its 3 plumes and `enginePlumes`. Devastator passes all checks. Chimaera overrides fog `renderers` without the plumes → visible through fog; fix in the Chimaera work, `ShipEnginePlumePrefabTests` fails on it until then.
- Read-back check on all 73: plumes are shared instances, in both lists, width = scale, no trail. Unity compile 0 errors. Aft + side preview renders reviewed per ship.
- Tests not run (local override). In-game look and battle cost → [[TODOs/User_Acceptance_Queue]].

### Decision changes

- **Width ignores scale.** View-aligned `LineRenderer` width does not follow Transform scale (probe: scale 10 → length 10×, width 1×). Setup tool also sets `widthMultiplier = width`. The plan's "scale = width" assumption was wrong.
- **Budget raised** to ≤4 (≤5 over 400 units): symmetric quads (Home One, MC80 Independence, Justice, Resolute, Venator, Rothana, Remonda, Malevolence, CD273, NebulonB, MC75) read better than 3.
- **Exit z** = more aft of center ray hit and ring median (ring radius = width/2); glow-mesh `min.z` alone was wrong on Executor/Acclamator.
- Nozzle data per ship: aft render + connected pieces of glow meshes; MC50 from mesh pieces after an image misread.

### Reusable guidance

- Extracted to [[AssetPipeline/Guides/SHIP_ENGINE_TRAIL_GUIDE]] (rules, placement procedure, edge cases).

## Optional visual upgrades (not in scope until approved)

- Engine color per faction: prefab variants overriding only the gradient (same material → still one shared material). Match existing glow meshes (ISD blue/white).
- Brighter core: `DefaultVolumeProfile` bloom intensity is 0, threshold 0.9 → an HDR tint has no effect until bloom is enabled. Battle scene volumes not checked.
- Capital length: if 14 × width reads too long on ISD/Executor in the Phase 2 gate, shorten the ratio in the shared prefab for all ships, not per ship.

## Edge Cases

- Non-uniform scale on a plume → width undefined; throttle changes positions, never scale.
- Nozzle recessed in the hull → move `z` aft to the exit plane (CR70: `-6.420` → `-6.830`).
- Ship pooled or restored from save → plume state derives from speed each tick; nothing to reset.
- Model reimport → plumes live under `bodyTransform` in the view prefab, not inside the model instance.

## Open

- Remove the 52 inactive legacy `Engines` particle systems (18 prefabs)? Needs user approval; separate cleanup.
- Plume budget (≤3 / ≤5) and 0.01 throttle step are starting values; unmeasured.
- Batching of many `LineRenderer`s sharing one material: not measured in this project.

## Files

- `Assets/Prefabs/Models/Ships/*/View/*ShipView.prefab` (73)
- `Assets/Scripts/Components/Ship/Movement/ShipMoveComponent.cs`, `ShipMovementTweenPlayer.cs`
- `Assets/Scripts/Components/FogOfWar/FogVisibilityComponent.cs`
- `Assets/Art/Materials/Vfx/ShipEngineTrail.mat`, `Assets/Art/Textures/Vfx/ShipEngineTrailProfile.png`
- `Assets/Scripts/Tests/Editor/Navigation/ShipNavigationServiceTests.cs`
- [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]
