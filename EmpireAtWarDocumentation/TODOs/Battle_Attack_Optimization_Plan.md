# Battle Attack Optimization Plan

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

- Manual angle/target/timing parity and lifecycle scenarios.
- Matched Forward+ / Deferred+ battle captures; keep fleet, camera, lights, resolution, shadows identical.
- Capture `Hybrid Batch Group` draw evidence; inspect GPU pass timings before more material changes.

## Files

- [[Battle_Attack_Optimization_Plan - Research]] — phase gates, implementation records, full timing tables, material inventories, sources.
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs` — weapon coordination.
- `Docs/BATTLE_PERFORMANCE_REVIEW.md` — historical capture baseline.
