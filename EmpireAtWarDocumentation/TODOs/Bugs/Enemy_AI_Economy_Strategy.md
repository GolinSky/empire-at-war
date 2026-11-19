---
category: Bugs
status: in-progress
created: 2026-11-19
follows: "[[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]]"
---
# Enemy AI Economy Strategy

## Status 2026-11-19 (current)
- Plan history: E1–E7 (`2fa16bea3`) → battle 13:52 failed → regression fixes X1–X5 (`8e2ebefe6`, review fixes `28d1a41fd`) → source review R1–R9 / M2–M5 → R1–R9 implemented (`89810dcb2`); map M2/M4/M5 committed by the map session (`ac7ff075c`).
- Regression fix plan (done) → [[Done/Bugs/Enemy_AI_Economy_Strategy_Regression_Fixes]].
- Review R1–R9 and map findings → section "Review 2026-11-19" below (was a separate Review note).

### Test results 2026-11-19 (agent-run, EditMode)
- Before R1–R9: `EnemyAi` 493/501, `Ships` 213/213, `Abilities` 222/222, `Capture` 131/131, `Navigation` 380/380, `Core` 165/166.
- The 8 `EnemyAi` failures were stale fixtures (fixed in `89810dcb2`); the `Core` failure was a CLI-bridge timeout log caused by status polling (passes alone).
- After R1–R9: `EnemyAi` 503/503, `Capture` 131/131, `Map` 125/125, `Core` `ShipAutoPower_…` passes alone.
- `MapGeneration` (explicit, 3 seeds): Open Field fingerprints pass after the M5 correction; other failures were CLI-bridge timeout logs from overlapping polling, not generation results.
- Not rerun after R1–R9: `Ships`, `Abilities`, `Navigation` (passed before; no later edits in their code).
- Testing note: poll `test_status` sparingly (≥ 20 s) and never from two scripts at once; status polling during a run logs "Main thread operation timed out" errors that fail log-sensitive tests.

### Open
- [x] R2 step 4: covered by the rebuild (far reinforcements stay separate while either group fights). Verify in a battle.
- [ ] Rebuild P1–P7: run `EnemyAi` tests and the 1v1 battle (see "Rebuild — objective selection").
- [ ] Tests for R2 (centre cap, expansion in the same cycle), R3 (unseen income split), R5 (zone reservation), R6 (eligibility, group cap). Planner/builder need heavy fixtures; add with the 2v2 battle evidence. R1 and R4 have tests.
- [ ] Battle: UltraHard Small 1v1 (X1–X5 + R-fixes acceptance; telemetry: `deployment` tier/heldSeconds, `zoneOwner`, `expansionAssigned`, `sample` teamHeldZones/visibleAdvantage/unseenInvestment).
- [ ] Battle: 2v2 Medium (AI ally + 2 AI enemies) and 1v1 Large; compare `zoneOwner`, `deployment` tier, `objectiveSwitch`, `expansionAssigned`.
- [ ] R7: restate acceptance gates as travel ETA + capture time + 60 s in [[TODOs/User_Acceptance_Queue]] when the first non-Small battle is planned.
- [ ] Rear deployment on ring maps: check the share of `tier=A` vs `B/C/D`.

## Evidence — battle 2026-11-19 16:30 (UltraHard Small 1v1, code `00d0c90e7`, dirty)
Files: `BattleTelemetry/Loading_20261119_163020_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_163022_map.csv`, `Battle_20261119_163142.csv`.
- User: "results are amazing"; noticed no satellites after the AI took the centre.
- Log time 51.0 s = battle 0 s. Battle 1,633 s. AI Separatist (1718, −724); human Republic (−1718, 724). Map seed −1067870165.
- AI main station died ≈ 1,426 s; backup station (bought 646 s, placed 736 s at (−1150, −1150)) died 1,562 s.
- Telemetry ids: `zoneOwner zone:1` = map zone 3 (human side), `zone:2` = map zone 4 (AI side).

### Worked
- X1: 94 deployments, all `tier=A`, max `heldSeconds` 4.0. No blocked queue.
- X2: 3 Patrol Frigates + 2 C-9979 before level 2, none after until the last stand (9 `critical=True` corvettes 1,360–1,397 s).
- X3/X4: no opening passivity; centre taken 455 s, retaken 790 s; Consolidate 487–631 s, no base push.
- Outer zones: AI side sites 18/19/17 at 68–82 s, human side sites 15/16/14 at 86–102 s.
- Levels 2–5 at 90 / 240 / 420 / 645 s; `IncreasedStationIncome` I 415 s, II 645 s.

### Failures
- **F1 (P0) Money not spent at the population cap.** Money 13k (597 s) → 74k (713 s) → 86k (≈ 790 s); 42–69k until 1,075 s. `production-wait reason=capacity` ≈ 560 rows, 597–1,190 s. In that window: 1 defend platform, 5 satellites, 1 research. Fleet fell 22 → 7 ships; centre lost 715 s and 984 s.
- **F2 (P0) Centre gets 3 satellites for 6 pads.** First hold 455–715 s: pads 4/8/9 at 460–466 s; pads 5/6/7 owned 472–685 s, never built. Cause: `EnemyDefenseStrategy.ZONE_SATELLITES = 3` counted per point (`point.Existing`), in `NeedsMandatorySatellite` and `TrySelectScored` (line 34).
- **F3 (P1) Defend platforms: 2 in 1,633 s, both at the centre** (Xq6 455 s, Golan III 791 s). AI-side relay held 80–664 s and 754 s → end, home zone all battle: none. Cause not confirmed (`TryGetDefensePosition` → `TryGetUndefendedZonePosition` for non-centre zones).
- **F4 (P1) Human-side outer zone lost 307–351 s, never retaken (1,280 s).** One ship objective (`zone:1`, 439–511 s, `invalid:unsafe`); `expansionAssigned zone=expansion:1` × 10 (455–790 s) produced no capture objective.
- **F5 (P2) State flapping.** Group 11 EngageLocal ↔ RaidStructures 20 switches 844–970 s; CaptureZone ↔ EngageLocal ↔ GuardSite 1,455–1,545 s.
- **F6 (P2) Backup station** placed 90 s after purchase, `nearestHostile=338.7`.
- **F7 (P2) Opening centre lost.** Human owned the centre at 55.8 s; AI took pads 9/8 (48/57 s) and mine 3 (156 s), lost all by 190 s. Station attacked 193–240 s (`defendHome`).
- Research was exhausted by 645 s (Separatist data: `ReinforcedStructures` has 1 tier) → not a money sink.

