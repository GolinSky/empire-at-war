---
category: Bugs
status: in-progress
created: 2026-11-26
---
# Battle Facing, Attack-Move Groups and Xyston Targeting

## Goal
- #1 Ships stop turning their stern to the enemy during fights (Victory II is the reported case).
- #2 Attack-move groups: no identical detours or pointless turns; positions use ship role and height.
- #3 Ships stop flapping between hold and approach at the main-battery range edge (Xyston is the evidence case).
- #5 Every targeted auto ability obeys the player's Attack target (reported on the Xyston).
- Out of scope (user 2026-11-26): Resurgent-specific tuning; #4 ships going back and idling.

## Status
- 2026-11-26: plan created. Same day: every finding and proposed fix re-checked against live source and the telemetry logs (section **Verification 2026-11-26**). No code changed, no tests run.
- Result: #3 and #1 causes were wrong or incomplete → fixes replaced. #5 fix is right but hits Ion Pulse → new decision D5. Target stickiness has little evidence → deferred. #2 code facts hold; design still needs D1–D3.
- 2026-11-26 user decisions: D1 (rows relative to the group; long-range, artillery, anti-strikecraft never in front), D2 (height levels must share a slot), D4 + D5 (ordered target is the only auto-cast target for all enemy-target abilities; wait when out of range; auto logic after it dies). Open: D3 confirmation, D6. No code changed.
- 2026-11-26 user decisions, round 2: D6 (ability skipped when it cannot hit the ordered target), D7 (one ranking for Superlaser and Ion Pulse), D3 (shared group route in this plan, now), D2 optional, back row = `Artillery` and `AntiStrikecraft` roles. No open decisions left.

## Evidence sources
- User's last battle 2026-11-26 20:55–21:35 was a **release** player build → only `Loading_20261126_205509_ai.csv`. `ShipOrderTelemetry` is `UNITY_EDITOR || DEVELOPMENT_BUILD` only, so no `_ai_orders.csv`.
- Used Editor/dev logs in `%USERPROFILE%/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/`: `Loading_20261124_*`, `Loading_20261125_*`, `Loading_20261126_{154309,173404,174814,180931}_ai_orders.csv`.
- Mount dump: read-only Unity eval of `ResurgentShipView`, `VictoryIIShipView`, `XystonShipView` prefabs + `WeaponsData.asset` (2026-11-26).
- Next captures: build the player as **Development** so `_ai_orders.csv` is written.

### How to read the log (traps found during verification)
- `facingDot` is measured against `attack target ?? tracked contact ?? nearest radar contact` (`ShipOrderTelemetry.Record`). With `targetId = 0` it says nothing about a fight: a ship flying to its point with an enemy behind it reads −1.
- `movement` column = `Phase;queued;inQueue;braking;settling;failed;destX;destZ;speed`. `destX/destZ` is the **planned** destination (`Model.Destination`), not the requested one.
- `attackLeg` / `attackHold` rows are events, `sample` rows are every 5 s. Count time with `sample` rows only.
- Break per-ship sequences at `issued` rows; otherwise a new order counts as a target change.

## Verification 2026-11-26

| # | Plan claim | Verdict |
|---|---|---|
| 1 | Attack orders stern-free since `85614d1dc` | True for Victory II (0 / 152). All ships still settle stern-first under Attack: 37 samples in the logs after the commit. |
| 1 | Attack-Move stern = settle hops + target churn | Partly. 324 of 391 stern samples have **no target** (285 en route by design, 39 returning). With a target: 67, of which 42 are settle hops. |
| 1 | Settle hop turns away because the job checks current forward | Wrong cause. The job's goal is always sideways. The **planner moves the goal**: see #1. |
| 1/2 | Target churn ~30% | 19% when sequences break at `issued`. 649 of 821 drops of a ship target follow its death. Live ship dropped: 172 / 8,330 pairs (2.1%); in reach when dropped: 37 (0.4%). |
| 3 | Xyston closes in 75 times inside engagement range | 71 of the 75 are `attackLeg` event rows. Time-weighted: 4 of 251 samples (1.6%). The defect is hold ↔ leg flapping. |
| 3 | Leg re-issue comes from `PursuitDestinationUpdateDistance` / `_wasMoving` | Wrong. `MainBatteryReaches` has no held margin → hold and approach alternate. |
| 5 | Priority ranking beats the ordered target | Confirmed (`AutoPowerCombatSnapshot.FindPriorityTarget`). The shared fix also changes Ion Pulse, where station-first is deliberate and tested. |
| 5 | D4 "wait" matches the non-priority path | Wrong. That path waits only for `IssuesAttackOrder` abilities (Assault, Concentrate Fire, Lucky Shot). |
| 2 | Slot spacing, nearest-first assignment, own routes, no roles | Confirmed in code. No route data in telemetry → "same curve" not measurable yet. |

