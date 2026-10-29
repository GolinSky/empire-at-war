---
category: Optimization
status: in-progress
created: 2026-10-22
updated: 2026-10-22
---
# Burst Jobs Optimization — Research

- Audit date: 2026-10-22, revision `4a1257b10` (dirty, unrelated files). Evidence label: *observed in source* unless stated. Nothing measured in this audit.
- Baseline: Unity `6000.4.7f1`, Burst `1.8.29`, Collections `6.4.0`, Mathematics `1.3.3`. No `com.unity.test-framework.performance`.
- `BurstAotSettings_StandaloneWindows.json`: optimisations on, safety checks off, `CpuTargetsX64 = 72` (confirm target set in Burst AOT settings UI before reading assembly).
- No job sets `FloatMode` / `FloatPrecision`; all default (strict).
- Rules: [[Rules/UNITY_JOBS_AND_THREADING_RULES]]. Context: [[Done/Optimization/Battle_CPU_Fixes_Plan]].

## Job inventory

| Job | Type | Scheduler / rate | Sync | Batch |
|---|---|---|---|---|
| `WeaponTargetSelectionJob` | `IJobParallelFor` | `TargetSelectionBatch`, per LateTick | Schedule → Complete | `TARGET_JOB_BATCH_SIZE = 1` |
| `TargetSlotPositionJob` | `IJobParallelForTransform` RO | `TargetSelectionCapture.BeginPositionRead` | overlapped, completed in `Refresh` | 64 |
| `AttackDueJob` | `IJobParallelFor` | `CombatAttackCoordinator.ProcessDueEventsBatched` | Schedule → Complete | `DUE_JOB_BATCH_SIZE = 32` |
| `RadarPositionJob` → `RadarDetectionJob` | transform RO → `IJobParallelFor` | `RadarScanService.RunJobs` | chained, Complete | 64 / 4 |
| `BoltPlacementJob` | `IJobParallelForTransform` (write) | `BoltFlightBatch` per frame | Schedule → Complete | n/a |
| `FighterPlacementJob` → `FighterBankJob` | transform write, chained | `FighterPoseBatch` per frame | Complete | n/a |
| `ShieldSurfaceClipJob` | `IJob` `.Run()` | `Shield.GetSurfacePosition` per shot/bolt query | main thread | n/a |
| `FogResetJob`, `FogFadeJob`, `FogWriteJob` | `IJob` `.Run()` | `FogOfWarSystem` (256² default) + `MiniMapFogMask` (128²) | main thread | n/a |
| `FogRevealJob` | `IJobParallelFor` over rows | same | Schedule → Complete | 16 rows |
| `NavigationGridBlockJob` → `NavigationRegionJob` | `IJobParallelFor` → `IJob` | `ShipPathGrid.Build` | chained, Complete | 256 |
| `NavigationRouteJob`, `NavigationNearestReachableJob` | `IJob` | `ShipPathGrid` per plan (≤4 plans/frame) | Schedule → Complete | n/a |

- Every job completes in the same frame (JOB-05). Only `TargetSlotPositionJob` overlaps main-thread work.
- Historical measured costs (unmatched captures, Battle CPU plan): fog self **1.148 → 2.043 ms** (stress), nav flood **4.315** + nearest-reachable scan **3.892 self ms/frame**, `Projectile.Track` 18.5 → 0.83 ms after `ShieldSurfaceClipJob`, target batch inclusive **1.6–2.7 ms**.

## Findings per job

