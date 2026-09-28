# Ship and Squadron Entity Analysis

## Scope

- Static review: 2026-09-25; revision `0efa88cb`; branch `fix/battle-related-bugs`.
- Findings describe code before the 2026-09-26 implementation.
- Execution status: [[TODOs/Ship_Squadron_Entity_Simplification_Plan]].

## Rules

- Between entities → `IEntity`, `EntityLocator`, facades.
- Own transform → injected `EntityBindType.ViewTransform`.
- Another entity's transform → cached transform facade; never add Unity types to `IEntity`.
- Inside entity → entity commands components; components report events/read-only observations.
- Components may read sibling state; never command siblings or inject their owner.
- Ship state transitions have one owner; states report `IsComplete`.

## Findings

- `IShipMoveComponent`: 15 members mixing movement, selection, radar, modifiers, wiring.
- `HealthModel.Transform`: 38 cross-entity reads; replace with transform facades.
- Ship transitions came from 4 locations; order/state had duplicated truth and mapping.
- Flee should override the current order from one place.
- Squadron: approximately 300 lines; keep readable switch/pilot flow rather than forcing ship state classes.

## TODO

- Verify shared order semantics: duplicate Move/AttackMove/Retreat; formation offsets.
- Review `IHardPointModel.Transform` separately.
- Cache radius scans only if profiling justifies it.

## Files

- [[SHIP_SQUADRON_ENTITY_ANALYSIS - Research]] — dependency tables, 38-read breakdown, state analysis, source evidence.
- [[SCRIPTS_CODE_AUDIT_2026-09-12]], [[TODOs/Ship_Movement_Simplification_Plan]], [[TODOs/SHIP_ACTIONS_PLAN]].
