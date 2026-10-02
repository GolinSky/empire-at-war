# Project TODOs & Backlog

## Active Tasks

### Features

- [ ] **Review ship abilities implementation**
  - **Plan**: [[TODOs/Features/Ship_Abilities_Plan|Ship Abilities Plan]]
  - **Status**: 2026-10-02 balance/descriptions updated: Boost Weapon Power = damage ×2 / reload ×0.3 / speed ×1; Assault = damage ×2 / reload ×0.5 / speed ×2; both active 7 s, then recovery 50 s. Unity live readback and saved asset verified; no new console errors; no automated tests or Play Mode run.
  - **Remaining**: implementation review and acceptance evidence; commit `dc869bbb` recorded review as needed.

- [ ] **Implement UI tooltip system**
  - **Plan**: [[TODOs/Features/Tooltip_System_Plan|Tooltip System Plan]]
  - **Research**: [[TODOs/Features/Tooltip_System_Plan - Research|Tooltip coverage catalog and codebase findings]]
  - **Rule**: only `ITooltipService` shows or hides tooltips; views → presenter → service; `UiController` is not bound to the service.
  - **Status**: Phases 0–8 implemented; pure service, saved MPUIKit UI, presenter integrations, and 13 per-unit matchup assets. Compilation and asset-reference inspection clean; sample tooltip visually checked.
  - **Remaining**: in-game acceptance (disabled cards, live refresh/rebind, fog/drag/HUD/route lifecycle, screen edges and teardown). EditMode tests authored; execute only on request.

- [ ] **Close settings audit follow-ups** (Wave 1)
  - **Plan**: [[TODOs/Features/Settings_Audit_Followups|Settings Audit Follow-ups]]; parent [[TODOs/Features/Settings_Implementation_Plan|Settings Implementation Plan]]
  - **Status**: ST1–ST7 implemented; prior Play Mode smoke check done. 2026-10-02: fullscreen Audio / Display / Controls / Camera Penpot design implemented in the prefab, including main-menu styling and selected-tab states. Import, script diagnostics, Inspector references and all four isolated previews checked; 21 bindings render in Controls. Live category/rebind interaction remains unverified.
  - **Remaining**: run the new settings EditMode tests (on request); manual restart-persistence, display Keep/Revert/timeout, and corrupt-file checks.

### Bugs

- [ ] **Fix scene-teardown pool errors and material leaks** (Wave 1)
  - **Plan**: [[TODOs/Bugs/Scene_Teardown_And_Material_Leaks|Scene Teardown and Material Leaks]]
  - **Status**: steps 1–5 in `38f3e681`; compile clean. **Remaining**: manual skirmish exit check and placement material-count check.

- [ ] **Replace silent null returns with fail-fast/Try APIs** (Wave 1)
  - **Plan**: [[TODOs/Bugs/Fail_Fast_Null_Returns|Fail-Fast Null Returns]]
  - **Status**: code + tests in `32c5f1a1`. **Remaining**: clean compile (blocked by other Wave 1 WIP) and manual skirmish check.

### Optimization

- [ ] **Optimize battle particle rendering**
  - **Plan**: [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan|Particle rendering optimization]]
  - **Research**: [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan - Research|2026-09-30 capture and prefab analysis]]
  - **Status**: Profiler export repaired; 2 regression tests passed; original recording recovered; 65 prefabs / 111 particle systems inspected. Jobs review confirms immediate completion, main-thread flood execution and per-Plan grid rebuilds.
  - **Remaining**: compare ~200 FPS battle start with ~12 FPS mid-battle; prioritize ship/navigation CPU growth and accumulating combat effects. Star reduction deferred pending isolation.

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

- [ ] **Coordinate audit remediation (parallel plans)**
  - **Plan**: [[TODOs/Refactoring/Audit_Remediation_Coordination|Audit Remediation Coordination]]: waves, file ownership, Unity lane.
  - **Remaining**: Wave 1 (6 plans) → Wave 2 (2 plans). Input system deferred.

