---
category: Bugs
status: todo
agent_marker: AGY
created: 2026-11-18
---
# [AGY] Ship Attack Order Hold-in-Range Band and Formation Offset Fix

## Goal
- Hold position immediately (without moving or running away) whenever a ship ordered to Attack is already within a valid firing distance **range (band)** of its target, whether stationary or already moving.
- Fix `UnitOrderService.AttackOffsets` and `AttackTargetState.UpdateFormationMove` so group Attack orders never send ships thousands of metres away or to the far side of the target.

## Root Causes (Source-Verified)
1. **Moving ships ignore `IsInRange` (`AttackTargetState.cs:240-253`)**:
   - `AttackTargetState.UpdateMoveState` gates holding on `if (!_shipMoveComponent.IsMoving && IsInRange)` (`L241`).
   - A ship already moving when the Attack order arrives — or entering `IsInRange` (`<= EngagementRange * 0.8f`) while moving — skips `HoldAndLookAtTarget()` and keeps driving inward to `MINIMUM_RANGE_FACTOR * EngagementRange` (`0.5f`, `L248`) or `StandoffPosition` (`0.65f`).
2. **Artillery ships ignore the hold band while moving and retreat when inside `0.8x` reach (`AttackTargetState.cs:351-371`)**:
   - In `TryUpdateArtilleryMove`, `if (_hasPursuitDestination && moving && (hold || retreat == _isArtilleryRetreat)) return true;` (`L364`) runs **before** `if (hold) { HoldAndLookAtTarget(); ... }` (`L365-371`). A moving artillery ship entering the hold band (`<= 0.95f * _artilleryReach`) finishes its previous move leg instead of stopping.
   - Without main-battery cover (`!_artilleryHasMainCover`, `L355-357`), an artillery ship ordered to attack while closer than `max(_artilleryReach * 0.8f, contact)` sets `retreat = true`, turns stern-first, and drives away to `0.9f * _artilleryReach` (`L389-402`).
3. **`ENGAGE_RANGE_FACTOR = 0.8f` shrinks `IsInRange` below weapon reach (`WeaponComponent.cs:33, 404-406`)**:
   - `GetEngagementRange(targetClass)` is already a DPS-weighted average below `MaxWeaponRange`; multiplying it by `0.8f` makes a ship already firing effectively at `0.81–0.95 × EngagementRange` evaluate `IsInRange == false` and start moving closer on a fresh Attack order.
4. **Same-target Attack re-issue resets state because `AttackOffsets` changes (`UnitOrderService.cs:83-108`, `ShipOrderRunner.cs:156-160`, `AttackTargetState.cs:96-137`)**:
   - `UnitOrderService.AttackOffsets` always emits a non-zero offset (even 1 ship gets `radius = maximumRadius * 2f` at `heading` from current selection center).
   - Re-issuing Attack on the same target after moving or changing selection changes `formationOffset`, failing `_orders.Matches(UnitOrderType.Attack, target, offset)` (`ShipOrderRunner.cs:160`) and `IsTheSameTarget(mainTarget, formationOffset)` (`AttackTargetState.cs:103, 129-137`), calling `ResetPursuit()` → `Enter()` (`_isClosingRange = false`).
5. **Two-stage `UpdateFormationMove` + $R_{\max}/\sin(\pi/N)$ $360^\circ$ ring (`UnitOrderService.cs:100-106`, `AttackTargetState.cs:71-72, 195-212`)**:
   - `AttackOffsets` collects all `IAttackFacade` receivers (`L85`) — including `Squadron` even though `Squadron.Attack` ignores `formationOffset` (`Squadron.cs:313-317`) — and places them around a full $360^\circ$ circle of radius $\max(2 R_{\max}, R_{\max} / \sin(\pi / N))$. With an Executor (`R = 1059.44m`) and `N = 22`, `|offset| = 7,444.31m`, causing `UpdateFormationMove` Stage 1 (`MovementTargetPosition`) to drive ships to the map edge before turning 180° for Stage 2 (`StandoffPosition`).

