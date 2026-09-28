# UI Refactoring Playbook

## Goal

- Move feature UI to explicit MVP responsibilities.
- Reference implementation: `ReinforcementUi`.
- Read [[PROJECT_ORGANIZATION]] before applying placement changes.

## Responsibilities

- `<Feature>Data` → serialized configuration.
- `<Feature>Model : PureModel` → runtime state, rules, C# events.
- `<Feature>Service` → gameplay requests; no UI dependencies.
- `<Feature>UiController` → presenter, UI factory/setup/disposal.
- `<Feature>Ui : BaseUi` → rendering and input forwarding.

## Implementation

1. Record failure, dependencies, asset paths/GUIDs, repository and Addressables mappings.
2. Create `<Feature>Data`; copy and compare every serialized value/reference through Unity tooling.
3. Remap repository/Addressables; verify import before deleting the old model asset.
4. Convert model to constructor-injected `PureModel`.
5. Move gameplay into service; rename UI-only `I<Feature>Command` to `I<Feature>Presenter`.
6. Use non-generic `BaseUi`; supply model/presenter/data before `Initialize()`.
7. Bind data/model/service/controller in the feature scope; verify creation, input, events, disposal.

## Rules

- Setup order: `CreateUi` → `SetModel` / `SetPresenter` / `SetData` → `Initialize`.
- No feature property injection during UI prefab construction.
- Subscribe in `Initialize`; unsubscribe symmetrically in `Dispose`; call `Dispose` from `OnDestroy`.
- Disposal must be safe more than once; do not initialize in `Awake`/`Start`.
- Keep dictionary wrappers private; expose focused configuration lookups.
- Preserve `Assets/AddressableAssetsData` structure.
- Run automated tests only on explicit user request.

## Edge Cases

- Missing Inspector reference/local rendering bug with valid MVP → fix locally.
- Any unverified value/reference/GUID/mapping → stop asset migration.
- Unrelated scene binding failure → record blocker; preserve task scope.
- Generic `BaseUi<TModel, TCommand>` → hidden container-scope dependency; replace with explicit setup.

## Files

- [[UI_REFACTORING_PLAYBOOK - Research]] — code templates, field classification, Reinforcement graph, detailed checklist and task record.
