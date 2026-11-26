---
category: Bugs
status: in-progress
created: 2026-11-25
follows: "[[Done/Bugs/Enemy_AI_Opening_And_Task_Groups]]"
---
# Enemy AI Station Push and Ally Assault

## Goal
- An AI with the advantage attacks and destroys hostile stations.
- An allied AI joins the human's attack on a hostile station.
- Opening–mid game behaviour stays (user rule).

## User report (2026-11-25)
- AI ally does not help the player attack or push the enemy.
- Enemy AI never pushes a station, even with fleet advantage and level 5.

## User rules (2026-11-25)
- **Stations must die in every mode.** Destroying a station cuts the economy. An AI without a station spawns no ships. For a human the loss is less strict but still heavy.
- **Assault is needed everywhere.** An assault includes clearing the hostiles around the station.
- **Centre:** important at the start and in mid game. In the end game the whole fleet assaults the hostile bases; that outranks the centre.
- **On the way:** the assault fleet fights hostiles it meets on the route to the base.

## Evidence — 2v2 2026-11-24 23:15 (host, release build, 3,490 s)
- Files: `LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/Loading_20261124_231516_ai.csv`, `Player.log` (1,174 `[EnemyAI]` lines).
- Setup: Player0 human Empire + Player2 AI Empire vs Player1 / Player3 AI Rebellion. UltraHard. `Objective=DestroyEnemyFleet`.
- `AssaultBase` rows: **0 of 1,174**. Last 14 battles: 3 rows in 1 battle (20:46).
- Victory condition in all 77 recorded maps: `DestroyEnemyFleet`.
- Unit cap from ≈ 1,200 s: Player1 118/120, Player2 120/120, Player3 120/120 (`production-wait reason=capacity`).
- Bank at the end: Player2 1,207,713; Player1 737,324; Player3 peak 215,263; human 330,694.

| Player | Time | State | Ships | Fleet advantage | Fact |
|---|---|---|---|---|---|
| Player2 (ally) | 1,900–3,000 s | `RaidStructures` / `CaptureZone` | 19 | 12–64 | main force 1–2 ships; 17–19 in group `zone:0` on "Guard the centre"; `idleShips` 7–16 |
| Player1 | 1,682–2,898 s | `Hold` "Answering an allied station under pressure" | 15–17 | 3.0–5.7 | `idleShips` 11–17; 1 order per 60 s; `assaultRaw` up to 100 |
| Player3 | 1,679–2,972 s | `DefendBase` | 4–16 | 0.7–3.3 | under the human's siege; `canAssault` never true |

- `canAssault=True` samples: Player1 17 of 219, Player2 35 of 221, Player3 0 of 207. State in those samples: `RaidStructures`, `Hold`, `HuntFleet`. Never `AssaultBase`.

## Causes (source `43b829e86`)
### C1 Assault is switched off in `DestroyEnemyFleet` (main cause)
- `EnemyStrategicDecisionModel._rules`: `AssaultBase` 0.76 needs `VictoryCondition != DestroyEnemyFleet`; 0.75 and 0.5 need `== DestroyOpponentBase`.
- Left: `AssaultBase` 0.2 → `EnemyShipCount == 0 && !IsCentreGuardGoal`. A living station keeps building ships → never true.
- `BattleVictoryModel.IsDefeated`: `DestroyEnemyFleet` → base dead **and** `UnitCount == 0`. The base must die in both modes.
- Guard added in `2fa16bea3` (economy strategy). Tests in `EnemyStrategicDecisionModelTests` encode it (`[TestCase(DestroyEnemyFleet, HuntFleet)]`).

### C2 Hunt never goes to a station
- `canHunt` needs `!IsNearHostileBase(target)` (`BASE_EXCLUSION_MARGIN` 900).
- Hostile fleet at its station → no hunt, no assault → the war stops at the zone line.
- `HuntFleet` 0.75 + `HYSTERESIS` 0.05 = 0.80 > `AssaultBase` 0.76 → a hunting fleet would not switch to an assault.

