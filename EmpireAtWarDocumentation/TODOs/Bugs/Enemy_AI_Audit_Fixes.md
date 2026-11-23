---
category: Bugs
status: in-progress
created: 2026-11-23
follows: "[[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups]]"
---
# Enemy AI Audit Fixes

## Goal
- Fix the faults of the read-only audit `Enemy AI Audit.md` (repo root, 2026-11-23) that still exist after Round 10.
- No tuning. Every step removes a rule collision, a dead path or a wrong input.
- Opening–mid game behaviour stays (user rule, [[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups]]).

## Status (2026-11-23)
- **Phases 1–3 implemented** (A1–A5, A7, R1; P1–P7, I4; X1–X6, X8, X9). Compile clean (`unity recompile`, 0 errors). Tests written and reviewed, **not run** (local override).
- Next: user battle gate (Order step 2), then Phases 4–5; Phase 6 any time.
- P6 reserve question decided (user 2026-11-23, options C + D): see the P6 item.
- Implementation notes (deviations / details):
  - A2: blocked set passed as `EnemyZoneAllocator.BlockedZoneIds` (property, like `StrongEconomy`), not an `Allocate` parameter.
  - A4: "held" structures count inside the zone radius (`AddOwnArmedStructures(..., out inside)`); `OwnStructures`/`OwnSupport` still use 2 × radius.
  - P3: `HasMandatoryPad(snapshot, isPadWaiting)` skips pads in the churn wait.
  - P5: `firstPlatformsOnly` = `hasShipOption ? !shipBlockedByCapacity : canOrderShips && HasFittingShipType()`.
  - P7: second station skipped while miners are missing (`MiningFacilityCount < target && HasMiningOption`), not only on a `Mining` category.
  - X2 defence: a ship with no clear defence point holds at the gather point when it lost its point/target or is idle; released defenders and idle ships outside the detachment hold there too (Stop only without a gather point).
  - X2 fallback point: zone force with a capture objective gets `guardPosition` = its zone's staging point, else home; `hasGuardSite` in the snapshot is unchanged (decision unaffected).
  - X4: `ForgetShip(ship, keepGathering)`; Rally keeps its own rally anchor and hold point (no re-issued guard orders).
  - X5: `AiOrderDeduplicator.RetainOnly(receivers)` called each `Execute`; an attack passes when the ship now attacks another known target (no 10 s wait).
  - X6: offset attacks go through `Filter` (same target + slot within 50 = repeat; ended attack on the same slot waits 10 s).
  - X9: `EnemyTaskForceExecutor.IsAssemblingCapture` → `ReportCaptureExecution(null)`; the objective is still reserved for allies.