### `WeaponTargetSelectionJob` (`Components/Weapon/WeaponTargetSelectionJob.cs`)
- L63: `math.length(planar)` sqrt for **every visited candidate**, before the range reject at L80. Far candidates pay sqrt for nothing.
- L65: second sqrt `math.length(direction) <= 0.00001f` for the degenerate check → `lengthsq <= 1e-10f`.
- L64: `NearestDistance` needs real distance; `sqrt(min(dsq))` is bit-identical to `min(sqrt(dsq))` (sqrt monotone + correctly rounded; `length(float2) = sqrt(dot)`).
- L73/L80 range test is not bit-identical in squared space (`Max²` rounds). Safe form: reject with `dsq > (Max·(1+1e-4)+1e-4)²` only **outside** the fallback band; inside the band keep the exact current sqrt path.
- Control flow is `continue`/early `return` around expensive work (`LookRotationSafe`, `inverse`, `EulerZXY`). `math.select` does not apply there; it only applies to the cheap filter predicate (L53–L59).
- Data is gather-indexed (`CandidateGroups → GroupEligible`, `CandidateSlots → SlotFlags/SlotPositions`) → no loop vectorization expected. Cache locality matters more than SIMD.
- Preference ≠ Any → two passes over the same span (L49–L51); each candidate's flags/position re-read.
- `TARGET_JOB_BATCH_SIZE = 1` with ~200 requests × ~170 candidates; may be right for uneven work — measure 1/4/8.
- **Do not** use `FloatMode.Fast`: fast-math may fold `math.isfinite` (L38–L40, L65) and break the serial-fallback contract (JOB-11).
- Unrelated: `[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]` lives in this job file (L8). Not changed.

### `RadarDetectionJob` (`Services/Radar/RadarDetectionJob.cs`)
- Already `math.lengthsq` vs `RangeSquared`. Correct.
- Inner loop per target: `bool IsDetectable` + `Hostility[row + OwnerSlot]` gather + branch + conditional `bits |=`. Best candidate in the codebase for the `if` / `?:` / `math.select` experiment.
- Eligibility (detectable ∧ hostile) depends only on owner slot, not on the radar → precompute one eligibility bitset per owner slot (few players) once per scan; inner loop becomes `eligible & inRange`.
- `Positions` is `float3`, only `.xz` read → 33 % wasted bandwidth. SoA `float` X / Z arrays allow 4/8-wide range tests.
- `RadarScanService` pre-fills positions with NaN on the main thread per target (L156); `RadarPositionJob` could write NaN on `!isValid` instead.

### `AttackDueJob` (`Components/Weapon/AttackDueJob.cs`)
- Work per record ≈ two compares. Schedule + Complete + 32-element batches likely cost more than the work. Main thread then re-loops all records anyway.
- Options: run the predicate inline in the apply loop, or `.Run()`, or batch size ≥1024. Decide by the existing `DueBatchSchedule/Complete` markers.
- `&&` → `&` lets Burst emit branch-free code; check in Burst Inspector.

### `ShieldSurfaceClipJob` (`Components/ViewComponents/Health/ShieldSurfaceClipJob.cs`)
- Up to **1,024 planes** per call; `.Run()` per shot query on the main thread (`ShieldComponent.GetSurfacePosition`).
- Loop body branches 3 ways + early exit → blocks vectorization. Branch-free form:
  - `t = gap / approach`; `enter = max(enter, select(-inf, t, approach < 0))`; `exit = min(exit, select(+inf, t, approach > 0))`; `miss |= (approach == 0) & (gap < 0)`.
  - Same result: early exit only short-circuits a MISS. Division by 0 yields ±inf/NaN that `select` discards; requires strict float mode (keep default).
- AoS `float4` planes → SoA (`nx, ny, nz, w`) needed for 4/8-wide `dot`. Planes are baked per shield, so conversion happens once at `Shield` init.
- Bigger win than SIMD: batch all clip queries of a frame into one `IJobParallelFor` instead of N `.Run()` calls. Requires the caller to accept deferred results → design change, out of first scope.

### Fog jobs (`Entities/FogOfWar/*.cs`, `FogVisibilityGridModel`)
- Per frame: `Fade.Run()` (+ `WriteBytes.Run()` when changed); per reveal tick: `Reset.Run()` → `Reveal.Schedule.Complete()`. All main-thread or immediate sync.
- `FogFadeJob`: `continue` + scalar `changed` + data-dependent branches → likely scalar. Branch-free:
  - `next = select(Current + select(-MaxDelta, MaxDelta, delta > 0), Target, abs(delta) <= MaxDelta)`; `changed |= delta != 0`.
  - Keep `select(..., Target, ...)`: `Current + delta` is not bit-equal to `Target`.
