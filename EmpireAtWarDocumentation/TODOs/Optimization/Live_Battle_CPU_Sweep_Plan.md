---
category: Optimization
status: todo
created: 2026-11-16
---
# Live Battle CPU Sweep

## Goal
- Find and remove every remaining main-thread cost and stall in the `profiler` save battle after [[Done/Optimization/Live_Battle_CPU_And_Pool_Demand]].
- Turn the observed ~40 → 60–80 FPS gain into a matched, repeatable measurement.
- Keep firing cadence, AI decisions and movement behaviour unchanged unless a step says otherwise.

## Important Values
- Post-fix Editor sample (2026-11-16 13:52 UTC, 2,140 frames, High Fidelity, 1468×714, RTX 5070 / 9800X3D):
  - Frame avg 14.01 ms, p95 26.26, p99 37.29, max 420.0.
  - Main thread avg 12.34 ms; render thread 0.69 ms; GPU 0.48 ms → battle is main-thread bound.
  - GC alloc avg 304 KB/frame, p95 2.58 MB, max 29.1 MB.
- Native 40-frame capture `Logs/RenderAudit/20261116T134817413Z-profiler` (self ms/frame, main thread):

| Marker | ms/frame | Max call | Calls/frame |
|---|---|---|---|
| `EnemyAI.Ship.Decision.*` | 0.52 avg | 11.48 | ~2/s per AI |
| `WaitForJobGroupID` | 0.69 | 0.84 | 45 |
| `Battle.Squadron.Flight` + `Pilot` + `Squadron.Tick` | 1.00 | — | 110 each |
| `Battle.Weapon.Tick` | 0.51 | 0.81 | 204 |
| `BattleVictoryService.Tick` | 0.20 | 0.31 | 1 |
| `Battle.Attack.TargetBatch.*` | 0.33 | 0.64 | 1 |
| `Battle.Ship.Audio` | 0.18 | 0.24 | 52 |
| `EditorLoop` + `Profiler.FlushMemoryCounters` | 2.19 | 5.04 | Editor only |

- Stress capture (24 extra capitals, pre scan fix): `Battle.Ship.Attack.HardPointApproach` mean 1.57 ms/frame, p95 9.94, max 41.1; up to 81 calls/frame.
- Boost stress: 292 clones over ~290 frames at 1 clone/frame → max firing wait 6.24 s. Clone cost 1.55–2.32 ms (Editor).
- Top-40 self markers sum to ~8 of 11.5 ms → steady cost is spread thin; spikes and stalls are the main target.

## Rules
- One change → one matched before/after capture. Same save, camera, quality, resolution, time scale, ≥3 × 30 s windows; report median/p95/p99/max and GC bytes.
- Editor numbers rank suspects only. Confirm gains in a Development Player before any FPS claim.
- Add a dev-only `ProfilerMarker` before optimising an unattributed cost. No fix without a marker or allocation stack naming it.
- Tests stay disabled locally; user runs categories listed per phase. Play Mode captures are user-run or explicitly requested.
- Delete each `profiler.data` after its summary JSON is saved.

## Implementation

### Phase 0 — Baseline and acceptance of the last round
- [ ] User runs `Navigation`, `Combat`, `EnemyAi` (scan fix and probe are untested since the last edit).
- [ ] Re-measure Praetor (130 mounts) vs moving Executor (177 points): expect ≤0.7 ms/frame from the 100 ms cadence, lower with the cached-point scan.
- [ ] Record 3 × 30 s Editor windows on the `profiler` save → new baseline row in this note.
- [ ] Build a Development Player, repeat the same 3 windows → Player baseline. Editor overhead to subtract: ~2.2 ms/frame.
- [ ] Add a repeatable stress scenario: the 24-capital reinforcement + Boost script from `Logs/BattleAudit/20261116-cpu-followup/` as a named step, not an ad-hoc eval.

