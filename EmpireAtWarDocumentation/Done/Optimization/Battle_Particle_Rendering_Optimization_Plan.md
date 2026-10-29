---
category: Optimization
status: done
created: 2026-09-30
updated: 2026-10-11
completed: 2026-10-29
disposition: merged
merged_into: "[[TODOs/Optimization/Battle_FPS_Recovery_Plan]]"
---
# Battle particle rendering optimization

> Merged 2026-10-29 into [[TODOs/Optimization/Battle_FPS_Recovery_Plan]]: open items transferred there; this note is history.

## Goal
- Reduce measured particle simulation, geometry and transparent rendering cost while preserving combat readability.
- Establish a matched battle baseline; separate gameplay/navigation CPU cost from VFX cost.
- Related work: [[Done/Optimization/Battle_Attack_Optimization_Plan|Attack and pooling plan]].

## Decision
- Chosen: use the latest complete saved battle capture → consolidate squadron indicators → halve persistent beam smoke → verify specific assets. CPU implementation belongs to the CPU plans.
- 2026-10-11 user direction: apply changes from existing captures; skip broad idle-session measurements. Capture specific elements only when needed.
- Why: user reports ~200 FPS at battle start with stars present, falling to ~12 FPS mid-battle; recovered frames show ship/navigation CPU pressure.
- Stars: defer optimization unless matched isolation shows their cost changes materially during combat.
- Avoid: blanket GPU-instancing flags, global material replacement, pipeline switches or gameplay timing changes.
- 2026-10-11 investigation: 1,234 transparent draws; 156 are 39 four-draw squadron icons. Ion Glow's 215/219 draws match ordinary muzzle-flash signatures; shared-material cloning remains unproven.
- Historical 14:50 CPU audit: radar 619.70/629.77 ms. Later 15:25 battle capture after centralized radar changes: 8.581 ms/frame; do not keep obsolete radar implementation as the current top priority. Current CPU work: [[TODOs/Optimization/Battle_CPU_Performance_Plan]].
- Latest GPU snapshot: transparency 1.755 ms, shadows 0.218 ms, bloom 0.046 ms, SSAO 0.055 ms. Whole-frame/motion-blur GPU timing remains unavailable; matched experiments remain unfinished.

## Implementation

### Next rendering experiments — 2026-10-11
- [x] Rendering agent verified 949 selected-event indices, draw categorization, texture signatures, world indicator structure and CPU/CPUInline/GPU timing column order; detailed findings remain in Research.
- [x] Use `20261011T115023720Z-capture` as the latest complete saved battle frame (949 events), per user direction. The later event-799 timeout has no saved frame; startup/partial isolation captures are not acceptance benchmarks.
- [x] Inspect runtime muzzle material ownership: observed renderers share `MuzzleFlash.mat`, instance ID 63632; no per-renderer material clones found in this snapshot. Historical 215/219 attribution remains signature-based; submission timing is still unmeasured.
- [ ] Isolate smokeSheet03 (247 draws), BlasterBolt (183), BlasterBolt_Tiled (111) independently; record visible effects, vertices, material/state groups, submission CPU and GPU scope time.
- [ ] Separate batching/submission experiments from smoke size/lifetime/density/overdraw experiments; change one variable, restore diagnostic toggles.
- [x] Consolidate all 18 squadron prefabs: shared frame/background shader; integrate icon/shadow for 10 matching unpacked sprites; retain separate artwork in 8 prefabs. Current enabled graphics: 2 or 3, previously 4.
- Latest capture composition projects 156 → 115 indicator draws (41 fewer, 26.3%); this is a prefab-based projection, not a new Frame Debugger or FPS measurement.
- [x] Preserve indicator component logic, selection/ownership colors, visibility, sorting, zoom and hit testing. Read UI_UX_GUIDELINES; verify serialized bindings for all 18 and render selected/unselected ownership-color fixtures.
- [ ] Test shadows only after higher-priority groups: old scope 0.218 ms GPU; 347 ship + 7 wreck draws, no combat-particle ShadowCaster draws. Measure caster distance/LOD/wreck lifetime; no blanket particle-shadow change.
- [ ] Obtain usable motion-blur timing with matched variants. Bloom 0.046 ms and SSAO 0.055 ms in the old adjacent snapshot → low priority; 16 bloom passes are mostly reduced resolution.
- Acceptance: repeat matched runs, improvement above run variation, appearance/lifecycle parity; do not infer material cloning from 363 break events or treat SRP incompatibility as a missing UnityPerMaterial buffer.
- Historical navigation investigation below is evidence; current CPU ownership and ordering live in [[TODOs/Optimization/Battle_CPU_Performance_Plan]]. Do not duplicate its implementation.

1. [x] Repair Profiler export and replay the failing recording.
   - Marker IDs replace nullable-name dictionary keys; nullable names remain visible. Two EditMode regression tests passed.