- `FogResetJob`: `Current[i] > 0 ? Base : 0` → `math.select`; trivially vectorizable.
- `FogFadeJob` + `FogWriteJob` read/write the same 65k cells twice → fuse into one pass writing bytes.
- `FogRevealJob` L38: `distSqr > sqrOuterRadius` test per x → compute the row span directly: `half = (int)floor(sqrt(sqrOuter - dy²))`.
- `FogRevealJob` L41: `(float)math.sqrt((double)distSqr)`. For integer `distSqr < 2^24`, float `math.sqrt((float)distSqr)` is bit-identical (double rounding is innocuous for sqrt: 53 ≥ 2·24+2). Verify with an exhaustive EditMode test over `0..maxSqr`.
- `if (visibility > Target[index]) Target[index] = visibility` → `math.max` (always writes; fine, row-owned).
- Visual-only → `FloatMode.Fast` acceptable here **after** profiling, never for gameplay jobs.

### Navigation (`Services/ShipNavigation/*.cs`)
- Already `distancesq` in `NavigationGridBlockJob`, `HasLineOfSight`, ring search. `NavigationGridCells.FindNearestCell` L65 `math.distance` runs once per call → fine.
- `NavigationRouteJob.Search` resets `CostSoFar/CameFrom/Closed` for **all cells** per plan (L65–L70) → generation-stamp array instead (O(visited)).
- `NavigationGridBlockJob`: O(cells × obstacles) with early break → rasterize each obstacle's bounding box instead.
- `HasLineOfSight`: every obstacle per test; bounding-box/segment prefilter.
- Measured hotspot is flood + `TryFindNearestReachableCell` (Battle CPU plan), not SIMD. Branchy A*/heap code: `math.select` irrelevant.

### Transform jobs
- `BoltPlacementJob`, `FighterPlacementJob/BankJob` use `Schedule` (write): parallel only across hierarchy roots. Verify bolts/fighters are root objects or under many roots; one common parent → single-threaded.
- All complete immediately in the same Tick. Option: schedule in Update, complete in LateUpdate (same frame, no latency change).
- `FighterBankJob`: `quaternion.AxisAngle(float3(0,0,1), a)` → `quaternion.RotateZ(a)`.
- `RadarPositionJob`, `TargetSlotPositionJob`: `ScheduleReadOnly`, already parallel. Fine.

## Branch / select experiment design
- Variants: `if`-chain, `?:`, `math.select` (and `&` instead of `&&`).
- Targets: `RadarDetectionJob` inner bit loop (primary), `WeaponTargetSelectionJob` filter predicate (L53–L59), `ShieldSurfaceClipJob` plane loop, `FogFadeJob` loop.
- Assembly checklist (x86-64): `jmp/je/jne` branches; `cmov` scalar select; `vblendps` SIMD select; `vaddps/vmulps` packed vs `addss/mulss` scalar.
- Burst Inspector shows codegen only; speed comes from Player benchmark (Development build, safety checks off, Burst AOT).
- `Loop.ExpectVectorized()` fails compilation when a loop does not vectorize → use in experiment branches, keep in production only for loops that must stay vectorized.
- `Hint.Likely/Unlikely` only on fallback paths (`RequiresSerialFallback`) after a profile shows mispredicts.

## Phase 1 Editor results (2026-10-22, indicative)
- Setup: Editor `6000.4.7f1`, Burst JIT synchronous, safety off, `.Run()` single thread; 10 warmup × 30 measurements × 5 runs; one session. Not a Player result.
- Sizes: radar 128 queries × 1,024 targets, 4 owner slots (3/4 hostile), every 64th target NaN. Filter 256 requests × 192 candidates, 1,024 slots, 16 groups/request.
- Ratio = share of detectable targets (radar) / eligible groups and valid slots (filter).

| Kernel | Variant | 0.05 | 0.5 | 0.95 |
|---|---|---|---|---|
| Radar | `if` | **62.6** | 202.4 | 332.5 |
| Radar | `?:` | 170.7 | **170.8** | **135.6** |
| Radar | `math.select` | 171.5 | 170.9 | 158.3 |
| Filter | `if` | **33.4** | 450.2 | 294.0 |
| Filter | `?:` | 155.1 | 150.2 | **178.1** |
| Filter | `math.select` | 140.1 | **137.7** | 208.0 |

