---
category: Features
status: done
created: 2026-11-14
completed: 2026-11-14
acceptance: pending-user
---
# Enemy AI Patrol and Recovery Relocation

## Goal

- AI decisions stop flip-flopping: engage ↔ retreat and capture/patrol/anchor targets commit long enough to act.
- AI patrols the whole map: zones, mining sites, hostile structures and fogged regions get revisited on a schedule.
- When losing badly, AI rebuilds its station and free-placed structures in a **safe allied area or empty sector**, not next to the threat.
- Every new behavior writes telemetry, so acceptance is checked from battle logs.

## Owned Findings

- Old-home spawning after primary station loss → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance|Enemy AI and Faction Balance]] (Evening 19:09 audit).
- EngageLocal ↔ RetreatValue flip t230–255 and capture flip site:7 → site:2 → site:6 at t293 (`Loading_20261113_202230`) → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review 2026-11-13]] "First check". Owned by Step 0 here.

## Source Facts (pre-implementation snapshot, 2026-11-14)

### Decision stability

- `EnemyStrategicDecisionModel`: RetreatValue = `0.8 × AtMost(local, RetreatAdvantage)`; EngageLocal = `0.55 × AtLeast(local, RetreatAdvantage)`.
- `ResponseCurve.SOFTNESS = 0.25` → with cutoff `0.8`: retreat fades `0.8 → 1.0`, engage rises `0.6 → 0.8`.
- Scores cross at local advantage ≈ **0.86**. `HYSTERESIS = 0.05` moves that point by only ≈ **0.0125**.
- Logged local advantage `1.03 / 0.83 / 0.95 / 0.78` crosses 0.86 almost every tick → 4 switches in 25 s. Squadron launch/death moves the raw value every tick; no smoothing, no minimum time in state.
- Capture choice (`EnemyStrategicContextBuilder` ~590–619): `RETAIN_SAFETY_SCALE` only loosens the safety check for the current target. Selection is plain lowest `GetCaptureScore(origin, …)` from the fleet center; current target gets no score bonus → new ships move the center → target flips within one tick.

### Patrol

- Scout target priority (`EnemySquadronCommander.TryGetScoutTarget`, line 205): allied marker → unexplored spot only when `TryGetPosition` fails → stale intel (`_intel.TryGetScoutTarget`) → nearest hostile base only when `Sightings.Count == 0`. No visit history.
- One scout max (`AssignScout`, line 182); none when 0 capture squadrons, or capture targets exist with only 1 squadron. Hangar-launched squadrons excluded.
- `IssueOrder` (line 258): `_stationDefense` overrides the scout order.
- Hunt (`SquadronTargetSelector`) attacks visible enemies only; it does not explore fog.

### Recovery and placement

- `EnemyStationPressureTracker.IsStationCritical` = `HasPressure && (durability < 0.5 || ETA < 120 s)`; pressure clears after `QUIET_SECONDS = 30`.
- `TryBuySecondStation` (`EnemyProductionStrategy` line 330): blocked while critical; bought at once after main station loss (`!HasHostBase`).
- `EnemySiteConstructionController` line 48: no site construction while critical.
- Free placement: `EnemyFactionController.Build` → `GenerateMapCoordinates()` for `MiningFacilityUnitRequest`, `DefendPlatformUnitRequest`, `SecondStationUnitRequest`. Position chosen **at build completion**; no threat check. Failure throws → caught → refund.
- `EnemyStructurePlacementService.TryGetPosition`: rings around `GetStationPosition(ProductionHost)` → own captured zones (`CopyOwnedCapturableZoneBounds`) → map-wide rings from the old station. Allied zones unused; no threat ranking.
- Destroyed-position memory: last `10`, exclusion radius `1` unit → effectively no avoidance.
- `EnemyStrategicContextBuilder.FindRetreatAnchor` (line 787): safe default home, else nearest allied, uncontested, `IsObjectiveSafe` capturable zone. Not shared with placement.
- `EnemyProductionStrategy` line 154 and `EnemySquadronCommander` line 210 also call `TryGetPosition` as a "can place" probe → a safety ranking changes both.

### Telemetry

