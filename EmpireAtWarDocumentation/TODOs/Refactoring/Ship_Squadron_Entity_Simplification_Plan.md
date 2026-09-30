---
category: Refactoring
status: in-progress
---
# Ship and Squadron Entity Simplification Plan

## Lifecycle Review

- Reviewed: 2026-09-30; implementation recorded in `e830e120`. The plan still requires behavior verification for its documented changes; keep active.

## Recorded Status

- Source: [[Architecture/SHIP_SQUADRON_ENTITY_ANALYSIS]].
- Plan: 2026-09-25; implemented 2026-09-26 on `fix/battle-related-bugs`.
- Recorded working tree: uncommitted; Unity compilation/Console had zero errors.
- Tests updated to compile; automated tests and Play Mode were not run.

## Rules

- Small, independent, behavior-preserving changes.
- Each component remains one `MonoComponent`; no Presenter/View split.
- No frameworks, event buses, or domain-service layers.
- Root `AGENTS.md` → Entity Communication is authoritative.

## Decisions

- Cross-entity transform access → `IEntityTransformFacade`; `IEntity` remains Unity-free.
- Component notifications → C# events/read-only observers; entity owns wiring.
- Ship state changes → one `ShipOrderRunner`; states report completion.
- Keep readable Squadron switch/pilot flow; unify shared order semantics.

## TODO

- Validate recorded behavior changes in Play Mode when authorized.
- Recheck live source and commit state before resuming historical steps.

## Files

- [[TODOs/Refactoring/Ship_Squadron_Entity_Simplification_Plan - Research]] — step table, user decisions, behavior notes, modified tests and execution record.