### C3 The strike force has no ships
- Only the reserve (`IsMainForce`) can assault; `canAssault` needs `_assaultCapitals.Count >= 3` **in the reserve**.
- `EnemyZoneAllocator.LeanZones` = `LateGameSituation` = team station pressed or armada. A winning AI is never "late game" → zone groups keep the fleet.
- Player2: reserve `Committed=2/19`, `capitals` 2 ↔ 6 between samples, 17 ships guard the centre with no hostile near (`localAdvantage` 100).

### C4 No centre → no assault, at any advantage
- `canAssault` needs `centre == null || securedCentre`; else `IsCentreGuardGoal` blocks the 0.75 / 0.5 rules below 2 × the attack ratio.
- Player1: `decisiveSurplus=True`, fleet advantage 3–5, unit cap, 400,000+ banked → `canAssault=False` (centre held by Team0).

### C5 Ally defence answer parks the fleet
- `SelectAllyDefenseRequest` → `Defend` marker at the station point → `ExecuteMarker` → one `IssueAttackMove` to that point; later cycles send nothing while the point is unchanged.
- The request holds its attackers (`DefenseRequest.Attackers`) but the answer never targets them.
- No end condition except the request going stale (10 s after the pressure ends) → 1,216 s parked.
- Likely, not verified: the attackers stood outside the parked ships' engagement range. The compact log has no order rows.

### C6 An allied AI cannot see a human attack
- `TeamAiCooperation.PublishStrike` is called only by AI main forces; `AddAlliedStrikeStrength` counts only those.
- A human push is invisible unless the human places an Attack marker (`AllyAiMarkerSource.SelectRequest`).
- `FocusEnemy` is picked from the AI's own home (`FindFocusEnemy(home)`), not from where the team fights.

### C7 Log cannot name the blocking condition
- `AssaultGate` lacks: victory condition, `securedCentre`, `stationPressure`, `hasHostBase`, reserve ship count, `canAssemble`, `outnumbered`.
- Release log has no order rows → C5 cannot be proven from a player build.

## Fix steps
### P0
- [x] **S1 Assault in every mode (C1; user rule).** `AssaultBase` 0.76: drop the `!= DestroyEnemyFleet` term. `AssaultBase` 0.75 and 0.5: drop the `== DestroyOpponentBase` term. No victory-condition term is left in any assault rule. `canAssault` stays the gate.
- [x] **S1a Assault clears the station's surroundings (user rule).** Target order inside `BASE_EXCLUSION_MARGIN` of the hostile station: ships attacking the assault force → other hostile ships → armed platforms → the station. The station is attacked while no mobile hostile is known there. Check `EnemyTaskForceExecutor` assault targeting first; change only what differs.
- [x] **S1b Fight on the way (user rule).** The assault body attack-moves to the base and engages hostiles on its route (existing `IsEngagingEnRoute` / W66 path); it resumes the assault when the fight ends. No retreat from a route fight the body wins.
- [x] **S2 Assault outranks hunt at a station (C2).** In `DestroyEnemyFleet`: hostile ships away from a station → `HuntFleet`; no hunt target (`CanHuntFleet` false) → `AssaultBase`. Raise the assault scores above `HuntFleet` + `HYSTERESIS` only when `CanHuntFleet` is false; no new constants.
- [x] **S3 Fleet gathers to strike (C3).** New allocator input `PressSituation`: whole-fleet rating at the hostile station ≥ the attack ratio **and** unit capacity full or `HasAssaultSurplus`. Effect = `LeanZones`: a zone keeps only the force its hostiles need; the rest returns to the reserve. Count `_assaultCapitals` over the whole fleet for the gate; the strike itself needs 3 in the reserve (unchanged in `CanAssemble`).
- [x] **S4 End game: the whole fleet assaults, centre or not (C4; user rule).**
  - Start and mid game: unchanged. `canAssault` keeps `centre == null || securedCentre`.
  - End game: the centre and zone-majority terms are skipped; `isCentreGuardGoal` is false; committed ships = every ship (zone groups return to the reserve, S3).
  - **End game = situation, never the clock** (earlier user rule). Confirmed (D3, user 2026-11-25): station at the top level **and** (unit capacity full **or** `HasAssaultSurplus`). Fleet strength still decides *whether* to go (`assaultRaw` ≥ the attack ratio).
  - Hostile bases are assaulted one after another: nearest first, next one when it falls.