### Fixes 2026-11-19 (after the 16:30 battle; compiled in the Editor, tests not run)
- [x] **F2** `EnemyDefenseStrategy.TrySelectFill`: money above `ProductionReserve` fills every empty owned pad without hostiles, centre → relay → mining point. Called from `EnemySiteConstructionController.BuildNext` after mandatory satellites, mines and the scored choice. Required counts (3 per zone, 1 per mining point) unchanged.
- [x] **F3 part 1** `EnemyStructurePlacementService.TryGetUndefendedZonePosition`: "zone already has a platform" used `IsInZone` (2 × 270 = 540) while platforms are placed on rings from 716 (`zone.Radius` + station-sized `_placementRadius` + 12) → the centre always looked undefended and took every platform. Now checks within `max(2 × radius, first ring + 3 × 12)`.
- [ ] **F3 part 2** (unverified) relay and home zones: ring at 716+ from the relay centre needs own vision (`ReinforcementSpawnRule.IsOpen`) → likely no candidate without ships there. Check `placement request=<platform>` positions in the next battle.
- [x] **F1** (user 2026-11-19: "allow more"): when the chosen ship is affordable but blocked by the unit limit, held zones take further platforms. `EnemyProductionStrategy` passes `shipBlockedByCapacity` to `TryGetDefensePosition(allowExtra, …)`; every held zone gets its first platform before any gets another; `MAX_ZONE_PLATFORMS = 3`. Placement at build time always allows an extra (no refund loop). 20 s spacing and the fewest-copies type choice unchanged.
- [x] Data (user 2026-11-19): `DefendPlatformCatalog` Golan III and Empress `UnitCapacity` 3 → 0 (all platforms now 0). Limits: Xq6 1,000 × 5, Golan III 12,000 × 2, Empress 10,000 × 2 → up to 49k of platforms at 120/120. Applies to every faction and to the human player.
- Telemetry added (Editor / development builds):
  - `defensePlacementBlocked` (≤ 1 report per 30 s, one row per held zone below its limit): `zone`, `main`, `capturable`, `platforms`, `limit`, `firstRing`, and candidate counts `outOfBounds` / `notVisible` / `blocked` / `excluded` / `unsafe` / `occupied` → answers F3 part 2.
  - `expansionAssigned` gains `required`, `groupShips`; new `expansionReleased` (`zone`, `pos`, `owner`, `groupShips`) when a zone detachment is dissolved → answers F4.
  - Existing: `satellite` rows with reason `Fill owned pad` → F2.
- Tests added (`EnemyAi`, `EnemyStructurePlacementServiceTests`, not run): `DefensePosition_ZoneWithAPlatformOnItsRingIsSkippedForTheNextHeldZone`, `…EveryHeldZoneDefended_OnlyAnExtraRequestFindsAPosition`, `…ExtraPlatformsSpreadEvenlyAndStopAtTheZoneLimit`, `…IgnoresZonesHeldByAnotherPlayer`. Fixture now has a zone source, world snapshot and platform entity. `StructurePlacementServiceStub` updated for the new signature.
- Test run 2026-11-19 17:26 (agent, user-requested; EditMode, category `EnemyAi`, explicit excluded, code `cd52ee89a`): 510/510 passed in 12.3 s, including the 7 new tests. Other categories not run.
- Not covered by tests: `EnemyProductionStrategy` passing `shipBlockedByCapacity` (needs the heavy production fixture).
- Tests added (`EnemyAi`, `EnemyDefenseStrategyTests`): `Fill_EmptyPadOfAZoneWithItsRequiredSatellitesIsBuiltFromSpareMoney`, `Fill_WaitsWhileMoneyIsReservedForProduction`, `Fill_SkipsAPadWithHostiles`. No test for the platform check (`EnemyStructurePlacementServiceTests` has no zone/structure fixture).
- [ ] Battle check: centre pads 4–9 all get satellites after capture; platforms appear outside the centre; F4–F7 still open.

## Evidence — battle 2026-11-19 17:31 (UltraHard Small 1v1, code `5e4bff019`, dirty) — user: "bad results, I win fast"
Files: `BattleTelemetry/Loading_20261119_173121_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_173124_map.csv`, `Battle_20261119_173233.csv`.
- Human Empire (−662, 1688) vs AI Rebellion (662, −1688). Kashyyyk, seed 850465432, 45 asteroid fields. Log time 29.4 s = battle 0 s. AI station dead 820.7 s.
- Distances match the 16:30 map: centre 1,812 (was 1,865), own-side outer zone 2,346 (was 2,433) → map geometry is not the cause. No `stalled` objective; 5 `blockedApproach`, all hostile strength.

### Not caused by `cd52ee89a`
- No `Fill owned pad` satellite, no defend platform purchase, no `defensePlacementBlocked`, capacity ≤ 56/120 → none of the new paths ran. Strategic decision code unchanged by that commit.

### Cause chain
- **O1 Opening lost to squadrons.** Human had no ship until 104 s but owned both outer zones by 65–87 s (2 wings to each) and the centre by 142–180 s. AI: 2 wings on AttackMove (centre pads 5/6/4 at 52–59 s), 2–3 wings on Guard at home, 0 wings to outer zones. 16:30 battle: 3 wings out → one per zone.
- **O2 Own-side outer zone lost by ≈ 7 s.** `expansionAssigned` 34.5 s, eta 49.4 s → ≈ 84 s; human took pads 15/16 at 77–79 s, mine 14 at 87 s. First AI ship spawned 34 s (16:30: 25 s): 3 X-Wing wings cost 2,400 (Vulture: 600) before the first corvette.
- **O3 Weak Rebel opening fleet.** 6 Corellian Corvettes before level 2 (110 s). `fleetAdvantage` 0.17 at 128 s (3 corvettes vs 1 Raider), 0.17–0.25 at 148–182 s (4 vs Raider + Acclamator + Quasar + Victory I).
- **O4 No comeback behaviour.** From 180 s the AI held 1 mine + 1 pad; human held centre + both outer zones. `teamHeldZones=0` to the end.
  - Every capture objective rejected for 640 s ("route/local force advantage ≤ 0.80", "outnumber two to one"); only centre sites listed.
  - `RaidStructures`: 4 decisions, none after 110 s. No `expansionAssigned` after 115 s, no `expansionReleased`.
  - `EngageLocal` 96 decisions; "Hostiles near the fleet outmatch it; fight in place" × 75. `recoveryMode=True` 272 s → end, "No safe local retreat anchor".
  - One ship bought every ≈ 15 s and sent alone: AI lost 43 ships (13 at the centre, 12 in transit, 18 at home) vs human 17. Fleet 3–7 ships all battle.
- Faction vs playstyle: the causes below are faction-independent code paths; Rebellion only made the opening slower (O2, O3).

### Verified causes in code (2026-11-19, code `5e4bff019`)
| # | Cause | Code | Log evidence (17:31) |
|---|---|---|---|
| V1 | Safety is a yes/no gate: one group vs every known hostile within 1,000 of the route or destination; fail → rejected, nothing records what force would be enough | `EnemyStrategicContextBuilder.IsObjectiveSafe` → `IsRouteSafe` → `IsThreatSafe` (`RetreatAdvantage` 0.80); `_routeHostileShipCount >= 2 × ships` | "advantage 0,18–0,50 <= safety threshold 0,80", "outnumber … two to one" on every row 142–650 s |
| V2 | Three strength measures disagree: combat power (gate), capture strength = unit count (detachment sizing), ship count (2:1 rule) | `GetRequiredExpansionShips` (`HostileStrength + 1`) vs `IsThreatSafe` vs `IsRouteSafe` | `expansionAssigned required=1–2`; same zone dropped `invalid:unsafe` at 139.7 s |
| V3 | A hostile site with a built satellite or mine is not an objective at all (mine only if scouted); a visible armed structure rejects any route regardless of fleet size | `EnemySiteObjectives.IsCapturable` (state must be Neutral/Owned); `IsInsideStaticDefense` returns unsafe unconditionally | rejection lists contain only centre sites 0–3, 5 and `zone:0`; outer sites 14–19 never appear after 140 s |
| V4 | Outer zones stay reserved for their expansion detachment while the zone has any objective; the main force skips them | `_assignedExpansionZones` + `IsAssignedExpansionObjective`; `Reconcile` keeps the group while `HasExpansionObjective` | 4 `expansionAssigned` by 115 s, 0 `expansionReleased`, 0 later assignments |
| V5 | Fallback ignores the gate: with no objective the main force "guards" the centre even when the enemy holds it | `Build`: `guardPosition = centre.Center` for the main force; `GuardSite` commits all ships | GuardSite at 359, 401, 450, 551, 612 s (ships on AttackMove 1,300–1,950 away, 12 hostile ships) → EngageLocal each time |
| V6 | Outmatched + hostile within 1,000 → forced "fight in place" with all ships; no disengage | `EnemyStrategicDecisionModel.Evaluate` `standAndFight` override | "outmatch it; fight in place" × 75; `recoveryMode=True` 272 s → end; "No safe local retreat anchor" |
| V7 | Each new ship joins the main group alone, wherever it fights | `Reconcile` → `AssembleMainForce` | 1 purchase / ≈ 15 s; 43 ships lost (12 in transit); fleet ≤ 7 |
| V8 | Raids use the same gate | `FindRaidTarget`: `IsDefended` or `!IsRouteSafe` → skip | 4 `RaidStructures` decisions, none after 110 s |
| V9 | Opening wings: only purchased wings capture (hangar wings on Guard are skipped); a contested target needs `hostile / weight + 1` wings before others get one | `AssignCaptureTargets`, `EnemySquadronCaptureAssignments.AssignGroup` | 3 X-Wing wings; first 3 captures all centre pads (52–59 s); no outer pad. Per-wing targets were not logged → `wingAssignment` added |