### Phase 1 — AI decision spikes (8.8–11.8 ms per call, ~6 per second across three AIs)
- [ ] Add sub-markers inside `EnemyUnitCommander.EvaluateAndExecute`: `AssessStation`, `Reconcile`, per-group `Build`, `Executor`, capture probe, telemetry/log.
- [ ] Add sub-markers inside `EnemyStrategicContextBuilder.Build`: `FindRaidTarget`, `SelectCaptureObjective`, `BuildBaseThreat`, force builds, `AddTargetForce`.
- Source-verified suspects (not yet measured):
  - `Build` walks `_entityLocator.Entities` 5–7 times (`FindOwnBase`, `FindFocusEnemy`, `FindClosestEntity`, `_forceBuilder.Build` ×2–3, `FindRaidTarget`, `HasOwnStation`).
  - `FindRaidTarget` runs two full `BuildKnown` intel passes (`IsDefended`, `IsRouteSafe`) per visible hostile structure.
  - `AssessStation` repeats base-threat and force work that each group `Build` redoes.
  - Per-decision allocations: `$"zone:{i}"` objective IDs, `_captureReason` concatenation, `groupSummary`, five `AddTargetForce` closures, `new EnemyStrategicContext`, lists in `MoveShipsOutOfDefaultZone`.
- 2026-11-16: structural fix moved to [[TODOs/Optimization/Shared_Team_AI_Plan]] (shared snapshot, team planner, budgeted scheduler). Keep only the markers and item 3 here.
- [ ] Fix order once markers rank them:
  1. One per-decision snapshot shared by all groups: hostile sightings with confidence > 0, own/allied/hostile entity lists, bases, structures.
  2. Cache per-structure defended/route results for the decision instead of rebuilding forces per structure.
  3. Remove decision-path string building outside `UNITY_EDITOR || DEVELOPMENT_BUILD`, or build it only when the decision changes.
  4. If one decision still exceeds ~2 ms: evaluate one task group per frame instead of all groups in one frame.
- Avoid: lowering decision frequency further; changing which objective or target is chosen.
- Tests: `EnemyAi`.

### Phase 2 — Stalls and GC (max 420 ms frame, 29 MB single-frame allocation)
- [ ] Capture with allocation call stacks for 40 frames around a p99 frame; list top allocators by bytes.
- [ ] Attribute the 420 ms frame: GC collection, ship spawn, asset load or AI. Record marker and stack before choosing a fix.
- [ ] Route allocation fixes through [[TODOs/Optimization/CPU_Allocation_And_Jobs_Plan]]; spawn spikes through [[TODOs/Optimization/Ship_Pooling_Plan]]. Record only new findings here.
- Target: p99 ≤ 2 × median; no frame > 50 ms during combat without a spawn or load event.

### Phase 3 — Projectile pools (boosted firing wait 6.24 s)
- Decision: pre-grow on ship spawn. Why: clones exist before the first salvo, no firing-time hitch. Avoid: several clones per frame while salvos wait (trades wait for hitches); summed worst-case capacity (28,870 shots).
- [ ] Measure per shot prefab over the stress scenario: `PeakInUse + peak ReservedCount` ÷ `RegisteredCapacity` → the pre-grow fraction. Do not guess it.
- [ ] `ShotPoolPrewarmer`: when `RegisterCapacity` rises, raise the cache target to `max(floor, RequiredCount, fraction × RegisteredCapacity)`; keep 1 clone/frame; lower the target again on `ReleaseCapacity`.
- [ ] Profile one clone: add `Awake`/`OnEnable` markers on `DualProjectile` and `QuadProjectile` roots; find what costs 1.5–2.3 ms. Cheaper clone shortens every wait.
- [ ] Acceptance: stress scenario max admission wait ≤ one reload of the slowest affected weapon (confirm the number with the user), zero synchronous expansions, physical shots well below the old 28,870.
- Tests: `Combat` (`ShotEffectPoolLifecycleTests`).

