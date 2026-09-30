---
category: Optimization
status: todo
created: 2026-09-30
---
# Battle particle rendering evidence — 2026-09-30

## Important Values
- Capture: `Logs/RenderAudit/20260930T165740445Z-capture`; 19:57 Kyiv / 16:57 UTC.
- Original result: partial; Profiler summary failed, Frame Debugger and Render Graph completed; camera-selector value unreadable.
- Original files retained. Recovery: `output/RenderAuditReview_20260930/profiler-summary-recovered.json`.
- Recovered Profiler: two frames, indices 0/1 after loading; recovery timestamps are export time.
- Profiler draw calls: 451/457; SetPass: 239/247; triangles: 1,759,591/1,788,255; BRG draws: 146/148.
- Separate end-of-render UnityStats: 429/453 draws; 225/245 SetPass; values describe different frame identities.

## Decision
- User observation: stars spawn at battle start with ~200 FPS; mid-battle performance falls to ~12 FPS (~5 → 83.3 ms/frame).
- Inference: prioritize work that grows with combat: ship/navigation ticks, active effects, smoke/fire overlap and effect lifecycle. Stars alone are not established as the cause.
- Camera, quality, resolution and background population must be matched before treating the FPS difference as a controlled comparison.
- Chosen: investigate CPU ship/navigation work and accumulating combat effects first; defer star reduction.
- Why: `Battle.Ship.Tick` inclusive 55.45/46.95 ms, self 35.69/27.00 ms, 46 calls/frame.
- `NavigationFloodJob (Burst)`: 8/7 calls, 16.84/15.06 ms main-thread self time.
- `ShipPathGrid.EnsureFlood` runs block-grid and flood jobs through `Schedule(...).Complete()`; synchronous work verified in live source.
- Render thread: frame 1 `Semaphore.WaitForSignal` 64.63 ms of 70.48 ms; waiting is not GPU shading time.
- Frame durations: 165.66/70.48 ms; frame 0 also contains 76.36 ms unattributed root self time and JIT activity.
- Whole-frame GPU timing is unavailable. Two frames support investigation, not sustained FPS or percentile claims.

## Implementation

### Export repair and verification
- Replayed the original recording before editing → `ArgumentNullException` at `CaptureCpuHotspots`, nullable sample-name dictionary key.
- Group CPU samples by `GetSampleMarkerId`; emit `marker_id` and retain original nullable `marker`.
- Regression tests replay both captured frames and verify frame/thread exports, marker identity, call counts and inclusive timing.
- Filtered EditMode run: 2 passed / 0 failed / 0 skipped; main-menu scene checked clean beforehand.
- Unity compilation completed without errors. Existing unrelated hardpoint/audio MissingReferenceExceptions predate this task.
- Tests preserve and restore the Profiler buffer; original audit files remain unchanged.
- Run: `unity command run_tests --mode editor --filter RenderAuditProfilerToolTests --filter_type testName --include_explicit true --async_tests true --json`; check clean scenes first, poll `test_status`.

### Particle scope
- Read-only Unity inventory: 65 prefab assets under `Assets/Prefabs/Vfx` and `Assets/Prefabs/Models`.
- 111 particle-system configurations, including nested copies; 105 Billboard / 4 Stretch / 2 Mesh.
- 21 shared material asset paths; 10 legacy-shader materials referenced by 75 configurations.
- 19 engine configurations share `Assets/Art/Materials/Vfx/Engines.mat`.
- PPFX default/explosion09/smoke07 materials are shared by 21/20/20 configurations; material cloning is not demonstrated.
- 68 renderers request shadow casting, but this does not establish an active shader ShadowCaster pass or measured shadow cost.
- One configuration enables particle lights; no collision or ParticleSystem trails enabled in this scope.
- `active_self` records prefab-local activation, not runtime visibility; capacity and stored emission rates are not live population.

### Measured render evidence
- Frame Debugger: 287/289 events; 139/141 DynamicGeometry events, 35 HybridBatch events.
- Particle/ion shaders: 129/131 events; first frame has 90 different-material breaks and 25 SRP-incompatible breaks.
- Frame Debugger events may contain multiple draws; clear/compute events are not draw calls.
- Rendering Debugger transparent pass: 1.3341 ms CPU / 1.804288 ms GPU; opaque: 0.1507 / 0.176384 ms.
- SSAO GPU: 0.069888 ms; Bloom: 0.044032 ms; these snapshots do not support prioritizing post-processing over transparencies.
- ParticleSystem.Update main-thread self: 0.4570/0.4576 ms.
- Exported particle geometry self across workers: 2.3609/2.3408 ms; largest worker entry 1.7830/1.7894 ms.
- Worker self totals overlap in wall time; the largest geometry entry is not attributed to a particular system by this export.

### Priority assets
| Asset | Verified configuration / capture | Interpretation |
| --- | --- | --- |
| `SkirmishVfx.prefab/StarVfx` | 5,400/s, 15 s lifetime, cap 54,000, Billboard; captured 216,000 vertices / 324,000 indices / 4 draws | Capture contains 54,000 quads; present during reported fast start; defer until cost isolation identifies relevance |
| `ExplosionVfx.prefab/ppfxPipelineFire` | 100/s, lifetime 1–6 s, cap 1,000, AlwaysSimulate; named captured draws contain 348–359 quads | Persistent smoke accumulates; reduce measured overlap/emission |
| `LaserProjectile.prefab`, `ProtonBeamView.prefab` | Each embeds five hit-effect systems, including PPFX smoke/fire | Beam impact uses same costly smoke family |
| `Projectile.prefab`, `DualProjectile.prefab` | Stretch; capture: one draw, four vertices per named bolt; URP Unlit / legacy Additive (Soft) | Small geometry, repeated submissions; ordinary particle instancing toggle is insufficient |
| `ImpactEffects.prefab` | Four shared systems; stored rates 10/s but emission disabled | Runtime `Emit` drives impacts; no passive 10/s emission |
| `Engines.mat` users | Billboard, rateOverDistance 15/m, lifetime 1 s; shared material | Motion-driven demand; measure before reducing |
| `SkirmishVolumetricNebulaClouds.prefab` | 48 mesh particles, one instanced draw, 80 ray samples; particle sizes 2,600–3,800 | Submission is already instanced; pixel/raymarch cost remains a candidate |

### Runtime overrides and lifecycle
- `BoltShot.Play` disables emission, sets loop=false, emits one particle; lifetime = travel time + 1 s; profile size/color applied through particle data.
- `ImpactEffectView`: shield flash + ring; armor flash + 8 sparks; lifetimes 0.16/0.35/0.18/0.2–0.45 s.
- `BeamShot`: armor hit calls `hitEffect.Play(true)`; `StopBeam` stops emission and lets existing particles drain.
- Projectile/beam code uses particle startColor or LineRenderer vertex color; targeted search found no material-instantiation calls in weapon views.
- Transparent sorting can interleave different shared materials; 90 breaks do not mean 90 unique materials.
- Keep texture-sheet animation, additive/premultiplied/alpha blends and soft-particle behavior explicit during migration.
- Unity mesh-particle instancing guidance: https://docs.unity3d.com/Manual/PartSysInstancing.html.
- The generic Built-in instancing shader examples must not be copied unchanged into URP.

## Files
- Capture events: `Logs/RenderAudit/20260930T165740445Z-capture/frame-debugger-frame-{01,02}.json`.
- Snapshot settings/timings: `Logs/RenderAudit/20260930T165740445Z-capture/rendering-debugger.json`.
- Full prefab/material inventory and replay summary: `output/RenderAuditReview_20260930/`.
- Plan: [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan]].