- Median µs per kernel run. Branch-free variants cost the same at every ratio; `if` cost follows predictability.
- `if` wins only when most items are rejected by the first cheap test (0.05): short-circuit skips gathers and the distance math.
- At 0.5 (unpredictable) branch-free is 1.2× (radar) / 3.0× (filter) faster; at 0.95 2.5× / 1.6×.
- `?:` ≈ `math.select`: Burst lowers both the same way for scalars → choose by readability. Confirm in Burst Inspector.
- Decision input missing: real battle ratios. Production 2-player hostility is ~50 % → radar likely in the unpredictable regime.

## Sources
- Burst manual: loop vectorization, `Loop.ExpectVectorized`, `Hint`, `FloatMode`.
- Job system manual: batch sizes, dependencies, `WaitForJobGroupID`.

## Assembly inspection — 2026-10-22
- Burst 1.8.29 Inspector backend `BurstReflection.FindExecuteMethods` → `BurstCompilerService.GetDisassembly`; job attribute options retained, safety disabled, debug=0. No Player execution.
- `CpuTargetsX64 = 72` = SSE2 (8) + AVX2 (64), verified in installed `BurstPlatformAotSettings.cs` / `BurstCompilerOptions.cs`.
- Evidence: `Logs/BurstJobs/2026-10-22/Inspect.cs`, 12 `.asm` files, `instruction-counts.json`. Counts cover complete emitted job producer including scheduling wrapper; static counts are not executed-branch frequency.

| Variant | All jumps | cmov | blend | packed add/mul/sub | scalar add/mul/sub |
|---|---:|---:|---:|---:|---:|
| Radar if | 17 | 1 | 0 | 2 | 1 |
| Radar ternary/select (each) | 14 | 4 | 0 | 2 | 1 |
| Target filter if | 44 | 4 | 0 | 0 | 0 |
| Target filter ternary/select (each) | 12 | 6 | 0 | 0 | 0 |

- Counts match for SSE2 and AVX2; AVX2 uses VEX forms. Radar packed math operates on x/z within one target; no demonstrated cross-target loop vectorization.
- Decision: retain current production branch variants until ≥3 Player runs and actual pass-ratio evidence establish a winner.

## Navigation ownership audit — 2026-10-22
- `NavigationRouteJob.Search` still clears full-grid scratch arrays; `NavigationGridBlockJob` still tests cells × obstacles; LOS still tests every obstacle. Phase 5 implementation remains pending matched baseline measurements.
- `ShipPathGrid.Build` already computes regions once; `NavigationNearestReachableJob` uses `NavigationGridCells.FindNearestCellInRegion` ring search. Do not implement Battle CPU's stale per-plan flood replacement again.
- Old 4.315 ms flood / 3.892 ms nearest-search captures describe older implementations and unmatched workloads; they do not establish current cost.

## Remaining phase gates — 2026-10-22
- Shield batching: synchronous `Shield.GetSurfacePosition` returns the hit immediately; deferred results need the explicit Phase 4 design decision.
- Due inline/job and all batch-size choices: pending Phase 0 schedule/complete measurements. A two-comparison kernel alone does not measure copy, sorting and apply costs.
- Radar: snapshot/jobs/delivery occur inside `FixedTick`; rendered-frame delivery can change behavior across multiple fixed steps. Preserve fixed-step deadline and protect registration/buffer mutation before splitting.
- Fighters: pose batch late tick precedes combat so guns see current pose. Any overlap must preserve this order and complete before register/unregister/grow/dispose. Bolt pooling/removal has the same in-flight ownership requirement.
- Shield SoA/select, radar SoA/owner eligibility and navigation rewrites remain feasible experiments, not approval blockers. Establish representative baseline before changing these additional paths; no production winner selected from static code shape.
- Phase 6 remains gated on measurements from Phases 1–5. No fast-math, hints or batch-size edits in this pass.

