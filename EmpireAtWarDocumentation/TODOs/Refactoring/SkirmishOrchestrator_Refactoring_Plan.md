---
tags:
  - plan
  - refactor
  - ui
  - core-game
status: in-progress
created: 2026-09-23
executor: codex
category: Refactoring
---

# SkirmishOrchestrator Refactoring Plan

## Lifecycle Review

- Reviewed: 2026-09-30; the earlier proposed status is stale. Live source contains `SkirmishSessionModel`, `CoreGameUiController`, the focused orchestrator, and test sources; implementation history includes `00cb331a`.
- Keep active until the plan's route, visibility, teardown, and prefab acceptance checks are reconciled. Historical pending decisions are not current completion evidence.

## Status

- Historical proposal: 2026-09-23. Implementation now exists; see Lifecycle Review for current verification gaps.
- Read [[Rules/UI_CODE_BUILD_GUIDE]], [[Rules/UI_UX_GUIDELINES]], [[Architecture/UI_REFACTORING_PLAYBOOK]], and root `AGENTS.md` before implementation.

## Responsibilities

- `SkirmishSessionModel : PureModel` → `GameTimeMode`, `IsBattleEnded`, C# events.
- `SkirmishOrchestrator` → camera, time, user/battle events, exit; no UI types.
- `CoreGameUiController` → UI creation/disposal, route buffering, content visibility, end-game presenter ownership.
- `CoreGameUi : BaseUi, ICoreGameUi` → render state and forward input; explicit setup.

## Implementation

1. Add model/contracts, one type per file.
2. Refactor passive view; `SetModel` → `SetPresenter` → `Initialize`; render initial mode.
3. Move UI/route ownership to `CoreGameUiController`.
4. Slim orchestrator; move time-scale initialization from constructor to `Initialize`.
5. Bind controller in `SkirmishMainInstaller`; it alone implements `ISkirmishRouteNavigation`.
6. Verify no legacy references, then remove `CoreGameData` type/asset/mappings and `ICoreGameCommand`.
7. Preserve prefab GUID/serialized fields; verify routes, content, teardown; write tests without unrequested execution.

## Important Values

- Time scales: Common 1; SpeedUp 4; Pause 0.
- Target orchestrator: approximately 150 lines.
- Controller estimate: 230–260 lines; report size, split route registry only with approval.
- Keep repository key `CoreGameUi`; remove only legacy `CoreGameData` mapping/entry.

## Edge Cases

- Routes can register/change state before UI creation; activate buffered state afterward.
- Missing stored route state → active; Reinforcement explicitly starts hidden.
- Menu state applies Pause without changing stored requested mode; preserve this distinction.
- Battle end → flag + Pause; time/reinforcement controls become no-ops.
- Exit → Common before `IGameCommand.ExitGame()`.
- Initial selection null → still update group layout/content visibility.
- `RegisterRoute` retains its public argument check; no constructor null guards.

## Decisions Pending

- `SkirmishSessionModel` naming versus narrower time model + battle signal.
- Controller-owned UI visibility; optional `SkirmishUiRouteRegistry` split.
- `EndGamePresenter` keeps its file placement; only its owner changes.

## Files

- [[TODOs/Refactoring/SkirmishOrchestrator_Refactoring_Plan - Research]] — exact contracts, truth table, route consumers, asset GUIDs, teardown and acceptance checks.
