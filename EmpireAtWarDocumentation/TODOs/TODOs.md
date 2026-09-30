# Project TODOs & Backlog

## Active Tasks

### Features

- [ ] **Review ship abilities implementation**
  - **Plan**: [[TODOs/Features/Ship_Abilities_Plan|Ship Abilities Plan]]
  - **Remaining**: implementation review and acceptance evidence; commit `dc869bbb` recorded review as needed.

### Optimization

- [ ] **Optimize battle attacks and projectile reuse**
  - **Plan**: [[TODOs/Optimization/Battle_Attack_Optimization_Plan|Phased attack, pooling, Jobs + Burst, and instancing plan]]
  - **Execution**: Complete and analyze each phase before advancing; start with readable attack states and busy/cancellation fixes.
  - **Remaining**: manual parity, matched performance captures, measured Jobs thresholds, and actual instanced-draw evidence.

### Refactoring

- [ ] **Fix UI Service (Decouple Gameplay Services from UI)**
  - **Task**: Move UI creation, UI prefab instantiation, and `IUiService` dependencies out of gameplay services into UI prefabs / `<Feature>UiController` presenters.
  - **Details**: See [[TODOs/Refactoring/UI_Service_Refactoring|UI Service Refactoring]]
  - **Reference**: [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]]

- [ ] **Finish ship movement simplification verification and remaining phases**
  - **Plan**: [[TODOs/Refactoring/Ship_Movement_Simplification_Plan|Ship Movement Simplification Plan]]
  - **Remaining**: reconcile the recorded initialization-order retry and later phases against current behavior.

- [ ] **Verify ship and squadron entity simplification behavior**
  - **Plan**: [[TODOs/Refactoring/Ship_Squadron_Entity_Simplification_Plan|Ship and Squadron Entity Simplification Plan]]
  - **Remaining**: recorded behavior checks; implementation and compilation were previously recorded as complete.

- [ ] **Reconcile SkirmishOrchestrator refactor acceptance**
  - **Plan**: [[TODOs/Refactoring/SkirmishOrchestrator_Refactoring_Plan|SkirmishOrchestrator Refactoring Plan]]
  - **Remaining**: route, visibility, teardown, and prefab verification; implementation exists despite the historical proposed status.

- [ ] **Approve Ship Lit Autodesk conversion visuals**
  - **Plan**: [[TODOs/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan|Ship Lit Autodesk Material Conversion Plan]]
  - **Blocked**: recorded user visual approval of `Logs/ShipLitConversion/2026-09-28/review.html`.

- [ ] **Resolve codebase audit follow-up findings**
  - **Plan**: [[TODOs/Refactoring/Codebase Audit 2026-09-27/00 Overview|Codebase audit overview]]
  - **Remaining**: [[TODOs/Refactoring/Codebase Audit 2026-09-27/11 Re-audit 2026-09-30|Current re-audit findings]]; original 17 fixes are implemented.

### Tooling

- [ ] **Set up Jenkins local Windows builds**
  - **Plan**: private local documentation outside this vault; excluded from Git.

## Architecture Backlog

- [ ] Audit gameplay services for direct `IUiService` or `BaseUi` references.
- [ ] Migrate UI creation calls from services into feature UI controllers / presenters.
- [ ] Ensure all feature data container classes (`<Feature>Data`) inherit from `Data` ScriptableObject.

## Done

### Features

- [x] **Implement unit actions (ship orders)**
  - **Plan**: [[Done/Features/SHIP_ACTIONS_PLAN|Unit Actions (Ship Orders) implementation plan]]
  - **Completed**: 2026-09-30 closeout; implementation `ea3b8759`, subsequent retreat/UI changes `767da64c`.
  - **Verification**: live order APIs, action-prefab bindings, and acceptance-test sources checked; no tests or Play Mode run in this review.

### Refactoring

- [x] **Fix Project Organization & Asset Naming Defects**
  - **Plan**: [[Done/Refactoring/Project_Organization_Remediation_Plan|Project Organization & Asset Naming Remediation Plan]]
  - **Research**: [[Done/Refactoring/Project_Organization_Remediation_Plan - Research|Full Audit & Naming Mapping Catalog]]
  - **Completed**: 2026-09-30; implementation `3dcee4a6`.
  - **Verification**: all 736 manifest target paths/GUIDs match; additional SceneContext prefab GUID matches; three Resources bootstrap assets remain.
