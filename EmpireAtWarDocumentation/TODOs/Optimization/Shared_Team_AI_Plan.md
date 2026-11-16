---
category: Optimization
status: in-progress
created: 2026-11-16
---
# Shared Team AI

## Goal
- AI frame cost stays flat when the match goes from 3 to N AI players.
- Allied AIs plan from one shared picture and stop competing for the same objective.
- Keep each AI's own difficulty, faction, economy and fleet.

## Decision
- Chosen: three layers — battle-wide world snapshot → one planner per team → thin per-player commanders, all driven by one AI scheduler with a frame budget.
- Why: the user's "shared calculation" variant gives the CPU win; a scheduler gives the N-AI scaling; the team planner gives cooperation.
- Avoid: one literal AI that owns every AI fleet. Reasons below.

### User proposal checked against source
| Claim | Verdict | Evidence |
|---|---|---|
| Cost grows with every AI added | True | `AiPlayerScope` registers a full AI stack per player: `EnemyUnitCommander`, `EnemyStrategicContextBuilder`, `EnemySquadronCommander`, `HostileIntelGatherer`, `EnemyShipAbilityController`, `EnemyProductionStrategy`, patrol, recovery, placement. |
| Much of it is the same calculation repeated | True | 18 full `_entityLocator.Entities` loops and 15 full intel loops across `Services/Enemy` + ally markers, each run per player. Most filters are team-level (hostile, alive, visible), not player-level. |
| AIs already share nothing | False | `TeamIntelRegistry` holds one `HostileIntelModel` and one `EnemyPatrolMemory` per team. `AllyAiMarkerSource`/`AllyAiMarkerEmitter` pass requests between allies. `_ownTeamForce` counts allied strength. |
| "3 AIs must cooperate" | Only when allied | Roles: `AiOpponent`, `AiAlly`, `WaveAttacker`, `SimulationSide`. AIs can be on opposing teams or allied to the human → the shared unit is the team, never the whole battle. |
| One main AI controlling all groups cuts CPU | Mostly false | The expensive part is per task group (origin, own force, route safety), not per player. One brain still evaluates the same groups. Savings come from sharing inputs and from scheduling. |
| One main AI is simpler | False | Per-player state is load-bearing: `PlayerSlot.Difficulty` (decision interval 0.5–4 s, attack/retreat ratios), faction roster, economy and `ProductionHost`, station pressure, recovery anchor, super weapon, save sources, `AiStationLevelReplicatedState` for online play. |

- Already-wasted work found: `HostileIntelGatherer` runs once per AI, scans every entity each second and writes the same team intel. `VisionService.IsVisible` already treats allies as one viewer (`_relations.IsAllied(viewer, revealedTo)`) → N allied gatherers produce identical records.

## Important Values
- Current: each ship AI decision 8.8–11.8 ms; ~2 decisions/s per AI at the fastest difficulty → cost ∝ AI count.
- `EnemyStrategicContextBuilder.Build`: 5–7 entity scans + up to 2 intel passes per visible hostile structure, per task group, per decision.
- Target (confirm with user): total AI work ≤ 2 ms in any frame, independent of AI count; decision latency may stretch under load.
- Measure with existing `EnemyAI.Ship.Decision.*`, `EnemyAI.Squadron.Periodic.*`, `EnemyAI.Squadron.Launch.*` markers.

## Implementation