## #1 Ships show their stern during a fight

### Findings
- Stationary facing cannot pick a stern heading: `ShipMoveComponent.LookAtTarget` replaces any solved facing with `dot < 0` by the target bearing. Stern samples with a target are **while moving**.
- Victory II after `85614d1dc`: Attack moving 0 / 152 stern; Attack-Move moving 7 / 283; Idle 14 / 446.
- Attack-Move, moving, `facingDot < −0.5`, 4 logs of 2026-11-26: 391 samples.
  - No engagement target: 324 (83%). En route before any target this order: 285 → by design (`AttackMoveState` flies to the point and ignores fights outside its area). Returning after a lost target: 39.
  - With a target: 67. Settle hop (`settling=1`): 42. Other: 25.
- Attack order, moving, stern, logs after the commit: 229 / 8,333. Settle 37; `approach` 169; `stallRetry` 16; `artilleryApproach` 7. The `approach` cases checked (56 in the 4 logs) are travel legs to a formation slot: all farther than the standoff point, 52 beyond engagement range → not a fight.
- **Settle hops fly away from the target.** 213 mid-settle samples (166 Attack-Move, 47 Attack); 48 stern.
  - Hop direction vs target bearing: median 115° (Attack-Move), 127° (Attack); stern cases 113°–179°.
  - Goal ends farther from the target: 374 → 610 (Mediator), 476 → 692 and 634 → 911 (ISD I), Xyston hop 592 units.
  - `ShipSpacingJob.SlideGoal` cannot produce this: its goal is perpendicular to the target line and the step is at most the pair's contact distance.
- Cause (code confirmed; link to the samples is likely, not proven by a test):
  - `TrySettle` → `Plan(destination)` → `ShipNavigationService.TryPlanNearCandidates`. An occupied goal moves to a ring slot up to 2 rings × `2 × NavigationRadius` away (4 radii), first slot on the ship's side facing outward.
  - `KeepsHoldTargetInReach` checks only the **requested** goal. `Plan` accepts `plan.Destination` unchecked.
  - In a packed firing line the sideways spot is taken → the ship flies to a free slot behind it.

### Solution (revised)
1. `ShipMoveComponent.Plan`: while `_isSettling`, refuse a plan whose `plan.Destination` is not the requested goal (tolerance: a fraction of `NavigationRadius`). Reuse the existing refuse branch (`ARRIVAL_CORRECTION_MAX_ROUTE_FACTOR` loop case): `_isSettling = false`, `_failedDestination`, stop.
2. Telemetry: one `settle` row with requested goal, planned destination and angle to the hold target.
- Dropped: "judge the hop by target bearing in `ShipSpacingJob`". The goal is always 90° off the target line; at hop end the target is always slightly behind abeam, so the test would refuse every slide or none.
- Dropped: hop cap at `NavigationRadius`. The job's step is already bounded; the long hops come from the planner.
- Tests (Navigation): `ShipNavigationServiceTests` next to `CombatHold_SpacingNeverSettlesOutOfGunReachOfTheTarget`: settle goal occupied by another reservation → `TrySettle` false, ship stays. Existing `ShipSpacingJobTests` stay valid.

### Target stickiness (deferred)
- Evidence is thin: a live ship target in reach was dropped in 37 of 8,330 sample pairs. Limit: 5 s sampling hides changes between samples.
- Before any change: log the drop reason in `AttackMoveState` (`!Contains`, `!_isLeashed`, not preferred) as an event row, then decide.

## #2 Attack-move groups: same curve, odd turns, no roles, no height

