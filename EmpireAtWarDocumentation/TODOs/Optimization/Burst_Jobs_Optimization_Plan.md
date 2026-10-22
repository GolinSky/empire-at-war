---
category: Optimization
status: in-progress
created: 2026-10-22
updated: 2026-10-22
---
# Burst Jobs Optimization Plan

## Goal
- Cut Burst job + sync cost in battle without changing gameplay results.
- Priority: contiguous native data → scheduling/sync → `math.lengthsq` → real SIMD. `math.select`, `Hint.*`, `FloatMode.Fast` only after profiling.
- Audit evidence per job: [[TODOs/Optimization/Burst_Jobs_Optimization_Plan - Research]].

## Rules
- Follow [[Rules/UNITY_JOBS_AND_THREADING_RULES]]. Same-frame completion stays unless a phase states a latency decision.
- Gameplay jobs (targeting, radar, due, shield clip, navigation) keep bit-exact results or an EditMode parity test proves equivalence.
- `FloatMode.Fast`: never on `WeaponTargetSelectionJob` (fast-math may fold `math.isfinite`) or `ShieldSurfaceClipJob` (relies on ±inf). Fog only, after measurement.
- Measure → change one thing → measure. Codegen in Burst Inspector; speed in a Development Player (safety checks off). Editor numbers are indicative only.
- No agent Play Mode runs; user captures in-game. Compile + EditMode tests only on request.

## Implementation

### Phase 0 — Baseline and harness
- [x] Benchmark harness (user chose the package, 2026-10-22): `com.unity.test-framework.performance` `3.4.0` added to `Packages/manifest.json` (already resolved transitively; no version change).
  - `Assets/Scripts/Tests/Benchmarks/` → asmdef `EmpireAtWar.Tests.Benchmarks` (all platforms, PlayMode/Player): variant jobs, seeded data, abstract `BranchVariantParityTests` / `BranchVariantBenchmarks`, sealed `Player*` fixtures.
  - `Benchmarks/Editor/` → asmdef `EmpireAtWar.Tests.Benchmarks.Editor`: sealed `Editor*` fixtures (EditMode).
  - Kernels are layout copies; the assembly cannot reference `Assembly-CSharp`. Jobs use `[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]` and `.Run()` on one thread (kernel cost only, no scheduling).
  - Results: Editor → `%USERPROFILE%/AppData/LocalLow/DefaultCompany/EmpireAtWar/PerformanceTestResults.json`; Sample `Time` µs per 5 kernel runs.
- [ ] Record per-job baseline with existing markers (`TargetBatchSchedule/Complete`, `DueBatchSchedule/Complete`, nav, fog, `Projectile.Track`) in a Development Player at fixed workload.
- [ ] Profiler timeline: note `WaitForJobGroupID` on main thread per job; which jobs run on workers vs inline in `Complete()`.
- [ ] Verify Burst AOT CPU target (`CpuTargetsX64 = 72`) so Inspector shows the shipping instruction set.

### Phase 1 — Branch vs select experiment (the requested study)
- [x] `RadarDetectionJob` inner loop: `RadarDetection{If,Ternary,Select}Job` (`If` = verbatim production loop; others combine conditions with `&`).
- [x] `WeaponTargetSelectionJob` filter predicate (L53–L59): `TargetFilter{If,Ternary,Select}Job`; aim work replaced by counting (first accepted + accepted count). Expensive aim path keeps `if`.
- [x] Parity: 18/18 EditMode (`EditorBranchVariantParityTests`) — every variant equals a plain C# reference at ratios 0.05/0.5/0.95, incl. partial bit words and NaN positions.
- [x] Editor indicative timings (one run, Editor Burst JIT): branch-free flat; `if` wins only when the filter is predictable and rejects early. See Research → Phase 1 Editor results.
- [ ] Per variant, Burst Inspector (Jobs → Burst → Open Inspector, `EmpireAtWar.Tests.Benchmarks` jobs, x64 AVX2 target): count `jmp/je/jne`, `cmov`, `vblendps`, packed (`vaddps/vmulps`) vs scalar (`addss/mulss`).
- [ ] Player run: Test Runner → PlayMode → `PlayerBranchVariant*` → Run All Tests (Player), Development build. ≥3 runs, median/p95. **User step** (agent Play Mode/Player runs not allowed).
- [ ] Measure real battle ratios before choosing: share of detectable ∧ hostile radar targets (2 players → ~50 % hostile = unpredictable), share of candidates passing the target filter.
- [ ] Keep the winner only where it is measurably faster; record "no difference" results too.