### Phase 4 — Ship attack state
- [ ] Add a counter: hardpoint scans per frame and ships scanning while stopped.
- [ ] If stopped ships (holding in range or stalled out of range) still scan every frame at measurable cost: cache the last offset and apply the same 100 ms cadence; keep immediate rescan on arrival and target change.
- [ ] Stagger first scan per ship so a fleet ordered together does not scan in the same frame (stress max was 81 calls in one frame).
- Tests: `Navigation`, `Ships`.

### Phase 5 — Squadrons (≈1.0 ms/frame for 110 squadrons; scales with fleet size)
- [ ] Split `Battle.Squadron.Flight` into steering, formation and pose markers; find the dominant part before changing it.
- [ ] Count `EnemySquadronCommander.Command` calls per frame during a carrier launch burst (`LaunchDiagnostics` exists; no launch was sampled yet).
- [ ] If bursts occur: run `AssessThreats`/`AssignEscorts`/`AssignCaptureTargets` once per frame for all squadrons launched that frame; keep the immediate order for each.
- Tests: `Squadrons`, `EnemyAi`.

### Phase 6 — Steady per-frame costs
- [ ] `WaitForJobGroupID` 0.69 ms across 45 waits/frame: list which jobs are completed synchronously; merge or defer completions that nothing reads in the same frame.
- [ ] `BattleVictoryService.Tick` 0.20 ms every frame: check what it scans; move to events or a ≥0.5 s interval.
- [ ] `Battle.Weapon.Tick` 204 calls/frame: check cost per idle weapon component (no targets) and skip early.
- [ ] `Battle.Ship.Audio` 52 calls/frame, `Battle.Capture.Site.Visibility` 72 calls/frame, `UnitHoverService.Tick`: confirm each needs per-frame work.
- [ ] Target selection in stress: `CandidateVisits` ≈ 9,700/frame; check against [[TODOs/Optimization/Ship_Facing_Solve_CPU_Plan]] and the candidate cache before adding work here.

### Phase 7 — Final matched comparison
- [ ] Repeat Phase 0 windows in Editor and Development Player, plus the stress scenario.
- [ ] Report per phase: before/after median, p95, p99, max, GC bytes/frame; state workload differences.

## Edge Cases
- Evolving battle → two windows are never the same workload; compare only windows started from the same save at the same game time.
- Native profiler export and live `eval` probes add outliers; exclude the export frame.
- Paused or ion-disabled ships skip attack-state work → do not read their absence as a fix.
- Capture probe differences left as-is: no stall-rejection memory across detachments; recovery relay objective not probed.

## Files
- `Assets/Scripts/Services/Enemy/EnemyUnitCommander.cs`, `EnemyStrategicContextBuilder.cs`, `EnemyShipTaskGroupPlanner.cs`, `EnemySquadronCommander.cs`
- `Assets/Scripts/Components/ViewComponents/Weapon/ShotPoolPrewarmer.cs`, `ShotEffectCache.cs`, `ShotEffectPool.cs`
- `Assets/Scripts/Entities/Ship/StateMachine/AttackTargetState.cs`, `Assets/Scripts/Components/Weapon/WeaponComponent.cs`
- Evidence: `Logs/BattleAudit/20261116-live-cpu`, `20261116-cpu-fix`, `20261116-cpu-followup`

## TODO
- User: confirm the firing-wait acceptance number for Phase 3.
- User: approve deleting old `Logs/RenderAudit/*/profiler.data` (≈24 GB across 19 captures from 2026-11-14 and 2026-11-16; summaries stay).
- Related active plans: [[TODOs/Optimization/Battle_CPU_Performance_Plan]] (parent), [[TODOs/Optimization/CPU_Allocation_And_Jobs_Plan]], [[TODOs/Optimization/Ship_Facing_Solve_CPU_Plan]], [[TODOs/Optimization/Ship_Pooling_Plan]].
