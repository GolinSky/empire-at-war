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

## TODO
- [x] Category test run green (2026-11-27).
- [ ] User battle (Development build/Editor): dreadnought lifetime, attackers per dreadnought, idle back line, attack-move formation; AI attack-move strength.
- [ ] Then: Review → acceptance queue.

## Related
- [[TODOs/Bugs/Battle_Facing_AttackMove_And_Xyston_Fixes]] — role standoff (#2 step 3) superseded by formation hold; `attackMoveDrop` row removed.
- [[Done/Features/Focus_Fire_Cap_Plan]] — caps and hero rule updated 2026-11-27.