## Fog code generation — 2026-10-22
- Captured production `FogResetJob` and new `FogFadeWriteJob` on SSE2/AVX2 using Inspector backend, strict/default float mode and safety disabled; artifacts beside branch assembly.
- AVX2 reset: four YMM `vcmpltps`/`vandps`/`vmovups` groups per iteration (32 cells). Fused fade/write: two YMM groups (16 cells), `vsubps`, `vblendvps`, `vaddps`, `vmulps`, packed byte conversion, vector OR reduction.
- This establishes vectorized code generation, not a measured frame-time improvement.
- Tradeoff: fused pass writes byte memory even when visibility is unchanged; texture `Apply()` remains conditional. Compare stable and changing masks in Player before accepting performance.

## Implementation verification — 2026-10-22
- Unity compilation succeeds. `WeaponTargetSelection` 23/23 and `FogVisibility` 22/22 EditMode tests pass (45 distinct cases); Burst enabled, synchronous compilation enabled for verification, safety checks enabled. Original synchronous setting restored afterward.
- Target parity includes 11 new cases against frozen old Burst implementation; compares all result fields by float bits. Direct `1e-10f` degenerate cutoff validated at adjacent squared float values. Far rejection uses conservative `max(0, range) + 2*tolerance` bound; exact fallback tests retained within it.
- Fog sqrt parity checks every integer `0..16777216` inside a synchronous Burst job. Above that range original double sqrt remains. Reveal parity uses frozen original Burst loop, including large-radius overflow fallback; managed-reference comparison initially differed by a few ULP and was replaced with equivalent Burst execution without weakening equality.
- Fused fade/write preserves exact target assignment and unchanged-cell float values. Chain tests compare history on/off across 20 frames. Both actual callers complete before texture upload; no job survives the method return.
- Test artifacts: `Logs/BurstJobs/2026-10-22/weapon-tests.json`, `fog-tests.json`. No Play Mode/Player runs or matched speedup claim.
- Plan stays active: Player baselines, real pass ratios, ≥3 benchmark runs, remaining Phase 3/4/5 experiments and Phase 6 tuning remain outstanding.

## Continued implementation — due predicate
- User requested remaining work independent of Player measurements and deferred shield batching.
- `AttackDueJob`: `&&` → `&` for pure comparisons; no scheduler, batch or delivery changes.
- 8/8 EditMode tests pass with synchronous Burst and safety enabled; added 35 input combinations × 35 now/frame combinations including NaN, ±inf, signed zero and int extrema.
- Before/after SSE2/AVX2 assembly saved in `Logs/BurstJobs/2026-10-22/AttackDueJob-{before,after}-{CPU}.asm`; original loop uses scalar jumps, new AVX2 loop evaluates 32 records with packed float/int comparisons and AND. This is codegen evidence, not speed evidence.
- Due execution model and batch size remain pending Player schedule/complete measurements.

## Continued implementation — shield, radar and navigation
- Shield: `Shield.PackPlanes` builds persistent `float4x4` groups with X/Y/Z/W columns; unused lanes are parallel non-rejecting planes. Kernel does four-plane dot/divide/select, preserves original extrema order for signed-zero ties, accumulates MISS. 13 exact Burst parity cases pass. No deferred results.
- Shield SSE2/AVX2 disassembly confirms packed multiply/add/divide and 64-byte group stride. Outer extrema reduction remains ordered; `Loop.ExpectVectorized()` would test a different property than the explicit SIMD arithmetic. Inspect artifacts in `Logs/BurstJobs/2026-10-22/`.
- Radar: owner eligibility built once per scan, separate persistent X/Z buffers; original skip for ineligible targets retained. Producer dependency, batch sizes and same-`FixedTick` delivery unchanged. All 39 Radar-filter tests pass, including 7 kernel bitset parity cases, partial words, asymmetric hostility, NaN/inf, stale-mask clearing, destroyed transform, swap-removal and delivery unregistration. This run includes existing benchmark fixtures; no Player conclusion inferred.
- Radar destroyed-transform integration proves job writes NaN after a previously valid transform is destroyed while still registered; main-thread prefill removed. Owner×target eligibility preparation cost remains unmeasured.
- Navigation: one generation stamp per cell, initialize search scratch only on first visit; first generation and rollover clear stamps. Start parent explicitly reset to -1, including start=goal.
- Raster: single-writer obstacle-first enumeration; binary-search actual rounded grid coordinates for conservative axis bounds, retain strict original circle comparison. Reduces candidate checks but gives up cell-level parallel scheduling; Player acceptance remains open.
- LOS: rounded endpoint `from + (to-from)` defines bounding box; distance to box is a lower bound, original projection comparison retained for survivors.
- Navigation verification: 7 new parity cases (240 random raster layouts, boundary cases, 360 reused-buffer route searches/rollover, disconnected/blocked maps, 2000 LOS cases), 26 service tests and 1 grid test pass. Exact raw-path/waypoint comparisons retained. Independent source review found no actionable navigation or radar correctness/lifetime issue.