### Findings (code confirmed 2026-11-26)
- `UnitOrderService.IssueAttackMove(receivers, point)` → `Compact` → `FormationModel.CalculateCompactDestinations`: one clump around the click. Slot spacing = `max(2 × largest radius, …)`. Assignment: ship nearest the point first, each takes its nearest free slot.
- Each ship plans its own route (`AttackMoveState.Enter` → `MoveToPosition`). Speed is already shared through `CreateAttackMoveClusters` (pace `Formation`).
- No roles: on contact every ship `Pursue`s to `0.65 × own engagement range` on its own bearing (`ShipEngagement.GetStandoffPosition`, `STANDOFF_RANGE_FACTOR`). `AttackMoveState` has no artillery approach; only `AttackTargetState` has one.
- Height: `ShipSpacingJob` skips hull pairs whose `NavigationHullSpan`s do not overlap, but slots are planar only.
- Two issuers bypass `Compact`: `IssueAttackMove(receivers, destinations)` (`EnemyTaskForceExecutor` defence points) and `EnemyUnitCommander` (calls `CalculateCompactDestinations` itself). Role rows placed in `FormationModel` reach the second; the first keeps its own points.
- Not measurable yet: routes are not logged, so "same curve" has no numbers.

### Decisions (user 2026-11-26)
- **D1 rows are relative to the group, not fixed per class.**
  - Sort the group's line ships by hull class (`ShipClass.HullTier()`). The strongest class present forms the front row; the next classes fill the rows behind.
  - Example: `HeavyCruiser` is front when nothing bigger is in the group, middle when a `Capital` or bigger is present. Same for every class.
  - Never in the front row, whatever the class: long-range ships (Victory I as the example), artillery (`ShipData.UsesArtilleryStandoff`), anti-strikecraft ships. They go to the back row.
- **D2 height:** ships on different height levels **may** share one map slot when their hull spans do not overlap. Good to have, not mandatory; drop it if it complicates row packing.
- **D3 shared route:** build it now, in this plan (user 2026-11-26).

### Solution
1. **Row assignment** (`FormationModel`, pure data in: class tier, radius, back-row flag, height span):
   - Back row: back-row flag set.
   - Remaining ships: distinct tiers present, highest first → front, middle, …; at most 3 rows, lower tiers merge into the last line row.
   - Rows ordered along the group heading (group centre → point). Row gap = largest radius of the two rows; empty rows collapse.
   - One ship or one tier only → current compact clump.
2. **Back row = ship role** (`ShipData.Roles`, serialized `UnitRole`, available at runtime):
   - `UnitRole.Artillery` (set from `usesArtilleryStandoff`): Victory I, Victory, MC30c, Lupus, Peleset, Recalcitrant. They stop when missiles reach; the other weapons do not matter.
   - `UnitRole.AntiStrikecraft` (lasers / point defence are the largest weapon share): Tartan, Raider, Corellian Corvette, CD273, CR70, Carrack, Centax, Patrol Frigate, C9979, Captor, DH Omni, Diamond, Lupus, **Xyston**.
   - No "long range" threshold: artillery is the long-range role. Line ships stop when their main batteries reach, which already places them by range.
   - Check before coding: Xyston carries `AntiStrikecraft` (laser share) → it would go to the back row. Exclude BattleCruiser and bigger from the role rule, or fix the role in its data asset.
3. **Role standoff on contact**: front 0.65 × engagement (current), middle 0.75, back 0.9; never beyond full reach.
4. **Height-aware packing**: within a row, ships whose `NavigationHullSpan`s do not overlap take the same planar slot; each layer spaces by its own largest radius.
5. **Shared route** (D3, user 2026-11-26: now, in this plan): see below.
6. **Route logging**: one `route` row per attack-move ship at plan time (group id, slot row, first turn side, detour point, route length). Added with the new logic; gives the before/after numbers for "same curve".
- Tests: `FormationModelTests` (Navigation): strongest tier present is in front; heavy cruiser front without capitals, behind them with; back-row ships never in front; non-overlapping spans share a slot; overlapping spans do not. `UnitOrderServiceTests` (BattleInput): slots per role. `ShipPursuitTests`: back row stands off farther than front (relationships, not tuned numbers).

### Shared group route (D3)
- Today no group order shares a route: each ship plans its own path to its slot; only speed is shared (`PaceCluster`).
- Design:
  1. Plan one route for the group centre with the largest hull radius in the group.
  2. Each ship flies that route shifted by its slot offset.
  3. Check every shifted route for clearance with the ship's own radius; a blocked one falls back to the ship's own plan (current behaviour).
  4. The shared route ends at contact: once the ship engages, `AttackMoveState` moves it as today.
  5. A ship that replans alone (blocked destination, braking) leaves the shared route; no group replan.
- Known cost: one route sized for the largest hull skips gaps small ships could use. Accepted for group cohesion; the fallback covers blocked cases.
- Read first: `ShipRoutePlanner.Build`, `ShipNavigationService.Plan`, `IssueWaypointMove` (already shifts waypoints by slot offset).
- Tests (Navigation): shifted routes keep slot offsets on an open map; a shifted route through an obstacle falls back; a group of one plans as today.

