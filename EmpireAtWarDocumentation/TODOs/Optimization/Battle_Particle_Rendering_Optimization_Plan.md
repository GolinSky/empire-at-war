---
category: Optimization
status: todo
created: 2026-09-30
---
# Battle particle rendering optimization

## Goal
- Reduce measured particle simulation, geometry and transparent rendering cost while preserving combat readability.
- Establish a matched battle baseline; separate gameplay/navigation CPU cost from VFX cost.
- Related work: [[TODOs/Optimization/Battle_Attack_Optimization_Plan|Attack and pooling plan]].

## Decision
- Chosen: early-versus-mid-battle baseline → ship/navigation CPU attribution → combat-effect accumulation → targeted material experiments.
- Why: user reports ~200 FPS at battle start with stars present, falling to ~12 FPS mid-battle; recovered frames show ship/navigation CPU pressure.
- Stars: defer optimization unless matched isolation shows their cost changes materially during combat.
- Avoid: blanket GPU-instancing flags, global material replacement, pipeline switches or gameplay timing changes.

## Implementation
1. [x] Repair Profiler export and replay the failing recording.
   - Marker IDs replace nullable-name dictionary keys; nullable names remain visible. Two EditMode regression tests passed.
2. [x] Inspect particle prefab configurations, runtime overrides and capture events.
   - 65 prefabs; 111 particle systems; 21 referenced materials. See [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan - Research|Evidence and asset catalog]].
3. [ ] Record matched baseline and isolate cost.
   - Compare battle start (~200 FPS / 5 ms) with mid-battle (~12 FPS / 83.3 ms); values are user observations, not recorded benchmarks.
   - Match camera, resolution, quality, time scale and Editor layout; record changing ship/squadron counts, orders and shot activity.
   - Track active/idle/ownerless projectile effects, beam smoke, explosions, engine particles and pool growth; distinguish cold from warmed pools.
   - Repeat ≥3 captures covering ≥10 seconds each for idle/movement/firing/deaths; record source revision.
   - Compare frame/CPU/render/GPU median/p95/p99, draw/SetPass counts and particle Update/Geometry markers.
   - Temporarily isolate stars, nebula, engine trails and beam smoke one family at a time in a diagnostic run; restore afterward.
   - Also attribute `Battle.Ship.Tick` and `ShipPathGrid.EnsureFlood`: 7–8 synchronous flood jobs/frame in the two recorded frames.
4. [ ] Investigate ship/navigation CPU growth first.
   - Attribute `Battle.Ship.Tick` inclusive/self time and route queries to active orders and ships; distinguish computation, waits and allocations.
   - Measure `ShipPathGrid.EnsureFlood` frequency, grid sizes and invalidation; block-grid and flood jobs currently schedule and complete synchronously.
   - Propose route reuse/invalidation or scheduling changes only after attribution; preserve movement, obstacle avoidance and command responsiveness.
5. [ ] Reduce persistent beam-hit smoke and fire overlap.
   - `ppfxPipelineFire`: 100/s, lifetime 1–6 s; capture shows ~348–359 quads per named draw.
   - Experiment: 50/s, then 25/s; compare separately from lifetime/size changes so attribution remains clear.
   - Preserve armor/shield distinctions, impact timing and stop/drain behavior; check reuse, owner death and scene exit.
6. [ ] Test targeted materials and shared batching.
   - Migrate `DualProjectile.mat` and project-owned overrides of PPFX smoke/fire individually to matching URP blend/flipbook behavior.
   - Engines/projectiles/impacts already share materials; different-material batch breaks do not prove per-instance clones.
   - Attempt an atlas only for effects whose texture, blend, vertex streams and transparent ordering can be preserved.
   - Existing bolts are one stretched billboard each; consider a shared emitter/mesh submission only after measured submission cost justifies it.
7. [ ] Tune background and engine budgets only if isolation identifies material cost.
   - Stars are present at the reported ~200 FPS start; do not treat their 54,000-quad count alone as the cause of mid-battle slowdown.
   - Current stars: 5,400/s, lifetime 15 s, cap 54,000; compare population and screen coverage across both battle stages before reducing them.
   - Nebula: 48 instanced mesh particles, 80 ray samples/material; compare 64/48 samples only if pass measurements justify it.
   - Engines: 19 prefab systems sharing `Engines.mat`, 15 particles/m, 1 s lifetime; moving scenes determine actual demand.
8. [ ] Accept or revert each experiment before advancing.
   - Improvement must exceed repeat-run variation; keep before/after median/p95/p99 and visual evidence.
   - No gameplay, lifecycle, transparency-order or visual regressions; no new console/import errors.
   - Confirm meaningful improvements in a Development Player before making broad performance claims.

## Edge Cases
- Two saved Profiler frames are diagnostic evidence, not a statistically representative benchmark; whole-frame GPU time is unavailable.
- Profiler, Frame Debugger and Rendering Debugger record different frame identities; do not combine their timings as one frame.
- `maxParticles` is capacity; emission can be disabled or overridden. Prefab inventories include nested copies and inactive effects.
- Existing camera-selector MissingReferenceException is separate from the repaired Profiler export.
- Test fixture is explicit because it requires local `Logs/RenderAudit/20260930T165740445Z-capture/profiler.data`.

## Files
- `Assets/Scripts/Editor/RenderAuditProfilerTool.cs`
- `Assets/Scripts/Tests/Editor/RenderAuditProfilerToolTests.cs`
- `output/RenderAuditReview_20260930/{particle-inventory.json,profiler-summary-recovered.json,InspectParticles.cs}`