- [x] **S5 Ally defence answer fights (C5).** Answer = attack the request's attackers nearest the station (like `DefendBase` "intercept the visible threat"); attack-move to the station only while none is known. Idle answer (no hostile in engagement range for 30 s, same constant as the idle gate in W45) → release the answer for one `ZONE_STRIKE_SECONDS`-style cooldown so the normal decision (S1 assault = relief) runs.
- [x] **S6 Allied AI joins a human push (C6).**
  - Detect: allied human ships fighting inside `BASE_EXCLUSION_MARGIN` of a hostile station, or the station taking damage from the team (`EnemyStationPressureTracker` mirrored for hostile stations).
  - Effect on the AI main force: `FocusEnemy` = that station's owner; the human ships there count like `AddAlliedStrikeStrength`; `canAssault` skips the centre and zone-majority terms; state `AssaultBase`, reason "Joining an allied assault".
  - Stops under own station pressure, last stand or turtle (existing rules win).
  - Existing Attack / Defend / Retreat markers keep priority.

### P1
- [x] **S7 Telemetry (C7).** `AssaultGate` adds `objective`, `securedCentre`, `stationPressure`, `hostBase`, `reserveShips`, `fleetCapitals`, `canAssemble`, `outnumbered`, `press`, `allyAssault`. New compact row `allyDefense`: responder, requester, attackers known, nearest attacker distance, idle seconds.
- [ ] **S8 `outnumbered` check (`EnemyShipTaskGroup.Evaluate`).** Compares the focus **team's** known ships with the reserve's ship count → blocks offense for a small reserve. Compare strength (`assaultRaw`) or whole-fleet count. Verify against the S7 row first.
- [x] **S9 Tests (relations only; run on the user's request).** `EnemyStrategicDecisionModelTests`: assault chosen in `DestroyEnemyFleet` with `CanAssaultBase`; hunt kept while `CanHuntFleet`; update the `[TestCase(DestroyEnemyFleet, HuntFleet)]` cases whose contract changes. `EnemyUnitCommanderTests` / allocator tests: `PressSituation` returns zone ships to the reserve; decisive surplus assaults without the centre; defence answer orders an attack on a known attacker and releases when idle; human ships at a hostile station switch the allied AI to `AssaultBase`. Category `EnemyAi`.

### P2 (observations, no step yet)
- AI banks 0.4–1.2 M at the unit cap. S1–S4 turn the fleet into pressure; re-check the bank after the next battle.
- Player2 `zone:3` hit `stall=noFlip` after 120 s more than once → covered by [[TODOs/Bugs/Enemy_AI_Audit_Fixes]].
- 57 non-urgent state switches under 8 s (Player2 24, Player3 19, Player1 14) → W60 gate "≤ 1 per 8 s" fails; mostly `CaptureZone` ↔ `GuardSite` on site and zone groups.

## Implementation (2026-11-25, on `43b829e86`)
- Status: S1–S7 done, S9 tests written; Unity compile 0 errors. Tests not run (local override). S8 open.
- Defaults taken: **D1** automatic join (no marker needed); **D2** allocator `LeanZones` unchanged (a zone keeps the smallest winning force; its last ship stays unless own wings hold the zone).
- **S1** `EnemyStrategicDecisionModel`: no victory-condition term in any assault rule. 0.76 rule = `(HasSecuredCentre || IsEndGame || JoinsAllyAssault) && HasEnemyBaseTarget`.
- **S2** After scoring: best `AssaultBase` + a scoring `HuntFleet` rule + `DestroyEnemyFleet` → hunt wins. `CanHuntFleet` false skips hunt rules → assault. No new constant.
- **S1a / S1b** `EnemyStrategicContext.AssaultTarget` (`FindAssaultTarget`): local threat ship near the fleet → known hostile ship within `BASE_EXCLUSION_MARGIN` 900 of the station (nearest fleet) → visible hostile `DefendPlatform` there → station. Executor attacks it with `keepTargets: true`. Route fights reuse W66 (`IsEngagingEnRoute`) plus the local-threat pick.
- **S3** `AssessStation` → `FleetAssaultCapitals` (whole fleet) and `PressSituation` = (`IsAtUnitCap` || `HasAssaultSurplus`) && whole-fleet `RateReachAt` at the focus station × trade correction ≥ `RequiredAttackRatio`. Planner: `LeanZones = LateGameSituation || PressSituation`. Gate counts `max(group, fleet)` capitals; `CanAssemble` unchanged.
- **S4** `EndGameSituation` = (`IsAtUnitCap` || `HasAssaultSurplus`) && `EnemyFactionModel.IsAtTopLevel`. End game: centre and zone-majority terms skipped, `isCentreGuardGoal` false, committed = every ship, executor `eligibleShips` = all; planner with press + end game → every zone force back to the reserve.
- `EnemyFactionModel.IsAtUnitCap` set by `EnemyProductionStrategy` (= `shipBlockedByCapacity`); `IsAtTopLevel` = `GetCurrentLevelFactionData() == null`.
- **S5** `SelectAllyDefenseRequest(out attacker)`: nearest known visible hostile ship within `LOCAL_ENGAGEMENT_RADIUS` of the allied station → `ExecuteMarker` issues `Attack` for a Defend marker with a target; a new target re-orders. No attacker and no local threat for `ALLY_DEFENSE_IDLE_SECONDS` 30 → released for `ALLY_DEFENSE_RELEASE_SECONDS` (= `ZONE_STRIKE_SECONDS` 90).
- **S6** `FindAllyAssaultStation`: hostile station with ≥ `ALLY_ASSAULT_MIN_SHIPS` 3 allied human ships within 900 → focus enemy + base target = that station, `Cooperation.FocusEnemy` set, gate skips centre, zone majority, capitals and reinforcement plan (still `assaultRaw` ≥ 1, no own station pressure). Reason "Joining an allied assault."; executor sends every ship; `CanAssemble` skips the capital check. Human ships at the station count through the W63 reach rating.
- **S7** `AssaultGate` adds `fleetCapitals`, `objective`, `securedCentre`, `stationPressure`, `hostBase`, `reserveShips`, `press`, `endGame`, `allyAssault`; group appends `canAssemble`, `outnumbered`. New event row `allyDefense` (responder, requester, attacker id, `attackersKnown`, `nearestAttacker`, `idleSeconds`, `released`), one row per change.
- **S9** Tests (category `EnemyAi`, not run): `EnemyStrategicDecisionModelTests` — `FleetObjective_AssaultsTheStationWhenNoHuntTargetIsLeft` (2 cases), `EndGame_WholeFleetAssaultsWithoutTheCentre`, `MidGame_SameFleetKeepsExpanding`, `AllyAssault_JoinsTheAlliedAssault`; `EnemyUnitCommanderTests` — `AssaultBase_EndGameSendsEveryShip`, `AllyDefenseAnswer_AttacksTheKnownAttacker`. Existing `[TestCase(DestroyEnemyFleet, HuntFleet)]` contracts kept (hunt still wins while a hunt target exists). Not covered: `PressSituation` allocator path, ally-assault detection, idle release (need a full commander fixture).
- Reusable guidance: none extracted yet — rules wait for the battle gate.

## Evidence — 2v2 2026-11-25 20:43 + 21:20 (release 0.1.84, contains `84e655e23`)
- Files: `Loading_20261125_204342_ai.csv` (rows start at 606 s: first 10 min missing), `Loading_20261125_212043_ai.csv` (2,845 s, no outcome line), `Player.log`.
- S1–S6 work: 21:20 enemies `AssaultBase` P1 39 / P3 21 samples; 20:43 ally P2 24 samples, 12 with "Joining an allied assault".
- **L1 Ally leaves after the first station (S2 too wide).** 20:43: P1 main station dies ≈ 1,545 s; P2 then `HuntFleet` / `EngageLocal` / `Hold` intercept 1,560–1,816 s while P1's second station dies ≈ 1,800 s. `allyAssault=True` in 28 samples, 16 not `AssaultBase` (Hunt 6, EngageLocal 7, Hold 3); `endGame=True`. Cause: `EnemyStrategicDecisionModel.cs:353` swaps any best `AssaultBase` for `HuntFleet` in `DestroyEnemyFleet`, with no `IsEndGame` / `JoinsAllyAssault` exemption.
- **L2 Centre mines never built (21:20).** P2 owned centre mines `site:0` 53→1,073 s, `site:2` 194→1,033 s, `site:3` 206→1,057 s: 0 mines started. `FindMineSite` skips centre mines until 3 own operational satellites stand in the centre; P2 built 0 there.
- **L3 Held pads left empty.** P2 held centre pads 4, 8–11 for 824–1,049 s with 54k–134k banked; 3 satellites built 250–1,024 s, all on lone lane pads. No `siteBuild` row → the selector picked no centre pad (likely `HasHostiles`, not verified: the centre was a constant fight). Retakes: P1/P3 with 300k+ fill a retaken 5-pad centre one pad at a time (`HasSatelliteUnderConstruction`, 243 `oneWaiting` rows); 22 retaken pads lost again still empty.
- **L4 Opening misses home mines.** Wing groups work only the centre (group A) and relay zones (outer groups); a lone home mining point gets no wing. Ship captures are reserved team-wide without home preference: P1's single ship took P3's home mine `site:27` at 200 s (staged 124–200 s); P3 never took it; P1's own home mine fell at 131 s. P1 started with 1 of 4 home pads, P3 with 0 (south homes 4 of 4).
- **L5 Crash loop.** 248 × `NullReferenceException` at ≈ 815 s (P3): `EnemyRecoveryAnchor` keeps the `UpdateRecovery` safety lambda; `EnemyStructurePlacementService` calls it later from `EnemySquadronCommander.AssignScout` → `IsObjectiveSafe` reads a destroyed ship in `_ownShips` → the wing capture pass aborts each tick.
- Noise: `counter-selection` 6,491 of 12,380 rows (52 %).
- Telemetry gap: no row when the site builder picks nothing with empty owned pads.

## Fixes L1–L5 (2026-11-26, uncommitted; Unity compile 0 errors; tests not run)
- [x] **L1** `EnemyStrategicDecisionModel` S2 swap skipped when `IsEndGame` or `JoinsAllyAssault`. Armada intercept already stands down while the plan is `AssaultBase`/`HuntFleet`, so no change there.
- [x] **L2** `EnemySiteConstructionController.IsCentreReadyForMines`: centre mines go ahead once 3 standing **team** satellites are in the centre, or no own empty centre pad can take a satellite now (none owned, all built, or hostiles on them).
- [x] **L3** `StartSatellite`: a second satellite may start while one builds when its price fits the spare money (`Money − ProductionReserve`). New `siteBuild` row `reason=noChoice;emptyPads;hostilePads;mandatory;spare`, one per change → confirms the unverified `HasHostiles` cause next battle.
- [x] **L4** `EnemyStrategicContextBuilder.IsBesideAlliedAiBase`: ship capture (pool zones + group selection, filter `allyHome`) skips facility sites within `EnemySquadronCommander.ALLIED_BASE_SITE_RADIUS` 1,000 of an allied AI's station. Wings unchanged: lone home mining points still get no wing group.
- [x] **L5** `EnemyRecoveryAnchor`: route safety rated only inside `Update` (anchor + allied zones cached); `TryGetBuildAnchor` / `CopySafeAlliedZoneBounds` no longer call the fleet lambda between updates.
- Tests (category `EnemyAi`, not run): new `EnemyStrategicDecisionModelTests.EndGameOrAllyAssault_KeepsAssaultingWhileAHuntTargetExists` (2 cases); `EnemyRecoveryAnchorTests.RouteSafety_IsRatedOnlyDuringUpdate_PlacementBetweenUpdatesReusesIt`, `UnsafeRouteAtUpdate_KeepsTheAnchorAndZoneOutOfPlacement`; `EnemyDefenseStrategyTests.Construction_SpareMoneyStartsASecondSatelliteWhileOneIsUnderConstruction`. Changed: `Construction_StartsNoSatelliteWhileAnotherIsUnderConstruction` now reserves all money (contract: no spare → wait).
- Reviewed, no change: `FleetObjective_AssaultsTheStationWhenNoHuntTargetIsLeft` (no end game / ally flag), `EndGame_WholeFleetAssaultsWithoutTheCentre`, `AllyAssault_JoinsTheAlliedAssault`, other `EnemyRecoveryAnchorTests`, `EnemyUnitCommanderTests` recovery case (2546–2553), construction-order tests. No test builds an allied AI station near a capture site.
- Not covered by tests: L2 centre gate (private, needs site + presence fixture), L4 filter (needs a world snapshot with an allied station).
- Battle gate (next 2v2 host log): ally `AssaultBase` until the last hostile station falls; no owned centre mine unbuilt > 120 s; `noChoice` rows explain empty held pads; each AI's opening ship takes its own home mine; 0 `NullReferenceException`.

## Decisions needed (user)
- **D1 Ally join trigger (S6).** Default: automatic when your ships fight at a hostile station. Alternative: only on your Attack marker.
- **D2 Zone cost of a strike (S3).** Default: zones keep the force their known hostiles need; an empty zone keeps none. Alternative: every held zone keeps ≥ 2 ships (W60 garrison rule) during a strike.
- **D3 Centre (S4) — decided 2026-11-25:** centre first in the start and mid game; in the end game the whole fleet assaults. End game = top station level **and** (unit cap full **or** `HasAssaultSurplus`) — confirmed by the user 2026-11-25.
- **D2 note:** "whole fleet" (user) → in the end game zones keep no ships; D2 now applies only to mid-game strikes.

## Carried over from [[Done/Bugs/Enemy_AI_Opening_And_Task_Groups]] (not implemented there)
- [ ] **W8 / W29 Ship role labels → unit choice by role.** Blocked: user review of the role flags; re-run `Propose Ship Roles` first.
- [ ] **W58 Route fallback.** 160 × `Tangent route build failed (code 1)`; needs a `ShipTangentFailureSnapshot` from a development build.
- [ ] **W44 Test gaps** (list in the old plan, Round 9 P2).
- User gates W14, W45, W60 → [[TODOs/User_Acceptance_Queue]].

## Acceptance (next 2v2 host log, UltraHard)
- `AssaultBase` rows > 0 for an AI at fleet advantage ≥ 2 and the unit cap; first row ≤ 120 s after both hold.
- A hostile station loses hull to AI ships.
- Allied AI: `AssaultBase` "Joining an allied assault" ≤ 60 s after the human engages a hostile station.
- No `Hold` "Answering an allied station" with `idleShips` ≥ half the group for > 60 s.
- Opening unchanged: first 5 min state mix as in 23:15.

## Files
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs` — rules (S1, S2).
- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs` — `canAssault`, `canHunt`, `SelectAllyDefenseRequest`, `AssaultGate` (S2–S7).
- `Assets/Scripts/Services/Enemy/EnemyShipTaskGroupPlanner.cs`, `Assets/Scripts/Entities/EnemyFaction/Models/Objectives/EnemyZoneAllocator.cs` — `PressSituation` (S3).
- `Assets/Scripts/Services/Enemy/EnemyShipTaskGroup.cs` — request order, `outnumbered` (S5, S8).
- `Assets/Scripts/Services/Enemy/TeamAiCooperation.cs`, `EnemyStationPressureTracker.cs` — human strike picture (S6).
- `Assets/Scripts/Services/Enemy/EnemyTaskForceExecutor.cs` — `ExecuteMarker` (S5).
- `Assets/Scripts/Services/Enemy/EnemyAiTelemetry.cs` — rows (S7).
- Tests: `Assets/Scripts/Tests/Editor/EnemyAi/`.
