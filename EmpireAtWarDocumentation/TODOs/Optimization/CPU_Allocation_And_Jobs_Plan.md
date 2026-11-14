---
category: Optimization
status: in-progress
created: 2026-11-14
---
# CPU Allocation and Jobs Plan

## Goal

- Remove recurring managed allocations and redundant scans found by the 2026-11-14 static audit.
- Gate every Jobs/Burst rewrite behind a matched profiler capture.
- Related CPU owner: [[TODOs/Optimization/Battle_CPU_Performance_Plan]]. Rules: [[Rules/BATTLE_PERFORMANCE_GUIDELINES]].

## Validation · 2026-11-14

- Method: the 28 runtime audit claims (allocation, Jobs/Burst, networking, other CPU) checked against live source. No profiler capture, no tests, no code changed.
- Result: 28 confirmed in source; 1 sub-claim unverified; 0 false. 6 priorities changed; 7 items added.
- Not checked: the 6 loading/editor items in Phase 5 and the `ReplayNotifier` note → verify before starting Phase 5.
- Audit priorities were effort/value guesses. No millisecond or GC figure exists for any item.

### Priority changes

| Item | Audit | Validated | Why |
|---|---|---|---|
| `ShipTangentRouteBuilder` native lists (`:29`, `:31`, `:116`) | Medium | Low | `Allocator.TempJob` + `.Run()`; no GC; per route build only |
| `OnlineMatchService.Tick:135` list copy | Listed | Drop | Runs only while a player is inside the reconnect grace window |
| `HealthModel:203` LINQ | Low | Medium (online clients) | Inside the replica health apply path, not only generator transitions; caller rate unmeasured |
| `SpawnAreaOverlay.Redraw` jobs | Candidate | Low | Component enabled only during placement (`:142` / `:148`) |
| `RadarDetectionJob` spatial grid | Candidate | Benchmark only | Already Burst + per-owner eligibility bitset |
| `SlowTickService` per-frame cap | Consider | User decision | Changes documented catch-up contract (`:43`) and existing tests |

### Resolved during implementation

- `SquadronFlightComponent.Initialize` owns `_poseSlots`. Respawn reuses the array for matching fighter counts and registers fresh pose slots.

### Added by validation

- `EnemyStrategicContextBuilder:304`, `:1033`: `ownShips.Exists(ship => …)` inside a per-entity predicate → one closure + delegate per entity scanned, plus O(entities × ships).
- Same file: capturing lambdas per `Build` (`:303–318`, `:395`, `:472`, `:523`, `:828`, `:1030`); `IsDefended` runs per raid candidate, `IsRouteSafe` per capture candidate.
- Same file `:629`: `$"zone:{i}"` + `new EnemyCaptureObjective` per capturable zone per `Build`; `:393` string concat per `Build`.
- `HazardService.RefreshMemberships:60`: scans all entities with 3–4 facade lookups at 10 Hz even when `_sources.Count == 0`.
- `FleetCommandService.Tick:50`: second full entity scan at 4 Hz even when `_commanders.Count == 0`.
- `DecorAsteroidRenderer.Render:104–107`: `bounds.SqrDistance(cameraPosition)` computed twice per visible instance.
- `ShipNavigationService:194`, `:279`, `ShipRoutePlanner:75`: single-piece `new[] { piece }` copied again by `ShipRoute`.

## Rules

- One phase per change set. Do not mix allocation fixes with scheduling changes.
- Phase 1–3: behavior-identical. Verify by source diff + affected EditMode fixtures (authored, run only on user request).
- Phase 4: no implementation without a matched before capture → protocol in [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Important Values]].
- `EnemyStrategicContextBuilder.Build` lends collections until the next `Build`; any pooled `ForceComposition` follows the same lifetime.
- No stat-pinning assertions; test wiring, ordering and identity only.
- Components stay single classes; no new helper/manager types for one-use scratch.

## Implementation

- Pre-change source revision: `cd8ba087e`. Code commits: Phase 1 `7220b4f6e`, Phase 2 `a17cac31b`, Phase 3 `29f6624d0`, Phase 5 `095db9bed`.
- Unity 6000.4.7f1 compiled the current checkout with zero script compilation errors. Source and fixture diffs checked. Automated tests and Editor/Development Player captures pending user testing.
- Configured AI decision intervals: Easy 4 s, Medium 2.5 s, Hard 1.25 s, Ultra Hard 0.5 s. A decision can build once per group and events can trigger earlier decisions. Measure actual calls/s in the capture.

