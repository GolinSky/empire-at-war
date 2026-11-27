---
category: Features
status: in-progress
created: 2026-11-27
---
# Attack-Move Spread Fire, Formation Hold and No Idle Back Line

## Goal
- Attack-move groups hold formation and spread their guns over many enemies, not one.
- Ships of an attack order never sit idle after the target dies (back line went AFK).
- Dreadnoughts stop dying to pile-ups (10–12 ships on one).

## Evidence (2026-11-27, `Loading_20261127_103730_ai_orders.csv` + 64 `Battle_*.csv`)
- Dreadnought damage: 97% from ships (Capital 35%, BattleCruiser 27%, Dreadnought 20%, HeavyCruiser 13%); squadrons 1.2%, stations 1.8%.
- Per shot a dreadnought takes less turbolaser damage than a capital (matrix 0.7 vs 0.9) → volume is the cause.
- 5 Praetors/Mandators lived 43–63 s; peak 10–12 attackers: up to 8 slot holders + 4–6 attack-move overflow.
- Player0 attack orders: 95 × "target destroyed; no replacement" → idle median 23 s, max 129 s; 73 of 83 had radar contacts out of reach.

## Decisions (user 2026-11-27)
- Ship focus-fire caps: Corvette 1, Frigate 1, Cruiser 2, HeavyCruiser 2, Capital 3, BattleCruiser 4, Dreadnought 5. Squadrons unchanged. Hero bonus slot removed.
- Attack-move **holds formation**: fly to the slot, stop there, never chase. Enemies out of every gun's reach are ignored until the formation gets close.
- **Gun spread**: per gun, preferred class first (heavy guns on ships), then the enemy fewest of this ship's guns shoot, then nearest. No main target.
- Attack order, target dead, nothing in reach → **attack-move** (not idle).
- Attack orders keep focus fire (slot cap limits it).

## Implementation
- `WeaponComponent.SetSpreadFire(bool)` (`IWeaponComponent`). `_engagedGroups[hardpoint]` = group of its last started salvo. `IsCrowded`: guns on group > max(1, ceil(liveGuns / listed groups)). Crowded → doomed tier in `IsDoomed` → job, batch fallback and serial path all skip it while another enemy of the same preference is in reach.
- `AttackMoveState` rewritten: `MoveToPosition(slot)` → no stops en route → on slot: `HoldPosition` + `LookAtTarget` on `AttackMoveEngagement.SelectTarget` (preferred, in reach, threat, fewest facing). Never `AddTarget(MainTarget)`. `IsComplete` = arrived + searched + group reports no enemy. Spread fire on in `Enter`, off in `Exit`.
- Removed with the old chase: en-route fight (`IsEngagingEnRoute` on `IUnitOrderObserverFacade`, `AiOrderDeduplicator` skips, `EnemyTaskForceExecutor.ForgetEngagedEnRoute` / `TryFindEngagedEnRoute`, D3 drop request beside an engaged ship), row standoff (`ShipEngagement.GetStandoffFactor`, `AttackMoveTo(..., row)`, layout row memory), `attackMoveDrop` telemetry. Row *slots* (`FormationModel.CalculateRowDestinations`) stay.
- `ShipOrderRunner.CompleteOrder`: Attack target destroyed, no replacement in reach → `AttackMove` to `GetFightApproach()` (engagement range of `AttackTargetState.LastTargetPosition` on own bearing; own position when already that close), own `AttackMoveEngagement`.
- `UnitOrderService.IssueOverflow`: overflow attack-moves to engagement range of the target on its own bearing (`IAttackMoveFacade.GetEngagementRange`; squadrons 0 → the target point), not onto the target.
- AI uses the same states: AI attack-move also holds formation and no longer chases inside its area.