- `EnemyAiTelemetry` → `_ai.csv`, 20 columns. `RecordDecision` writes `decision` (state/objective/defense/anchor change) or `sample` (5 s). `RecordEvent(player, faction, kind, reason)` writes free `kind` rows with `key=value` details in `reason`.
- Schema reference: [[Tooling/Battle_Telemetry|Battle Telemetry]].

## User Decisions (2026-11-14)

- **Patrol force:** squadron scout + one cheapest available ship (corvette class) as patrol ship on Normal+.
  - Easy: squadron only. Patrol ship released when station defense or a task force needs it.
- **Recovery trigger:** "losing badly" in general, not only station loss.
  - Main station lost, **or** hostile fleet attacking with own fleet weaker than `RetreatAdvantage`, **or** `IsStationCritical`.
  - Recovery mode lifts the `IsStationCritical` block on `TryBuySecondStation`.
  - **Distance scales with power:** near parity → backup station near (zone the fleet can cover); clearly weaker → far (farthest safe anchor from the threat).
- **Allied area:** every ally zone/station counts — human or AI (`IPlayerRoster.IsAllied`).
- **Empty area:** "far" is defined by fog/intel, not a fixed distance.
  - Candidate = sector with no hostile sightings/structures in intel; ranked by distance from known hostiles.
  - Kinds: ally station/zone; empty corner/edge on big maps; **neutral empty relay** → capture first, then build beside it.
  - Hostile station positions are map knowledge (`TryGetNearestHostileBase`) → always penalized, even under fog.
- **Stability fixes + logs** included in this plan (2026-11-14).

## Decision

### 0. Decision stability (fixes t230–255 and t293)

