# TODO: Fix UI Service Architecture

## Goal

- Move UI creation and prefab instantiation out of gameplay services.
- Remove gameplay-service dependencies on `IUiService`.
- UI prefabs / `<Feature>UiController` presenters own UI lifecycle.
- Follow MVP: [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]].

## Requirements

1. **Services must remain pure gameplay/business logic**:
   - `<Feature>Service` must not depend on `IUiService`, concrete `BaseUi` implementations, or UI-only types.
   - Gameplay services must not instantiate UI prefabs or handle UI event rendering.
2. **Presenters & Controllers handle UI lifecycle**:
   - `<Feature>UiController` creates the UI, passes all dependencies through methods, and manages lifecycle.
   - Presenter coordinates Model and View, subscribes to Model events (`System.Action`), and updates the View.
3. **Views handle rendering only**:
   - `<Feature>Ui : BaseUi` renders state and forwards user input.
     - Contains no gameplay rules.

## Reference

- [[UI_REFACTORING_PLAYBOOK|UI Refactoring Playbook]]
- [[PROJECT_ORGANIZATION|Project Organization Guide]]
