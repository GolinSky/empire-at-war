---
type: engineering-reference
system:
  - ship-vfx
  - battle-rendering
verified: 2026-11-20
verification: "73 ship views + Chimaera/Devastator variants saved and reloaded by Unity; wiring check passed on all 75. Unity compile 0 errors. Tests not run (local override). In-game look and battle profiling pending user."
tags:
  - vfx
  - optimization
  - unity
---
# Ship Engine Trails

## Goal

- One shared engine exhaust on every ship view: `Assets/Prefabs/Vfx/Ships/ShipEnginePlume.prefab` (one native `TrailRenderer`).
- Per ship: nozzle position, one width per nozzle. No per-ship curves, colors, materials or lifetimes.
- Rollout record → [[Done/Features/Ship_Engine_Plume_Rollout_Plan]].

## Decision

- **Chosen (user, 2026-11-20):** `TrailRenderer` with lifetime `time = 14 × width / undamaged pace`.
- **Why:** trail length = speed × time → 14 × width at full pace, shorter when slow, grows from 0 on departure, bends in turns.
- **Avoid:** fixed-length local-space `LineRenderer` plume (shipped first the same day). It reads as on/off at full size and never bends.
- **Lifetime cap 1.5 s** (`MAX_LIFETIME`, user 2026-11-20): uncapped capitals (ISD II ≈7 s, Executor ≈26 s) reshaped too slowly in turns. Capped trails are shorter than 14 × width (ISD II ≈30 units at full pace).
- **Big ships at half width** (user 2026-11-20): `ShipClass` Capital, BattleCruiser, Dreadnought → width ≈ 0.45 × nozzle diameter. Others keep ≈ 0.9 ×.
- **Alpha** lowered for all (user 2026-11-20): `(0, 0.5)`, `(0.14, 0.38)`, `(1, 0)`.

## Rules

- Engine trails are unmodified instances of the shared prefab. Allowed overrides: Transform, `m_Name`, `m_Layer`, `m_Parameters.widthMultiplier`.
- Every trail on a ship view is listed in `ShipMoveComponent.enginePlumes` and `FogVisibilityComponent.renderers`. Every ship view has ≥1. `ShipEnginePlumePrefabTests` enforces this.
- Place trails only with `ShipEnginePlumeSetup.Apply`; it removes earlier instances (any component version) and hand-made trails, then rebuilds both lists. Rerun it to move or resize.
- Budget: ≤4 trails per ship, ≤5 for hulls over 400 units. Pick the main nozzles; split wide flat banks into trails of width = bank height.

## Implementation

```csharp
// Editor: replaces existing engine trails; saves the prefab.
ShipEnginePlumeSetup.Apply(prefabPath,
    new[] { new Vector3(0.005f, -0.251f, -6.830f) },  // ship-root-space nozzle exit centers
    new[] { 0.95f });                                // trail width per nozzle, world units
```

- Parent: `ShipMoveComponent.bodyTransform` (follows bank; survives model reimport). Instance scale stays 1; width = `widthMultiplier` (View-aligned width ignores Transform scale).
- `new ShipEnginePlumes(trails, Model.UndamagedSpeed)` in `Initialize()` sets `time = min(14w / pace, 1.5 s)`, `minVertexDistance = pace × time / 9` (≈9 segments, CR70: 1.5).
- Spawn → `emitting = false` + `Clear()`. Hyperspace finish or save restore → `emitting = true`. Stop/ion/anchor need no code: no movement → no new points, trail shrinks over `time`.
- Engine damage → slower ship → shorter trail. Fog uses `forceRenderingOff`, independent of emission.
- CR70 check: 14 × 0.95 / 50 = 0.266 s ≈ approved 0.26 s.

## Important Values

| Setting | Value |
|---|---|
| Width curve | `(0, 1)`, `(0.14, 1)`, `(1, 0.05)` |
| Color | `(1, 0.99, 0.96)` → `(1, 0.91, 0.80)` |
| Alpha | `(0, 0.5)`, `(0.14, 0.38)`, `(1, 0)` (CR70 approved 0.85 / 0.64; lowered 2026-11-20) |
| Alignment / texture mode | View / Stretch |
| Material | `ShipEngineTrail.mat` + `ShipEngineTrailProfile.png` (4 × 64, mipmapped, trilinear, clamp) |
| Corner / cap vertices | 0 / 0 |
| Prefab emitting / autodestruct | off / off |
| Shadows, probes, lighting data | off; motion vectors `ForceNoMotion` |
| Width per nozzle | ≈ 0.9 × nozzle diameter (CR70 0.95); Capital / BattleCruiser / Dreadnought ≈ 0.45 × |

## Placement procedure

1. Render the prefab from behind (ortho, preview scene) → nozzle `x, y` and diameter. Named glow meshes help: split them into connected pieces; one piece ≈ one nozzle.
2. Exit plane `z`: ray-cast the hull from behind at the center and on a ring of radius width/2; take the more aft of center hit and ring median. Recessed nozzle → ring rim; protruding cone → center tip.
3. `ShipEnginePlumeSetup.Apply`, then render aft (nozzle rings) and side to check the start point.

## Edge Cases

- Glow-mesh bounds lie: Executor glows sit 250 units forward of hull min z; Praetor/Mandator nozzles are under overhangs. Trust the ray-cast, not hull bounds.
- Engines mid-hull (MC30c, Bulwark, Dauntless ventral pair, Remonda) are real exit faces; the trail runs past the hull.
- Image-read nozzle positions can miss by a nozzle width (MC50). Prefer mesh-piece centers when they exist; always check the aft render.
- Prefab variants inherit base trails and `enginePlumes` (Chimaera, Devastator ← `ISDIIShipView`). A variant that overrides `FogVisibilityComponent.renderers` must re-add them after every base rebuild (Chimaera fixed twice on 2026-11-20).
- Changing the shared prefab's renderer type drops instance references → rerun `Apply` on every ship, then fix variant fog lists.
- Tests compare with `GetCorrespondingObjectFromOriginalSource`; a variant's direct source is the base view's instance.
- Capital length: 14 × width. If ISD/Executor read too long in game, change `LENGTH_PER_WIDTH` in `ShipEnginePlumes` for all ships.

## Files

- `Assets/Prefabs/Vfx/Ships/ShipEnginePlume.prefab`
- `Assets/Scripts/Components/Ship/Movement/ShipEnginePlumes.cs`, `ShipMoveComponent.cs`
- `Assets/Scripts/Editor/Ship/ShipEnginePlumeSetup.cs`
- `Assets/Scripts/Tests/Editor/Ships/ShipEnginePlumePrefabTests.cs`, `Navigation/ShipNavigationServiceTests.cs`
- `Assets/Art/Materials/Vfx/ShipEngineTrail.mat`, `Assets/Art/Textures/Vfx/ShipEngineTrailProfile.png`
- [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]
