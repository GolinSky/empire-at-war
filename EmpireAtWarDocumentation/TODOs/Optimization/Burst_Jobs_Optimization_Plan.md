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
- [x] Verified `CpuTargetsX64 = 72` against Burst 1.8.29 source: SSE2 (`1 << 3`) + AVX2 (`1 << 6`). Assembly captured for both targets on 2026-10-22.

### Phase 1 — Branch vs select experiment (the requested study)
- [x] `RadarDetectionJob` inner loop: `RadarDetection{If,Ternary,Select}Job` (`If` = verbatim production loop; others combine conditions with `&`).
- [x] `WeaponTargetSelectionJob` filter predicate (L53–L59): `TargetFilter{If,Ternary,Select}Job`; aim work replaced by counting (first accepted + accepted count). Expensive aim path keeps `if`.
- [x] Parity: 18/18 EditMode (`EditorBranchVariantParityTests`) — every variant equals a plain C# reference at ratios 0.05/0.5/0.95, incl. partial bit words and NaN positions.
- [x] Editor indicative timings (one run, Editor Burst JIT): branch-free flat; `if` wins only when the filter is predictable and rejects early. See Research → Phase 1 Editor results.
- [x] Captured all six benchmark variants through the Burst Inspector compiler backend for SSE2 and AVX2; instruction counts and assembly: `Logs/BurstJobs/2026-10-22/`. Ternary/select have matching counts; scalar conditional moves, no blend instructions. Radar packed arithmetic covers one target's x/z coordinates, not SIMD across targets. See Research.
- [ ] Player run: Test Runner → PlayMode → `PlayerBranchVariant*` → Run All Tests (Player), Development build. ≥3 runs, median/p95. **User step** (agent Play Mode/Player runs not allowed).
- [ ] Measure real battle ratios before choosing: share of detectable ∧ hostile radar targets (2 players → ~50 % hostile = unpredictable), share of candidates passing the target filter.
- [ ] Keep the winner only where it is measurably faster; record "no difference" results too.

### Phase 2 — `lengthsq` and sqrt removal (bit-exact)
- [x] `WeaponTargetSelectionJob`: conservative squared-distance fast reject outside fallback band; exact sqrt path inside. Nearest-distance sqrt once per return; `lengthsq <= 1e-10f` cutoff verified with adjacent representable squared values.
- [x] 11 new scheduled parity cases vs frozen original Burst job: seeded geometry/tilts/filtering, range boundaries, near-zero cutoff, nonfinite/overflow directions; full result fields bit-identical. 23/23 target-selection tests pass (2026-10-22).
- [x] Float sqrt for integer `distSqr <= 2^24`; double fallback above. Exhaustive synchronous-Burst equality over `0..2^24` passes; higher-integer fallback samples covered.
- [x] Exact row x-span removes per-x circle check; legacy loop retained when outer radius >46340 to preserve integer-overflow behavior. Bitwise parity vs frozen original Burst reveal passes, including off-grid/overlap/soft edges/overflow.

### Phase 3 — SIMD-friendly loops (`math.select`, SoA)
- [x] Shield planes packed once into four-lane SoA groups; packed dot/divide/select with ordered extrema reduction and accumulated MISS. 13/13 exact Burst parity cases pass (partial groups, random/parallel/inside/tangent/nonfinite/signed zero). SSE2/AVX2 assembly confirms packed operations. `Loop.ExpectVectorized()` is inapplicable to the intentional ordered outer reduction; explicit SIMD verified directly. Same-call completion retained.
- [x] Select-based exact fade/OR reduction plus `FogFadeWriteJob` fusion. Fade/byte and 20-frame chained/history parity pass. AVX2 assembly confirms 16-cell vector loop. Stable-frame byte-write overhead remains a Player measurement gate.
- [x] `FogResetJob` uses `math.select`; AVX2 assembly confirms 32-cell vector loop. SSE2/AVX2 artifacts saved.
- [x] Radar per-owner eligibility bitset + separate X/Z native buffers implemented; 7 kernel parity cases and lifecycle regressions pass. Full radar filter 39/39 EditMode (including existing tests/benchmark fixtures). Same `FixedTick` delivery and batches preserved; preparation cost still needs Player measurement.
- [x] `AttackDueJob` predicate uses `&`; 8/8 EditMode cases pass, including NaN/±inf/frame extrema. Before/after Burst assembly confirms scalar branches → packed comparisons/AND (AVX2 32-record loop). Batch remains 32; no timing claim.
- [ ] Choose due inline vs job/batch ≥1024 from Phase 0 schedule/complete measurements.