- `EnemyAdvantageSmoother` (pure C#, per task group): exponential average of `LocalAdvantage` and `FleetAdvantage`, time constant `ADVANTAGE_SMOOTHING_SECONDS = 4`. Snapshot exposes raw + smoothed; rules use smoothed.
- Separate enter/leave cutoffs for retreat:
  - Enter RetreatValue: smoothed local ≤ `RetreatAdvantage` (0.8).
  - Leave to EngageLocal: smoothed local ≥ `RetreatAdvantage × RETREAT_EXIT_SCALE` (`1.25` → 1.0).
  - Implemented as a state-aware threshold in the rule curves, not by raising `HYSTERESIS`.
- Minimum time in state `MIN_STATE_SECONDS = 8` for RetreatValue ↔ EngageLocal ↔ CaptureZone.
  - Bypassed by emergencies: no own ships, station lost, DefendBase with station pressure, no safe retreat while cornered.
- Target commitment (`EnemyTargetCommitment`, shared by capture, patrol and recovery anchor):
  - Keep current target unless it becomes invalid (owned, unsafe at retain threshold, reserved, stalled) **or** a challenger scores ≥ `SWITCH_MARGIN = 20%` better.
  - Minimum time on a target `MIN_TARGET_SECONDS = 10` unless invalid.
- Starting values are proposals; tune from logs after the first battle.
- Avoid: raising `HYSTERESIS` globally (affects every rule); per-rule timers spread across files.

### A. Map patrol memory

- `EnemyPatrolMemory` (pure C#, per team, beside `TeamIntelRegistry`): sector grid + points of interest (relays, mining sites, satellite pads, known hostile structure positions).
- Each entry stores `LastSeenTime` from `IVisionService.IsVisible(owner, point)` sampled on the AI tick (no per-frame cost).
- Patrol target = argmax `staleness × value / travelCost`, excluding blocked points (`IBattleAreaService.IsBlocked`) and unsafe routes (`IsObjectiveSafe`); chosen through `EnemyTargetCommitment`.
  - Value: own/allied mine > neutral mine/relay > last-seen hostile structure > empty sector.
- `TryGetScoutTarget`: allied marker → build-space → one scorer comparing patrol staleness vs stale intel → nearest hostile base.
- Patrol ship (Normal+): cheapest ship detached from fleet groups; follows the same patrol targets; released on station defense or task-force need.
- Why: one map-knowledge source for scout, capture and placement. Avoid: a second scout commander; per-frame vision sampling.

### B. Recovery anchor

- `EnemyRecoveryAnchor` (pure C#) returns a rebuild anchor:
  1. Own default home if no station threats and route safe (same rule as retreat).
  2. Own or ally zone/station (human or AI), uncontested, `IsObjectiveSafe`.
  3. Empty sector from `EnemyPatrolMemory`: seen within `N` s, no hostile sightings, reachable.
  4. Neutral empty relay → capture objective first; placement waits until owned.
  5. None → no rebuild; scout toward best unexplored sector.
- Score = `w × nearOwnFleet + (1 − w) × farFromThreat`, `w` from smoothed fleet advantage (parity → near, weak → far). Chosen through `EnemyTargetCommitment`.
- Extract zone scoring from `FindRetreatAnchor` so retreat and recovery share one rule.
- Recovery mode latch: on by trigger (smoothed advantage under attack for ≥ `5 s`, station lost, critical); off when smoothed advantage ≥ `1.0` for `20 s` with no pressure, or the new station is built at the anchor.

### C. Placement uses the anchor

- Recovery mode search order: anchor rings → own/ally zones ranked by safety → map-wide rings **from the anchor**.
- Reject candidate within known hostile reach (intel, weapon range + margin) or the destroyed-position radius.
- `DESTROYED_POSITION_EXCLUSION_RADIUS`: `1` → structure scale (≥ placement radius; final value set in implementation).
- Capture-site mines/satellites stay on their sites; only free-placement requests move.
- Position still picked at completion (`GenerateMapCoordinates`) → uses current safety. No safe spot → existing refund path + `placementDeferred` log.
- Surviving fleet retreats to the recovery anchor to cover new structures.

### D. Telemetry

All rows in `_ai.csv` via `EnemyAiTelemetry`; details as `key=value` in `reason`. Coordinates XZ, `F1`.

| `kind` | When | Details |
|---|---|---|
| `decision` / `sample` (extended) | existing | add `localRaw`, `localSmoothed`, `fleetSmoothed`, `stateHeldSeconds`, `recoveryMode` |
| `stateSwitch` | strategic state changes | `group`, `from`, `to`, `localRaw`, `localSmoothed`, `heldSeconds`, `bypass` (none/noShips/stationLost/defend/cornered), top-2 rule scores |
| `stateSwitchHeld` | a better rule blocked by min time / exit cutoff (first per episode) | `group`, `current`, `blocked`, `scoreGap`, `heldSeconds` |
| `objectiveSwitch` | capture/patrol/anchor target changes | `domain` (capture/patrol/anchor), `fromId`, `toId`, `fromScore`, `toScore`, `margin`, `heldSeconds`, `cause` (invalid:reason / better / none) |
| `patrolTarget` | patrol target assigned | `unit` (squadron/ship id), `target` (poi id or sector x,z), `poiKind`, `staleness`, `value`, `travel`, `score`, `runnerUp` |
| `patrolVisit` | patrol target seen/reached | `target`, `stalenessBefore`, `unit` |
| `patrolShip` | patrol ship assigned/released | `ship`, `action`, `reason` |
| `patrolCoverage` | every 60 s | `sectorsSeen/total`, `maxStaleness`, `poiMaxStaleness`, `stalestPoi` |
| `recoveryMode` | latch on/off | `on`, `trigger` (stationLost/fleetWeak/critical/recovered/stationBuilt), `fleetSmoothed`, `threat` x,z |
| `recoveryAnchor` | anchor chosen/changed | `kind` (home/ownZone/allyZoneHuman/allyZoneAi/allyStation/emptySector/neutralRelay), `id`, `pos`, `w`, `threatDistance`, `oldStationDistance`, `candidates`, rejected counts per reason |
| `placement` | free placement chosen | `request`, `pos`, `source` (station/ownZone/allyZone/anchor/map), `anchor`, `nearestHostile`, `nearestDestroyed`, `recoveryMode` |
| `placementDeferred` | no safe spot at completion | `request`, `reason` (noSafeCandidate/anchorNotOwned/noAnchor), `refund` |

- Update [[Tooling/Battle_Telemetry|Battle Telemetry]] AI CSV section with these kinds.

## Log Acceptance Checks

Run after one battle per check scenario; compare against `Loading_20261113_202230` / `Loading_20261113_190921`.

| # | Check | Pass |
|---|---|---|
| 1 | RetreatValue ↔ EngageLocal flips per group | `stateSwitch` between them with `bypass=none` and `heldSeconds < 8` = **0**; ≤ 2 such switches in any 30 s |
| 2 | Raw vs smoothed advantage | swings like 0.78 ↔ 1.03 in `localRaw` do not cause `stateSwitch` unless `localSmoothed` crosses enter/exit cutoffs |
| 3 | Capture target flips | `objectiveSwitch domain=capture cause=better` with `heldSeconds < 10` = **0**; every `cause=invalid` names a reason |
| 4 | Patrol coverage | `patrolCoverage` sectors seen ≥ 80% by 10 min on Small/Medium; no own/allied mine `poiMaxStaleness` > 180 s while patrol is active (values tune after first run) |
| 5 | Patrol is running | ≥ 1 `patrolTarget` per minute when not in station defense; `patrolShip action=assigned` on Normal+ |
| 6 | Recovery order of events | `recoveryMode on` → `recoveryAnchor` → `placement source=anchor` (or `placementDeferred`) for SecondStation |
| 7 | No old-home rebuild | no `placement` within home-area radius of the destroyed station while `recoveryMode=on` and threat known |
| 8 | Near/far by power | `w` high (parity) → smaller `threatDistance` than `w` low (weak) across recovery episodes |
| 9 | Allied areas used | at least one `recoveryAnchor kind=ally*` in a team game with allied zones |
| 10 | Destroyed-spot avoidance | `placement nearestDestroyed` ≥ new exclusion radius always |

- Optional helper: a small parse script in `Tools/` summarizing checks 1–10 per file. Write only if manual CSV checks prove slow.

## Implementation

1. ~~User answers open questions~~ (done 2026-11-14).
2. **Step 0 stability:** `EnemyAdvantageSmoother`, retreat enter/exit cutoffs, `MIN_STATE_SECONDS` with emergency bypass, `EnemyTargetCommitment` on capture selection; `stateSwitch`, `stateSwitchHeld`, `objectiveSwitch` logs, extended decision rows.
3. `EnemyPatrolMemory` + sector grid + POI registration; vision sampling on AI tick; `patrolCoverage` log.
4. Patrol target scoring in `EnemySquadronCommander.TryGetScoutTarget` via commitment; `patrolTarget` / `patrolVisit` logs.
5. Patrol ship (Normal+); `patrolShip` log.
6. Extract shared safe-zone scoring from `FindRetreatAnchor`; add `EnemyRecoveryAnchor`; `recoveryAnchor` log.
7. Recovery mode latch + `TryBuySecondStation` critical-gate lift + fleet retreat to anchor; `recoveryMode` log.
8. `EnemyStructurePlacementService` recovery search order, hostile-reach rejection, larger destroyed radius; `placement` / `placementDeferred` logs.
9. Update [[Tooling/Battle_Telemetry|Battle Telemetry]].
10. Tests authored (EnemyAi); not run under local override unless the user asks.
11. User plays battles → agent checks logs against **Log Acceptance Checks** → tune values.

## Tests (EnemyAi category)

- Stability: raw advantage alternating 0.78/1.03 each tick → state stays put; smoothed below 0.8 → RetreatValue; leaves only at ≥ 1.0; emergency bypass switches immediately.
- Commitment: challenger 10% better → keep; 25% better after min time → switch; invalid current → switch at once.
- Capture: fleet center shift from joining ships does not change target within min time.
- Patrol: unseen sector beats recently seen; owned mine revisited after staleness; blocked/unsafe points skipped; patrol beats stale intel when staler.
- Patrol ship: assigned on Normal+, not on Easy; released on station defense.
- Recovery anchor: safe ally zone over threatened old home; contested zone rejected; human and AI ally zones accepted; empty sector fallback; neutral relay → capture first; none → scout.
- Recovery trigger: weak smoothed advantage under attack → on; parity → near anchor; weak → far anchor; latch off rules.
- Placement in recovery mode: near-threat candidate rejected; near-destroyed candidate rejected; capture-site requests unaffected.
- Existing `EnemyStructurePlacementServiceTests` and decision-model tests keep passing (no recovery mode → unchanged order). Assert relationships, not tuned numbers.

## Edge Cases

- Smoothing delays a real collapse → emergency bypass covers station loss / no ships / cornered; check `stateSwitch bypass` counts in logs.
- Min time keeps a losing engage up to 8 s → acceptable; tune from check 1 vs losses.
- Fog: hostile near anchor unseen → patrol staleness gates anchors (sector seen within `N` s).
- All ally zones contested → empty sector; tiny maps may have none → defer, never old-home rebuild under threat.
- Allied production host (`SelfProductionAccess`) alive → anchor still a safe zone, not the ally's station position by default.
- New station far from mines → income loss; mining free placement follows the new station.
- Survival mode: confirm whether recovery applies.
- Telemetry volume: `stateSwitchHeld` once per episode; `patrolCoverage` every 60 s.

## Files

- `Assets/Scripts/Entities/EnemyFaction/Models/{EnemyStrategicDecisionModel,ResponseCurve,EnemyStrategicSnapshot}.cs`
- `Assets/Scripts/Services/Enemy/{EnemyStrategicContextBuilder,EnemySquadronCommander,EnemyStructurePlacementService,IEnemyStructurePlacementService,EnemyProductionStrategy,EnemyStationPressureTracker,EnemySiteConstructionController,EnemyShipTaskGroup,EnemyUnitCommander,TeamIntelRegistry,EnemyAiTelemetry}.cs`
- `Assets/Scripts/Entities/EnemyFaction/Controllers/EnemyFactionController.cs`
- `Assets/Scripts/Services/Station/SelfProductionAccess.cs`
- `Assets/Scripts/Entities/Squadron/SquadronTargetSelector.cs` (Hunt visibility rule, unchanged)
- Tests: `Assets/Scripts/Tests/Editor/EnemyAi/{EnemyStructurePlacementServiceTests,EnemyUnitCommanderTests,EnemyFactionControllerLifecycleTests}.cs` + decision-model tests

## TODO

- [x] Open questions answered (2026-11-14)
- [x] Step 0: smoothing, retreat enter/exit, min time, target commitment + logs
- [x] Patrol memory + scout scoring + logs
- [x] Patrol ship (Normal+) + log
- [x] Shared safe-zone scoring + recovery anchor + log
- [x] Recovery latch + second-station gate + fleet retreat to anchor + log
- [x] Placement recovery mode + hostile/destroyed exclusion + logs
- [x] Battle Telemetry note updated
- [x] EnemyAi tests authored
- [ ] User acceptance → battles/log checks 1–10 and tuning, tracked in [[TODOs/User_Acceptance_Queue]].

## Implementation Result (2026-11-14)

- Implemented stability, team patrol memory/scout and Normal+ corvette patrol, recovery latch/anchor, neutral relay capture, safe free placement, hosted recovery purchase and required event telemetry.
- Live capture already retained viable objectives until stall; replaced implicit retention with explicit 10 s / 20% commitment. Fresh contact resets the empty-force sentinel before 4 s smoothing; neutral-relay and patrol stalls invalidate targets with retry cooldowns.
- Recovery runs from strategic `AssessStation` before group decisions, including zero-ship safe rebuilds. Recovery state prioritizes anchor coverage/required relay capture; pressure no longer sends reinforcements back to old-home defense during relocation.
- Values: sector grid 6×6; visibility sample 3 s; coverage event 60 s; recent-sector age 60 s; patrol stall 30 s; weak latch 5 s; parity release 20 s; hostile margin 100 units / unknown range 500 units; destroyed exclusion max(32 units, placement radius); lost-home exclusion max(64 units, default-zone radius) + placement clearance.
- Empty sectors require recent vision; static hostile structures use remembered patrol points because mobile intel alone excludes free structures. Placement rechecks current ownership, contesting, age and known reach at completion.
- Direct combat controllers do not run strategic recovery/patrol ship updates. Strategic AI uses the shared policy; no save format change.
- Reusable guidance extracted → [[Architecture/Enemy_AI_Objectives]] and [[Tooling/Battle_Telemetry]]. Existing large strategic classes remain scoped; future capture-policy extraction is preferable to unrelated refactoring.
- Verification: Unity script compilation/import and loaded runtime types; scoped diff/source/DI checks. `EnemyAi` regression cases authored for stability, patrol, latch/anchors, placement and hosted rebuild. Tests and Play Mode not run under local override.
- User gates: one battle per scenario, log acceptance checks 1–10 and tuning; execute `EnemyAi` tests only on explicit request. No runtime coverage or balance pass claimed.
