---
category: Optimization
status: in-progress
---
# Battle Attack Optimization Plan

## Lifecycle Review

- Reviewed: 2026-09-30; remains in progress. Implemented phases still lack the recorded behavior-parity, crossover, and instancing/performance evidence.

## Goal

- Preserve combat behavior while reducing measured attack CPU/allocation cost.
- Sequence: simplify → measure → pool → measure → schedule → measure → target selection → Jobs/Burst → instancing.
- General quality/lighting/resolution changes are outside the original scope.

## Rules

- Ordinary shots reuse turret objects; sequence, effect lease, and damage event have separate lifetimes.
- Preserve shot counts, cooldowns, targets, impact timing, pauses, cancellation, and release behavior.
- One combat coordinator; focused state/pool helpers; no whole-unit ECS or projectile-trajectory rewrite.
- Compare total prepare/schedule/wait/commit cost; retain serial execution where faster.
- Existing notes record separate authorization for Phase 6 and PlanetView-only shadow reception changes.
- Use Serena / official `unity` CLI; follow root `AGENTS.md` for current tooling and test gates.

## Implementation

1. Separate attack sequence from projectile visual lifetime.
2. Pool projectile effects; bound retained capacity and drain effects after owner release.
3. Centralize salvo/impact scheduling; verify frame/observer timing differences against coroutines.
4. Use flat candidate lists and numeric `WeaponTargetSelector`; snapshot positions within each attempt.
5. Measure 5A target selection before 5B sequence progression; reject slower Jobs paths.
6. Verify shader/mesh/material eligibility, actual instanced draws, appearance, and matched performance captures.

## Recorded Results

- Instancing preparation: 59 material flags enabled; 391 MeshRenderers audited.
- Forward+ + GPU Resident Drawer `Instanced Drawing`; SRP Batcher enabled; BRG variants `Keep All`.
- Compatibility passed; active instanced draws and performance gain remain unproven by available captures.
- `battle_20260917_104702_273`: 458 frames, 10.006 s, Apple M2, 2940×1506, target 60 FPS.
- 65.1% frames >16.67 ms; 15.1% >33.33 ms; GPU timings available for 359/458 frames.
- Zero projectile Instantiate calls; pool acquisition 0.055 ms average / 0.131 ms p95.

## Edge Cases

- Shield/preview unique materials → source instancing flag alone is insufficient.
- Same material with different mesh/submesh/pass/state → separate draw groups.
- Cross-weapon frame position cache → may be stale without established movement/update ordering.
- Missing draw/batch counters or matched workloads → no performance attribution.
- PlanetView shadow exception applies only to its background mesh.

## TODO

### Current priorities — 2026-10-11
- Execution plan: [[TODOs/Optimization/Battle_CPU_Fixes_Plan]] (phases 1–3 cover target preparation and due events; phase 7 supersedes "Busy means … waiting for its leased effects" — readiness no longer waits for visuals).
- Evidence and capture manifest: [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research]]; latest 15:25 battle remains CPU-bound after radar improvement.
- [x] Trace current target capture and due-event apply code; distinguish preparation, job dispatch, commit and nested GC costs.
- [ ] Target preparation first: latest 21.118 ms/frame, ~200 requests / 34,144 candidate records; split eligibility, position snapshot and span-copy costs.
- [ ] Measure duplicate targets across hardpoints; test shared snapshots only with explicit movement/commit ordering and generation/version invalidation.
- [ ] Attribute 9,864 fallback events / 15,579 requests by reason; preserve exact serial targeting parity before reducing fallbacks.
- [ ] Split DueBatch.Apply into collect/sort, event lookup, shot emission, impact/damage and resulting effects; latest mean 16.864 ms, maximum 287.191 ms.
- [ ] Profile linear TryFindEvent searches before replacing them; preserve stable due order, cancellation, generations and reentrant record changes.
- [ ] Diagnose projectile acquisition/creation under warmed firing and deaths: 462 instances / 78 frames, 1.325 ms/frame instantiate; measure active/idle/retired/high-water counts.
- [ ] Split BoltShot.Update into flight/tracking, impact completion and effect maintenance; current marker is not turret aiming.
- [ ] Coordinate allocation stacks with [[TODOs/Optimization/Battle_CPU_Performance_Plan]]; do not double-count GC or projectile work inside due apply.
- Acceptance: matched repeated captures show improvement beyond variation; verify target priority, range/arc edges, shot counts, impact timing, pause, owner/hardpoint death, stale results and pool drain.
- Job dispatch/wait is currently small (target 0.056 ms, due 0.033 ms average); further job parallelization is not the first measured priority.


- Manual angle/target/timing parity and lifecycle scenarios.
- Matched Forward+ / Deferred+ battle captures; keep fleet, camera, lights, resolution, shadows identical.
- Capture `Hybrid Batch Group` draw evidence; inspect GPU pass timings before more material changes.

## Files

- [[TODOs/Optimization/Battle_Attack_Optimization_Plan - Research]] — phase gates, implementation records, full timing tables, material inventories, sources.
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs` — weapon coordination.
- `Docs/BATTLE_PERFORMANCE_REVIEW.md` — historical capture baseline.
