---
type: engineering-reference
system:
  - ship-vfx
  - battle-rendering
verified: 2026-11-20
verification: 73 ship view prefabs saved and reloaded by Unity; wiring check
  passed on all 73; aft and side preview-scene renders reviewed per ship. Unity
  compile 0 errors. Tests not run (local override). In-game look and battle
  profiling pending user.
tags:
  - vfx
  - optimization
  - unity
---
# Ship Engine Plumes

## Goal

- One shared engine exhaust on every ship view: `Assets/Prefabs/Vfx/Ships/ShipEnginePlume.prefab` (one local-space `LineRenderer`).
- Per ship: nozzle position, one width per nozzle. No per-ship curves, colors, materials or lifetimes.
- Replaced the CR70-only `TrailRenderer` (2026-11-20). Rollout record → [[Done/Features/Ship_Engine_Plume_Rollout_Plan]].

## Rules

- Never add a `TrailRenderer` to a ship view. `ShipEnginePlumePrefabTests` fails on one.
- Plumes are unmodified instances of the shared prefab. Allowed overrides: Transform, `m_Name`, `m_Layer`, `m_Parameters.widthMultiplier`.
- Every plume is listed in `ShipMoveComponent.enginePlumes` and `FogVisibilityComponent.renderers`. Every ship view has ≥1 plume.
- Place plumes only with `ShipEnginePlumeSetup.Apply`; it keeps scale, width and both lists in sync. Rerun it to move or resize.
- Budget: ≤4 plumes per ship, ≤5 for hulls over 400 units. Pick the main nozzles; split wide flat banks into plumes of width = bank height.

## Implementation

```csharp
// Editor: replaces existing plumes and any TrailRenderer; saves the prefab.
ShipEnginePlumeSetup.Apply(prefabPath,
    new[] { new Vector3(0.005f, -0.251f, -6.830f) },  // ship-root-space nozzle exit centers
    new[] { 0.95f });                                // plume width per nozzle, ship-root units
```

- Parent: `ShipMoveComponent.bodyTransform` (follows bank; survives model reimport).
- Instance `localScale` = width / body lossy scale → stretches the local-space length to 14 × width.
- Instance `widthMultiplier` = width. **View-aligned `LineRenderer` width ignores Transform scale**; only positions scale (probe 2026-11-20: scale 10 → length 10×, width 1×).
- Runtime: `ShipEnginePlumes.SetThrottle(speed / undamaged pace)` in `ShipMoveComponent.Tick()` rewrites positions 1–2 (z = −1.96·f, −14·f); `enabled = f > 0`. Skips changes < 0.01.
- Throttle 0 → plume off: hyperspace arrival, ion-disabled, stopped/anchored. Engine damage → shorter plume. Fog uses `forceRenderingOff`, independent of `enabled`.

## Important Values

| Setting | Value |
|---|---|
| Positions | `(0,0,0)`, `(0,0,-1.96)`, `(0,0,-14)` local |
| Width curve | `(0, 1)`, `(0.14, 1)`, `(1, 0.05)` |
| Color | `(1, 0.99, 0.96)` → `(1, 0.91, 0.80)` |
| Alpha | `(0, 0.85)`, `(0.14, 0.64)`, `(1, 0)` |
| Alignment / texture mode | View / Stretch |
| Material | `ShipEngineTrail.mat` + `ShipEngineTrailProfile.png` (4 × 64, mipmapped, trilinear, clamp) |
| Corner / cap vertices | 0 / 0 |
| Shadows, probes, lighting data | off; motion vectors `ForceNoMotion` |
| Width per nozzle | ≈ 0.9 × nozzle diameter; CR70 0.95 (approved look) |

## Placement procedure

1. Render the prefab from behind (ortho, preview scene) → nozzle `x, y` and diameter. Named glow meshes help: split them into connected pieces; one piece ≈ one nozzle.
2. Exit plane `z`: ray-cast the hull from behind at the center and on a ring of radius width/2; take the more aft of center hit and ring median. Recessed nozzle → ring rim; protruding cone → center tip.
3. `ShipEnginePlumeSetup.Apply`, then render aft (nozzle rings) and side (plumes at throttle 1) to check start point and alignment.

## Edge Cases

- Glow-mesh bounds lie: Executor glows sit 250 units forward of hull min z; Praetor/Mandator nozzles are under overhangs. Trust the ray-cast, not hull bounds.
- Engines mid-hull (MC30c, Bulwark, Dauntless ventral pair, Remonda) are real exit faces; the plume runs past the hull.
- Image-read nozzle positions can miss by a nozzle width (MC50). Prefer mesh-piece centers when they exist; always check the aft render.
- Prefab variants inherit the base plumes and `enginePlumes` (Chimaera, Devastator ← `ISDIIShipView`). A variant that overrides `FogVisibilityComponent.renderers` must re-add them: Chimaera (2026-11-20) misses all 3 → plumes visible through fog; contract test fails on it.
- Tests compare with `GetCorrespondingObjectFromOriginalSource`; the direct source of a variant's plume is the base view's instance.
- Capital length: 14 × width. If ISD/Executor read too long in game, shorten the ratio in the shared prefab for all ships.

## Files

- `Assets/Prefabs/Vfx/Ships/ShipEnginePlume.prefab`
- `Assets/Scripts/Components/Ship/Movement/ShipEnginePlumes.cs`, `ShipMoveComponent.cs`
- `Assets/Scripts/Editor/Ship/ShipEnginePlumeSetup.cs`
- `Assets/Scripts/Tests/Editor/Ships/ShipEnginePlumePrefabTests.cs`, `Navigation/ShipNavigationServiceTests.cs`
- `Assets/Art/Materials/Vfx/ShipEngineTrail.mat`, `Assets/Art/Textures/Vfx/ShipEngineTrailProfile.png`
- [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]
