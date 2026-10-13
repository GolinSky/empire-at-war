# UI Code Build Rule

- Scope: new UI features in Empire At War.
- Adapted from [SoulsLike UI Code Build Guide](obsidian://open?vault=SoulsLikeGameVault&file=Knowledge%2FGuides%2FUI%2FUI%20Code%20Build%20Guide).
- Before UI changes: read [[Rules/UI_UX_GUIDELINES]].
- Before new asset folders: read [[Architecture/PROJECT_ORGANIZATION]].
- Mixed existing responsibilities → [[Architecture/UI_REFACTORING_PLAYBOOK]].

## 1. Plan the responsibilities

- Before implementation: list Model, View, and Presenter responsibilities.

- **Model:** Pure C# runtime state, rules, and C# events.
  - Keep Unity references, serialization, and prefab creation out of it.
- **View:** A top-level `<Feature>Ui : BaseUi` or existing project equivalent.
  - Render model state and forward input.
  - Use explicit `[SerializeField]` Inspector references for Unity components; serialized field names are unprefixed camelCase.
- **Presenter:** `I<Feature>Presenter` defines UI intent.
  - `<Feature>UiController` implements it, creates the view through `IUiService`, supplies its dependencies, and owns initialization and disposal.
  - Forward gameplay requests to a focused service.
- **Data and service when needed:** Put serialized configuration in a `Data` asset and gameplay or application flow in a service.
  - Do not create either solely to satisfy a template.

- One top-level C# type per file.
- Type-first `Assets/Scripts` layout: `Entities`, `Components`, `Services`.
- Do not import SoulsLike namespaces, `Assets/Scripts/Ui`, `CustomButton`, or VContainer patterns.

## 2. Wire the view lifecycle

1. Define `I<Feature>Presenter` only when the screen has user intents. Controllers use concrete screen views; world-space and shared widget interfaces remain where useful.
2. Create the view with `BaseUi` and a serialized `CanvasGroup` assigned in the Inspector.
   - Keep the view limited to rendering and input forwarding.
3. In `<Feature>UiController`, call `CreateUi<FeatureUi>()`; `IUiService` loads `typeof(TUi).Name` and instantiates the component under `IUiPlacement.GetParent<TUi>()`.
   - `UiPlacement` owns the single type-to-parent map; controllers never choose slots.
4. Supply the model, presenter, and data through explicit methods before `Initialize()`.
   - Test the setup order and required prefab references.
   - Subscribe to controls and model events in `Initialize()`; remove the same subscriptions in `Dispose()`.
   - Have `OnDestroy()` call `Dispose()` as a safety net.
5. Register the model, controller, service, and data in the owning **Zenject installer** with the lifetime the feature requires.
   - Do not make a prefab resolve feature-specific dependencies from a different container.

```text
CreateUi<TUi> -> AssignPresenter -> explicit model/data setup -> Initialize -> Show
```

- Screen identity and prefab key: concrete view type and `typeof(TUi).Name`.
- No screen subcontainers, screen-interface casts or `[Inject]` on screen views.
- Colocate `<Feature>Ui`, `I<Feature>Presenter` and `<Feature>UiController` under `Assets/Scripts/Entities/<Feature>/` (nested feature folders allowed); normalize their namespaces. Keep models, shared components, Services, Editor and Tests separate.
- Route controllers use `<Feature>RouteUiController` and `IUiRoute`; passive screens need no presenter interface.
- Scene-placed `LoadingUi` is owned by its local controller and does not depend on scene-bound menu/battle UI services.

## 3. Create the prefab

- Save reusable UI prefabs under `Assets/Prefabs/Ui/<Feature>/` where that feature folder fits the existing layout.
- Attach the top-level view to its intended root.
  - Assign its `CanvasGroup` and every serialized control, text, and icon reference in the Inspector.
- Keep child widgets as focused `MonoBehaviour` components unless they are independent top-level views.
- Apply [[Rules/UI_UX_GUIDELINES]] for buttons, color, and preservation of existing icons and panel styling.
- `Assets/ThirdParty/MPUIKit` is optional; use it when needed, without requiring conversion of standard UI components.
- Create or change prefabs with Unity-aware tooling.
  - Preserve metadata and serialized references; complete Unity import and save verification as required by the repository's asset-persistence rules.

## 4. Register the Addressable

- Add the UI prefab to the existing `Ui` Addressables group without changing `Assets/AddressableAssetsData` folder structure.
- Ensure the requested key `<Feature>Ui` resolves.
  - Use that direct Addressable address or the existing `Assets/Settings/AssetMappingData.asset` mapping from the key to the prefab's `AssetReference`.
- This project has one `assetMappings` dictionary in `AssetMappingData`; it does not have the source guide's `uiMappings` dictionary.
- Verify the prefab resolves through `IAssetService` and the view component matches the requested concrete view type.

## 5. Verify the completed UI

- Verify Inspector references, Addressable key/mapping, Zenject bindings.
- Verify initialization/disposal, input forwarding, model-to-view updates.
- Verify Unity import and Console state.
- Run automated tests only on explicit user request.