## Burst cache verification correction
- Initial scheduled shield/radar tests executed stale pre-layout kernels despite clean script compilation. Shield packed columns were read as old individual planes; radar output remained unwritten at the old `WordsPerQuery` offset.
- `BurstCompiler.TriggerRecompilation()` alone did not invalidate those assemblies. Used installed Burst's `CompilerCommandDirtyAllAssemblies` then `TriggerRecompilation` through Unity eval; recovery script saved as `Logs/BurstJobs/2026-10-22/RefreshBurst.cs`.
- After invalidation, unchanged shield tests pass 13/13 and radar 39/39; assembly shows new packed layout. Reran earlier layout-stable kernels too: weapon 23/23, fog 22/22, due-filter 23/23. This supersedes the earlier cache-dependent verification evidence.
- No source behavior was weakened to accommodate stale-kernel failures. No Play Mode or Player execution.

## Continued pass acceptance
- 156 distinct EditMode cases pass after cache invalidation, deduplicated by full test name across saved result files; includes 4 existing `ShieldSwap_UsesNewSurfaceForImpacts` prefab integration cases. `verified-cases.json` lists every case.
- Unity compilation succeeds; new scripts/metas imported; `git diff --check` passes. Synchronous Burst setting restored to its original false value. No Unity asset/configuration edits required.
- Remaining gates: Player timings/workload ratios and branch/batch selection; timing splits need runtime hierarchy/dependency/ownership evidence; deferred shield batching needs explicit result-latency design; Phase 6 remains measurement-gated.
- Codegen ≠ speed: shield scans all planes instead of early parallel MISS exit; radar adds owner×target preparation; obstacle-first raster trades cell parallelism for fewer candidates; fused fog writes stable masks. Measure each full feature path before performance acceptance.


### Same-frame scheduling validation — 2026-10-22
- `BoltFlightBatch.Tick` tracks/lands then schedules placement; `LateTick` completes at -1500 before combat (-1000). Public `Advance` remains synchronous for direct callers.
- Complete before Add, repeat schedule, Clear, Dispose, listed-bolt Play and ResetVisuals. Play guard is essential: replay writes Transform before Add and an already-listed bolt skips Add. Prewarm has no BoltFlights dependency yet, so guard uses `IsInFlightBatch`.
- Bolt disposal order 1100 precedes entity scopes 1000 (descending disposal). Reset/cancel hooks also cover `SimulationResetService` clearing pools before batch Clear. Live skirmish tick gate stays enabled; only squadron pooling toggles entity gates.
- Bolt spawn audit: pool, ion ability and superweapon instantiate without parent; no later parenting found. Independent roots permit transform scheduling; no measured parallelism/speed claim.
- Radar gathers live state/queries/hostility, schedules and flushes position reads, prepares independent eligibility/output buffers, schedules dependent detection, completes in `finally`, then delivers. Snapshot and callback ordering remain inside one FixedTick. RadarJob samples cover scheduling/dispatch/completion; snapshot preparation remains separately marked.
- Fighter scheduling deferred: battle coordinator Update runs before entity coordinators producing poses. Lifecycle order sorts only scope-local callbacks; moving to Tick would schedule stale poses or force early completion in Set. A global post-Update bridge would expand lifecycle scope. Existing LateTick has no independent work between placement → dependent bank → Complete. Squadron/fighter transforms share scope roots; no hierarchy redesign performed.
- Validation: 83 distinct EditMode cases passed: Bolt 26 (7 new scheduling cases), Radar 41 (2 new same-tick/snapshot cases), ShotEffectPoolLifecycleTests 13, LifecycleContractTests 3. Reports: `Logs/BurstJobs/2026-10-22/{bolt,radar,pool,lifecycle}-scheduling-tests.json`. Clean compile; Console errors empty since cursor 15240 through 15274. Burst cache invalidated before tests; synchronous compilation setting restored false afterward.
- Independent radar review found no data ownership/dependency issue. Explicit `ScheduleBatchedJobs` offers workers preparation overlap; wake-up overhead and overall benefit still require Player measurement.
- Independent bolt review found no actionable correctness issue across Fire/Update/reset/cancel/pool callbacks and teardown. Bolt scheduling does not force an extra dispatch flush; actual worker overlap depends on engine/other-job dispatch before LateTick. Validate timeline before adding another wake-up cost.


