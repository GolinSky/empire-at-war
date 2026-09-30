---
category: Refactoring
status: todo
created: 2026-09-30
tags:
  - code-audit
  - coordination
---
# Audit Remediation — Coordination

## Goal
- Fix the open findings of the 2026-09-30 re-audit through parallel, file-disjoint agent plans.
- Source audit (archived): [[Done/Refactoring/Codebase Audit 2026-09-27/11 Re-audit 2026-09-30|11 Re-audit 2026-09-30]]. Baseline HEAD `ebb174ed`.

## Plans

| Wave | Plan | Findings | Unity Editor | Category |
|---|---|---|---|---|
| 1 | [[TODOs/Bugs/Scene_Teardown_And_Material_Leaks]] | RT1, RT2, RT3, CO4 (`UnitSpawnView`) | yes (spawn prefabs) | Bugs |
| 1 | [[TODOs/Bugs/Fail_Fast_Null_Returns]] | CO1, CO4 (`AddressableAssetService`) | no | Bugs |
| 1 | [[TODOs/Refactoring/Capture_Reinforcement_Tally_Dedup]] | CO2 | no | Refactoring |
| 1 | [[TODOs/Refactoring/Unit_Request_Identity_Keys]] | CO3 | yes (1 script move) | Refactoring |
| 1 | [[TODOs/Refactoring/Explicit_Component_Binding]] | CO4 (rest) | yes (prefabs) | Refactoring |
| 1 | [[TODOs/Features/Settings_Audit_Followups]] | ST1–ST7 | no | Features |
| 2 | [[TODOs/Refactoring/Type_File_Split_And_Placement]] | CO6, CO7 | yes (new scripts, moves) | Refactoring |
| 2 | [[TODOs/Refactoring/Injection_And_Guard_Conventions]] | CO5, CO8 | no | Refactoring |

## Rules
- One agent per plan. An agent edits only files in its plan's `Files` section. If a change needs another file → stop and report it as a blocker; do not edit it.
- Wave 2 starts after every Wave 1 plan is merged: it touches files owned by Wave 1 (`Squadron.cs`, ship states, `ReinforcementUi.cs`, `ReinforcementZonesSystem.cs`).
- **Unity lane:** only one agent at a time drives the Unity Editor (prefab edits, `AssetDatabase` moves, refresh/recompile). Code-only steps run freely. Hold the lane only for the asset step.
- **Compile isolation:** parallel agents in one working tree break each other's compiles. Choose before launch: one git worktree per plan, or shared tree with sequential compile checks. (User decision; agents must not create branches on their own.)
- Tests: write/update tests as listed; run them only when the user explicitly asks (AGENTS policy). Never claim unrun tests passed.
- Each plan follows the Obsidian plan lifecycle: status updates, checkboxes, move to `Done/<Category>/` on completion.

## Out of scope (not planned)
- **Input system** (user: do not touch for now): IN1 marquee-stuck-on-lock, IN5 map ownership split, IN6 `"UI"` layer string ×3, IN7 private `ToNumerics`, IN8 `SelectionInput` namespace.
- IN2–IN4 closed by `5ae00d3b` (rebinding wired to the settings UI, Escape cancel, cross-map conflicts); not re-verified because input is out of scope.
- CO9 direct `UnityEngine.Random` in AI: no test currently needs determinism.
- CO10 large files (`CombatAttackCoordinator` 506, `BattlePerformanceCapture` 500, `ShipNavigationService` 487, `WeaponComponent` 471): legacy; refactor only on request.

## File ownership (Wave 1)

| Plan | Owns |
|---|---|
| Teardown/Leaks | `Services/UnitWreck/UnitWreckService.cs`, `Services/UnitExplosion/UnitExplosionService.cs`, `Entities/Reinforcement/UnitSpawnView.cs` + spawn preview prefabs, `Components/ViewComponents/FogOfWarSystem.cs` |
| Fail-fast | `Services/SceneService/SceneData.cs`, `Services/Repository/AddressableAssetService.cs`, `Components/BaseSystem/IAssetService.cs`, `Components/AttackComponent/AttackDataFactory.cs` + its 8 callers, `Services/ShipAbilities/ShipAbilityService.cs` |
| Tally dedup | `Services/CaptureSites/CaptureSitesSystem.cs`, `Services/ReinforcementZones/ReinforcementZonesSystem.cs`, `Services/Ship/ShipPopulation.cs`, `Tests/Editor/AuditSharedOperationsTests.cs` |
| Identity keys | `Entities/Faction/Ui/BuildPipelineView.cs`, `Entities/Reinforcement/ReinforcementUi.cs`, `Services/Reinforcement/ReinforcementService.cs`, `Entities/EnemyFaction/Models/UnitLimitKey.cs` and its users |
| Component binding | `Components/Ui/Base/UiInstaller.cs`, `Components/Utils/DrawCircle.cs`, `Components/Utils/Ui/SafeAreaHelper.cs`, `Entities/ReinforcementZones/ReinforcementZoneView.cs`, `Services/SceneContext/ViewInstallers/Base/StaticViewInstaller.cs`, `Components/Utils/DebugRangeCircle.cs` + affected prefabs |
| Settings | `Services/Settings/*`, `Services/Graphics/*`, `Entities/MainMenu/Settings/*`, new settings tests. Not `Services/Input/*`. |

## TODO
- [ ] Wave 1: all six plans done and merged.
- [ ] Wave 2: both plans done and merged.
- [ ] Close this note: move to `Done/Refactoring/` with the outcome.
