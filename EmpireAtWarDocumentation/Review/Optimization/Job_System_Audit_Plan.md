---
category: Optimization
status: review
created: 2026-11-26
acceptance: pending-user
updated: 2026-11-26
---
# Job System Audit Plan

## Goal

- Fix the Job System scheduling gaps found by the 2026-11-26 static audit, in priority order.
- Each fix keeps results identical; only timing and allocation change.
- Rules: [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]. Related owners: [[TODOs/Optimization/CPU_Allocation_And_Jobs_Plan]], [[TODOs/Optimization/Ship_Facing_Solve_CPU_Plan]].

## Audit · 2026-11-26

- Method: static review of 30 production jobs and 19 `Complete()` sites against 10 common Job System mistakes. No profiler capture, no tests, no code changed.
- Result: 3 real findings, 2 minor, the rest compliant. No millisecond figure exists for any item.
- Source review 2026-11-26: all 4 findings confirmed in code; 30 jobs counted; only 5 files call `ScheduleBatchedJobs()`. Priorities, Phase 2 gate and Phase 1 test category corrected below. No code changed, no tests run.
- Unity 6000.4 `JobHandle.ScheduleBatchedJobs` docs: `Schedule` → local queue; `ScheduleBatchedJobs` → available to workers.

## Findings by priority

| # | Priority | Finding | Mistake | Owner |
|---|---|---|---|---|
| 1 | Cheap, unmeasured | Target-selection transform reads never flushed | #1 | this plan |
| 2 | Medium | `AttackDueJob` too cheap to schedule | #1, #10 | this plan (gate shared with CPU plan Phase 4 row) |
| 3 | Medium | Facing solver: 7 `TempJob` arrays + `.Run()` per Burst sweep, up to 2 sweeps per solve | #5, #7 | [[TODOs/Optimization/Ship_Facing_Solve_CPU_Plan]] "Burst path cost" |
| 4 | Optional | Spacing and ambience jobs never flushed; no expected gain | #1 | this plan |
| 5 | None | Unmarked `[ReadOnly]` fields | #6 | reviewed, no action |

## Phase 1 · Flush target-selection reads (cheap, unmeasured)

- [x] Done 2026-11-26: `JobHandle.ScheduleBatchedJobs()` after `_origins.BeginRead()`; comment now explains the flush. Worker overlap not yet seen in a capture.
- `TargetSelectionBatch.cs:217–218` schedules `TargetSlotPositionJob` (`TargetSelectionCapture.BeginPositionRead:137`) and `TargetOriginReadJob` (`TargetOriginCapture.BeginRead:58`), then copies candidate buffers on the main thread.
- Comment at `:216` claims workers read positions during the copies. Neither method calls `JobHandle.ScheduleBatchedJobs()` → jobs likely start only at `_origins.CompleteRead()` (`:242`) / `_capture.Refresh()`, on the main thread.
- Fix: one `JobHandle.ScheduleBatchedJobs()` after `_origins.BeginRead()` in `TargetSelectionBatch`. Reference pattern: `RadarScanService.cs:158`.
- Safe: main-thread work between schedule and `CompleteRead()` (`:223–240`) touches only managed arrays and `_candidateSlots` / `_candidateGroups` / `_groupEligible`; neither read job uses them.
- Gain cap: min(copy block `:223–240`, read-job time). `Complete()` already pulls workers in → today's loss is only the missed overlap. Worker wake-up has its own cost.
- Also fixes the stale comment at `:216`.
- Verify: `TargetBatchCaptureCopy` marker overlaps worker `TargetSlotPositionJob` / `TargetOriginReadJob` in a capture; origin `CompleteRead()` and `TargetBatchCapturePositions` waits shrink; `TargetBatchCapture` total does not grow.
- Tests: `Combat` (`WeaponTargetSelectionJobTests`) and `Capture` (`TargetSelectionCaptureTests`). Results unchanged by design.

## Phase 2 · Attack due batch (Medium, measure first)

- [x] Done 2026-11-26: collected single pass chosen (benchmark below). Removed `AttackDueJob`, `AttackDueJobTests`, `_dueInputs`, `_dueResults`, `DUE_JOB_BATCH_SIZE`.
- Benchmark `due_microbenchmark_20261126_194856.csv` (Editor, before the change; median of 3 run medians, ns):