## #3 Hold and approach alternate at the main-battery range edge

### Findings
- Rule (code confirmed): `WeaponComponent.MainBatteryReaches` returns false while any main battery that bears bow-on, and could reach at hull contact, is out of range. Missiles, ions and secondaries do not count.
- Used by `AttackTargetState.IsInReach`, `IsInHeldReach` and `ShipEngagement.CanHoldOn`, always with `reachFactor 1f` → the hull-distance test has the 1.1 held margin, the main-battery test has none.
- Xyston mounts: 6 × `DualHeavyTurboLaser` mains (585), all aft (local z −44…−64); 5 × `HeavyDualMissileLauncher` (715), 6 × `HeavyDualIonCannon` (650).
- Xyston Attack samples, all logs: 131 hold (median distance 640, engagement 667), 116 pursuing beyond engagement range, 4 pursuing inside it. → It closes about 30–70 units inside its engagement range, then holds. Small cost.
- **Flapping.** `20261124_173558` ship 978, target 895: `hold:inReach` → `leg:approach` six times in 5.5 s, 0.1–9 units moved per cycle, same destination (926,−1111). Xyston rows: 101 `attackHold`, 109 `attackLeg`.
- All ships: 861 of 27,260 legs re-issued to the same destination within 2.5 s (705 `approach`, 99 `stallRetry`).
- Each flip: `HoldPosition` → `Stop()`, next tick a new plan. Attack-Move has the same test (`CanHoldOn`), so the same flapping.
- Resurgent has the same mount pattern (16 mains, 8 octuples aft); not fixed separately (user).

### Solution (revised)
1. Keep the entry rule: a ship starts holding only when every bow-on main reaches (user rule from `a0bc9955d`).
2. New held rule: a ship already holding keeps holding while the mains that reach carry ≥ 50% of the bow-on main weight (`_facingWeights`). Gives a margin without silent mains.
3. Apply in `AttackTargetState.IsInHeldReach` and `ShipEngagement.CanHoldOn(isHolding: true)`.
- Dropped: "≥ 50% weight to start holding". It changes how far every ship closes and reverses `a0bc9955d`; the Xyston cost of the current entry rule is 1.6% of samples.
- Dropped: "keep no held margin". That rule is the flapping cause.
- Dropped: search in `PursuitDestinationUpdateDistance` / `_wasMoving`.
- Tests: `WeaponCandidateTests` (Combat): held test true with half the bow-on main weight in reach, false below; `MainBatteryReaches_NeedsEveryBearingMainInRange_AndSkipsMainsThatCannotHelp` stays valid. `ShipPursuitTests` (Navigation): a holding ship does not start a leg when one main drops out. Update fakes if the interface changes: `ShipPursuitTests`, `ShipStopTests`, `SquadronTargetSelectorTests`.

## #5 Xyston auto-cast ignores the Attack target

### Findings
- Xyston abilities: `XystonSuperlaser` (58, range 1000, auto-power role) and `TractorBeam` (22).
- `SuperlaserSettings.GetTargetPriority`: Capital 4, BattleCruiser 3, Dreadnought 2, station/structure 1, others 0.
- `AutoPowerCombatSnapshot.FindPriorityTarget`: highest priority in range wins; the Attack-order target only breaks ties. Order Attack on a station or cruiser → the superlaser fires at any capital within 1000.
- `IonPulseSettings` uses the same path. Its station-first rule is deliberate (`1459cfce7`) and tested: `IonPulseTargetPriorityTests.StationInRange_BeatsDreadnoughtAndAttackTarget`, `WithoutStation_BiggestClassWins`.
- `ShipAbilityService` (authority cast) skips its own order override for priority abilities and casts on the target `FindTarget` returns → one fix place is enough. Its comment "a station before the attack target" needs an update.
- Non-priority path: ordered target first when targetable and in range; it waits only when `definition.IssuesAttackOrder`. Superlaser does not issue orders.

### Decision (user 2026-11-26, D4 + D5)
- One rule for **every auto ability that needs an enemy target** (`RequiresEnemyTarget`), priority abilities included (Superlaser, Ion Pulse):
  - Ship has an Attack order → the ordered target is the only auto-cast target.
  - Ordered target out of ability range → the ability **waits**. No other target.
  - Ordered target destroyed → the order ends → the ability's own auto logic applies again (priority ranking, nearest, …).
