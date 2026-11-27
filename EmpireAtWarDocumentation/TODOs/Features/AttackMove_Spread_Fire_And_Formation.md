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

## TODO
- [x] Category test run green (2026-11-27).
- [x] Battle check #2 analysed (above).
- [ ] Idle leak fixes: category run (Navigation, Ships, EnemyAi, BattleInput).
- [ ] User battle: idle back line after the leak fixes; AI behaviour with the fight watch.
- [ ] Then: Review → acceptance queue.

## Related
- [[TODOs/Bugs/Battle_Facing_AttackMove_And_Xyston_Fixes]] — role standoff (#2 step 3) superseded by formation hold; `attackMoveDrop` row removed.
- [[Done/Features/Focus_Fire_Cap_Plan]] — caps and hero rule updated 2026-11-27.
