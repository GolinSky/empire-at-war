# Project TODOs & Architecture Backlog

## 🛠️ UI Refactoring

- [ ] **Fix UI Service (Decouple Gameplay Services from UI)**
  - **Task**: Move UI creation, UI prefab instantiation, and `IUiService` dependencies out of gameplay services into UI prefabs / `<Feature>UiController` presenters.
  - **Reference**: [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]]
  - **Guidelines**:
    - Gameplay services (`<Feature>Service`) must NOT depend on `IUiService`, concrete `BaseUi` implementations, or UI-only types.
    - Move UI creation logic and lifecycle orchestration to UI prefabs (`Assets/Prefabs/Ui/`) and `<Feature>UiController` / presenters.
    - Keep Models pure C# and decouple UI events.