### Telemetry added 2026-11-19 (Editor / development builds; compiled, not battle-tested)
- `captureBlocked` (≤ 1 / 15 s per group with no objective): `ships`, `mainForce`, `expansionZone`, `focus`, `candidates`, `ranked`, then a count per removing rule (`reservedForExpansionDetachment`, `mainForceCentreOnly`, `nearHostileBase`, `reservedByOtherGroup`, `stalledRetry`, `notInFocusZone`, each safety reason).
- `expansionIdle` (≤ 1 / 20 s, one row per outer zone when no ship is sent): `owner`, `mining`, `objective`, `eligible`, `reservedByAlly`, `groupShips`, `groupNoViableSince`, `canOpenGroup`, `unassigned`, `mainShips`, `required`, `eta`.
- `wingAssignment` (on change, ≤ 1 / 6 s): wing counts and every target as `(x z):needN:gotM[:opening][:threatened][:pad]`.

## Rebuild — objective selection (implemented 2026-11-19; compiled, tests not run, battle pending)
### Implementation result 2026-11-19 (user: "execute", pull-back decision taken as proposed)
- Reference sheet → [[Architecture/Enemy_AI_Objectives]] section "Zone objectives".
- [x] P1 `Entities/EnemyFaction/Models/Objectives/`: `EnemyZoneVerdict`, `EnemyZoneObjective`, `EnemyZoneAssessment`, `EnemyZoneEvaluator`. Tests `EnemyZoneEvaluatorTests` (9).
- [x] P2 `Services/Enemy/EnemyZoneObjectiveCollector.cs`: zones from capture candidates; defenders = intel + visible armed structures.
- [x] P3 in place, not a new allocator class: `GetRequiredExpansionShips` by combat power; main force no longer skips expansion-reserved zones; zone/centre detachments released after 45 s of gathering; far reinforcements stay separate while either group fights (closes R2 step 4).
- [x] P4 `SelectCaptureObjective` uses zone verdicts; route/2:1/static/capture-strength gates removed from selection; staging guard point; main force guards the centre only when held; pull-back rule (`CanDisengage` → `RetreatValue`, 8 s hold).
- [x] P5 raids: route gate removed, static coverage skip; wings: one per opening zone first.
- [x] P6 `zoneEval` telemetry; `captureBlocked` only when no Go/Assemble; `expansionIdle` kept.
- [x] P7 dead code: `GetOwnCaptureStrength`, `CAPTURE_STRENGTH_MARGIN`, `_routeHostileShipCount`, `_checkingExpansion`; architecture note updated.
- Deviations: zone score = existing best-site score − missing-force cost (keeps today's Go ranking); structure DPS is a flat estimate (60/class, no weapon profile in the catalog); unseen-fleet share for never-scouted zones not built.
- Tests: added `EnemyZoneEvaluatorTests`, 3 pull-back tests (`EnemyStrategicDecisionModelTests`), 1 wing test. Changed contract → updated `CentreCapture_ReinforcesWhenKnownDefendersOutmatchTheGroup`, `CaptureSelection_ZoneVerdictWeighsTheGroupForceAgainstEveryZoneDefender`, `CaptureProbe_OpeningCentreFocusDetachesIntoAContestedCentre` (presence count no longer blocks). Not run (local override). Category: `EnemyAi`.
- [x] Test run 2026-11-19 19:02 (user-run, `TestResults_20261119_190221.xml`): 4222/4227 passed, 1 failed, 4 not counted as pass or fail (likely skipped or inconclusive; not checked). Failure `RaidSelection_RejectsUndefendedTargetBehindStrongerRouteForce(0,False)` asserted the removed raid route gate (V8) → rewritten as `RaidSelection_RouteForceNoLongerBlocksButAStaticDefenseDoes` (route force no longer blocks; a covering armed structure does). Compiled; not rerun.
- [ ] Battle: UltraHard Small 1v1 replay of the 17:31 situation; check `zoneEval`, `captureBlocked` = 0, no `GuardSite` at hostile zones, `RetreatValue` with reason "pull back to the gathering point".

### Original proposal
### Decision
- Replace the per-site yes/no safety gate and its fallbacks with one zone-level evaluator that always returns a plan.
- Why: V1–V8 are one design fault — "unsafe" ends the decision instead of producing "what is needed". Patching single thresholds has failed 3 times (X3/X4, R2, this battle).
- Avoid: new thresholds, new special cases for centre / expansion / raid, more fallback states.

### Rules
- **Objective = zone** (centre, each relay, each mining point). Sites, pads, mines and hostile structures inside it are its contents, not separate objectives.
- **One strength measure**: `CombatMatchup.Advantage(assigned force, defenders)`. Defenders = known hostile units in the zone (intel confidence-weighted) + armed structures in range as combat profiles (satellites, platforms). No unit-count rule, no 2:1 rule, no separate capture-strength rule for safety.
- **Verdict is never a bare reject**:
  - `Go` — assigned force ≥ commit threshold.
  - `Assemble` — force too small: stage at the nearest held zone (home zone if none) outside hostile reach, add ships until `Go`. Records `requiredForce`.
  - `Deny` — hard reasons only: inside the hostile base exclusion before the assault gate, or unreachable. Denied zone with hostile mines → sabotage by wings.
- **There is always a target** while the AI has ships: cheapest `Go`, else cheapest `Assemble`. Score = value (mines, pads, relay, centre bonus) ÷ (travel time + required force).
- **Behind on zones** (team holds fewer zones than the enemy): own-side zones first; the centre only as `Assemble`; never guard a zone the team does not hold.
- **Engagement**: outmatched group away from an owned station or site → disengage to its staging point and re-evaluate there. Fight in place only when defending the station or a held site, or when cut off.
- **Reinforcements** rally at the group's staging point and join when the group is there or the batch meets the requirement; no ship travels alone into a fight (closes R2 step 4).
- **No exclusive zone reservation**: one allocator assigns ships to zones by score until `requiredForce` is met; unused ships stay in reserve at staging. Team reservations (`TeamAiCooperation`) keep working per zone.
- **Wings (opening)**: one wing per uncaptured zone before any zone gets a second; hangar wings on Guard stay with their carrier.

### Implementation
- [ ] **P1 Model (pure C#, no Unity types)**: `EnemyZoneObjective` (id, owner, value, defenders, statics, travel), `EnemyZoneVerdict` (`Go` / `Assemble` / `Deny`, `requiredForce`, staging point, reason), `EnemyZoneEvaluator`. Tests first: each verdict, structure counted as defender, always-a-target, behind-on-zones ordering.
- [ ] **P2 Collection**: build zone objectives from `IReinforcementZoneSource`, `ICaptureSiteDirectory`, intel sightings and visible hostile structures; hostile-built pads and mines stay inside their zone objective (replaces `IsCapturable` filtering for planning).
- [ ] **P3 Allocator**: replaces `TryCreateCaptureGroup`, `TryReinforceCentreGroup`, `TryAssignExpansionShip`, `TryDetachForCapture`, `_assignedExpansionZones`, `GetRequiredExpansionShips`, `CentreGroupAtCap`. Staging and reinforcement rally live here.
- [ ] **P4 Group decision**: `SelectCaptureObjective` picks the site inside the assigned zone only (nearest capturable, then blocking structure); delete `IsObjectiveSafe` / `IsRouteSafe` / `IsThreatSafe` / `IsInsideStaticDefense` use for objective selection. `GuardSite` only for held zones. Replace `standAndFight` with the engagement rule.
- [ ] **P5 Raids and wings**: raid targets come from `Deny` / `Assemble` zones; wing opening spread.
- [ ] **P6 Telemetry**: one `zoneEval` row per zone on verdict change (`verdict`, `required`, `assigned`, `defenders`, `statics`, `eta`, `score`, `staging`, `reason`); remove `captureBlocked` / `expansionIdle` when superseded.
- [ ] **P7 Cleanup**: remove dead fields and tests of the old gate; update [[Architecture/Enemy_AI_Objectives]].

### Files
- New: `Entities/EnemyFaction/Models/Objectives/` (`EnemyZoneObjective`, `EnemyZoneVerdict`, `EnemyZoneEvaluator`), `Services/Enemy/EnemyZoneObjectiveCollector.cs`, `Services/Enemy/EnemyZoneAllocator.cs`.
- Changed: `EnemyStrategicContextBuilder.cs` (≈ 1,950 lines → selection and safety code removed), `EnemyShipTaskGroupPlanner.cs`, `EnemyStrategicDecisionModel.cs`, `EnemySquadronCommander.cs`, `EnemySquadronCaptureAssignments.cs`, `EnemySiteObjectives.cs`.

### Edge Cases
- No held zone → staging = home zone / station.
- Station under pressure → existing `DefendBase` path keeps priority; allocator pauses.
- Unknown defenders (never scouted zone) → treated as empty in the opening grace (180 s), then by the unseen estimate share.
- Team games: required force may be met by allied strike strength (`AddAlliedStrikeStrength`).
- Centre with 6 pads and 4 mines: one zone objective; the site order inside is pads → mines.

### Acceptance (replay of the 17:31 situation)
- Enemy holds centre + both outer zones, AI fleet 0.4 of the enemy: AI stages at home, takes its own-side outer zone when `requiredForce` is met, wings raid mines; no ship dies alone in transit; no `GuardSite` at a hostile zone.
- Telemetry: every 15 s at least one zone with verdict `Go` or `Assemble`; `captureBlocked` rows = 0.

### Open decisions for the user
- Disengage when outmatched (new engagement rule) — the earlier plan removed retreat because fleets collapsed while fleeing. Proposed: disengage only to the staging point, with the existing 8 s state hold.
- Scope order: P1–P4 first (one change set), then P5.

## Reopened 2026-11-19 — acceptance failed (user: "full failure")
- `2fa16bea3` played 2026-11-19 13:52 on UltraHard (Separatist AI, top-right). User: lots of corvettes again, one sabotage, idle instead of acting, no early centre take/retake, unsure it captured even 2 mining spots.
- Implementation history (E1–E7 in `2fa16bea3`) kept below. Fix the regressions first (X1–X5), then re-check E1–E7.

## Evidence — battle 2026-11-19 13:52 (UltraHard, code `2fa16bea3`, dirty)
Files: `BattleTelemetry/Loading_20261119_135239_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_135243_map.csv`, `Battle_20261119_135406.csv`; Editor.log `Difficulty=UltraHard`.
- Log time 20.4 s = battle 0 s. AI station (1118, 1087) top-right; human Republic (−1118, −1087). Outer zones: 3 (−1545, 1453) top-left, 4 (1545, −1453) bottom-right.
- Main station died ≈ 580–600 s (hull 46,969 at 570 s). Backup station placed 606 s at (−1667, 1000) by patrol; dead by 776 s. Battle ≈ 810 s.
- Decisions: EngageLocal 74, RebuildFleet 56, CaptureZone 20, DefendBase 10, Raid 6, Hold 5, GuardSite 4. Top reasons: "Hostiles near the fleet outmatch it" 58, "No combat ships are available" 56.

### X1 — Finished ships never deployed (P0, regression)
- 7,262 `deploymentBlocked` (minutes 4–9: 354, 767, 1163, 1576, 1962, 1419); only 1 `deploymentForced`.
- AI combat ships on the map: **0 from 296 s to 634 s** while purchases continued.
- Human squadrons reached 246–304 from the AI station at minutes 3–4; human ships 324–551 at minutes 7–9.
- Cause (source): new rear deployment in `EnemyFactionController` — `hasRear` → `TryGetDefenseSpawnPosition` / `TryGetBestDefenseSpawnPosition(stationAnchor, _defenseDeploymentThreats)` rejects every point near known threats; the bounded-hold forced spawn (8 s) is not reached on this path.
- Effect: at 609–614 s, after the backup station appeared, 3 Providence + 3 Recusant + squadrons deployed at once — the held queue.

### X2 — Corvette spam (P0, regression)
- 32 C-9979 (22–209 s, ≈ 38k credits) + 6 Patrol Frigates; still C-9979 after level 2 (108 s) until 209 s; then Diamond chains (236–348 s, 609–746 s).
- Each bought the moment money reached the price (money at purchase 1,200–1,460; `counter=0`, `critical=False`).
- Cause: E5 "build queue never idles while money ≥ the cheapest useful item" → the cheapest ship wins every tick; the AI never saves for better ships.

### X3 — Unseen-enemy estimate makes the AI passive (P0, regression)
- `fleetAdvantage` 0.69 at 31 s with 1 own ship vs 0 seen; 0.06 at 142 s with 0 seen; ≤ 0.15 all battle.
- Cause: `EstimateUnseenFleetInvestment` (`EnemyStrategicContextBuilder.cs:1479`): before any hostile ship is seen, `lastSeen` = battle start → hostile income × min(elapsed, 90 s) × 0.5 + `UNSCOUTED_FLEET_INVESTMENT 1200`; plus `fleetRaw /= _hostileEconomyPressure`.
- Effect: "Hostiles near the fleet outmatch it" ×58, Hold/EngageLocal instead of capture.

### X4 — No centre (P0)
- Centre objectives: pad 8 at 62.6 s (groups 3/4) → `invalid:blockedApproach` 71.8 / 75.4 s → Hold; site 0 at 112.6 s → `blockedApproach` 137.7 s.
- `blockedApproach` = the centre form of `unsafe` (`EnemyStrategicContextBuilder.cs:1149`), driven by X3.
- No AI satellite at the centre the whole battle (satellites: home pad 13 at 9 s, top-left pads 16 / 15 at 75 / 132 s).

### X5 — Expansion and sabotage thin (P1)
- Mining: home site 12 mine 57 s; top-left pads 16/15 (71/82 s), mine 14 later dropped `unsafe` (151.8 s); bottom-right sites 19/17 targeted 86–104 s. Second site mine started 90 s. No centre mines.
- Sabotage: RaidStructures 6 decisions ("raids an undefended structure" ×3).

## Fixes (reopened)
- Detailed fix plan → [[Done/Bugs/Enemy_AI_Economy_Strategy_Regression_Fixes]] (causes with source lines, changes, tests, verification; implemented 2026-11-19, pending next UltraHard battle).
- [x] **X1** Rear deployment must always resolve: try rear → station sides → home zone → forced spawn after the bounded hold (8 s) on every path; never keep finished ships in the queue for minutes. Add a test: threats around the station still deploy within the hold limit.
- [x] **X2** Replace "never idle" with "never idle **after** saving for the chosen unit": pick the best affordable-soon ship for the level/role and save for it; cheapest-ship buys only for the opening corvette and real emergencies. Cap level-1 corvettes (C-9979, Patrol Frigate) to ≤ 1–2 alive at faction level ≥ 2.
- [x] **X3** Unseen estimate: no unscouted penalty in the opening (before the first hostile contact use 0, or only scouted mines); cap the unseen share (e.g. ≤ the AI's own fleet value); do not divide by economy pressure twice. Opening captures/centre never blocked by an estimate alone.
- [x] **X4** Centre is the opening priority: `blockedApproach` only for seen hostile ships/defenses stronger than the group (R10 strength), not for the unseen estimate.
- [x] **X5** Re-check E1 (every safe mining zone) and sabotage after X1–X4; record per-zone ownership timeline in the next battle.
- [x] Tests: deployment under station threat (X1), production saving (X2), unseen estimate in the opening (X3), centre not blocked by estimate (X4). Categories `EnemyAi`, `Ships`, `Economy`.


## Goal
- AI plays the economy game: owns the centre plus every safe mining zone, keeps building (mines, satellites, defenses, levels, research, ships) and only assaults the human base when it is decisive.
- Follows [[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]] (user 2026-11-19: huge improvement; sabotage perfect; centre retake amazing).

## User rules (2026-11-19, final)
- Capture **all** mining on the map except mining near hostile structures (near the enemy base). One empty outer zone left for 1–2 min is a bug.
- After the centre is captured/retaken: **do not push the enemy station**. Heavy cruisers barely hurt a station; the human still has money (3 mining spots) and ship production → the AI fleet dies there.
- Instead: hold the centre; rebuild all mining and satellites; level up; research; build ships; build defenses (different defend platforms, staggered, not all at once); sabotage; retake remaining zones. Keep the build queue running all the time.
- Zone + mining on the same spot = reinforcement deployment + mining + defenses = money = the main advantage.
- Second station: allowed again at **low priority**, not early. Build it when the main station is dying/about to die, or late with surplus money. Place it on an empty safe spot found by patrol, not next to the main zone.
- Station income research (`IncreasedStationIncome`): high priority.

### Battle flow (user 2026-11-19, final)
1. Early: take as many zones as possible; build mining, satellites (rules above).
2. Take the centre and keep it.
3. Build defenses and level up.
4. Retake or take all remaining zones. If that is not possible → sabotage: kill hostile mining facilities.
5. Base attack only when the assault gate below is met.

### Assault gate (user 2026-11-19, final)
- AI station at level 4 and the attack fleet is level-4 ships (capital ships).
- Reinforcement plan: more ships keep arriving during the attack (production + held reserve sent in waves).
- Zone control: AI holds more than 50 % of the zones (not all needed — the human retaking zones will interrupt the attack).
- Money override: a lot of money + a big fleet can start the attack even when the human has more mining facilities.
- Otherwise: stay in the flow above (hold, build, retake, sabotage).

## Evidence — battle 2026-11-19 12:39 (UltraHard, Separatist AI, code `529094e87`)
Files: `BattleTelemetry/Loading_20261119_123927_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_123930_map.csv`, `Battle_20261119_124037.csv`; Editor.log `Difficulty=UltraHard`.
- Log time 20.7 s = battle 0 s. Battle ≈ 1,108 s (longest so far). AI station (−1214, −1262) bottom-left; human Republic (1214, 1262) top-right; AI home zone 2 (−443, −1262); human home zone 1 (308, 1262).
- Outer mining zones: zone 3 (1590, −1466) = mine 14 + pads 15/16, 2,804 from the AI; zone 4 (−1590, 1466) = mine 17 + pads 18/19, 2,729 from the AI.

### Worked (keep)
- `IncreasedProduction` I at 0 s, II at 86 s. Combat research from 40 s (ImprovedDefenses, ImprovedWeapons, EnhancedEngines, ReinforcedStructures).
- Ships during the level-2 upgrade: Patrol Frigate 10 s, C-9979 15 s, Patrol Frigate 26 s.
- Centre: satellites 45/48/57 s, retaken later (satellites 451–457 s, 4 centre mines 478–517 s).
- Decisions: EngageLocal 107, CaptureZone 90, RebuildFleet 42, HuntFleet 16, Raid 3, AssaultBase 3. No retreat collapse.
- `IncreasedStationIncome` is bought right when it unlocks: I (data `AvailableLevel` 4) at 423 s ≈ 15 s after level 4; II (level 5) at 643 s ≈ level 5 done.

### Mining zones (E1)
- Zone 3: AI created a new capture group for pads 15/16 **8 times** (58–146 s: groups 5, 6, 8, 9, 11, 13, 14, 16); each dropped `invalid:unsafe` (103.6, 135.1, 248 s). Zone was empty until 120 s (then 2 human squadrons); first AI unit there at 240 s. Mine 14 targeted once (900 s). Never owned.
- Zone 4: groups 64–128 s (sites 19/18/17); human had 1 unit there from 60 s. AI satellites on pad 19 only at 592–790 s; mine 17 retargeted 584–917 s.
- By ≈ 4 min the AI held home + centre only.

### Post-centre push (E2)
- 420–499 s: group 23 flips CaptureZone ↔ HuntFleet (8 switches); 523 s AssaultBase (`localRaw=100`, nothing visible).
- HuntFleet at 430 s: own 10 vs enemy 1 seen, `fleetAdvantage 2.64`; 455 s: own 9 vs 0 seen, advantage 22.68 (R11 economy term not enough).
- 552–767 s: ≈ 27 AI ships lost, mostly 150–900 from the human home zone and 600–1,250 from the human station. Own ships 12 (607 s) → 2 (785 s).

### Defenses (E4)
- One defend platform (Xq6 at 33 s) for the whole 1,108 s battle.

### Purchases / spawns (E5)
- Level-5 counter picks of level-1 corvettes: Patrol Frigate 1,062.7 s + C-9979 1,063.7 s (`counter=1`, `critical=False`, money 7.5k).
- Normal deployments spawn on the home-zone side, 650–990 from the station (866–1,028 s); 298 `deploymentBlocked` at minutes 17–18.
- Max money at a purchase 20,641 (median 3,730).

## Implementation

### E1 — Expansion

- [x] Assign one persistent detachment to each eligible outer mining zone. Rank open objectives by travel ETA and remember the zone while its group waits for reinforcements.
- [x] Size contested capture groups from sampled hostile strength plus one ship-equivalent. Check known route threats, static defenses and clearance before moving. Reserve assigned zones across groups.

### E2 — Consolidate and assault

- [x] Guard the secured centre, support expansion and use wings or a small ship group to raid exposed hostile income.
- [x] Require a level 4+ station, at least three level 4+ capital ships, a held reserve and capital production, and a strict majority of uncontested capturable zones. The surplus exception also requires clear fleet advantage. Reevaluate each decision.
- [x] Send paid reserves in waves during the assault. Keep ships outside the strike group on centre duty.

### E3 — Unseen fleet

- [x] Add estimated unobserved reinvestment to remembered combat strength. Use known mine claims and last observed effective mining income, including production research. Use the estimate for hunting and assault.

### E4–E7 — Production

- [x] Place one defend platform per safe held zone where space permits, centre and captured relays first. Prefer a type with fewer copies and space purchases by 20 s.
- [x] Look for an affordable useful purchase when the preferred category is blocked. Protect higher-priority spending while production is active. Reject level-1 corvettes at faction level 3+ and launch normal reinforcements behind the station.
- [x] Allow one backup station after the opening for imminent station loss, or from 480 s at twice its price after upgrades and research. Place it at a safe patrol-known point at least 1,400 units from home and its relay. Prefer owned zones.
- [x] Prioritize `IncreasedStationIncome` on unlock at levels 4 and 5. Keep the existing unlock data.

## Verification

- Runtime and Editor assemblies compiled with the Unity Bee response files and standalone Roslyn. Reviewed EnemyAi, Capture, Economy, Ships and the affected Core composition fixture. Tests and playtests were not run.
- 2026-11-19: agent-run EditMode results → "Test results 2026-11-19" in Status above. Playtests still pending.
- User acceptance → [[TODOs/User_Acceptance_Queue#Enemy AI Economy Strategy]].
- Reusable rules → [[Architecture/Enemy_AI_Objectives]].

## Order
1. E1, E3 (economy picture), E2 (strategy posture).
2. E5, E4.
3. E6, E7.

## Files
- `Services/Enemy/EnemyShipTaskGroupPlanner.cs`, `EnemyStrategicContextBuilder.cs`, `EnemySiteObjectives.cs`, `EnemySquadronCaptureAssignments.cs`, `EnemyProductionStrategy.cs`, `EnemySiteConstructionController.cs`
- `Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`, `EnemyProductionDecisionModel.cs`
- `Entities/EnemyFaction/Controllers/EnemyFactionController.cs`, `Services/ShipSpawning/ShipSpawnPoints.cs`
- `Services/CaptureSites/ICaptureSitePresence.cs` (strength API)

## Tests
- Categories: `EnemyAi`, `Capture`, `Economy`, `Ships`. No stat-pinning asserts. Not run by agents (local override).

## Review 2026-11-19 (merged from the Review note)
### Scope
- AI plan: this plan + [[Done/Bugs/Enemy_AI_Economy_Strategy_Regression_Fixes]]; commits `2fa16bea3` (13:44), `8e2ebefe6` (14:30), `28d1a41fd` (14:42).
- Map plan: [[Done/Bugs/Map_Enemy_Station_Near_Allies_Fix]]; uncommitted diff in `StationPlacer.cs`, `StationSlotGeometry.cs`, `MapLayoutGenerator.cs`, `MapStationSlotTests.cs`, `MapLayoutGeneratorTests.cs`.

### Verdict
- Direction is right: flow (zones → centre → build → retake/sabotage → gated assault) matches the user rules; X1–X5 are in source as described.
- Nothing after `8e2ebefe6` is proven: last test result file is 2026-11-19 00:01, last battle 13:52 — both older than the fixes.
- Only played setup: Small, 1v1, UltraHard. Team games and Medium/Large have open defects (R1–R9).
- Map fix: geometry correct for balanced teams; does not touch Small or duels → cannot change the pending 1v1 acceptance battle.

### AI plan vs source
| Item | Source | State |
|---|---|---|
| X1 deployment tiers A–D, 15 s cap, tier E keeps ship queued | `EnemyFactionController.TryFindShipSpawnPosition`, `ShipSpawnPoints.TryGetRearSpawnPosition` | matches |
| X1 partial defense wave at 8 s | `DeployDefenseWave` (`anyPastHoldLimit`) | matches |
| X2 save for chosen ship | `EnemyProductionDecisionModel.Evaluate` returns `None` when unaffordable | matches |
| X2 corvette cap | `MAX_LEVEL1_CORVETTES_AT_LEVEL2 = 2`, critical fallback | matches |
| X3 opening grace, unseen cap, half-visible floor | `EstimateUnseenFleetInvestment`, `Build` (`OPENING_UNSEEN_GRACE_SECONDS 180`, share 0.5) | matches |
| X3 economy pressure once | `fleetRaw` only; not in `IsThreatSafe` | matches (also divided in `UpdateRecovery`, separate purpose) |
| X4 centre strength with squadrons, reinforce not drop | `GetOwnCaptureStrength`, `NeedsCentreReinforcement`, `TryReinforceCentreGroup` | matches; see R2 |
| E1 one detachment per outer mining zone, ETA-ranked | `EnemyShipTaskGroupPlanner.TryAssignExpansionShip` | matches; see R5, R6 |
| E2 assault gate | `Build`: ≥ 3 L4 capitals, `HasAssaultReinforcementPlan`, zone majority or surplus | matches for 1v1; see R1 |
| E4 one platform per held zone, 20 s spacing | `TryGetDefensePosition`, `DEFENSE_BUILD_INTERVAL` | matches |
| E6 backup station | `TryBuySecondStation` (480 s, 2× price), `TryGetSecondStationPosition` (≥ 1400) | matches |
| E7 station income research first | `CanResearchPriorityIncome` | matches |

- Stale note: regression plan says review fixes are "uncommitted" → they are in `28d1a41fd`.
- Uncommitted, unrelated: `EnemyUnitCommanderTests.cs` constructor fix for `UnitOrderService` (2 lines).
- Works as intended for teams: shared focus enemy, combined strike strength, station defense request to the nearest allied main force, team-wide hostile force.

### User decision (2026-11-19)
- R4: income is shared across allies → an allied AI taking mines beside a teammate's base is fine. No exclusion.
- Instead: lower the priority of mining sites near a base. Agent reading: sites near an **allied** player's station rank after other free sites. See R4 fix.

### Findings and fixes — more AIs, allies

#### R1 (P1) Assault gate counts only this AI's zones
- Where: `EnemyStrategicContextBuilder.Build` → `if (zone.Owner == self && !zone.IsContested) heldZones++`.
- Effect: 2v2 / 3v3 → two allied AIs split the zones → neither holds > 50 % → assault only through the money path. Zones of a human ally do not count. 3+ teams → > 50 % of all zones is rarely reachable.
- Fix:
  1. Count zones held by the team: `_playerRoster.IsAllied(self, zone.Owner) && !zone.IsContested`.
  2. Two teams: keep `2 * teamHeld > capturableZones`.
  3. Three or more teams: `teamHeld` > every enemy team's count and ≥ `capturableZones / teamCount` (needs user confirmation; the user rule only defines > 50 %).
  4. Keep `ownedCapturableZoneCount` (telemetry) per AI; add `teamHeldZones` to the `sample` event.
- Tests (`EnemyAi`): zones owned by an ally count toward the gate; contested allied zone does not; enemy-owned majority blocks.

#### R2 (P1) Centre reinforcement has no cap and blocks expansion
- Where: `EnemyShipTaskGroupPlanner.TryCreateCaptureGroup` → `if (TryReinforceCentreGroup()) return;` before `TryAssignExpansionShip`; one ship per decision cycle (UltraHard 0.5 s).
- Effect: centre hostile strength = all enemies there. With 2–4 enemies the AI feeds ships one by one and assigns no expansion ship while the request stands. Only brake: route safety (`RetreatAdvantage`).
- Fix:
  1. Cap the centre group: stop reinforcing at `max(2, ceil(0.5 × ships outside expansion detachments))`.
  2. Do not return after a reinforcement: run `TryAssignExpansionShip` in the same cycle.
  3. If the cap cannot reach `HostileStrength`: no more ships; the centre group holds at the nearest held zone and waits for the main force (existing `blockedApproach` → Hold path) instead of trickling in.
  4. Reinforcements join only when within `FleetCohesion.RADIUS` of the group or after the group regroups, so ships arrive together.
- Tests (`EnemyAi`): centre request + open expansion zone → both get a ship in one cycle; centre group never exceeds the cap; hostile strength above the cap → no further reinforcement.

#### R3 (P2) Unseen estimate uses team income against one AI's fleet
- Where: `GetHostileEconomyPressure` sums the whole enemy team into `_hostileIncome`; `EstimateUnseenFleetInvestment` caps at `0.5 ×` this AI's fleet value.
- Effect: in team games the cap is always hit → `fleetRaw = 1 / (1/visible + 0.5)` after first contact → each allied AI rates itself weaker than in 1v1.
- Fix: divide `_hostileIncome` by the number of living allied players before the estimate (each ally faces its share). `visibleAdvantage` already uses the combined allied strike force for the main group.
- Tests (`EnemyAi`): same hostile income, 1 vs 2 allied players → unseen investment halves; opening grace unchanged.

#### R4 (P2, decided) Mines near an allied base get lower priority
- Where: `GetCaptureScore` (ship groups), `GetRequiredExpansionShips` (expansion ETA), `EnemySiteObjectives.AddSquadronTargets` (wings).
- Fix:
  1. New helper `IsNearAlliedBase(position)`: any `_worldSnapshot.Bases` entry allied to but not owned by this AI within `LOCAL_ENGAGEMENT_RADIUS` (1000).
  2. `GetCaptureScore`: add `ALLY_BASE_MINE_PENALTY` (travel-distance units, start 600 = `SATELLITE_PAD_PRIORITY` scale) for `candidate.IsMine` near an allied base.
  3. `GetRequiredExpansionShips`: add the same penalty as seconds to `arrival` (÷ ship speed) so those zones are assigned last.
  4. Squadron targets: pass the flag so wings prefer other free sites first.
  5. Threatened own sites and blocking-facility raids are not penalised.
- Tests (`EnemyAi`): two equal-distance free mines, one near an allied station → the other is selected first; only the near-ally mine left → it is still taken. Ordering asserts only, no penalty value.
- Open: if "near base" also meant the AI's own home mine, say so — today the home mine is an opening priority (`IsHomeMineOnOpeningRoute`).

#### R5 (P2) Expansion zones are reserved per AI, objectives per team
- Where: `AssignedExpansionZones` is local to one planner; `TeamAiCooperation` reserves objective ids only.
- Effect: two allied AIs send detachments to the same zone, then split its sites by id. One wasted ship trip per zone.
- Fix: publish `expansion:{zoneIndex}` through `TeamAiCooperation.AddReservations` when a zone is assigned; `TryAssignExpansionShip` skips zones `IsReservedByAlly`. Release on group removal in `Reconcile`.
- Tests (`EnemyAi`): ally holds the zone reservation → no second detachment; ally releases → zone becomes assignable.

### Findings and fixes — map size
- Zone counts: Small 2 outer relays, Medium 4, Large 6; archetype maps add `(players − 3) / 2` pairs + profile pairs. All plus the centre. Max players: Small 4, Medium 6, Large 8.

#### R6 (P1) Expansion has no distance or count limit
- Where: `TryAssignExpansionShip`, `IsExpansionObjective`.
- Effect: every capturable relay with mining gets a detachment; main force can be drained to 3 ships. Relays are ≥ 1100 / 1300 / 1500 from every station (`AdditionalZoneStationDistance`) → `BASE_EXCLUSION_MARGIN 900` never excludes a relay. Medium/Large: half the relays are on the enemy side; single ships are sent there.
- Fix:
  1. Eligibility: a zone is ship-expansion eligible when its nearest allied station is not farther than its nearest hostile station, or the team holds the centre uncontested.
  2. Enemy-side zones before the centre is held → squadron raids / sabotage only (existing `RaidStructures`, wings).
  3. Count cap: expansion groups ≤ `ceil(ships / 2)`; keep the existing "main force ≥ 3" guard.
  4. ETA ranking unchanged inside the eligible set.
- Tests (`EnemyAi`): enemy-side zone not assigned before the centre is held, assigned after; group count never exceeds the cap.

#### R7 (P2) Acceptance numbers are Small-only
- "Centre before 120 s", "outer zone by 240 s", "empty zone for 1–2 min is a bug" assume ≈ 2,800 travel. Large duel: stations ≈ 10,400 apart.
- Fix: restate each gate as `travel ETA (station → zone at group speed) + capture time + 60 s`. Add telemetry event `expansionAssigned` (zone, eta, ship) so the `zoneOwner` timeline can be checked against it. Update the queue lines in [[TODOs/User_Acceptance_Queue]] when the first non-Small battle is planned.

#### R8 (P2) Few ships early on big maps
- Corvette cap + saving → 1–3 ships until level 3. Medium/Large need 4–6 detachments + centre; coverage depends on squadrons (`MINIMUM_CAPTURE_WINGS 3`).
- Fix (proposal, balance — needs user OK): `capture wings target = max(3, eligible expansion zones + 1)`; corvette cap unchanged (user rule X2). Wings take pads and free mines; ships follow by ETA.
- Tests: none with numbers (no stat-pinning); assert the target grows with eligible zones.

#### R9 (P2) Cost grows with players × zones
- Where: `GetRequiredExpansionShips` → `HostileReachesFirst` scans all sightings × all `_shipService.Ships`, per zone × per unassigned ship, inside `FinishDecisionCycle`.
- Fix: in `BeginCaptureProbe` build one list of confident hostile ship sightings with their `NavigationSpeed` (one pass over ships); per zone pick the nearest eligible mine once; per ship compute only the ETA. Profile `EnemyAI.Ship.CaptureProbe` on Large 8-player before and after.

#### Unverified — rear deployment on other maps
- Rear rings at 1.5–3× `HomeAreaRadius` need own vision (`ReinforcementSpawnRule.IsOpen`); on ring maps the rear points at the map edge (stations ≥ 546 from the border).
- Check: share of `tier=A` vs `B/C/D` in `deployment` telemetry on a ring-map battle. If A is rare: sample only in-bounds, visible points first, or register vision on the rear ring.
- Fixed constants that stay valid on all sizes: `HomeAreaRadius 270`, rear rings ≤ 810, `LOCAL_ENGAGEMENT_RADIUS 1000`, backup station ≥ 1400.

### Map fix — implementation check
- Ring weights: 2v2 60° / 120°, 3v3 45° / 90°, 4v4 36° / 72°, Orbital 3v3 40° / 80° → match the plan table. Balanced teams keep point symmetry and equal centre distance.
- Duel / FFA: `teamSizes.Count == count` → no weighting; RNG call order unchanged → same output. Small: `groupTeams = false`, swap fallback kept.
- Open Field 2v2: allied corners pulled on the shared edge to pull ≈ 0.65 → Medium ally ≈ 2,840, enemy ≈ 4,370.
- Plan closed during this review (`done`, `acceptance: pending-user`); its diagnostic agrees: ring 2v2 1.73, 3v3 1.85–1.88, 4v4 1.90–1.93, Open Field 2v2 1.52/1.57.
- Known and recorded in the plan (not fixed): Open Field 3v1 0.92–0.96 (pull skipped, it broke mirrored relay pairs); Twin Pass 2v2 1.43, 3v1 0.70, 2v2v2 0.37 → an enemy can still start beside an ally.
- Baseline hashes: recomputed by editor eval of the test's `Fingerprint`. Not a test run.

#### M2 Division by zero for a one-team roster on an open arc
- `WeightTeamGaps`: `boundaryGaps = 0` → `boundaryAngle` = NaN. Reachable only if a one-team roster reaches Orbital Ring with > 2 players.
- Fix: call `WeightTeamGaps` only when `teamSizes.Count > 1 && teamSizes.Count < count` (one team → equal spacing).

#### M4 Uneven rosters lose mirror symmetry on Medium/Large
- Previously equal spacing; attempts 60–119 now re-roll the same layout type. Diagnostic covered 3 seeds. A layout that fails 120 times throws.
- Fix: run the `MapGeneration` sweep with 2v1, 3v1, 2v1v1, 3v2. If any roster fails: for uneven rosters use equal spacing (all-ones `teamSizes`) on attempts ≥ 60.

#### M5 `MIN_PULL` loop can step to 0.25
- Float steps of 0.05. Fix: integer step counter (`for step = 1..14`, `pull = 1 − step × 0.05`). Cosmetic.

### Map fix → AI plan
- No code coupling: the AI reads stations, zones and sites at runtime (`AiWorldSnapshot.Bases`, `IReinforcementZoneSource`, `ICaptureSiteDirectory`).
- Unchanged: Small maps, all duels, FFA → the pending UltraHard acceptance battle is not affected.
- Improves AI assumptions in team games: enemy no longer next door → "centre = front", rear deployment (away from the centre) and `_strategicCenter` (home ↔ enemy base midpoint) point the right way.
- Not improved: Twin Pass team games and Open Field 3v1 → there "centre = front" can still be wrong.
- Changes to expect:
  - Relays and mines move (placed relative to stations) → different neighbour/outer zones per AI.
  - Allied AIs share the same nearest zones more often → R5 matters more.
  - Zones in the widened gap between teams become the contested ones; zones between allies are safe → R6 eligibility (nearest station is allied) fits this layout.
  - Enemy stations cluster → an assault on one station is inside reach of the second (2,500–4,200) → the R1 gate should stay strict.
  - Allied station defense arrives faster (shorter distance).
  - Allied bases closer → more mines fall under the R4 "near allied base" penalty radius.
- Spacing after the fix stays above every AI radius: smallest ally distance ≈ 2,500 (Orbital 3v3) vs 1,000.
- Saved skirmish setups with allies regenerate with new station positions.

### Implementation 2026-11-19 (user: "execute")
- User approved the open proposals by asking to execute: R1 step 3 (3+ teams) and R8 (wing target) built as proposed.

#### Test run before the changes (EditMode, `include_explicit false`)
- `EnemyAi` 493/501 (8 failed), `Ships` 213/213, `Abilities` 222/222, `Capture` 131/131, `Navigation` 380/380, `Core` 165/166.
- All 8 `EnemyAi` failures were stale or broken fixtures, not game defects:
  - `BlockedShip_RetainsPurchase…`, `BlockedShip_ProductionLostDuringRetry…` ×2: `OperationalProductionAccess.TryGetProductionBase` threw `NotImplementedException`; since `2fa16bea3` every deployment asks for the base (rear spawn) → build caught and refunded. Fake now returns false.
  - `MiningRaid_DoesNotOverrideEmergencyOrDecisiveAttack(…0.5…)`: low local advantage without `hasLocalThreat`; stand-and-fight needs a threat since `a945072cc`. Test passes `hasLocalThreat` when local advantage < 1.
  - `OutmatchedOverall_EngagesWeakerHostilesNearFleet`: expected 20 committed; UltraHard `CommittedFleetRatio` 0.85 since `529094e87` (reserve) → ⌈20 × 0.85⌉ = 17. Asserts against the profile ratio now.
  - `CaptureProbe_LoneShipCanDetachForSafeMine`: probe helper passed a null ship service; expansion ETA scans ships. Helper uses an empty `ShipService`.
  - `UnseenEstimate_*` ×2: null combat-profile catalog → asset load. New `VenatorCatalog()` helper.
- `Core` failure `ShipAutoPower_ObservesLiveWeaponsAndShieldRecoveryGates`: CLI bridge error log ("Main thread operation timed out") caused by status polling during the run; passes alone.

#### AI changes
- R1 `CountTeamHeldZones`: team-held uncontested zones; 2 teams → `2 × held > zones`; 3+ teams → more than every enemy team and `held × teams ≥ zones`. `sample` logs `teamHeldZones`. Test `AssaultGate_CountsZonesHeldByTheWholeTeam`.
- R2 `TryReinforceCentreGroup`: cap `max(2, ⌈0.5 × ships outside expansion groups⌉)`; at the cap `CentreGroupAtCap` → a stronger centre is rejected (`blockedApproach` → Hold, waits for the main force); reinforcement no longer returns before expansion (both in one cycle). Step 4 (join only within cohesion radius) not built: the capture executor already rallies stragglers; check in the next team battle.
- R3: `_hostileIncome` ÷ living allied players (allied stations alive); team pressure ratio unchanged.
- R4: `IsLowPriorityAllyMine` (free mine within 1000 of an allied player's station, not threatened, not blocking) → `GetCaptureScore` −600 value, expansion ETA +600 / speed; wings: `EnemySquadronCaptureAssignments.DeprioritizeNear` (commander marks targets near allied stations, radius 1000). Own home mine unaffected. Test `SquadronCapture_SiteBesideAnAlliedBaseIsTakenAfterOtherFreeSites`.
- R5: assigned expansion zones published as `expansion:{zoneIndex}` in the shared reservation set each cycle; `TryAssignExpansionShip` skips zones an ally reserved; released when the group is removed.
- R6 `IsShipExpansionEligible`: nearest allied station ≤ nearest hostile station, or the team holds the centre uncontested; new expansion groups ≤ `⌈ships / 2⌉`.
- R7: telemetry `expansionAssigned` (zone, ship, eta, battleTime). Acceptance-queue gates are restated when the first non-Small battle is planned (unchanged rule).
- R8: capture wing target = `max(3, eligible expansion zones + 1)` via `IEnemyAiStateProvider.EligibleExpansionZoneCount` (default 0 = AIs that do not expand); corvette cap unchanged.
- R9: hostile ship speeds built once per capture probe (`GetHostileShipSpeeds`, reset in `CollectCaptureObjectives`); the ETA race no longer scans all ships per sighting.

#### Map changes
- M2: `WeightTeamGaps` only for `1 < teamSizes.Count < count` (one team → equal spacing).
- M4: uneven rosters (team sizes differ) use equal spacing from attempt 60 (`HasUnevenTeams`, `MapLayoutGenerator.GenerateLayout`).
- M5: pull loop capped by a step count; the pull keeps the accumulated float so existing layouts stay bit-identical (a first version computing `1 − step × 0.05` changed all 6 Medium/Large Open Field fingerprints and was reverted).

#### Not done / still open
- R2 step 4 (see above). Planner tests for R2/R5/R6 and an R3 test not added (planner needs heavy fixtures); verify in the 2v2 Medium battle.
- Map M2/M4/M5 were committed by the map session in `ac7ff075c` together with its own uneven-roster swap fallback (its sweep: 27/96 → 1/96 failures).

#### Test run after the changes (EditMode)
- `EnemyAi` 503/503, `Capture` 131/131, `Map` 125/125 (before another session's in-progress atmosphere edit; a later `Map` run hit its temporary compile error), `Core` `ShipAutoPower_…` passes alone.
- `MapGeneration` (explicit, 3 seeds): Open Field fingerprints pass after the M5 correction; remaining failures were CLI-bridge "Main thread operation timed out" log errors from overlapping status polling, not generation results.
- Not rerun after the changes: `Ships`, `Abilities`, `Navigation` (passed before; no later edits in their code).

### Recommended order
1. User: UltraHard Small 1v1 battle + `EnemyAi`, `Ships`, `Capture`, `Economy` fixtures. Gates everything else.
2. User: `Map` + `MapGeneration` for the map fix; then M2, M4.
3. Implement R1, R2, R6, R4 (one change set, `EnemyAi` tests).
4. One 2v2 Medium battle (AI ally + 2 AI enemies) and one 1v1 Large battle; compare `zoneOwner`, `deployment tier`, `objectiveSwitch`.
5. R3, R5, R7, R8, R9 from that evidence.