## Tests
- New/changed: `Attack_TargetKilledWithNothingInReach_AttackMovesToFiringRangeOfTheFight`, `AttackMove_SpreadsFireWhileActiveAndMakesNoEnemyTheMainTarget`, `AttackMove_EnRouteShipKeepsItsCourseThroughEnemiesInReach`, `AttackMove_ArrivedShipTurnsToAnEnemyBeyondReachWithoutLeavingItsSlot`, `AttackMove_ArrivedShipCompletesOnceTheGroupReportsNoEnemy`, `AttackMove_AfterAKillTurnsToTheNextEnemyWithoutMoving`, `SpreadFire_GroupOverItsShareOfTheShipsGunsIsSkippedLikeADoomedOne`, `Attack_OverTheFocusFireCap_..._AttackMoveToEngagementRangeOfTheTarget`.
- Removed (feature gone): `AttackMoveRows_RearRowsStandOffFartherButWithinReach`, `AttackMove_EnRoute*` chase/break-off cases, `AttackMove_LostTargetSearchesBeforeReturningToThePoint`, `ShipEngagedEnRoute_KeepsItsFightUntilARetreat`, `EngagedEnRoute_ShipIsNotReorderedAndTheGroupAsksOnceForADrop`.
- 2026-11-27 EditMode, explicit off: Navigation 421/421 (after fixing a wrong expected value in the new AFK test: ship at 0, target at 500 → 400), Combat 345/345, BattleInput 489/489, Ships 490/490, EnemyAi 841/841, Abilities 268/268, Squadrons 236/236.

## Edge Cases
- Lone ship on attack-move: same rules; turns to the nearest enemy the group picks; guns spread.
- Ship pushed off its slot after arrival: not re-planned (holds where it is).
- Overflow ship already within engagement range: attack-moves in place.
- Crowded check counts guns by their last started salvo; a gun may hop between near-equal enemies on later salvos (bounded by the share).

## Battle check 2026-11-27 #2 (`Loading_20261127_135651_ai_orders.csv`, loaded save, 1,249 s)
- Team Player0+2 won in 21 min; enemy lost 188 ships, Player0 36 of 48.
- Dreadnoughts: Praetor in main fight 158 s (before 43–63 s). Praetor 1475 (30 s) and Mandator 1510 (55 s): solo Attack on enemy station (id 2) 1–2 s after spawn, 21–25 contacts, ~9k HP/s lost → not a cap issue.
- Max ships with a dreadnought as main target 4 (cap 5; before 5–11). Enemies in gun range: median 3–4 (before 5–7).
- Attack-move stopped samples: median 0 from slot (before ~600). Player1 median 116.
- Damage to dreadnoughts: Capital 62%, BattleCruiser 28% (Bulwark, Providence, Invincible, Recusant).
- Idle with enemies on radar, Player0: 686 s / 21 min (before 3,890 s / 40 min). Causes: held replacement target left reach → Idle (263 s); attack-move ended, enemies then arrived inside radar beyond guns (296 s, 39 cases); fresh reinforcements (240 s, normal).

## Idle leak fixes (2026-11-27)
- `CompleteOrder`: Attack with `HoldAttackPosition` (auto replacement) ending while the target lives → `AttackMove` to `GetFightApproach()`; telemetry reason "Attack target left weapons reach; attack-moving to the fight."
- Attack-move ends → `_watchesForFight`. Idle ship checks radar every `FIGHT_WATCH_INTERVAL` 0.5 s; nearest non-strikecraft enemy beyond engagement range → `AttackMove` to engagement range on own bearing (telemetry kind `fightWatch`). Any `Issue` (Stop, Move, Guard…) or `Release` ends the watch.
- Applies to AI ships too (AI orders replace it).
- Tests: `Attack_TargetKilled_PicksInReachContactAndAttackMovesToItWhenItLeaves` (was `…AndHoldsWhenItLeaves`, expected Idle), new `AttackMove_EndedThenAnEnemyAppearsBeyondReach_ClosesOnlyOnAShip`, `AttackMove_EndedThenStopped_StaysPutWhenAnEnemyAppears`.