### Phase 4 — Scheduling and sync points
- [x] Both fog callers use `UpdateAndWriteBytes`: optional Reset → Reveal → fused Fade/Write; one same-frame `Complete()` before conditional texture `Apply()`. Reveal list cleared after completion. 22/22 fog tests pass; Player performance acceptance pending.
- [x] Bolt placement: `Tick` schedules; `LateTick` completes before combat. Independent spawn roots verified. Replay/reset, Add, repeated scheduling, Clear and Dispose complete pending work; disposal precedes entity scopes. Public `Advance` stays synchronous.
- [x] Fighter scheduling: project `IPostTickable` bridges VContainer PostUpdate after registered Tick/MonoBehaviour Update producers. Placement → bank dispatched in PostTick; LateTick (-2000) completes before combat (-1000), same frame. Mutations complete pending work; LateTick reapplies newest poses after early completion. Spawn/respawn/despawn/root-write and disposal boundaries are explicit. Shared hierarchy roots remain unchanged; worker scaling/benefit needs Player measurement.
- [x] Radar position reads dispatched before independent eligibility/result preparation; detection depends on positions and completes before same-`FixedTick` delivery. All live getters finish before dispatch; `finally` completes work on failure. No frame/physics-step latency added.
- Verification 2026-10-22: bolt/radar pass: 83 distinct EditMode cases, clean compile/Console. Fighter follow-up: 73 distinct cases pass (Fighter 46, PostTick lifecycle 5, verified composition 19, contracts 3), clean compile. Composition logged unrelated `ShipAmbienceService.Dispose` EditMode `Object.Destroy` errors; no clean-Console claim for that run. Player overlap benefit and dispatch overhead remain unmeasured.
- [ ] Batch-size sweep: `TARGET_JOB_BATCH_SIZE` 1/4/8, `DETECTION_BATCH_SIZE` 4/8/16, `REVEAL_ROWS_PER_BATCH` 8/16/32.
- [ ] `ShieldSurfaceClipJob`: evaluate batching all frame clip queries into one `IJobParallelFor` — needs deferred results; **design decision before implementation**.

### Phase 5 — Algorithmic (navigation; measured hotspot)
- [x] Navigation search uses per-cell generation stamps and lazy scratch initialization; generation rollover clears stamps. Repeated-search/rollover/disconnected-route parity passes against frozen original.
- [x] Single-writer obstacle-first raster clears output, binary-searches actual rounded node coordinates for conservative bounds, then retains original strict circle predicate. Random/edge parity passes. Worker-count/performance tradeoff remains unmeasured.
- [x] LOS uses distance to the rounded segment bounding box as a conservative lower bound before the unchanged projection predicate; random/tangent/degenerate parity and existing routes pass.
- [x] Coordinated against live source (2026-10-22): `ShipPathGrid.Build` already computes persistent regions once; `NavigationNearestReachableJob` uses region-constrained ring search. Battle CPU's older per-plan flood/full-grid proposal is stale; do not duplicate it. Current matched performance acceptance remains open.

### Phase 6 — Targeted low-level tuning (only after Phases 1–5 measured)
- [ ] `Hint.Unlikely` on `RequiresSerialFallback` paths if mispredicts show.
- [ ] `FloatMode.Fast` on fog jobs only; pixel diff must be visually acceptable.
- [ ] Minor: `FighterBankJob` `quaternion.RotateZ` remains under Phase 6 measurement gate.
- [x] Radar position job writes NaN to X/Z for invalid transforms; main-thread prefill removed with Phase 3 layout change. Integration test detects target, destroys transform while still registered, rescans and confirms no stale position.

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
- Harness package decision resolved: performance testing 3.4.0 installed. Open decision: deferred shield clip batching (Phase 4).
- Player baseline, ≥3 branch benchmark runs and real battle pass ratios remain user steps. Do not select production branch variants, batch sizes or Phase 6 tuning without them.
- Scheduling constraints: radar currently snapshots and delivers inside `FixedTick`; fighter poses complete before combat late ticks. Preserve those deadlines when evaluating earlier scheduling.