### Phase 0 · Baseline

- [ ] Capture managed allocations per frame in a crowded skirmish (Editor deep-profile off, then Development Player).
- [ ] Record `GC.Alloc` bytes/frame for: `EnemyStrategicContextBuilder.Build`, `SquadronPilot.TickEngage`, `ShipNavigationService` planning, `HazardService.Tick`, `FleetCommandService.Tick`.
- [ ] Record AI `Build` calls per second per difficulty (interval unverified).

### Phase 1 · Recurring managed churn

- [x] `SquadronPilot:117`, `:230` → per-pilot `List<IHardPointModel>` + `CopyShipUnits`; replace `_aimUnits.Length` with `Count` (`:238–243`). Refresh stays 0.5 s (`AIM_REFRESH_INTERVAL`).
- [x] `EnemyStrategicContextBuilder:364`, `:471` → reuse `ForceComposition` instances (5 `float[]` each) from a builder-owned list; `Clear()` exists (`ForceComposition:105`).
- [x] `EnemyStrategicContextBuilder:304`, `:1033` → `HashSet<long>` of own ship ids filled once per `Build`.
- [x] `ShipTangentRouteBuilder:136` + `ShipRoute:20` → single copy. Add a `ShipRoute` constructor that copies from `NativeArray<ShipRoutePiece>`; keep the defensive-copy constructor for list callers.
- [x] `NetworkTickService:92` → narrow health capture on `IUnitSaveFacade` / `IStructureSaveFacade`; full `Capture()` no longer built to read `.Health`.
- [x] `HazardService` → return before the entity scan when `_sources.Count == 0` and `_memberships.Count == 0`.
- [x] `FleetCommandService` → skip the recipient scan when `_commanders.Count == 0`; still clear `_affected`.

### Phase 2 · Reuse existing storage

- [x] `ShipNavigationService:203`, `:259` build a `ShipRouteSpeedProfile` only for `Duration`; `ShipMovementTweenPlayer:287` builds another. Carry the profile on `ShipNavigationPlan` only when route, speed, rotation speed, initial speed and start distance match; otherwise rebuild.
- [x] `ShipRouteSpeedProfile:34` temporary `boundary[]` → reusable scratch.
- [x] `SquadronFlightModel.Spawn:31–37` → keep the 4 arrays + `FighterKinematics` objects when count matches; reset in place. Health-model reuse excluded (subscriptions, identity).
- [x] `KeyframeChunker.Split:16–21` (called by `HostTickReplicator:60` at 20 Hz) → reused result list; `Array.Empty` for the empty chunk. Chunk-array pooling only after Mirror send ownership is verified.
- [x] `FormationModel:146`, `:157`, `:177` → scratch flags + active-slot flags; preserve assignment and tie order. Command-time only.

### Phase 3 · Redundant scans and serialization

- [ ] `HostReplicatedStateSender:38` → per-state revision check before `Capture()`; requires a revision on `IReplicatedState`. Continuous timers need a separate baseline → design before coding.
- [x] `EnemyStrategicContextBuilder` → replace per-call capturing lambdas on hot predicates (`IsDefended`, `IsRouteSafe`, `BuildOwnNear`) with cached delegates reading fields.
- [x] `EnemyDefenseSnapshotBuilder:84–121` (3 s interval) → group sites by `PointId` once; snapshot base positions once. `existing` list is retained by `EnemyDefensePad` → reuse needs ownership change.
- [x] `HealthModel:203` → single indexed loop.
- [x] `DecorAsteroidRenderer:104–107` → compute instance distance once.
- [x] `SimulationHudUi.Render:32` (every frame via `SimulationHudUiController.Tick`) and `SurvivalHudUi.Render:30` → cache displayed second/counts/phase.
- [x] `EntityComponentLifecycle:28` → indexed loop.

### Phase 4 · Measured candidates (no work before capture)

