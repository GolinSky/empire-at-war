---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: implemented
scope: read-only source review
updated: "2026-09-27\r"
category: Refactoring
---

# Reuse — UI assets and extraction rules

> [!info] Implementation update
> R3 implemented using injected IShipIconProvider backed by existing ShipUiData; selection model and observer no longer expose Sprite lookup.
> See [[TODOs/Refactoring/Codebase Audit 2026-09-27/09 Implementation Results|implementation results and verification]]. Evidence/line numbers below describe the original audit snapshot unless marked implemented.

- [[TODOs/Refactoring/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

## R3 — ShipUiModel doubles as an icon repository

- **Priority:** P2 · **Confidence:** confirmed MVP boundary issue.

- **Evidence:** [Assets/Scripts/Entities/ShipUi/Model/ShipUiModel.cs:28](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/ShipUi/Model/ShipUiModel.cs#L28) returns Unity Sprites through ShipUiData; [Assets/Scripts/Entities/ShipUi/Model/IShipUiModelObserver.cs:17](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/ShipUi/Model/IShipUiModelObserver.cs#L17) exposes that Unity type.
- ShipUi and ShipGroupUi both obtain icons through this model.

- **Solution:** keep selected ship/squadron/ability state in the pure model.
- Reuse ShipUiData behind a small icon-provider boundary consumed by the presenter/view setup.
- Pass icon presentation data to views.
- Do not duplicate the existing icon database.

- **Reuse:** selected-unit details and grouped-unit UI share the same icon lookup without depending on selection-state internals.

- **Future verification:** identical icons for ship and squadron types, group display and no-selection states; model contracts become Unity-free.
- Any later UI edit must first read the full UI_UX_GUIDELINES note and, for new UI features, UI_CODE_BUILD_GUIDE.

## Where extracted code should live

| Kind | Examples from this audit | Placement principle |
|---|---|---|
| Stateless geometry/adaptation | D1 coordinate adapter, D2 segment math | Narrow Components/Utils helper; mark Unity-bound adapters clearly |
| Shared state/lifetime or scene queries | C3 combat batch, D3 ship population | Focused Services domain; explicit dependencies |
| Domain rules | R1 accuracy, C1 time policy | Pure model near its feature |
| Unity presentation/assets | R2 layer resolution, R3 icons, C4 fog rendering | Adapter/view/presenter boundary |
| Offline authoring | D6 PCM/WAV encoder | Tools/Audio |

- Extract when there are actual shared consumers or a clear dependency boundary.
- Do not move every private method into Utils.
- Keep compact/battle formation policies, feature visibility rules, and entity lifecycle ownership distinct.

## Suggested implementation order

1. Clarify C1 semantics before changing code.
2. Take bounded reuse wins: D1, D3, D5, D6.
3. Improve model boundaries: R1–R3.
4. Refactor larger systems one at a time: C2–C4.
5. Perform L1–L4 cleanup independently of behavior changes.

- The original audit performed no mutations.
- Implementation now exists as recorded in the linked results; unexecuted checks below remain future acceptance criteria.