## Stop at own reach (user 2026-11-27, replaces "no stops en route")
- Decision: the attack-move point marks the **enemy area**, not a place to fly into. Option chosen: each ship stops when an enemy is in its own weapon reach (not whole-group stop, not area-only enemies).
- Evidence (17:05 AI vs AI `Loading_20261127_170552_ai_orders.csv`, 17:11 skirmish `Loading_20261127_171139_ai_orders.csv`): ISDII and Tector had an enemy in reach 460–490 before their slot and stopped 39 from it; 61 Player0 orders kept flying with an enemy in reach.
- `AttackMoveState.Tick`: moving + search due + `HasEnemyInReach(isHolding: false)` → `Stop`, `_hasArrived`, `_isStoppedShort`. Stopped short and no enemy in held reach (`HELD_REACH_FACTOR`) → `MoveToPosition(slot)` again; never past the slot. Strikecraft never stop the approach. Area cleared → ship finishes at its slot, order completes as before.
- Fire question answered (code read): spread fire never idles a gun. Crowded/doomed groups are a fallback tier (pass order: preferred normal → preferred doomed → other normal → other doomed). 10 ships vs 2: every ship fires; each splits its guns about evenly over the enemies it reaches; no cross-ship cap in attack-move (focus-fire caps are for Attack orders); overkill check (`DOOMED_MARGIN` 1.25) only demotes a target to the fallback tier.
- Tests: `AttackMove_EnRouteShipStopsOnceAnEnemyIsInItsOwnReach` (was `…KeepsItsCourseThroughEnemiesInReach`), new `AttackMove_StrikecraftInReachDoNotStopTheApproach`, `AttackMove_EnemyBeyondReachDoesNotStopTheApproach`, `AttackMove_StoppedShortFliesOnWhenNoEnemyStaysInReach`; `AttackMove_ShipStoppedFarShortOfItsSlotTriesAgainInsteadOfHolding` enemy moved beyond reach. Runs: ShipPursuitTests 99/99, Ships 491/491, Simulation 57/57, EnemyAi 841/841, RealEntityCompositionTests 38/38.
- Simulation (AI vs AI) has `SimulationSetup.UseAttackMove` (default on): idle units attack-move as one group to the midpoint of both fleets' centres.

## Battle 2026-11-27 17:58 (`Loading_20261127_175813_ai_orders.csv`, skirmish, 2,028 s) → fixes
- **No DPS (user).** Weapons always had a target, but held attack-move ships had an enemy inside firing range (`IsInRange` = 0.8 × engagement range) only 5–12% of samples; nearest enemy at 0.87–0.93 of max range. 41% stopped short + 19% on slot with the nearest enemy at 0.8–1.0 of max range. Cause: the stop used bare reach (`CanEngage`) → both sides stood at the range edge.
  - Fix: stop when a non-strikecraft enemy is `IsInRange` and `MainBatteryReaches` (bow-on mains); stay while `CanHoldOn(isHolding: true)`; else fly on.
- **Ships too close (user).** 288 / 1,007 neighbour pairs of held attack-move ships closer than `r1 + r2`; 208 of them stopped short (rear rows flew onto a front line that stopped before its slot). Slots: median nearest gap 1.04 × `r1 + r2`.
  - Fix 1: `FormationModel.ROW_SLOT_CLEARANCE` 1.5 on row depth, line spacing and the one-row compact clump.
  - Fix 2: `AttackMoveEngagement.SetPullBack/ClearPullBack/TryGetPullBack`: a ship stopping short shares slot − stop position; members whose slot is ahead-of-it-along-the-shortfall behind it shift their goal back by it (largest wins; ships beside keep theirs). `AttackMoveState._goal` replaces the slot for move/arrival/retry; re-targets when the goal moves > `NavigationRadius`.
- **Mediator turns sideways (user).** Held 550–700 from enemies it could not reach, `facingDot` 0.5–0.6 (gun-arc solve toward an out-of-reach target). Fix: facing target beyond held reach → `LookInDirection(bearing)` (bow-on); `LookAtTarget` (gun solve) only in reach.
- **Squadrons stay near the formation (user).** Player0 squadrons on attack-move: no target in 73% of samples (Attack 0%). Cause: `Squadron` searched only after its point; escorting ships stop short, so squadrons never arrived. Fix: search en route every `ShouldSearchTargets` tick.
- Tests: `AttackMove_RearShipStopsBehindAShipAheadThatStoppedShort`, `AttackMove_ArrivedShipFacesAnEnemyBeyondReachBowOnWithoutLeavingItsSlot` (was `…TurnsToAnEnemyBeyondReach…`), `PullBack_AppliesOnlyToSlotsBehindTheStoppedMemberAndLeaveClearsIt`, `PullBack_TheLargestShortfallAheadWins`, `RowDestinations_FrontLineStandsOnThePointAndRowsStandBehindItWithAHullGap`, `RowDestinations_OneTierMatchesTheCompactClumpWithAHullGap`. No unit harness drives a real `Squadron`: squadron change covered by compile + Squadrons category only.