- Audit was written before commit `fc774bb4c` (Round 10). All 68 findings re-checked in source on `fc774bb4c` (no uncommitted AI changes).
- Result: **51 confirmed, 15 narrowed or changed, 1 fixed (A6), 1 never true (C2).**
- User's three items: all confirmed → A1, A2, A3 (Phase 1).
- User answers 2026-11-23: Q1, Q5, Q5a and Q6 decided. SQ1 rewritten: escort preferred but never a blocker; raid and bomber purchases sized to the mine's hull and damage reduction. Q2–Q4, Q7–Q12 still on their defaults.
- Wing findings renamed **SQ1–SQ8** here (audit "W1–W8" collides with the vault's W-items).
- Line numbers below = `fc774bb4c`. `CB` = `EnemyStrategicContextBuilder.cs`, `EPS` = `EnemyProductionStrategy.cs`, `EXE` = `EnemyTaskForceExecutor.cs`, `SQC` = `EnemySquadronCommander.cs`, `DM` = `EnemyStrategicDecisionModel.cs`.

## Decisions needed (user) — defaults are applied unless changed
- **Q1 (P1) Station about to fall, best defender unaffordable.** **Decided (user 2026-11-23):** buy the best defender it can pay for now. Avoid: keep saving (today).
- **Q2 (A1) When is a zone force "stalled"?** Default: only while it is below its attack line; the 120 s "no site taken" rule stays. Avoid: keep 1.25 and trim late-game forces to 1.25.
- **Q3 (A4) One own satellite in a hostile-owned zone.** Default: zone is not "held" (no top priority, no below-even defence); the satellite still counts as support.
- **Q4 (R1) Who defends a zone.** Default: hostiles within 500 of the zone edge **or** within 500 of any site of the zone. Avoid: flat radius + 1,000 (counts fights outside the zone).
- **Q5 (SQ1) Bombers raid a mine.** **Decided (user 2026-11-23):** escort is a preference, never a blocker. Fighters go along to protect bombers from hostile squadrons when the group has them; with no fighter the bombers still raid. The AI needs *more* bombers when the enemy has the structure hull and damage-reduction researches. → SQ1.
  - Reading of the user's wording ("good rule but mandatory to follow … must be a blocker"): taken as "not mandatory … must not be a blocker", consistent with the first answer. Confirm if wrong.
- **Q5a Enemy research level.** **Decided (user 2026-11-23):** the AI reads the enemy's research level directly.
- **Q6 (SQ3) Outer wing group with no site it can win.** **Decided (user 2026-11-23):** flies home / to the nearest held zone. Avoid: parks on a contested site (today, "never idle"). Changes 1 test.
- **Q7 (I4) No affordable defender.** Default: "critical" uses the quickest unlocked defender's arrival; 120 s only with no ship type at all.
- **Q8 (P7) Second station during a station emergency.** Default: never bought while critical, and after miners / a due level.
- **Q9 (D4) Safety gates on UltraHard.** Default: new `SafetyAdvantage` = the old retreat values (Easy 0.8, Medium 0.7, Hard 0.7, UltraHard 0.8); `RetreatAdvantage` 0.5 only decides the local retreat.
- **Q10 (wing cadence).** Default: no change (3 s on every level). Option: follow the ship interval, floor 1.5 s (wing pass costs up to 6× on UltraHard).
- **Q11 (P11) Builds per ship type.** Default: keep "1 in production + 1 waiting", fix the comment.
- **Q12 (D2) Retreat exit ratio × 1.25.** Default: none (retreat is latched until the gather point); correct the vault wording.

## Phase 1 — Zones keep their force (P0)
- [x] **A5 Garrison released after 120 s (worse than the audit).** `EnemyShipTaskGroupPlanner.IsStalledByResult` (`:443-449`) runs for every zone force not under attack. A cleared zone takes no site → "stalled" 120 s after the last site flip → ships freed. `EnemyZoneAllocator` sends no new ship to a cleared zone (`:171`) → the W47 garrison is never restored.
  - Fix: `NoteStalledSieges` skips the no-flip rule when `EnemyZoneAllocator.IsCleared(zone)` or `zone.OpenSites == 0`.
- [x] **A1 Stall clock vs attack line.** Clock restarts only at advantage ≥ `STALL_CLEAR_ADVANTAGE` 1.25 (`Planner:421-422`). `Trim` in `LeanZones` cuts to the smallest Go force (`Allocator:231-253`); Go = expected gain > 0, often < 1.25 → marked stalled after 90 s.
  - Fix (Q2): clock restarts when the force's verdict is Go; delete `STALL_CLEAR_ADVANTAGE`.
- [x] **A2 Mark does not keep ships away.** Mark → `_current = RESERVE` (`Planner:160-162`) + weak-target skip (`:477`). The zone stays in `_order` (`Allocator:163`); `TryFill` takes the nearest free ships = the same ships. Caution (`EnemyZoneCaution`) only records a failure when ships leave, never for a stall.
  - Fix: planner passes a per-zone "blocked" flag to `EnemyZoneAllocator.Allocate`; a blocked zone is skipped in `_order`, `AssignLeftovers` and `WeakTarget`. Exempt: `IsHeldUnderAttack`.
- [x] **A3 One retreat slot.** `_retreatedZoneId` / `_retreatedUntil` (`Planner:68-69`; written `:377`, `:412`, `:434`).
  - Fix: `Dictionary<string, float>` zone → blocked-until; expired entries pruned. Same store feeds A2.
- [x] **A4 "Held" too easy (Q3).** `held = ownStructures > 0 || team owns relay` with structures out to 2 × radius (`EnemyZoneObjectiveCollector:169, 172`); `IsHeldUnderAttack = IsHeld && any defender` (`EnemyZoneObjective:30`) → tier −1, `DEFEND_FLOOR` 0.6, exempt from trim and stall.
  - Fix: a relay owned by a hostile → not held; structures count for "held" only inside the zone radius. `OwnSupport` unchanged.
- [x] **R1 Zone defenders miss edge sites (Q4).** Units: radius + `ZONE_DEFENDER_MARGIN` 500 (`Collector:411`). Sites join out to 2 × radius (`Planner.IsInZone:627-628`); statics 2 × radius + 500 (`:416`); wings 1,000 from the point (`SQC:1304-1310`).
  - Fix: `IncludeDefender` also accepts a hostile within 500 of a candidate site of the area. Correct the "+ 1000" wording in [[Architecture/Enemy_AI_Objectives]]. Done: that note already said radius + 500; updated with the site rule, held rule, stall and block rules.
- [x] **A7** `EnemyZoneEvaluator.Select` gets `strongEconomy` (`CB:1935` calls it without; allocator passes it at `:160`).
- Fixed, no work: **A6** (`IsCleared` = owned ∧ no armed hostile, `Allocator:85-88`).

## Phase 2 — Production never freezes (P0)
- [x] **P2 Due level blocks the minimum fleet.** `Evaluate`: `IsLevelDue` (`EnemyProductionDecisionModel:157`) before `NeedsMinimumFleet` (`:160`). `ShipOrdersCount` never drops on ship death → from level 2 every non-critical pass returns Level or None until the upgrade is queued.
  - Fix: in `IsLevelDue` the orders term becomes `!NeedsMinimumFleet(ShipCount) && ShipsOrdered >= …`. Level-1 branch unchanged.
- [x] **P1 Critical station buys nothing (Q1).** `EPS:235` calls `TrySelectShip` without `requireAffordable`; pick unaffordable → `canBuildShip` false → `Evaluate` returns None (`:154-155`). `GetArrivalSeconds` has no time-to-afford term.
  - Fix: `requireAffordable: true` at `:235`; nothing payable → counter path (`AddCounterOption:746` already filters by money and capacity).
- [x] **P3 Pending satellite freezes ship money.** `PriorityConstructionPending = mandatorySatellite || mineSite >= 0` (`EnemySiteConstructionController:88-89`) ignores `HasSatelliteUnderConstruction()` and `IsPadWaiting()`. Blocks ships (`EPS:433`), platforms while a build runs (`:394`), the super weapon.
  - Fix: `mineSite >= 0 || mandatorySatellite && !HasSatelliteUnderConstruction()`; `HasMandatoryPad` skips pads in the 45 s wait.
- [x] **P4 One-ship-at-a-time throttle.** `EPS:394-398` returns for Ship / Defense while any build runs and an income tier is unaffordable; no minimum-fleet exemption.
  - Fix: add `&& !NeedsMinimumFleet(shipCount)`; comment the rule.
- [x] **I4 Critical threshold inverted (Q7).** `EnemyStationPressureTracker:40-42`: no affordable defender → 120 s; 400 s arrival → 800 s.
  - Fix: `EPS.GetQuickestDefenderArrival` falls back to the quickest unlocked defender ignoring money.
- [x] **P7 Second station in an emergency (Q8).** `EPS:348` before `Evaluate`; only guard `!critical && PriorityConstructionPending` (`:515-517`).
  - Fix: early return on `critical`; call moved below `Evaluate`, skipped for `Mining` / `Level` / due level.
- [x] **P5 Platforms outbid the fleet (W27).** `firstPlatformsOnly = hasShipOption && !shipBlockedByCapacity` (`EPS:281`) → false when no ship is affordable → second ring / centre platform allowed.
  - Fix: true when a ship type fits free capacity but none is affordable.
- [x] **P6 "Saving" at the unit cap.** `savingForShip` true for an affordable ship blocked only by capacity (`EPS:298`) → research capped at 25 % (`:329-336`), `DefenseReserve` held (`:274`).
  - Fix: research cap only when `!shipBlockedByCapacity`.
  - `DefenseReserve` (user 2026-11-23, C + D): it only shields money from optional satellites (scored, fill, repair); mandatory satellites, platforms, research and levels ignore it.
    - Choice fits the free capacity → whole price (unchanged).
    - Choice does not fit (unit cap) and the fleet is quiet → 0: money arms pads.
    - Does not fit and fighting → price of the ship one loss makes room for: the choice when it fits once the largest own ship type is lost, else the dearest orderable ship that would fit then; never above the choice.
    - Fighting = own or team station pressure, main force in EngageLocal / DefendBase / HuntFleet / AssaultBase / RaidStructures / RetreatValue, or an own ship lost within `RECENT_SHIP_LOSS_SECONDS` 60 s.
    - Code: `EnemyProductionStrategy.GetShipReserve`, `SelectShipReserve`, `FitsAfterOneLoss`, `GetReplacementPrice`. "At the cap" = choice does not fit (not only "affordable but does not fit"), so a quiet AI does not flip between 0 and the full price.
    - Tests (not run): `EnemyProductionDecisionModelTests.ShipReserve_AtTheCapOnlyWhileFighting_AndNeverAboveTheChoice`, `FitsAfterOneLoss_FreeCapacityPlusTheLargestOwnShip`. Not covered: the strategy-level fight signal and the replacement price.
    - Battle check: `production-wait reason=capacity` rows vs empty owned pads; a freed slot refilled within one production pass during a fight.

## Phase 3 — No ship without an order (P0)
- [x] **X1 + X8 Support ships in a targeted attack.** `EXE.IssueAttackOrHunt:863-872`: without a secured centre, support = ships in `eligibleShips` only → in AssaultBase every non-capital gets no order. Reachable only on a map with no centre zone (`canAssault` needs `centre == null || securedCentre`, `CB:1421`). Hunt / Raid support Guards the anchor into the fight.
  - Fix: drop the `eligibleShips` filter; support holds at `GuardPosition` (`HoldAtPoint`) when there is one, else `GuardSupport`.
- [x] **X2 Remaining Stop / no-order paths** (Hold now attack-moves to the gather point; these are left):
  - Winning EngageLocal sends ships beyond `count` away to the gather point (`EXE:877 → 1118`); `SplitBody` takes list order, not nearest → fix: EngageLocal commits every ship (`EXE:283`).
  - `ExecuteDefense` Stop with no clear defence point (`:405-410`) and `ReleaseUncommittedDefenders` Stop (`:431-447`) → `HoldAtPoint` at the gather point.
  - Steady Hold / Rebuild: `EnemyShipTaskGroup.Evaluate` skips `Execute` (`:111-133`) → a ship that joins the group gets nothing → also execute when `Ships.Count` changed.
  - Stop with no gather point (zone force with a capture objective, no staging point): Hold, Retreat without anchor, null target, empty `Assemble` (`EXE:220, 742-745, 780-784, 793-797`) → give zone forces the zone staging point or home as fallback.
- [x] **X4 Marker and Rally leave stale memory.** `ExecuteMarker` (`EXE:665-704`) and `Rally` (`:710-720`) do not clear per-ship memory → Capture (`:587-589`), strike (`:895-897`), screens (`:842`), defence (`:378`) trust it.
  - Fix: one private method with the `ForgetEngagedEnRoute` loop body (`:249-257`); called from all three.
- [x] **X5 Deduplicator drops a needed order.** `IsRepeat` compares order type only (`AiOrderDeduplicator:209-222`); memory survives a ship leaving the group; patrol orders bypass it (`EnemyPatrolShipController:81`).
  - Fix: prune `_issued` to the group's receivers in `Execute` (`EXE:171-186`); for Attack also compare `AttackTarget`.
- [x] **X6 Offset attacks bypass the deduplicator.** `IssueAttack(receivers, target, offsets)` → `Pass` (`Dedup:62-66`): ended attacks re-sent every decision; an interceptor whose attack ended on the same `DefenseTarget` is never re-sent (`EXE:378`).
  - Fix: record offset attacks with `REISSUE_SECONDS`; add `|| CurrentOrder != Attack` at `EXE:378`.
- [x] **X3 Stale strike state.** Reset block (`EXE:151-166`) keeps `_wasReady`, `_readyTarget`, `_heldAnchor`, `_battleTarget`, `_battleOffsets` → Hunt X → Defend → Hunt X launches at 50 %. Fix: clear them there.
- [x] **X9 Assembly gate trips the capture stall.** Gate (6+ ships, < 75 % in the body) stops the anchor (`EXE:479-486`); stall clock runs (`CB:1989-2005`, 20 s) → objective rejected.
  - Fix: `EnemyShipTaskGroup.Evaluate` reports `null` to `ReportCaptureExecution` while assembling.

## Phase 4 — One strength picture (P1)
- [ ] **R2 Group strength without its wings (narrowed).** Fleet and local advantage now count wings through reach. Still ships-only (`_ownForce`): armada test `CB:902`, `PublishStrike` `:1337`, zone selection `:1339 → 1935`, `baseThreatRatio` `:1385`, `IsDefended` `:1751`, `IsThreatSafe` `:2368`, `IsCutOff` `:2538`.
  - Fix: `Build` (`CB:1256-1258`) adds the launched, guarding wings of each group ship to `_ownShipIds` (same filter as `AddEscortingAirWing`).
- [ ] **C1 Threat flag and rating disagree.** `HasLocalThreat` needs a visible unit not near a hostile base (`CB:1553-1565`); `LocalAdvantage` counts platforms, stations, repairers, unseen sightings. Under platforms disengage never fires (`DM:339-341`).
  - Fix: snapshot flag `hasLocalFight = !_localThreatForce.IsEmpty` for the disengage test; `HasLocalThreat` keeps stand-and-fight and EngageLocal.
- [ ] **R3 Intercept rated at the wrong place (narrowed).** Platforms and far allies no longer count. `RateAgainstArmada` rates at `armada.AssetPosition` (`CB:950`); the marker goes to `armada.Centre` (`:925`).
  - Fix: rate `mobile` with `RateReachAt(…, armada.Centre, false)`.
- [ ] **C6 Losing force published as available.** `PublishStrike` (`CB:1337-1338`) runs before retreat / local rating exist; only test = no own station pressure.
  - Fix: move below the smoother sample (`:1432`); unavailable while recovering, retreating or locally below the line.
- [ ] **C3 Ally request overrides a fight.** `SelectAllyDefenseRequest` (`CB:842`) has no state check; armada answer excludes Hunt / Assault / Defend / Retreat, not EngageLocal (`:897-898`).
  - Fix: no ally answer in EngageLocal / RetreatValue; add EngageLocal to the armada exclusion.
- [ ] **C4 Double count.** `TeamAiCooperation.CanWin` (`:230-237`) adds request defenders (all allied units within 1,000) plus every available strike → an ally already at the station counts twice. Fix: skip a strike whose `Center` is within 1,000 of the station.
- [ ] **C7** Armada test uses the group at `CB:902`, the fleet at `:1130` → use `_root._fleetForce` in both.
- [ ] **C5 Armada look-ahead.** Only the 20 s point is tested (`HostileArmadaTracker:144-157`); velocity uncapped, a membership change shifts the centre. Fix: test `Centre` and the look-ahead point; keep the old velocity when the ship set changed.
- [ ] **D1 State hold blocks a needed fight (wider after Round 10).** Local between 0.8 × and 1 × the retreat line: `Bypass` stays "none" (`DM:353`) → `held` restores the old state and its ship count (`:371-385`). Fix: `held` needs `!standAndFight`.
- [ ] **D4 One knob, many gates (Q9).** `RetreatAdvantage` also gates ally answers (`CB:846`), armada "outmatching" (`:1140`), route safety (`:2369`), cut-off (`:2539`), patrol safety (`EnemyPatrolSafety:76`). Intercept is **not** affected (uses `RequiredAttackRatio`, `CB:919`).
  - Fix: `EnemyAiDifficultyProfile.SafetyAdvantage` read at those five places.
- [ ] **C8 Target force without platforms (narrowed).** Station weapons now count; armed satellites / platforms within 1,000 of the target do not (`CB:1642-1654`), though `IsDefended` counts them. Fix: `AddArmedStructures` in `AddTargetForce`; check a raid target that is itself armed is not counted twice.
- [ ] **I1 Intel ghosts.** A sighting < 240 s counts at full strength at its last position for zone defenders and armadas. Fix: `TeamHostileIntelService.GatherAll` forgets a sighting whose last position is visible while the unit is not.
- [ ] **I3 Unseen estimate zeroed by one visible ship (narrowed).** `EstimateUnseenFleetInvestment` uses the newest `SeenAt` (`CB:2433-2455`). Fix: price-weighted mean age.
- [ ] **C9** Recovery still divides by economy pressure (`CB:2588-2589`) though the fleet rating no longer does → drop the divisor; `FindFocusEnemy` from home, not the fleet centre.
- [ ] **C10** `FindLastStandTarget` filters from the fleet centre, rates at the local origin (`CB:1434`) → pass `localOrigin`.

## Phase 5 — Wings (P1)
- [ ] **SQ1 Mine raid empties the zone and is not sized to the mine (Q5, revised 2026-11-23).**
  - Fault 1: `AssignMineRaid` (`SQC:1185-1232`) sends **every** member of the group, fighters included (`_raidWings` = all members, `:1190-1198`, `:1231`). Raiders are skipped by `AssignGroup` (`:957`) but still counted as capturers (`:866-872`) → the zone gets no wing and no replacement. All bombers sit in `_outerGroups[0]` (`:720-727`).
  - Fault 2: the raid never looks at the mine itself. Checks = `damage > 0`, a bomber present, raiders match the armed defenders (`WinsRaid:1288-1293`). Mine hull and the owner's damage reduction are not read.
  - Fault 3: production buys at most `BOMBERS_PER_HOSTILE_MINE` 3 per known mine (`EPS:38`, `:1178`), whatever the mine's toughness; the AI reads only income research (`EPS:1367-1369`).
  - Values (`EmpireFaction.asset`, `RebellionFaction.asset`, class Structure): `HullCapacity` tiers 1.05 → 2.0; `DamageTaken` 0.85 → a fully researched mine needs up to 2 ÷ 0.85 ≈ 2.35 × the bomber damage. Not checked: whether tiers stack or replace.
  - Fix:
    - Escort (preferred, not required): the raid takes 1 fighter or interceptor wing of the group when one is free, plus as many more as it needs to match the known hostile squadrons and armed defenders at the mine. The group's other fighters stay on the zone.
    - No free fighter wing → bombers raid alone. A missing escort never blocks or delays the raid; only a mine the raiders cannot match waits (`WinsRaid`, as today).
    - Raiding wings do not count as capturers, so the zone gets a replacement wing.
    - `AssignSatelliteRaid` follows the same escort rule.
    - Bombers wanted per mine = 3 × (mine hull ÷ base hull) ÷ owner's `DamageTaken` multiplier for Structure. Same number in the wing path and the counter path (closes P10).
    - A raid starts once the group's bombers reach that number, or all bombers the cap allows are alive.
    - Doc comment "Bombers never go alone" (`SQC:1182`) → reworded to the escort preference.
  - Research source (Q5a, decided): hull from the sighting (`ResearchHealthData` applies it live); damage reduction from the mine owner's `IFactionResearchModelObserver.GetMultiplier(ResearchStat.DamageTaken, ShipClass.Structure)`.
- [ ] **SQ4 Station wing mislabelled (worse than the audit).** `IsBesideOwnStation` false when any own ship is nearer than the station (`SQC:309-321`) → tracked as hangar-launched → stays on Guard of the station forever, no group, no orders. Carrier wings whose carrier died skip station defence (`:467`).
  - Fix: decide by the guarded entity (check `IUnitOrderObserverFacade` exposes it); `:467` uses `IsFree`.
- [ ] **SQ7 Home defence switched off while hostiles stand on an own site.** `IsHeldUnderAttack` returns false on `HasTargetsIn(zone)` (`SQC:784-790`); a threatened own site is a target. Fix: ignore threatened own sites in that test.
- [ ] **SQ6 Scout keeps its capture slot (more frequent since W68).** `AssignGroup` runs before `AssignScout` (`SQC:896-918`); slot and strength stay. Fix: choose the scout before `AssignGroup`.
- [ ] **SQ3 Parks on a site it cannot win (Q6).** `EnemySquadronCaptureAssignments.AssignPart:260`, no `Wins` check.
- [ ] **SQ2 Bomber strike has no range.** `TryFindShipFightTarget` (`SQC:1003-1024`): nearest attacked ship, any distance. Fix: `BOMBER_STRIKE_RADIUS` (value: user or one engagement radius).
- [ ] **SQ5 Structure reach.** Wings 1,000 (`SQC:1299-1305`), ships 500 → site ratings use `STATIC_DEFENSE_REACH`; zone-centre callers keep a zone-sized reach.
- [ ] **SQ8 Escort targets reshuffle.** Round-robin over dictionary order (`SQC:510-515`). Fix: keep each escort's ship while it is still a target. Six 3D distances → planar.

## Phase 6 — Low and cleanup (P2)
- [ ] **P8** second-station level price counted twice after the main station fell (`EPS:461-464`); unplaceable miners counted.
- [ ] **P9** surplus loop ignores the waived second-station reserve (`EPS:502`).
- [ ] **P10** counter bombers have no `BOMBERS_PER_HOSTILE_MINE` cap (`EPS:690-697`) → done with SQ1 (one bomber number for both paths).
- [ ] **P12** paid platform retries placement every 5 s with no end (`EnemyFactionController:972-1002`) → one retry with relaxed flags.
- [ ] **P13** `GetShipTierFloor` ignores types at their purchase limit (`EPS:1019-1027`; heroes only since Round 10).
- [ ] **P11 (Q11)** comment says "none waiting", code allows 1 + 1.
- [ ] **X7** defence retry angles reach −90° / +75° (`EXE:391-404`) → clamp to the slot arc. Confirm ± 30° is a rule.
- [ ] **X10** `_defenseAdvance` never falls back (`EXE:354-360`). **X11** site guards join `_body` and skew the local origin; 5th escort shares the 1st point (`EXE:295-306, 539-551`).
- [ ] **Cycle** `LastSnapshot` from a changing primary group (`EnemyUnitCommander:470-472`) → prefer the main force; pad value uses that group's local rating (`EnemyDefenseStrategy:228-230`) → rate the pad itself.
- [ ] **Telemetry (new)** `FinishDecisionCycle` drops `isLateGame`, `hasReachedRetreatPoint`, `tradeCorrection` (`EnemyUnitCommander:505-514`) → every row logs defaults.
- [ ] **S1** multi-frame ship cycle keeps its old `DueTime` (`EnemyAiScheduler:224-244`); production has no stagger (`EPS:166`).
- [ ] **C13** fleet investment switches at literal level 4 (`CB:1253-1261`). **C14** ally defence takes the first request, not the most urgent. **I2** confidence curve dips at 67 s (`IntelConfidence:23-28`). **I5** `AddShare` adds the full `UnitCount` (telemetry only now). **D3** raid commit literal 2 (clamped by the executor; telemetry only).
- [ ] **Constants.** One shared `ENGAGEMENT_RADIUS` (1,000; 10 constants) and `WEAPON_REACH` (500; 9 uses); literal level `4` → one named tier (`CB:1254, 1261`; `EPS:340, 412, 427`; `EnemyFactionController:388, 588`; `EnemyFactionModel:34`). Values that should agree: 800 (`ROUTE_DEFENDER_REACH`, `FleetCohesion.RADIUS`, `HOSTILE_CLEAR_RADIUS`), 900 (`BASE_EXCLUSION_MARGIN`).
- [ ] **Wing cadence (Q10).**

## No work
- **C2** never true: `retreatAnchor = withdrawPoint ?? FindRetreatAnchor(…)` (`CB:1365`) → `CanDisengage` implies `HasSafeRetreat`.
- **A6** fixed by W65. **C11** one-evaluation lag, harmless. **C12** matches its doc comment. **S2** no trigger found. **D2** covered by the retreat latch (Q12).
- Outside the audit, to check while in the files: `ExecuteMarker` Retreat reads `RetreatAnchor.Value` unguarded (`EXE:691`); last stand forces GuardSite without `HasGuardSite` (`EnemyShipTaskGroup:102-104`).

## Tests (written with each phase; run only on the user's request)
- Category `EnemyAi` for every phase; `Navigation` if `FleetCohesion` changes. Relations only, no tuned values.
- New relation tests:
  - Phase 1: garrison of a cleared zone survives the no-flip time; a Go force is never marked stalled; two zones blocked at once; a blocked zone gets no ship from the same pool; hostile on an edge site counts as defender.
  - Phase 2: wiped fleet + due level → ship; critical + unaffordable best → affordable defender bought; satellite under construction does not block ships.
  - Phase 3: every ship of an assault group has an order; a ship joining a holding group gets the hold order; order to a new point after a foreign order is not dropped.
  - Phase 4: carrier group rates higher with its wings; platforms without visible units allow a disengage; retreating force is not counted by allies.
  - Phase 5: a raid with a free fighter wing takes it as escort and leaves the group's other fighters on the zone; a bombers-only group still raids; more bombers are wanted for a mine with more hull or damage reduction (values read from the faction asset, never literal); scout frees its capture slot; station-launched wing takes orders with a ship nearby.
- Tests that pin today's behaviour and change with the fix:
  - `EnemyUnitCommanderTests.SquadronRaid_BombersRaidAGuardedMineOnlyTogether`, `SquadronRaid_OuterGroupsRaidTogether_OneMinePerGroup` (SQ1): bombers-only raids stay valid; review the cases where fighters go along to an undefended mine.
  - `EnemySquadronCaptureAssignmentsTests.AssignInZone_NoWinnableSpot_TakesTheRemainingSiteNearestHomeAndIsNeverIdle` (SQ3).
  - `EnemyUnitCommanderTests.HangarWings_LaunchedBesideTheStationAreFree_BesideACarrierTheyStay` (SQ4).
  - `EnemyUnitCommanderTests.RetreatValue_NoSafeAnchorStopsTravelToLostHome` (X2 fallback point).
  - Review: `EnemyOrderRulesTests` (stall), `EnemyZoneEvaluatorTests`, `EnemyProductionDecisionModelTests.DueLevel_IsBoughtOrSavedForBeforeOptionalUnits`, `EnemyStationPressureTrackerTests.Critical_FollowsTheDefenderArrivalTime`, `EnemyDecisionStabilityTests.StandAndFight_DoesNotFlipBetweenRetreatAndLocalEngagement`, `TeamAiCooperationTests`, `HostileArmadaTrackerTests`, `AiOrderDeduplicatorTests`.

## Order
1. Phase 1 → compile → Phase 2 → compile → Phase 3 → compile.
2. User battle (UltraHard, same matchups as W60). Watch: captured zone keeps its ships; no bank > 50,000 with free capacity; `idleUnderFire` 0 rows > 10 s.
3. Phase 4, Phase 5 → compile → second battle.
4. Phase 6 any time; constants last (touches every file).

## Files
- `Assets/Scripts/Services/Enemy/`: `EnemyShipTaskGroupPlanner.cs`, `EnemyZoneObjectiveCollector.cs`, `EnemyProductionStrategy.cs`, `EnemySiteConstructionController.cs`, `EnemyStationPressureTracker.cs`, `EnemyTaskForceExecutor.cs`, `EnemyShipTaskGroup.cs`, `AiOrderDeduplicator.cs`, `EnemyStrategicContextBuilder.cs`, `TeamAiCooperation.cs`, `HostileArmadaTracker.cs`, `TeamHostileIntelService.cs`, `EnemySquadronCommander.cs`, `EnemySquadronCaptureAssignments.cs`, `EnemyUnitCommander.cs`, `EnemyAiScheduler.cs`.
- `Assets/Scripts/Entities/EnemyFaction/Models/`: `Objectives/EnemyZoneAllocator.cs`, `Objectives/EnemyZoneEvaluator.cs`, `EnemyProductionDecisionModel.cs`, `EnemyStrategicDecisionModel.cs`, `EnemyAiDifficultyProfile.cs`, `EnemyDefenseStrategy.cs`.
- Size note: `EnemyStrategicContextBuilder` 3,114 lines, `EnemyProductionStrategy` 1,433, `EnemySquadronCommander` 1,340, `EnemyTaskForceExecutor` 1,128 → a split is wanted, not part of this plan.

## Verification (Phases 1–3, 2026-11-23)
- `unity recompile`: 0 errors, `compilationFailed: false`. Console errors present are from an earlier test run (10:58, 11:13), not this change.
- New tests (category `EnemyAi`, not run): `EnemyZoneAllocatorTests.Allocate_BlockedZonesGetNoShip_TheOpenZoneTakesThePool`, `Allocate_BlockedHeldZoneUnderAttack_StillGetsShips`; `EnemyProductionDecisionModelTests.WipedFleet_RebuildsBeforeALevelDueByOrders`; `AiOrderDeduplicatorTests.Attack_IsSentAgainWhenTheShipNowAttacksAnotherTarget`, `OffsetAttack_EndedOnTheSameTargetWaits_ANewSlotPasses`, `RetainOnly_ForgetsShipsOutsideTheGroup`; `EnemyUnitCommanderTests.AssaultBase_ShipsOutsideTheCapitalForceHoldAtTheGatherPoint` (test `Context` helper gained `baseTarget`, `assaultCapitals`).
- Reviewed, no update needed: `RetreatValue_NoSafeAnchorStopsTravelToLostHome` (executor without gather point still stops; fallback lives in the context builder), `Allocation_ForceStalledBelowAnEvenFightIsReleasedAfterNinetySeconds`, DefendBase executor tests, `Hold_AttackMovesToTheGatherPointAndNeverStopsAwayFromIt`, `Rally_*`, `AiOrderDeduplicatorTests` (existing), `DueLevel_IsBoughtOrSavedForBeforeOptionalUnits`, `DepletedFleet_*`, critical-station production tests (corvette rule also filters the counter path).
- Not written yet from the Phase 1–3 list: cleared-zone garrison survives no-flip time; hostile on an edge site counts as defender; satellite under construction does not block ships; ship joining a holding group gets the hold order.

## Verification before implementation
- Read myself: planner, zone allocator, evaluator, objective, collector, scout gate, caution, hold assessor (A1–A7, R1); spot checks of P2 (`EnemyProductionDecisionModel:157-189`) and X1 (`EXE:863-872`).
- Read by 5 read-only agents, each told to disprove the audit: production, executor, strength rating, decision model + intel, wings + scheduler. Their line references are not all re-read by me.
- Not verified: anything in a battle; test bodies beyond names; `IUnitOrderObserverFacade` guard target (SQ4); whether any shipped map has no centre zone (X1).
