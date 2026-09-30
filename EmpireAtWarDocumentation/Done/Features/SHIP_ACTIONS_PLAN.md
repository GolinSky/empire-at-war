---
category: Features
status: done
completed: 2026-09-30
---
# Ship Actions Plan

## Lifecycle Review

- Closed out: 2026-09-30. Implemented in `ea3b8759`; later retreat/UI changes recorded in `767da64c` supersede the original countdown and Move-button design.
- Verification: live `UnitOrderService` exposes the planned order APIs; `CoreGameUi.prefab` retains seven action bindings; acceptance-test sources exist. No automated tests or Play Mode run during this review.

## Goal

- One `IUnitOrderService` API for player and enemy AI.
- Ships → all orders; station/platforms → Attack + Stop; mining facilities → none.
- Panel: `ShipActions` in `Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab`.

## Rules

- Receivers are explicit selected/task-force entities; never affect unselected ships.
- Sample immediate receivers on button press; targeted receivers on confirming right-click.
- Selection change cancels pending actions; ability targeting and action targeting exclude each other.
- Actions are available when at least one alive receiver supports them.
- Entity command route uses `IEntity.TryGetCommand<T>` in the recorded plan; confirm live facade contracts before implementation.
- Preserve existing `SetContentLayout` edits, formation offsets, and receiver-side retreat destinations.
- Read root `AGENTS.md` and both UI rules; write planned tests, execute only on explicit request.

## Responsibilities

- `ShipOrderModel` → one current order and data; pure C#, positions as `FormationPoint`.
- `UnitActionTargetingModel` → pending player input mode.
- `UnitOrderService` → receiver dispatch and formation.
- `PlayerOrderInputHandler` → right-click priority and cancellation.
- `UnitActionsView` / presenter → buttons, availability, pending mode, retreat countdown.

## Implementation

1. Unify movement/attack dispatch and emit `OrderIssued` for feedback.
2. Add command contracts and one order lifecycle; deduplicate repeated AI orders.
3. Add AttackMove, Guard, WaypointMove, Hunt, Stop; wire 8 action buttons.
4. Add retreat countdown, station/default-zone destination, toggle cancellation.
5. Migrate enemy executor/commander to the same service; preserve stable formation slots.
6. Write acceptance tests; compile and persist changed Unity assets.

## Edge Cases

- Stop → clear order, target, waypoint placement, retreat countdown; idle does not chase.
- Repeated Retreat → retain running countdown.
- Guard target destroyed → Idle + clear order.
- Retreat countdown → current state continues; expiry → navigate without engagement; arrival → Idle, no despawn/battle end.
- Dead station → fixed default zone with `StartingOwner == S`; do not use capturable/current-owner random spawn position.
- Alt + right-click → append waypoints; Alt release → commit; Escape → discard.
- Enemy Stop → only ships with `CurrentOrder != None`; no per-tick idle target reset.

## Files

- [[Done/Features/SHIP_ACTIONS_PLAN - Research]] — command APIs, receiver matrix, state behavior, wiring, AI mapping, full acceptance criteria.
- Hardpoint targeting and per-type `ShipGroupUi` cards remain outside the recorded plan.