- Avoid: ranking by class, station-first or nearest while an Attack order is live.
- Reverses `1459cfce7` for the ordered case: Ion Pulse fires at the ordered ship even with a station in range. Station-first stays for ships without an Attack order.

### Rule table
| Situation | Auto-cast target |
|---|---|
| Attack order, target valid for the ability, in range | Ordered target |
| Attack order, target valid, out of range | None (wait) |
| Attack order, target the ability can never hit (class rule, e.g. Tractor Beam vs station, Superlaser vs corvette) | None: the ability is skipped for this order (D6, user 2026-11-26) |
| Attack order, target cloaked or no longer visible | Order completes (`AttackTargetState.IsComplete`) → auto logic |
| Target destroyed, order complete | Auto logic |
| Hardpoint attack order | Same as Attack: the ordered ship |
| Attack-Move, Move, Guard, Idle, Hunt | Auto logic (no ordered target) |
| Manual cast by the player | Unchanged: the clicked target |
| Ability without an enemy target (shields, power) | Unchanged |

### Solution
1. `AutoPowerCombatSnapshot.FindTarget`: one ordered-target block before both paths (priority and nearest):
   - `CurrentOrder == Attack`, ordered target live → return it when `CanTarget` and `ShipAbilityRange.IsInRange`, else `null`.
   - No fall-through to auto logic while the order is live.
2. `FindPriorityTarget`: remove the ordered tie-break (`distance = −1`); it is unreachable with a valid ordered target and unwanted otherwise.
3. `ShipAbilityService` authority cast (~L185): same rule; drop the `IShipAbilityTargetPriority` exclusion and the `IssuesAttackOrder` special case (now true for all).
4. `ShipAbilityDefinition.IssuesAttackOrder`: check remaining users; delete if only these two.
- Tests (Abilities):
  - `IonPulseTargetPriorityTests`: `StationInRange_BeatsDreadnoughtAndAttackTarget` and `WithoutStation_BiggestClassWins` change (contract changed): ordered target wins; add the same cases without an order to keep the ranking covered.
  - New: ordered target out of range → `null` although a higher-priority target is in range (Superlaser and a non-priority ability).
  - New: ordered target the ability cannot hit → `null` although a valid target is in range.
  - `ShipAbilityServiceTests` L77 already expects `ordered` or `null`; review for the non-`IssuesAttackOrder` case.

### Why Superlaser and "capitals" appear here
- Issue #5 was reported on the Xyston. Its targeted auto ability is `XystonSuperlaser`; that is the only reason the Superlaser is named.
- "Capitals" come from the auto ranking in code: without your order, a ranked ability picks its target by ship class. Today that ranking also overrides your Attack order → the bug.
- The fix is not Superlaser-specific: one rule for every targeted auto ability (Ion Pulse, Laser Beam, Composite Beam, Proton Beam, Ion Shot, Tractor Beam, Assault, Concentrate Fire, Lucky Shot, Superlaser).

### Facts checked 2026-11-26
- Attack orders have no class validation: `PlayerOrderInputHandler` checks only "is an enemy". A Xyston can be ordered onto a corvette; it attacks with its guns.
- Abilities have their own class rule (`CanTarget`): Superlaser hits frigates and larger, stations, structures; never corvettes or strikecraft.
- → With D6, a Xyston on a corvette Attack order keeps its Superlaser silent until the corvette dies or the order changes.

### Auto ranking (user 2026-11-26, D7)
- Superlaser and Ion Pulse use one order: **ordered target > station > Dreadnought > BattleCruiser > Capital > other ships**.
- "Ordered target" follows the rule table above: out of range → wait; it does not fall through to the station.
- Ion Pulse already ranks this way in code. Superlaser ranks Capital > BattleCruiser > Dreadnought > station/structure > other (since `b6ac97dc5`) → change `SuperlaserSettings.GetTargetPriority`; update `SuperlaserTargetRuleTests` (contract changed).
- Assumption: a `Structure` keeps the station's rank for the Superlaser (as today). Ion Pulse has no structure rank.
- Station-first stays for both when the ship has no Attack order.

### Attack order vs auto ability (user 2026-11-26)
- The Attack order drives the guns; the auto ability has its own target rule (`CanTarget`). The two never mix.
- Xyston ordered onto a corvette: guns attack; the Superlaser stays silent (it cannot hit corvettes) and picks nothing else while the order is live.
- Manual cast is validated: `ShipAbilityService` rejects a clicked target that fails `CanTarget` (`TargetRejected`).
- Auto cast is validated twice: `FindTarget` and the authority check before `Activate`.
- Test: Attack order on a corvette, valid capital in range → Superlaser auto-cast returns no target.