| Candidate | Source fact | Gate |
|---|---|---|
| Route search | `NavMeshRouteWorkspace:61` `Schedule().Complete()`; budget 4 plans / 2.0 ms checked between plans (`ShipNavigationService:115–136`) | First benchmark `Run()` vs `Schedule().Complete()` under `NavigationPathSearch`; async requests only if single plans exceed budget |
| Fighter integration | `FighterKinematics` is a managed class; `SquadronFlightModel.Step:56` main thread | Squadron flight share of main thread in a squadron-heavy capture |
| Bolt tracking | `BoltFlightBatch:64` managed `BoltShot.Advance` per bolt | `ProjectileTrack` marker share |
| AI force scoring | Managed scans in `ForceCompositionBuilder` | Re-measure after Phases 1 and 3 |
| Hazard membership | 10 Hz entity × hazard, interface `Contains` | Re-measure after the Phase 1 early-out |
| Decor culling | Per-instance loop each `LateUpdate` | Surviving instance count after cell culling |
| Attack due batch | `JOB_PROGRESSION_THRESHOLD = 64`, `DUE_JOB_BATCH_SIZE = 32` | Compare `DueBatch*` markers with the serial path |
| Radar grid | Per-radar scan of eligible bits | Sparse versus dense battle comparison |
| `AutoPowerCombatSnapshot:49` | Clears every bucket ever created | Bucket count in a long battle |

### Phase 5 · Loading and editor (separate from battle FPS)

- [x] `HazardShape` rents temporary bake arrays, and `FieldGrid` reuses its traversal queue and flags. Added a repeated-bake distance fixture. Seeded runtime verification is pending.
- [x] Reviewed `NavigationHazardRaster`. Its source snapshot, native costs and boundaries live with the map snapshot and are already shared by clearance/height layers. Keep that ownership.
- [x] `MonteCarloRunner` reads stopwatch timestamps directly. `BattleSimulation` reuses retired impact buckets and stores impacts as values in insertion order.
- [x] `BattleHitchCapture` records writer wait in `capture_write_wait_ms`. Include it in hitch interpretation.
- [x] Reviewed `ReplayNotifier`. Its observer snapshot is allocated only on state/speed transitions, preserving callback mutation semantics.

### Replicated-state revision design

- Track sampled state instance, revision and JSON per key. Instance identity handles a human/AI owner replacement under the same key. Keep JSON equality as the final broadcast guard and capture late-join baselines independently.
- Discrete candidates: both station levels. Survival can use its wire values: wave, phase and rounded preparation seconds.
- Economy, reserve, capture sites and reinforcement relays include continuous timers or progress. Keep the 0.5 s cadence until a timer baseline defines authoritative sample time, pause/rate and restore/reconnect behavior.
- Cover station deltas, rounded countdown, same-key owner replacement and late-join baselines before changing the sender.

## Edge Cases

- `SquadronHealthComponent.GetShipUnits` returns shared `_adapters` when all fighters are dead → `CopyShipUnits` already covers this fallback.
- Pilot aim list is refreshed at 0.5 s; destroyed hardpoints stay referenced for up to 0.5 s today → unchanged.
- Route builder retries once (`collapseLinks` true → false) → ownership-transfer constructor must not hold the disposed `NativeList`.
- Replans start above the feasible envelope → profile reuse with a different initial speed breaks braking.
- Paused profile (`maxSpeed <= 0`) has `Duration = +∞` → never reuse across an ion-disable change.
- Health percent unchanged → `NetworkTickService:85` already skips capture; saving applies only to changing entities.
- Hazard early-out must still release memberships after a map change (`UpdateState` → `ReleaseAll`).

## Tests to review per phase

| Phase | Categories |
|---|---|
| 1 | `Squadrons`, `EnemyAi`, `Navigation`, `Networking`, `Map`, `Core` |
| 2 | `Navigation`, `Ships`, `Squadrons`, `Networking` |
| 3 | `Networking`, `EnemyAi`, `Health`, `Map`, `Ui` |
| 4 | Per candidate; parity fixtures for any jobified path |

## Decision

- Chosen: allocation and scan fixes first; Jobs/Burst only after measurement.
- Why: the 19 Phase 1–3 fixes are source-verified; no capture shows any Phase 4 system dominating a frame.
- Avoid: async navigation or fighter Burst rewrite as the first step; collection swaps without a measured caller.

## TODO

- [ ] User decision: new standalone plan (current) or fold into [[TODOs/Optimization/Battle_CPU_Performance_Plan]] as the single CPU owner.
- [ ] User decision: `SlowTickService` per-frame cap (contract change).
- [ ] Matched Editor and Development Player allocation captures after the code handoff. Use the pre-change revision above and record dirty paths per [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Important Values]].
- [ ] Run the reviewed fixtures and verify seeded map output during user testing.