## Battle 2026-11-27 19:33 (`Loading_20261127_193334_ai_orders.csv`) → groups flew back on a new attack-move
- User: a new attack-move sent the whole group backwards and turned it around.
- Order t=327.92, 11 ships (Viscount, Mediator, Endurance, MC80b, MC75, 6 × MC30c), click ≈ (-708, -1800) toward the enemy. Every slot except the front pair lay 600–900 behind the ships (enemies 500–730 away).
- Cause: Viscount `NavigationRadius` 895 (others 36–125). Lone dreadnought joined the front line; lines spaced by their largest radius → Mediator 2,685 to the side, every other line ~1,440 behind the point (`ROW_SLOT_CLEARANCE` 1.5 made it worse).
- Fixes:
  - `AttackMoveState.Enter`: enemy (not strikecraft) in reach (`CanEngage`, held margin) → stop and fight from here, no move; off its slot → stopped short (no pull-back). Stay test is also reach-only (`HasEnemyInHeldReach` no longer needs `MainBatteryReaches`). Ship on its slot → arrived, completes normally.
  - `FormationModel.CalculateRowDestinations`: hull > `OVERSIZED_RADIUS_FACTOR` (3) × next-largest → own last line. Neighbour gap `SlotGap(a, b)` = `a + b + min(a, b) × 2 × (ROW_SLOT_CLEARANCE − 1)`. One depth per line: just far enough back that every hull clears every hull ahead (circle clearance), never ahead of the line in front. Per-ship depth tried and dropped: a smaller hull could stand level with a larger one (`RowDestinations_LargerHullsTakeTheRowsFirstLine`).
  - Replay of the real order (Editor eval): all 10 normal ships move forward 455–772 toward the click; Viscount forward 197, 1,295 behind the point (range 1,623).
  - Runs: Navigation 432/432, BattleInput 489/489, Ships 491/491, EnemyAi 841/841, Squadrons 236/236, Simulation 57/57, RealEntityCompositionTests 38/38, ShipPursuitTests 101/101, AttackMoveEngagementTests 11/11.
- Tests: `AttackMove_ShipAlreadyInReachStaysWhereItIsEvenWithItsSlotBehindIt`, `RowDestinations_OversizedHullStandsBehindTheRowsAndNoHullsOverlap`; `RowDestinations_FrontLineStandsOnThePointAndRowsStandBehindItWithAHullGap` now checks gaps, not a fixed depth.

## Giant hull on the point (user 2026-11-27)
- Decision: a dreadnought-size hull flies in the deepest height layer (Viscount `HeightTier` 0, `deep` -391; others -291…30), so it overlaps nobody: it takes the front line's point and never moves or holds back other ships.
- `FormationModel`: oversized hull (> 3 × next largest) → row `Front`, slot = the point; heading, centre, lines and the one-row clump are computed without it (`CalculateSpacedClump`, `CalculateCenter(positions, skipped)`). Replaces "own last line".
- `AttackMoveEngagement.SetPullBack/TryGetPullBack` take the flight height (`CurrentPosition.y`); a stop pulls back only members within `SAME_LAYER_HEIGHT` 50 → the Viscount stopping at its 1,623 range holds nobody back.
- Replay of the 19:33 order: Viscount slot on the point (+1,492 forward; it stops once in range), others +426…+768.
- Tests: `RowDestinations_OversizedHullTakesThePointAndTheOthersIgnoreIt` (replaces `…StandsBehindTheRows…`), `PullBack_AStopInAnotherHeightLayerHoldsNobodyBack`.

## TODO
- [ ] User battle: new attack-move on a group already in range → nobody turns back; mixed fleet with a huge hull.
- [ ] User battle: attack-move DPS, spacing, Mediator facing, squadrons engaging (17:58 fixes).
- [ ] User battle: attack-move stops at weapon reach (AI vs AI with attack-move groups).
- [x] Category test run green (2026-11-27).
- [x] Battle check #2 analysed (above).
- [x] Idle leak fixes: category run 2026-11-27, explicit off: Navigation 426/426, Ships 491/491, BattleInput 489/489, EnemyAi 841/841. Code in `2f4a68e7e` (committed by a parallel session with its squadron change).
- [ ] User battle: idle back line after the leak fixes; AI behaviour with the fight watch.
- [ ] Then: Review → acceptance queue.

## Related
- [[TODOs/Bugs/Battle_Facing_AttackMove_And_Xyston_Fixes]] — role standoff (#2 step 3) superseded by formation hold; `attackMoveDrop` row removed.
- [[Done/Features/Focus_Fire_Cap_Plan]] — caps and hero rule updated 2026-11-27.
