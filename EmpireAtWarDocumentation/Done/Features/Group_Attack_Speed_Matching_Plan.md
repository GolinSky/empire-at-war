---
category: Features
status: done
created: 2026-11-18
completed: 2026-11-18
acceptance: pending-user
---
# Group Attack Speed Matching

## Goal
- A group attack order (e.g. select all → right-click enemy) moves every ship at the speed of the slowest ship in the selection, so the fleet arrives together.

## Outcome (2026-11-18)
- Implemented with the recommended decisions; Unity compile passed. EditMode tests added/updated, not run (local override).

## Decision
- Scope: ships only (receivers with `IAttackFacade` + `IHardPointAttackFacade`). Squadrons and stationary attackers ignore the pace. Why: fighters at capital pace cannot screen.
- Orders: `Attack` + hard-point attack only. `AttackMove`, `Move`, waypoint move unchanged.
- Pace = live minimum uncapped speed (`ShipMoveModel.BaseSpeed`) of remaining members; `+∞` with < 2 members.
- Release per ship: attack state holds at firing range (`AttackTargetState.HasReachedRange`), any new order/Stop, ability approach (state leaves attack), ship released. Never re-capped on later pursuit.
- AI orders go through `UnitOrderService` → same behaviour.
- Saves: pace not persisted; loaded attacks resume at own speed. Networking: host-only; `ClientCombatEventReceiver` skips ships, so clients never run pace.
- Avoid: pace stored in `UnitOrderModel` (plan step 4) — membership lives in `ShipOrderRunner` (`_pace`, `_paceMember`); keeps the save model untouched.

## Implementation
- `Entities/BaseEntity/Orders/GroupPace.cs`: `Join(speed) → id`, `Leave(id)`, `Report(id, speed)`, lazy `Pace`. No event; runner polls each tick (O(1) report, recompute only when dirty).
- `UnitOrderService.CreatePace`: one `GroupPace` when ≥ 2 alive ship receivers; passed to every `Attack`/`AttackHardPoint`.
- `IAttackFacade.Attack(target, offset, GroupPace pace = null)`, `IHardPointAttackFacade.AttackHardPoint(..., GroupPace pace = null)`; abilities/saves/network callers unchanged.
- `ShipOrderRunner`: `JoinPace` after Attack/AttackHardPoint (incl. same-target refresh), `LeavePace` in `Issue`/`Release`, `UpdatePace` after `CompleteOrder` in `Tick` → `IShipMovement.SetSpeedCap(pace)`.
- `ShipMoveModel`: `BaseSpeed`, `SpeedCap` (`+∞` = uncapped), `Speed = min(BaseSpeed, SpeedCap)`.
- `ShipMoveComponent.PaceSpeed`: `+∞` while Arriving, ion-disabled or anchored (never holds group back). `SetSpeedCap` → `_motion.SetRouteSpeed` only when the cap changes.

## Edge Cases
- Ship already in range on issue → held on Enter/first tick → leaves before moving capped.
- Target killed → auto-retarget `Issue` drops the pace.
- Stations (`StationaryAttackFacade`) ignore the pace.

## Tests (not run)
- New `GroupPaceTests` (`Navigation`): min, leave, report, < 2 members.
- `ShipMoveModelTests.SpeedCap_LimitsSpeedOnlyBelowOwnSpeedAndKeepsEngineDamage` (`Navigation`).
- `ShipPursuitTests.Runner_GroupAttackApproachesAtSlowestPaceUntilInRangeOrNewOrder` (`Navigation`).
- `UnitOrderServiceTests` (`BattleInput`): ships share one pace / squadrons none; single ship none.
- Fakes updated: `NavigateStateTests`, `ShipStopTests`, `ShipPursuitTests` movement; `ShipAbilityServiceTests`, `ShipAbilitySymmetryTests`, `UnitActionsPresenterTests`, `UnitOrderServiceTests` attack fakes.
- Reusable guidance: none extracted (feature-specific behaviour, documented here).

## Files
- `Assets/Scripts/Entities/BaseEntity/Orders/GroupPace.cs`
- `Assets/Scripts/Services/UnitOrders/UnitOrderService.cs`
- `Assets/Scripts/Entities/BaseEntity/EntityFacades/IAttackFacade.cs`, `IHardPointAttackFacade.cs`
- `Assets/Scripts/Entities/Ship/EntityFacades/ShipOrderFacade.cs`, `StationaryAttackFacade.cs`
- `Assets/Scripts/Entities/Squadron/EntityFacades/SquadronOrderFacade.cs`
- `Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs`, `Entities/Ship/StateMachine/AttackTargetState.cs`
- `Assets/Scripts/Components/Ship/Movement/ShipMoveModel.cs`, `ShipMoveComponent.cs`, `IShipMovement.cs`

## Verification (2026-11-18)
- Battle log `Loading_20261118_182527_ai_orders.csv` (code `ee1dd83fa`): Player0 13-ship Attack at t≈106 s → ships 551–555 (own move max `30`) approached at `25` = slowest member; all members moved at one speed. No pace exceptions in `Editor-prev.log`.
- Full EditMode run `Assets/TestResults_20261118_184635.xml`: 4114 total, 58 failed. 2 from this change: `UnitOrderServiceTests.Attack_ShipsShareOnePaceAndSquadronsGetNone(False/True)` → the non-hardpoint branch passed the pace to squadrons. Fix: `UnitOrderService` passes the pace only to receivers with `IHardPointAttackFacade`. Compile passed; rerun pending.
- The other 56 failures are outside this change (no `GroupPace`/cap code on their stacks): `WeaponHardPoint.ExpectedShotsInFlight` NRE (`WeaponCandidate`/`Cadence`/`TurnRate`/`Range`/`CombatAttackCoordinatorSelection`, 37), `ShipPursuitTests` formation/standoff (4), `UnitOrderServiceTests.GuardReservesTargetHull_AttackIssuesHullRelativeSlot` (single-ship slot 5 vs 10, from attack-arc commit `8787a78e1`), `BattleMarkerProjection` (4), `SquadronIconGrouping` (2), `CaptureRingView` (2), `PlayerSave` AI super weapon (2), `SuperWeapon`, `EnemyUnitCommander`, `UnitSpawnView` (DarthVader prefab), `EntityBindings` (Delta7 prefab).

## TODO
- [x] Decisions (recommended defaults applied)
- [x] Steps 1–8
- [x] Test review/update
- [ ] User: in-game check + run `Navigation`, `BattleInput` when authorized → `TODOs/User_Acceptance_Queue.md`