### Check after the change
- AI ships follow the same rule (the rule reads the order, not who gave it). Expected fine (user). Scan: ability casts per AI ship-minute in `_ai.csv` before and after; a clear drop → report, do not patch silently.

## Plan
1. [x] #3 held rule for the main-battery test (+ tests). Largest measured defect, all ships.
2. [x] #1 refuse a settle whose planned destination moved; `settle` telemetry row (+ test).
3. [x] #5 ordered target is the only auto-cast target for every enemy-target ability; wait when out of range; skip when the ability cannot hit it; Superlaser ranking = Ion Pulse ranking (+ tests).
4. [x] #2 relative role rows, back row by role, role standoff (+ tests). Height-shared slots (D2, optional) dropped.
5. [ ] #2 shared group route + `route` telemetry row (+ tests). **Blocked on design**, see Implementation 2026-11-26.
6. [x] Attack-Move drop-reason telemetry (stickiness stays deferred).
7. [ ] User battle in a Development build; rerun the scans below.

## Implementation 2026-11-26
- #3: `IWeaponComponent.MainBatteryReaches(..., bool isHolding)`. Holding → reaching bearing mains need ≥ 50% of bearing `_facingWeights`. Entry rule unchanged. Used by `AttackTargetState.IsInHeldReach` and `ShipEngagement.CanHoldOn(isHolding)`.
- #1: `ShipMoveComponent.Plan` refuses a settle whose `plan.Destination` is > `0.25 × NavigationRadius` from the requested goal (`SETTLE_GOAL_TOLERANCE_FACTOR`), via the existing loop-refuse branch. Telemetry: `settle` row (`settle:planned|refused requested=… planned=… hopToTarget=…`) through `IShipMovementDiagnostics.TakeSettleEvent`.
- #5: `AutoPowerCombatSnapshot.FindTarget` → live Attack-order target is the only candidate (valid + in range, else `null`), before priority and nearest paths. Ordered tie-break removed. `ShipAbilityService.Execute` (automatic) uses the order target for every ability. `ShipAbilityDefinition.IssuesAttackOrder` deleted (no other users). `SuperlaserSettings.GetTargetPriority`: station/structure 4 > Dreadnought 3 > BattleCruiser 2 > Capital 1 > other 0.
- #2 rows: `FormationModel.CalculateRowDestinations` (front = largest non-back hull tier present, middle = smaller tiers, back = `IAttackMoveFacade.IsBackRow`; max 6 ships per line; line gap = larger radius; ships keep their side). One row → compact clump. Only `UnitOrderService.IssueAttackMove(receivers, point)` uses it.
- Back row: `ShipOrderFacade.IsBackRow` = `Artillery | AntiStrikecraft` role and hull tier < BattleCruiser → Xyston and bigger never back row. `IShipData` gained `ShipClass`, `Roles`.
- Role standoff: `FormationRow` → `ShipEngagement.GetStandoffFactor` front 0.65, middle 0.75, back 0.9; passed `AttackMoveTo(..., row)` → `AttackMoveState` → `Pursue`. Row not saved: a loaded attack-move uses Front.
- Drop telemetry: `attackMoveDrop` row (`drop:notInEngagement|leash|notPreferred target= destroyed= canEngage= dist=`).
- #2 shared route not built. Why: waypoint orders stop at each waypoint (`NavigateState` → `AdvanceWaypoint`), so shifted waypoints would add stops and turns. A smooth route needs `ShipMoveComponent`/`ShipRoutePlanner.Build` to accept preset waypoints (reservations, deferral, replica routes) and cannot be verified without a battle. Needs a user decision: build that planner change, or drop D3.
- Tests (2026-11-26, EditMode, explicit off): Combat 344/344, Abilities 268/268, BattleInput 487/487, Navigation 422/422, Squadrons 236/236, Ships 490/490 passed. New: `MainBatteryReaches_WhileHolding_NeedsHalfTheBearingMainWeightInRange`, `Attack_HoldingShipDoesNotStartALegWhenOneMainDropsOutOfRange`, `CombatHold_SettleGoalMovedByThePlannerIsRefusedAndTheShipStays`, Ion Pulse/Superlaser ordered-target cases, `AttackMove_GroupGetsRowsByRole_FrontRowNearestThePoint`, 4 `RowDestinations_*`, `AttackMoveRows_RearRowsStandOffFartherButWithinReach`. Changed contract: `IonPulseTargetPriorityTests` station/biggest-class cases now run without an order; `SuperlaserTargetRuleTests` ranking.
- No reusable guidance extracted yet: rules wait for the battle check (step 7).