### Phase 0 — Baseline and scaling test
- [ ] Build a repeatable skirmish at 1, 3 and 6 AI players (same map, seed, fleet caps). Record AI ms/frame (sum of AI markers), p95, max.
- [x] Add sub-markers inside `Build` and `EvaluateAndExecute` (shared with [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan#Phase 1 — AI decision spikes (8.8–11.8 ms per call, ~6 per second across three AIs)]]).
- Gate: the 1 → 3 → 6 curve defines the "before" slope. No slope → stop; fix the single-AI cost instead.

### Phase 1 — Battle-wide world snapshot (no behaviour change)
- [x] New battle-scope `AiWorldSnapshot`: built at most once per frame, on first request (`EnemyAI.WorldSnapshot.Rebuild` marker; skips destroyed/unitless entities before facade lookups).
- Contents: living entities with units split by owner and role (bases, structures, raid structures, units), cached position, unit type, ship class, base/unit/mining flags, hull/shields, offense scale (units only), and ship hangar count.
- [x] Replace per-player entity loops in `EnemyStrategicContextBuilder`, `ForceCompositionBuilder.Build`, `EnemyRecoveryAnchor`, `EnemySquadronCommander`, `EnemyPatrolService`, `EnemyDefenseSnapshotBuilder` with snapshot reads and remove legacy fallback branches.
- Edge: an entity removed mid-frame or mid-cycle → `EnemyUnitCommander`, `EnemyStrategicContextBuilder`, `ForceCompositionBuilder`, `EnemyRecoveryAnchor`, and `TeamAiPicture` check `IsDestroyed`/`TryGetEntity` and store resolved entities rather than calling `GetEntity` across frames.
- Tests: `EnemyAi`, `Squadrons`, `Core` (`RealEntityCompositionTests`).

### Phase 2 — One planner per team (no behaviour change)
- [x] Remove per-AI `HostileIntelGatherer` and run `TeamHostileIntelService` + `AiTeamMembership` once per active AI team (live `entry.Entity.IsCloaked()` check).
- [x] New `TeamAiPicture` per team (`EnsureRefreshed` on `AssessStation`, `Build`, `SelectCaptureObjective`, `BeginCaptureProbe`, `IsMarkerApproachSafe`, `FindRaidTarget`, `FindClosestKnownShip`, `IsDefended`): confident hostile sightings, hostile force near raid structures, `zone:{i}` capture objectives, and on-demand `GetOrBuildObjectiveThreat` for both `zone:{i}` and per-player `site:{i}` objectives (`EnemySiteObjectives` stays per-player in `EnemyStrategicContextBuilder`).
- [x] `IsDefended` and `IsObjectiveSafe` read cached per-structure / per-objective threats (skipping zero-distance `IsRouteSafe` when covered); `IsRouteSafe` sums from `TeamAiPicture.BuildKnownFromConfident`.
- Keep per group: origin, own force, ion risk, smoothing, commitment, stall history.
- Tests: `EnemyAi`, `Vision`.

### Phase 3 — AI scheduler with a frame budget (latency changes, decisions do not)
- [x] One battle-scope `EnemyAiScheduler` (required constructor takes `INotifier<BattleState>`, plus `SetElapsedMsProviderForTests`) replaces per-commander timers, legacy fallback paths, and `EnemyAiStagger.WorldChangeDelay` (`EnemyAiStagger.Offset` remains for initial staggering and `EnemyShipAbilityController`).
- Work unit: one task-group evaluation, one squadron-commander pass, one production tick.
- Rules: each unit keeps its due time from its owner's difficulty interval; scheduler runs due units oldest-first until the budget is spent; the rest wait a frame.
- [x] Split `EnemyUnitCommander.EvaluateAndExecute` into reconcile (cheap, once) + one group per unit of work (removed dead `EvaluateAndExecute`).
- [x] World-change events mark owners dirty; keep the 0.5 s minimum gap.
- Edge: paused battle freezes due times. Station under attack (`StationCritical`) jumps the queue.
- Edge: starvation → cap max delay (e.g. 2 × interval), then run regardless of budget and count it.
- Tests: `EnemyAi` (`EnemyDecisionSchedulingTests`, `EnemyUnitCommanderTests`, `EnemyProductionDecisionModelTests`, `EnemyPatrolMemoryTests`, `EnemyRecoveryAnchorTests`, `ForceCompositionBuilderTests`) and `Core` (`RealEntityCompositionTests`), ordering and eventual completion only, no wall-clock assumptions. (Authored; not run locally per test-execution override.)

### Phase 4 — Team cooperation (behaviour change, needs user approval per item)
- [ ] Team-level capture reservations: today `_reservedCaptureObjectives` lives in each `EnemyUnitCommander`, so two allied AIs can commit to the same objective.
- [ ] Shared focus enemy and combined-force check for attacks: allies join one attack when their summed advantage passes, instead of each judging alone.
- [ ] Shared station defense: nearest allied group answers, others keep their task.
- Rule: a human ally is never commanded; the planner only reads their units as allied strength and still answers their markers.
- Avoid: merging allied fleets into one task group; ships keep their owner's orders, difficulty and retreat rules.
- Tests: `EnemyAi`; in-game acceptance by user.

### Phase 5 — Scaling proof
- [ ] Repeat Phase 0 at 1, 3 and 6 AIs. Pass: AI ms/frame p95 within budget at every count; decision delay reported per difficulty.
- [ ] Development Player repeat before any FPS claim.

## Edge Cases
- AIs on opposing teams → separate `TeamAiPicture` each; only `AiWorldSnapshot` is shared.
- Free-for-all (every AI its own team) → Phase 2 gives no sharing; Phases 1 and 3 still apply.
- Mixed difficulty on one team → shared picture, per-player thresholds and intervals.
- `DirectCombatAiState` (tutorial, direct combat) and `WaveAttacker` → out of scope for the planner; still read the snapshot if they scan entities.
- Online: an AI taking over a human slot gets an `AiPlayerScope` mid-match → team planner must accept players joining and leaving.
- Saves: per-player AI save sources unchanged; scheduler and snapshot hold no saved state.

## Files
- `Assets/Scripts/Services/VContainer/Scopes/Players/AiPlayerScope.cs`, `SkirmishScope.cs`
- `Assets/Scripts/Services/Enemy/AiWorldSnapshot.cs`, `TeamHostileIntelService.cs`, `AiTeamMembership.cs`, `TeamAiPicture.cs`, `TeamAiPictureRegistry.cs`, `EnemyAiScheduler.cs`, `EnemyDecisionDiagnostics.cs`
- `Assets/Scripts/Services/Enemy/EnemyUnitCommander.cs`, `EnemyStrategicContextBuilder.cs`, `EnemyShipTaskGroupPlanner.cs`, `EnemySquadronCommander.cs`, `EnemyProductionStrategy.cs`, `EnemyRecoveryAnchor.cs`, `EnemyPatrolService.cs`, `EnemyDefenseSnapshotBuilder.cs`, `TeamIntelRegistry.cs`, `ForceCompositionBuilder.cs`, `EnemyAiStagger.cs`
- `Assets/Scripts/Services/TacticalMarkers/AllyAiMarkerSource.cs`, `AllyAiMarkerEmitter.cs`

## Next Steps · 2026-11-16
- State: Phases 0 (markers), 1, 2, 3 committed (`434b9d97a`). Full EditMode run: 63 failed of 3,992; 4 from this plan. PlayMode passed. No performance measurement yet.
1. Gate — source fixes for groups A, D, E of [[TODOs/Bugs/EditMode_Failures_2026-11-16_Plan]] committed (`28f4a760c`, `b94af9697`, `92e7e4fdc`). Reviewed `RealEntityCompositionTests`, `EnemyDecisionSchedulingTests`, and `EnemyProductionStrategyTests` against the archived failures. User run `TestResults_20261116_204943.xml`: `EnemyAi` 481/481, `Core` 152/154. Both remaining Core failures stopped at active HUD cleanup during save restore. `456c9a5d9` fixes both reported destruction paths. User run `TestResults_20261116_205825.xml`: `EnemyAi` 481/481 and `Core` 153/154. `e9dce256e` removes the remaining save fixture's stale expectation of HUD destruction errors. User run `TestResults_20261116_212228.xml`: `Core` 154/154 and `EnemyAi` 481/481. Test gate passed. Next: three-AI battle verification.
2. Gate — one real battle: start a skirmish with 3 AIs; check Console for container or scheduler errors; confirm AIs build, capture relay zones and capture sites, raid, and defend the station.
3. Phase 0 baseline (still open): same Full Large map and seed at 1, 3, 6 AIs. `MatchRules` caps Full Small at 4 slots and Full Medium at 6; 6 AIs plus the human need 7. Record per frame: `EnemyAI.Scheduler.Tick`, `EnemyAI.WorldSnapshot.Rebuild`, `EnemyAI.Ship.Build`, `EnemyAI.Context.*`, `EnemyAI.Production.Evaluate`; plus `StarvationRunCount` and decisions/s per difficulty. Record nested markers for attribution and exclude double-counting from frame totals.
4. Decide from step 3:
   - Scheduler tick p95 ≤ budget at 6 AIs → Phase 5 proof in a Development Player, then close Phases 1–3.
   - One unit still > budget → shrink the unit, in this order: skip `TeamAiPicture` structure threats when no group evaluates raids; reuse zone `EnemyCaptureObjective` instances between refreshes (currently reallocated on every refresh frame); pass snapshot positions into `IncludeNearbyOwn`; split `BeginDecisionCycle` (station assessment + reconcile) into its own unit.
   - `StarvationRunCount` rising → budget too small for the AI count; raise interval per difficulty before raising the budget.
5. Source check complete: `CombatMatchup.Advantage` is non-monotonic. Adding an unarmed, quickly killable class can lower `TimeToDestroy(own, hostile)` because it raises the killable durability share. For fixed own survival of 50,000 s, increasing killable durability from 1 to 2 while killable time rises from 100 to 100.1 s (plus 1,000 unkillable hull) raises advantage from about 0.71 to 1.00. Once the EditMode gate passes, use cached objective threat only at zero route distance and evaluate complete route threat for other captures.
6. Cleanup: `handle == null` guards in `EnemyAiScheduler`, `_zones != null` in `TeamAiPicture` (project no-silent-null rule).
7. Phase 4 (cooperation) only after step 4 passes and the user approves each item.

## TODO
- User: confirm the AI frame budget (proposed 2 ms) and whether slower decisions under load are acceptable.
- User: approve Phase 4 items one by one; they change how allied AIs play.
- Unmeasured: single-unit decision duration, `AiWorldSnapshot` rebuild cost (`EnemyAI.WorldSnapshot.Rebuild`), and whether the 2 ms frame budget holds under load remain unmeasured until Phase 0 and Phase 5 captures run.
- Unverified: `EnemyProductionStrategy` (796 lines) not read for this plan; include it in Phase 0 markers.
- Related: [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]] (Phase 1 there is replaced by Phases 0–3 here), [[Done/Optimization/Live_Battle_CPU_And_Pool_Demand]], [[Done/Optimization/Enemy_Production_CPU_Spike_Plan]].
