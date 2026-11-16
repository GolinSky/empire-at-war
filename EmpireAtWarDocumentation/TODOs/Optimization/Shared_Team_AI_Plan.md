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
- [x] New battle-scope `AiWorldSnapshot`: built at most once per frame, on first request.
- Contents: living entities split by owner and role (bases, structures, units), cached position, footprint radius, unit type, hull/shields, offense scale.
- [x] Replace per-player entity loops in `EnemyStrategicContextBuilder`, `ForceCompositionBuilder.Build`, `EnemyRecoveryAnchor`, `EnemySquadronCommander`, `EnemyPatrolService`, `EnemyDefenseSnapshotBuilder` with snapshot reads.
- Edge: an entity removed mid-frame → consumers still check `IsDestroyed`/`TryGetEntity` before issuing orders.
- Tests: `EnemyAi`, `Squadrons`.

### Phase 2 — One planner per team (no behaviour change)
- [x] Move `HostileIntelGatherer` from `AiPlayerScope` to one instance per team. Verified `VisionService` is ally-shared (`_relations.IsAllied`); implemented `TeamHostileIntelService` + `AiTeamMembership`.
- [x] New `TeamAiPicture` per team, refreshed on demand/scheduler: confident hostile sightings, hostile force near each capture objective and each structure, capture-objective candidate list with stable IDs (no per-decision `$"zone:{i}"` strings).
- [x] `IsDefended`, `IsRouteSafe` threat sums and `FindRaidTarget` read cached per-objective/per-structure hostile forces instead of calling `BuildKnown` per query.
- Keep per group: origin, own force, ion risk, smoothing, commitment, stall history.
- Tests: `EnemyAi`, `Vision`.

### Phase 3 — AI scheduler with a frame budget (latency changes, decisions do not)
- [x] One battle-scope `EnemyAiScheduler` replaces per-commander timers and `EnemyAiStagger`.
- Work unit: one task-group evaluation, one squadron-commander pass, one production tick.
- Rules: each unit keeps its due time from its owner's difficulty interval; scheduler runs due units oldest-first until the budget is spent; the rest wait a frame.
- [x] Split `EnemyUnitCommander.EvaluateAndExecute` into reconcile (cheap, once) + one group per unit of work.
- [x] World-change events mark owners dirty; keep the 0.5 s minimum gap.
- Edge: paused battle freezes due times. Station under attack (`StationCritical`) jumps the queue.
- Edge: starvation → cap max delay (e.g. 2 × interval), then run regardless of budget and count it.
- Tests: `EnemyAi` (`EnemyDecisionSchedulingTests`), ordering and eventual completion only, no wall-clock assumptions. (Authored; not run locally per test-execution override.)

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
- `Assets/Scripts/Services/Enemy/EnemyUnitCommander.cs`, `EnemyStrategicContextBuilder.cs`, `EnemyShipTaskGroupPlanner.cs`, `EnemySquadronCommander.cs`, `HostileIntelGatherer.cs`, `TeamIntelRegistry.cs`, `ForceCompositionBuilder.cs`, `EnemyAiStagger.cs`
- `Assets/Scripts/Services/TacticalMarkers/AllyAiMarkerSource.cs`, `AllyAiMarkerEmitter.cs`

## TODO
- User: confirm the AI frame budget (proposed 2 ms) and whether slower decisions under load are acceptable.
- User: approve Phase 4 items one by one; they change how allied AIs play.
- Unverified: whether `_entityLocator.Entities` scans or intel passes dominate a decision — Phase 0 markers decide the order of Phases 1 and 2.
- Unverified: `EnemyProductionStrategy` (796 lines) not read for this plan; include it in Phase 0 markers.
- Related: [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]] (Phase 1 there is replaced by Phases 0–3 here), [[Done/Optimization/Live_Battle_CPU_And_Pool_Demand]], [[Done/Optimization/Enemy_Production_CPU_Spike_Plan]].