## Battle 2026-11-27 (`Loading_20261127_103730_ai_orders.csv`, Editor/dev)
- Fixes held: settle hop angle median 90° (was 115–127°); 183 / 222 settles refused. Stern while moving in a fight: 23 / 492 samples.
- **Overshoot** (user: big front ships go far past the point). Block depth 1,300–2,800; lone front ship slot +978 (Secutor), +1,424 (Praetor) past the click. Causes: block centred on the click; line gap `r1 + r2 + max(r1, r2)`; lines spaced by the row's largest radius (Rothana 142, Praetor 317); a lone largest ship as the whole front row; middle-row lines filled by position (t=1041: Arquitens +532 ahead of Imperator +215).
- **Side ships turn again** (user: Victory). Re-issued attack-move every 7–14 s rebuilt the rows; ships near their slot got one behind them: course change 133–165° (t=1146, 3 of 3), 158°/164° (t=1314), 132° (t=1376). Not visible directly in 5 s samples.
- Also seen: 81 / 468 drops (17%) were `leash` drops of targets still in reach (out of reach from the slot). Not changed.
- Fix 2026-11-27:
  - `FormationModel.CalculateRowDestinations`: front line on the point, lines behind at `r1 + r2`; each line spaced by its own largest radius; a lone largest-tier ship joins the next tier in front; lines filled larger tier first.
  - `UnitOrderService.IssueAttackMove(receivers, point)`: same owner, same ships, new point ≤ `ATTACK_MOVE_SLOT_REUSE_DISTANCE` (300) from the last → keep each ship's slot offset and row.
  - `AttackMoveState.Enter`: a stopped ship already within `NavigationRadius` of its slot does not re-plan.
  - Tests: `FormationModelTests` 19/19, `UnitOrderServiceTests` 39/39, Navigation 424/425 before the test fix (test measured Z, not heading depth; fixed), BattleInput 488/489 likewise, Ships 490/490. New: `RowDestinations_FrontLineStandsOnThePointAndRowsCloseUpBehindIt`, `RowDestinations_ALoneLargestShipJoinsTheNextTierInFront`, `RowDestinations_LargerHullsTakeTheRowsFirstLine`, `AttackMove_RepeatedNearby_KeepsEachShipsSlotOffsetAndRow`, `AttackMove_FarFromTheLastOrder_RebuildsTheRows`.
  - Gate: user battle check of both behaviours.