### Phase 2 — `lengthsq` and sqrt removal (bit-exact)
- [ ] `WeaponTargetSelectionJob`: squared-distance fast reject outside the fallback band; exact sqrt path inside it. `NearestDistance = sqrt(min dsq)` once at the end. Degenerate check `lengthsq <= 1e-10f`.
- [ ] Parity test: random origins/tilted parents/ranges incl. exact-boundary distances → identical `CandidateIndex`, aim, `NearestDistance`, fallback reason vs current job.
- [ ] `FogRevealJob`: `math.sqrt((float)distSqr)` replacing double sqrt; exhaustive EditMode equality test over all reachable `distSqr`.
- [ ] `FogRevealJob`: compute row x-span from `dy` (`floor(sqrt(sqrOuter − dy²))`); drop per-x circle test. Pixel-equality test vs current output.

### Phase 3 — SIMD-friendly loops (`math.select`, SoA)
- [ ] `ShieldSurfaceClipJob`: branch-free plane loop (select enter/exit, `miss` flag); planes as SoA `float4` groups built once in `Shield`. Parity test over random segments incl. parallel/inside cases. Check `Loop.ExpectVectorized()` in an experiment build.
- [ ] `FogFadeJob`: branch-free step with `math.select`, `changed` as OR-reduction; keep exact `Target` assignment. Fuse with `FogWriteJob` (one pass writes bytes). Pixel-equality test.
- [ ] `FogResetJob`: `math.select`; confirm vectorized.
- [ ] `RadarDetectionJob`: per-owner eligibility bitset precomputed once per scan; positions as SoA `x`/`z` floats. Bitset parity test.
- [ ] `AttackDueJob`: `&` instead of `&&`; then decide inline vs job by Phase 0 schedule/complete cost (batch 32 → ≥1024 or main-thread loop).

### Phase 4 — Scheduling and sync points
- [ ] Fog: chain Reset → Reveal → Fade/Write as one dependency chain with a single `Complete()` before texture `Apply()`; replace `.Run()` calls (main-thread) with scheduled jobs where the grid is 256².
- [ ] Bolt/fighter transform jobs: confirm hierarchy roots allow parallel `Schedule`; schedule in Update, complete in LateUpdate (same frame).
- [ ] Radar: schedule detection earlier in the frame, complete at delivery (same frame).
- [ ] Batch-size sweep: `TARGET_JOB_BATCH_SIZE` 1/4/8, `DETECTION_BATCH_SIZE` 4/8/16, `REVEAL_ROWS_PER_BATCH` 8/16/32.
- [ ] `ShieldSurfaceClipJob`: evaluate batching all frame clip queries into one `IJobParallelFor` — needs deferred results; **design decision before implementation**.

### Phase 5 — Algorithmic (navigation; measured hotspot)
- [ ] `NavigationRouteJob.Search`: generation stamps instead of full-grid reset per plan.
- [ ] `NavigationGridBlockJob`: rasterize obstacle bounding boxes instead of cells × obstacles.
- [ ] `HasLineOfSight`: obstacle bounding prefilter.
- [ ] Coordinate with Battle CPU plan nearest-reachable fallback item; no duplicate ownership.

### Phase 6 — Targeted low-level tuning (only after Phases 1–5 measured)
- [ ] `Hint.Unlikely` on `RequiresSerialFallback` paths if mispredicts show.
- [ ] `FloatMode.Fast` on fog jobs only; pixel diff must be visually acceptable.
- [ ] Minor: `FighterBankJob` `quaternion.RotateZ`; `RadarPositionJob` writes NaN for invalid transforms (drop main-thread pre-fill).

## Edge Cases
- Target exactly on range/yaw boundary → must still route to serial fallback after Phase 2.
- `NearestDistance = +inf` when nothing visited → `sqrt(+inf) = +inf`, unchanged.
- Shield segment parallel to a plane and outside → MISS regardless of later planes.
- Shield clip `gap/0` → ±inf/NaN discarded by `select`; breaks under fast-math.
- Fog `Current + delta` ≠ `Target` in float → keep explicit select of `Target`.
- Radar destroyed transform → NaN position must still fail every range test.

## Files
- `Assets/Scripts/Components/Weapon/{WeaponTargetSelectionJob,AttackDueJob,TargetSlotPositionJob,TargetSelectionBatch,CombatAttackCoordinator}.cs`
- `Assets/Scripts/Services/Radar/{RadarDetectionJob,RadarPositionJob,RadarScanService}.cs`
- `Assets/Scripts/Components/ViewComponents/Health/{ShieldSurfaceClipJob,Shield}.cs`
- `Assets/Scripts/Entities/FogOfWar/{FogResetJob,FogRevealJob,FogFadeJob,FogWriteJob,FogVisibilityGridModel}.cs`
- `Assets/Scripts/Services/ShipNavigation/{NavigationRouteJob,NavigationGridBlockJob,ShipPathGrid}.cs`
- `Assets/Scripts/Components/ViewComponents/{Weapon/BoltPlacementJob,Squadron/FighterPlacementJob,Squadron/FighterBankJob}.cs`

## TODO
- Open decisions: benchmark harness package (Phase 0); deferred shield clip batching (Phase 4).