2. [x] Inspect particle prefab configurations, runtime overrides and capture events.
   - 65 prefabs; 111 particle systems; 21 referenced materials. See [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan - Research|Evidence and asset catalog]].
3. [ ] Record matched baseline and isolate cost.
   - Compare battle start (~200 FPS / 5 ms) with mid-battle (~12 FPS / 83.3 ms); values are user observations, not recorded benchmarks.
   - Match camera, resolution, quality, time scale and Editor layout; record changing ship/squadron counts, orders and shot activity.
   - Track active/idle/ownerless projectile effects, beam smoke, explosions, engine particles and pool growth; distinguish cold from warmed pools.
   - Repeat ≥3 captures covering ≥10 seconds each for idle/movement/firing/deaths; record source revision.
   - Compare frame/CPU/render/GPU median/p95/p99, draw/SetPass counts and particle Update/Geometry markers.
   - Temporarily isolate stars, nebula, engine trails and beam smoke one family at a time in a diagnostic run; restore afterward.
   - Raw timeline reviewed: 8 flood jobs/frame; 8/7 execute on main thread; 8 navigation-containing ticks account for ~96% of ship-tick time. Capture order/state and Plan-reason data next.
4. [ ] Investigate ship/navigation CPU growth first.
   - Attribute `Battle.Ship.Tick` inclusive/self time and route queries to active orders and ships; distinguish computation, waits and allocations.
   - Source review: each Plan creates/disposes a full-map grid; jobs complete immediately; managed candidate checks and route construction remain outside Jobs.
   - Check stopped/out-of-range Hunt/Guard and congested formation retries; these source paths can request routes every tick.
   - Measure Plan reasons, candidate counts, grid sizes and managed route validation before selecting a fix.
   - Propose route reuse/invalidation or scheduling changes only after attribution; preserve movement, obstacle avoidance and command responsiveness.
5. [ ] Reduce persistent beam-hit smoke and fire overlap.
   - [x] `LaserProjectile.prefab` and `ProtonBeamView.prefab`: nested `HitEffect/ppfxPipelineFire` 100/s → 50/s. Death-explosion source, lifetime 1–6 s, sizes and other hit systems retain their values.
   - [x] Seeded preview simulation: 362 → 185 live particles after 6 s; zero after draining for 6.1 s in both beam prefabs. Stop/impact code was not changed.
   - 25/s remains a later density experiment; retain 50/s until combat appearance and measured benefit justify another reduction.
   - Preserve armor/shield distinctions, impact timing and stop/drain behavior; check reuse, owner death and scene exit.
6. [ ] Test targeted materials and shared batching.
   - [x] Reconcile obsolete migration step: live `DualProjectile.mat`, `ShipExplosionSmoke.mat`, `Engines.mat` and `Vulture_MuzzleFlashHelper.mat` already use URP Particles/Unlit. No redundant material migration.
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

### Verification — 2026-10-11
- `SerializedReferenceTests`: 2 passed / 0 failed; MainMenuScene clean before the async EditMode run.
- Both new shaders: no compiler messages. All 18 saved indicator references, graphic counts and shared material assignments passed targeted asset validation.
- 720×720 before/after fixture: mean absolute RGB difference 0.015/255; 350 pixels differ by >2/255. Tiny sampling/edge differences remain; no missing or displaced icons observed.
- Assets imported/saved through Unity APIs. Diagnostic screenshot assets removed from `Assets`; external evidence retained.
- Remaining acceptance: combat appearance/reuse/death/scene-exit checks, submission/overdraw attribution, matched battle/Player frame timings. Keep this plan active; no FPS claim.

## Edge Cases
- Two saved Profiler frames are diagnostic evidence, not a statistically representative benchmark; whole-frame GPU time is unavailable.
- Profiler, Frame Debugger and Rendering Debugger record different frame identities; do not combine their timings as one frame.
- `maxParticles` is capacity; emission can be disabled or overridden. Prefab inventories include nested copies and inactive effects.
- Existing camera-selector MissingReferenceException is separate from the repaired Profiler export.
- Test fixture is explicit because it requires local `Logs/RenderAudit/20260930T165740445Z-capture/profiler.data`.

## Files
- `Assets/Art/Shaders/Ui/Squadron{Frame,Icon}.shader`
- `Assets/Art/Materials/Ui/Squadron{Frame,Icon}.mat`
- `Assets/Prefabs/Models/Squadrons/*SquadronView.prefab` (18)
- `Assets/Prefabs/Vfx/Projectiles/{LaserProjectile,ProtonBeamView}.prefab`
- `output/ParticleRendering_20261011/{asset-validation.json,indicator-verification.json,indicators-before.png,indicators-after.png}`
- `Assets/Scripts/Editor/RenderAuditProfilerTool.cs`
- `Assets/Scripts/Tests/Editor/RenderAuditProfilerToolTests.cs`
- `output/RenderAuditReview_20260930/{particle-inventory.json,profiler-summary-recovered.json,InspectParticles.cs}`