## Battle 2026-11-27 #2 (`Loading_20261127_135651_ai_orders.csv`, loaded save, Editor, 1,249 s)
- Build includes `504d831f3` (attack-move = fly to slot and hold, spread fire; role standoff and `attackMoveDrop` removed).
- Overshoot fixed: no ship passed its slot in any Player0 attack-move group (max reached ≤ slot depth in every group).
- Side-ship turns fixed: 0 / 37 re-issues changed course > 90° (45–78°: 3). No restart from a stopped slot with the same destination.
- Holding: 362 stopped attack-move samples, all with a facing target; 247 face it at dot ≥ 0.95. 6 runs ≥ 10 s face away (dot < 0.5): Imperator 1453 at t=767 for ~20 s (target 660 away, out of reach).
- Minor: 129 stopped samples 100–250 from their slot (no restart); 44 Idle samples with an enemy in weapon range (start of battle, after an Attack/Attack-move completed: Praetor 1329 ~40 s at t=322, Victory 1668 ~45 s at t=1166). `idleUnderFire` 2 only → guns still fired.
- Gate for both behaviours: met by telemetry; user confirmation pending. Step 5 still blocked.
- Minor-issue review (2026-11-27):
  - Facing away on the slot: not a defect. `LookAtTarget` turns to `IWeaponFacing.GetFiringTurnAngle` (most gun weight bearing, often broadside); telemetry `facingDot` is the nose only.
  - Idle with an enemy in range: not a defect for `IdleState` (turns to and marks the best threat in reach; `trackedContactId` set in every case). Held-replacement Attack ending → Idle is being changed by another session (`ShipOrderRunner` fight watch, uncommitted).
  - **Stop short of the slot: defect.** `AttackMoveState.Tick` set `_hasArrived` on the first `!IsMoving`, then `HoldPosition` stopped the ship for good. Seen: VenatorII 1584 held 1,292 from its slot (t=1072), Venator 1529 731 (t=1098), fresh Rothana 1585 894 (planner fallback 70 from spawn, t=775). Stops 112–284 away were planner relocations of an occupied slot (≤ 2 rings × 2 radii).
  - Fix: arrival = stopped within `ARRIVAL_RADIUS_FACTOR` (5) × `NavigationRadius` of the slot; farther → `MoveToPosition` retry every `MOVE_RETRY_INTERVAL` (2 s), turning to the facing target without a hold; order does not complete short of the slot.
  - Tests: `AttackMove_ShipStoppedFarShortOfItsSlotTriesAgainInsteadOfHolding`, `AttackMove_StopBesideAnOccupiedSlotIsTheArrival`; `RealEntityCompositionTests.SurvivalAttackers_ProfileRepeatedCompletedOrders` now stops the ship on its slot (`StopOnSlot`) before expecting completion. Results: ShipPursuitTests 91/93 (2 failures in the other session's uncommitted `ShipOrderRunner` work), Ships 491/491, RealEntityCompositionTests 38/38.

## Open decisions
- None. Decided: D1, D2 (optional), D3, D4, D5, D6, D7.

## Assumptions and unverified points
- #1 cause (planner moves the settle goal): code confirmed; link to the log samples is likely, not proven by a test. The new `settle` row proves or disproves it in the next battle.
- #2 "same curve": no numbers until the `route` row exists.
- Xyston has the `AntiStrikecraft` role in its data → decide exclusion rule or data fix when coding the back row.
- Superlaser: `Structure` keeps the station's rank (assumption).
- AI ability use under the new rule: expected fine, checked by scan.
- Target stickiness (keep the current target longer): deferred. In the logs a ship dropped a living target it could still shoot in 37 of 8,330 checks (0.4%); most target changes follow the target's death. Too rare to justify a rule change now; the drop-reason log shows whether it matters.
- Targeted-ability list read from `ShipAbilityCatalog.asset` by name; verify per entry when coding.

## Verification scans (repeat after fixes)
- Stern in a fight: `kind=sample`, `moving=1`, `targetId != 0`, `facingDot < −0.5`, split by `settling` (5th field of `movement`). Baseline 4 logs: Attack-Move 67 (42 settle).
- Settle geometry: for `settling=1`, angle between (`destX,destZ` − position) and the target bearing. Baseline median 115° / 127°; target after fix ≈ 90°.
- Flapping: `attackLeg` rows with the same destination (≤ 5 units) within 2.5 s of the ship's previous leg. Baseline 861 / 27,260.
- Churn: share of consecutive samples per ship with a different `targetId`, sequences broken at `issued`. Baseline 19%; live ship dropped 2.1%.
- Xyston: `sample` rows with `pursuing=1` and `dist <= engage`. Baseline 4 / 251.
- Routes: share of ships in one attack-move group with the same first turn side and detour point (`route` rows).
- AI abilities: casts per AI ship-minute in `_ai.csv`, before vs after #5.

## Test categories
- Navigation, Combat, Abilities, BattleInput. Run only touched fixtures (local override).

## Files
- `Assets/Scripts/Components/Ship/Movement/ShipMoveComponent.cs` (`TrySettle`, `Plan`, `KeepsHoldTargetInReach`)
- `Assets/Scripts/Services/ShipNavigation/ShipNavigationService.cs` (`TryPlanNearCandidates`), `ShipSpacingJob.cs`, `ShipSpacingCoordinator.cs`
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs` (`MainBatteryReaches`), `IWeaponComponent.cs`
- `Assets/Scripts/Entities/Ship/StateMachine/AttackTargetState.cs`, `AttackMoveState.cs`, `ShipEngagement.cs`
- `Assets/Scripts/Services/ShipAbilities/AutoPowerCombatSnapshot.cs`, `ShipAbilityService.cs`, `IShipAbilityTargetPriority.cs`, `Abilities/Superlaser/SuperlaserSettings.cs`, `Abilities/IonPulse/IonPulseSettings.cs`
- `Assets/Scripts/Services/UnitOrders/UnitOrderService.cs`, `Assets/Scripts/Components/Movement/Formation/FormationModel.cs`
- `Assets/Scripts/Services/Enemy/ShipOrderTelemetry.cs`

## Related
- [[Review/Bugs/Attack_Order_Hold_In_Range]] (attack ring, hold band)
- [[Done/Bugs/Capital_Movement_And_Class_Rework_Fixes]]
- [[Done/Features/Artillery_Ship_Standoff_Plan]]