### Fighter PostTick implementation — 2026-10-22
- Supersedes the fighter deferral above; user authorized the lifecycle change. Custom `EmpireAtWar.Lifecycle.IPostTickable` routes through `OrderedLifecycleCoordinator` → VContainer `IPostTickable`; callbacks use scope-local ordering, profiler markers and existing gates/disposal suppression.
- Installed VContainer 1.19.0 `PlayerLoopHelper`: `VContainerUpdate` → `ScriptRunBehaviourUpdate` → `VContainerPostUpdate`; `VContainerLateUpdate` precedes `ScriptRunBehaviourLateUpdate`. PostTick follows registered Tick and MonoBehaviour Update producers, not every delayed task or engine operation in the Update phase.
- `FighterPoseBatch.PostTick`: placement → bank dependency; flush with `ScheduleBatchedJobs`. LateTick at -2000 completes before combat at -1000. Direct LateTick remains synchronous; early completion followed by mutation causes latest poses to be scheduled again at LateTick.
- Complete before Set/Register/Unregister; preserve covering placement handle if bank scheduling fails. Dispose at 1100 precedes entity scopes (1000); unregister after terminal disposal is intentionally inert, Register rejects disposed owner.
- Complete before `SquadronFlightComponent.QueuePoses` / `ApplyPoses` root writes; `SquadronFactory.Create` before instantiation/parenting; `Squadron.Respawn` before activation/health restoration; `Despawn` before disabling hierarchy. No generic factory or hierarchy restructuring.
- Only production gate changes are squadron-local; pooled child gates cannot disable the battle batch completion. Shared hierarchy roots remain; worker scaling and dispatch cost are unmeasured.
- Verification: clean compile; 73 distinct EditMode cases pass: Fighter 46, PostTickLifecycleTests 5, CompositionTests 19, LifecycleContractTests 3. Shared runner was concurrently used by another task; an audio report initially occupying the composition result was discarded. Reran composition and captured all 19 matching test names directly from the status file. Includes actual installed phase inspection, child producer → parent scheduling → combat read, late mutation, pending slot reuse/growth, disposal/unregister and real entity spawn/pool round trips with pending work.
- Reports: `Logs/BurstJobs/2026-10-22/fighter-posttick-tests.json`, `posttick-lifecycle-tests.json`, `posttick-composition-tests.json`, `posttick-contract-tests.json`. Burst synchronous compilation restored false. No Player/Play Mode run.
- Console limitation: composition run logged unrelated `ShipAmbienceService.Dispose` line 187 using `Object.Destroy` in EditMode (concurrent audio changes); the final separately verified composition report contains 19/19 passed. Audio source not modified by this work. Earlier transient audio compilation errors cleared on final compile.
- Independent lifecycle/ownership reviews found no actionable scheduling defect. Engine animation/shared hierarchy overlap and actual frame benefit still require runtime observation; no performance improvement claimed.