## Important Values (`AttackTargetState.cs`, `WeaponComponent.cs`, `UnitOrderService.cs`)
- `ENGAGE_RANGE_FACTOR`: `0.8f`
- `MINIMUM_RANGE_FACTOR`: `0.5f`
- `ARTILLERY_RETREAT_FACTOR`: `0.8f`; `ARTILLERY_APPROACH_FACTOR`: `0.9f`; `ARTILLERY_HOLD_FACTOR`: `0.95f`
- `AttackOffsets` minimum ring radius: `maximumRadius * 2f` for `<= 2`, `maximumRadius / sin(pi / count)` for `>= 3`

## Implementation
1. **Define explicit Hold-in-Range band `[MinHoldDistance, MaxHoldDistance]` (`AttackTargetState.cs`, `WeaponComponent.cs`)**:
   - Standard ships: treat any distance `<= HoldMaxRange` (derived from live effective weapon reach / `0.95f * EngagementRange` up to `MaxWeaponRange` for the target class, `MinHoldDistance = 0f`) as inside the valid hold band when ordered to Attack so ships already firing never start a move leg.
   - Artillery ships: expand hold band on direct Attack orders (`hold` down to `contact + 1f` or require explicit retreat toggle) and never force a retreat away from a target on initial Attack when already within valid firing range.
2. **Stop immediately upon entering hold band (`AttackTargetState.cs`)**:
   - Remove `!_shipMoveComponent.IsMoving &&` from `UpdateMoveState` (`L241`) → call `HoldAndLookAtTarget()` immediately whenever `IsInRange` is true.
   - In `TryUpdateArtilleryMove` (`L364-371`), evaluate `if (hold)` before `if (_hasPursuitDestination && moving && ...)` so entering `hold` halts the current move leg instead of completing it to `0.9 * _artilleryReach`.
3. **Ignore offset-only diffs on same-target Attack re-issue (`ShipOrderRunner.cs`, `AttackTargetState.cs`)**:
   - If `_stateMachine.CurrentState == _attackTargetState` for the same `target` (and optional `hardPointId`) while `IsInRange` (or already in hold band), preserve current hold state instead of resetting pursuit on a recomputed `formationOffset`.
4. **Fix `UnitOrderService.AttackOffsets` and `UpdateFormationMove` (`UnitOrderService.cs`, `AttackTargetState.cs`)**:
   - Filter out entities whose `Attack` implementation ignores `formationOffset` (e.g. `Squadron`).
   - Replace the full $360^\circ$ $R_{\max}/\sin(\pi/N)$ ring with a bounded front/flank arc on the approach side (`heading` $\pm$ clamped arc) sized per ship pair rather than multiplying the largest ship's radius across every small ship/squadron.
   - Remove the Stage 1 `MovementTargetPosition` outer detour in `UpdateFormationMove` so ships converge cleanly toward their final standoff arc without flying outward first.

## Edge Cases
- Pursuing a moving target that flees outside `MaxHoldDistance` → resume approach toward `StandoffPosition`; once back inside `IsInRange` (with small hysteresis), stop and hold again.
- `TryUpdateHardPointApproach` against oversized `BattleCruiser`/`Dreadnought` targets → preserve hardpoint-reach check when `NavigationRadius > WeaponComponent.AttackDistance`.
- Same-target focus fire clicked twice to move Closer/Regroup? Explicit `Move` / `AttackMove` overrides hold as before; Attack re-click while already in range holds cleanly.

## Files
- `Assets/Scripts/Entities/Ship/StateMachine/AttackTargetState.cs`
- `Assets/Scripts/Entities/Ship/StateMachine/ShipEngagement.cs`
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs`
- `Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs`
- `Assets/Scripts/Services/UnitOrders/UnitOrderService.cs`
- Affected test suites to review (do not run under local override): `Assets/Scripts/Tests/Editor/Navigation/` (`ShipPursuitTests`), `Assets/Scripts/Tests/Editor/BattleInput/`

## TODO
- [ ] Approve hold-in-range upper bound (`0.95 * EngagementRange` vs live mount max range) and artillery direct-order retreat policy
- [ ] Exclude non-formation units (`Squadron`) and cap/arc `UnitOrderService.AttackOffsets`
- [ ] Fix `AttackTargetState.UpdateMoveState`, `UpdateFormationMove`, `TryUpdateArtilleryMove`, and `ShipOrderRunner.Attack`
- [ ] Review/update `ShipPursuitTests` and `UnitOrderService` EditMode tests
