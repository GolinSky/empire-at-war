# Project TODOs & Backlog

## 📋 Active Tasks

- [ ] **Fix UI Service (Decouple Gameplay Services from UI)**
  - **Task**: Move UI creation, UI prefab instantiation, and `IUiService` dependencies out of gameplay services into UI prefabs / `<Feature>UiController` presenters.
  - **Details**: See [[UI_Service_Refactoring|UI Service Refactoring]]
  - **Reference**: [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]]

---

## 📌 Architecture Backlog

- [ ] Audit gameplay services for direct `IUiService` or `BaseUi` references.
- [ ] Migrate UI creation calls from services into feature UI controllers / presenters.
- [ ] Ensure all feature data container classes (`<Feature>Data`) inherit from `Data` ScriptableObject.
