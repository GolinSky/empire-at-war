---
category: Optimization
status: todo
created: 2026-10-22
---
# Burst Jobs Optimization — Research

- Audit date: 2026-10-22, revision `4a1257b10` (dirty, unrelated files). Evidence label: *observed in source* unless stated. Nothing measured in this audit.
- Baseline: Unity `6000.4.7f1`, Burst `1.8.29`, Collections `6.4.0`, Mathematics `1.3.3`. No `com.unity.test-framework.performance`.
- `BurstAotSettings_StandaloneWindows.json`: optimisations on, safety checks off, `CpuTargetsX64 = 72` (confirm target set in Burst AOT settings UI before reading assembly).
- No job sets `FloatMode` / `FloatPrecision`; all default (strict).
- Rules: [[Rules/UNITY_JOBS_AND_THREADING_RULES]]. Context: [[TODOs/Optimization/Battle_CPU_Fixes_Plan]].

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
