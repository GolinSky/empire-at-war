---
type: technical-reference
system:
  - tooling
  - balance
verified: 2026-10-20
verification: source-and-repository-manuals
---
# GameDesign Tools

## Rules

- Balance Editor edits a staged draft; gameplay assets are written through Review/Apply. A displayed draft is not the current saved game balance.
- Battle Simulator reads live assets or the Balance Editor's saved draft into a read-only combat snapshot.
- Simulator results do not replace full battle acceptance or establish balanced gameplay. Its movement, targeting and ability assumptions must be read before interpreting results.
- Completed implementation plans remain historical evidence; operational guides and live tool source own current usage.

## Implementation

1. Open the Editor Hub from `Tools/Empire At War/Editor Hub`.
2. Use `Game Design/Balance Editor` for unit comparisons, shared-data inspection, draft edits and review/apply.
3. Use `Game Design/Battle Simulator` for seeded fleet comparisons; choose live or saved-draft data explicitly.
4. Review `Tools/BattleSimulator/README.md` for timing, assumptions, result interpretation and export details.

## Important Values

- Source inspection: 2026-10-20; no tools were executed or assets applied for this documentation task.
- Simulator defaults documented beside its source: fixed step `0.05 s`, default time limit `600 s`.
- Export location: `Library/BattleSimulator/<timestamp-id>/`; Markdown, summary, units, timeline and events CSV.
- Shared weapon/ability data can affect multiple units; inspect ownership before applying a balance draft.

## Files

- `Assets/Scripts/Editor/Balance/BalanceEditorWindow.cs` — draft loading, view routing and Apply.
- `Assets/Scripts/Editor/Balance/BalanceApplyService.cs` — scoped apply/restore.
- `Assets/Scripts/Editor/BattleSimulator/BattleSimulatorWindow.cs` — snapshot and simulation execution.
- `Tools/BattleSimulator/README.md`, `Tools/EditorHub/README.md` — canonical operational manuals.
- [[Done/Tooling/GameDesign_Balance_Editor]]; [[Done/Tooling/Battle_Logic_Simulator]] — historical delivery/verification.
- [[GameDesign/00_GameDesign]]; [[GameDesign/Combat]]; [[GameDesign/Units_And_Factions]].