| records | due | rescan | collected | job |
|---|---|---|---|---|
| 64 | 0 | 1,500 | 1,100 | 11,200 |
| 64 | 6 | 13,600 | 6,400 | 18,700 |
| 64 | 64 | 49,700 | 21,900 | 38,000 |
| 256 | 0 | 5,400 | 4,800 | 47,500 |
| 256 | 25 | 131,000 | 19,000 | 64,600 |
| 256 | 256 | 1,014,300 | 178,000 | 164,700 |

- Collected wins every row except 256 / 256 all due (job 8% faster). Few-due rows, the common case, are 3–10× faster.
- Production: `JOB_PROGRESSION_THRESHOLD` → `COLLECTED_DUE_THRESHOLD` (64); `ProcessDueEventsSerialCollected` → `ProcessDueEventsCollected`, no longer editor-only. Below 64 the rescanning serial path is unchanged.
- Capture schema: removed `due_batch_{prepare,job,schedule,complete,apply}_ns`, `due_input_capacity`, `due_result_capacity`, `due_buffer_growths`; `due_job_calls` → `due_collected_calls`; summary `due_job_threshold` + `due_job_batch_size` → `due_collected_threshold`. `Tools/BattleAudit/battle_audit.py` reads only `due_batch_ms` → unaffected. Older captures do not share these columns.
- Benchmark renamed `BenchmarkRescanAndCollectedDuePipelinesOnMatchedImpacts` (variants `Rescan`, `Collected`).
- Original analysis: `CombatAttackCoordinator.ProcessDueEventsBatched:331` (≥ `JOB_PROGRESSION_THRESHOLD` = 64 records): main thread fills `_dueInputs`, schedules `AttackDueJob` (one time/frame compare per record, batch 32), completes at once, reads every result back.
- Copy-in + copy-out likely cost more than the compare.
- Gate: compare with `ProcessDueEventsSerialCollected:309` (one pass, same sort + commit), not `ProcessDueEventsSerial`.
  - `ProcessDueEventsSerial` → `TryFindNextDue:505` rescans every record per due event (records × due) → different algorithm, unfair baseline.
  - `ProcessDueEventsSerialCollected` is compiled only under `UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Measure first with the existing microbenchmark in `CombatAttackCoordinatorDueTests` (`ExistingSerial` / `CollectedSerial` / `Job` → `BattleCaptures/due_microbenchmark_*.csv`). No profiler capture needed for the first decision.
- Options, pick after the benchmark:
  - Collected single pass in production → removes the job, `_dueInputs`, `_dueResults` and the copy-in. Simplest; preferred if it wins.
  - `.Run()` instead of `Schedule().Complete()` → no worker hop, keeps Burst, keeps both copies.
  - Persistent native due-time storage → removes copy-in; must mirror swap-removes (`RemoveSequenceAt`, `RemoveImpactAt`), `RetimeSequences` and re-adds in `CommitDueEvent`. Highest risk.
  - Raise the threshold.
- Avoid: async scheduling across frames; due events must fire this frame.
- Tests: `Combat` (`AttackDueJobTests`, `CombatAttackCoordinatorDueTests`).

## Phase 3 · Facing solver buffers (Medium)

- Owned by [[TODOs/Optimization/Ship_Facing_Solve_CPU_Plan]] (unchecked "Burst path cost" item). Do not duplicate work here.
- Fact: `WeaponFacingSolver.FindTurnBurst:162` allocates 7 `TempJob` arrays and copies the heading table per call when mount-target pairs (`_reachableTargets.Count`, all mounts) ≥ `BURST_PAIR_THRESHOLD` (64).
- `FindTurn` runs it for main batteries, then again for secondaries when main scores 0 → up to 14 allocations per solve.
- Called for stationary ships via `ShipMoveComponent.SolveFacing` → `WeaponComponent.GetFiringTurnAngle`; throttled by `_facingResolveInterval` and the facing scheduler → not per frame.
- Fix there: persistent solver-owned buffers grown with `NativeArrayBuffer.EnsureCapacity`; heading table packed once.
- [x] Done 2026-11-26 (user asked to complete the whole plan; facing plan item checked):
  - `WeaponFacingSolver : IDisposable` owns 7 `Persistent` buffers; jobs get exact-length `GetSubArray` views because `Execute` loops by `Offsets`, `Mounts` and `Scores` length.
  - `WeaponFacingSweepJob` now writes `Scores[step] = default` for sector-skipped headings; reused bounds would otherwise carry the last solve's scores.
  - Past an early break the job leaves later headings stale; the main loop never reads them, since it reaches `maximumScore` at the break step too.
  - `WeaponComponent.Release` disposes; a reused view reallocates lazily.
  - New test `ReusedBurstBuffersMatchAFreshSolverAfterASectorLimitedSolve`; Burst-path tests now `using var` the solver.

## Phase 4 · Flush polled jobs (optional)

- [x] Done 2026-11-26: one `ScheduleBatchedJobs()` after each schedule.
- `ShipSpacingBatch.Schedule:83` and `ShipAmbienceService.Schedule:168` schedule without `ScheduleBatchedJobs()`. Both poll `IsCompleted` on a later tick, so no main-thread block; start is delayed until Unity's next automatic flush.
- Results are read no sooner than the next `Tick` (`ShipSpacingCoordinator.Tick:42`, `ShipAmbienceService.Tick:94`) → an earlier start changes nothing unless the job would miss that poll. Expected gain: none. Unity's automatic flush timing not verified.
- Fix: add `JobHandle.ScheduleBatchedJobs()` after each schedule. Harmless; only worth doing together with Phase 1.
- Tests: `Navigation` (`ShipSpacingJobTests`, `ShipSpacingCoordinatorTests`), `Audio` (`ShipAmbienceJobTests`).

## Reviewed, no action

- `[ReadOnly]` (#6): unmarked fields in fog, NavMesh, grouping and facing jobs are read and written, or belong to single serial `IJob`s. Nothing is blocked from running concurrently.
- Compliant: fog `Update` → `LateUpdate`; fighter poses `PostTick` → `LateTick`; icon grouping, spacing, ambience, reachability poll `IsCompleted`; bolt layers use `CombineDependencies`; every chained dependency is a real data dependency.
- Batch sizes: 64 transform reads, 32 spacing/due, 1 target selection, 4 radar queries (expensive iterations).
- Allocation: runtime buffers `Persistent` + `NativeArrayBuffer.EnsureCapacity`; other `TempJob` uses are editor fallback (`Shield.ClipEnter:97`), NavMesh build or per-route builds.
- Layout and algorithms: radar SoA (`PositionX`/`PositionZ`); spatial hash in spacing and squadron grouping; radar brute force with bitsets (grid = benchmark-only candidate in CPU plan).
- `TransformAccessArray`s persist and update with `Add` / `RemoveAtSwapBack`.

## Rules

- One phase per change set. Phase 2 needs a matched before capture → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Important Values]].
- No behavior change; run only the categories listed per phase.

## Verification · 2026-11-26

- Unity 6000.4.7f1 recompile: 0 errors, 0 warnings; console clean.
- Benchmark: explicit test run alone by exact name (`--include-explicit true`, 1 test) through the Unity CLI; the runner script cannot select an explicit test. Passed.
- EditMode via `Run-EditModeTests.ps1`, explicit off: `Combat` 343/343, `Capture` 140/140, `Navigation` 415/415, `Audio` 134/134, `Core` 187/187 (after fix below).
- First `Core` run: 1 failure, `RuntimeSchema_AlignsBuiltinMarkersCountersAndMetricCount`. Cause: built-in counter offset `FIRST_BUILTIN_COUNTER` stayed 20 after 3 workload columns were removed → set to 17. The test now reads the constant instead of a literal 20.
- Not run: full suite, `EditorPlayMode`, Play Mode, profiler capture. New reuse test not checked against the unfixed job.
- `Tools/Jenkins/TEST_INVENTORY.md`: `AttackDueJobTests` removed, `Combat` 25 → 24. Its file totals (313 / 345) were already stale (live 412 / 457) and stay untouched.

## Reusable guidance

- No new rule extracted: "flush after `Schedule` when main-thread work follows" is already the article pattern and `RadarScanService` reference. Candidate for [[Rules/BATTLE_PERFORMANCE_GUIDELINES]] if the user wants it.

## TODO

- [ ] User capture: `TargetBatchCaptureCopy` overlaps worker `TargetSlotPositionJob` / `TargetOriginReadJob`; `TargetBatchCapture` total does not grow.
- [ ] User check: combat looks unchanged in a large battle (due events, ship facing).