- [ ] **Deduplicate capture/reinforcement squadron tally** (Wave 1)
  - **Plan**: [[TODOs/Refactoring/Capture_Reinforcement_Tally_Dedup|Capture Reinforcement Tally Dedup]]
  - **Remaining**: code + tests committed; manual capture/zone playtest pending.

- [ ] **Type unit request / reinforcement identity keys** (Wave 1)
  - **Plan**: [[TODOs/Refactoring/Unit_Request_Identity_Keys|Unit Request Identity Keys]]
  - **Remaining**: code committed (`cd7b6edb`), compiles clean; manual build-queue + all four reinforcement kinds playtest pending.

- [ ] **Remove runtime component lookups** (Wave 1)
  - **Plan**: [[TODOs/Refactoring/Explicit_Component_Binding|Explicit Component Binding]]

- [ ] **Split multi-type files and fix placement** (Wave 2)
  - **Plan**: [[TODOs/Refactoring/Type_File_Split_And_Placement|Type File Split and Placement]]
  - **Blocked**: Wave 1 merge; user decision on interface+impl pairs.

- [ ] **Apply injection and guard conventions** (Wave 2)
  - **Plan**: [[TODOs/Refactoring/Injection_And_Guard_Conventions|Injection and Guard Conventions]]
  - **Blocked**: Wave 1 merge; user decision on MonoBehaviour `[Inject]` style.

- [ ] **Refactor audio system (AudioService / Music / Ship SFX)**
  - **Plan**: [[TODOs/Refactoring/Audio_System_Refactor_Plan|Audio System Refactor Plan]]
  - **Rule**: `IAudioService` plays source+clip only; music in `IMusicService`; all ship SFX via `IShipSfxService.Try*` (may refuse).
  - **Status**: steps 1–7 implemented; Unity compile clean; shared clip data, pool, mixer, bindings, and persistence inspected.
  - **Remaining**: step 8 listening, ≥20-ship profiler, pause/music/exit acceptance. Open `MainMenuScene` has unsaved changes; no Play Mode or automated tests run.

- [ ] **Refactor camera system (input guard, smoothing util, scroll fix, cinematic data)**
  - **Plan**: [[TODOs/Refactoring/Camera_System_Refactor_Plan|Camera System Refactor Plan]]
  - **Rule**: `CameraService` owns the input-lock rule; `CameraInput` has no `enabled` fallback.
  - **Decision**: `VelocitySmoothing` stays in `Assets/Scripts/Components/Utils/`.
  - **Status**: sections 1–4 implemented; Unity compile clean; asset values, GUIDs and Addressables entry verified.
  - **Remaining**: manual lock/release, zoom/invert/rebind and cinematic acceptance; open `MainMenuScene` has unsaved changes. No automated tests run.

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

- [x] **Resolve codebase audit 2026-09-27**
  - **Plan**: [[Done/Refactoring/Codebase Audit 2026-09-27/00 Overview|Codebase audit overview]]
  - **Completed**: 2026-09-30. The original 17 fixes are implemented. The follow-up findings from [[Done/Refactoring/Codebase Audit 2026-09-27/11 Re-audit 2026-09-30|the re-audit]] are superseded by the Audit Remediation plans.
  - **Verification**: live-source checks recorded in notes 09–11; no tests run in the re-audit passes.

- [x] **Fix Project Organization & Asset Naming Defects**
  - **Plan**: [[Done/Refactoring/Project_Organization_Remediation_Plan|Project Organization & Asset Naming Remediation Plan]]
  - **Research**: [[Done/Refactoring/Project_Organization_Remediation_Plan - Research|Full Audit & Naming Mapping Catalog]]
  - **Completed**: 2026-09-30; implementation `3dcee4a6`.
  - **Verification**: all 736 manifest target paths/GUIDs match; additional SceneContext prefab GUID matches; three Resources bootstrap assets remain.
