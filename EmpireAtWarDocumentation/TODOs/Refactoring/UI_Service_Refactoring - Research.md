---
category: Refactoring
status: in-progress
updated: 2026-10-13
---
# UI Rework — Research

## Goal

- Evidence for [[TODOs/Refactoring/UI_Service_Refactoring|UI Rework Before VContainer]].
- Snapshot: 2026-10-13; checkout HEAD `88635ce69` with pre-existing local changes. Live source outranks historical notes.

## Decision

- Preserve the existing plan filename; expand its unfinished service/UI separation scope into the requested UI-first rework.
- Concrete view classes are intentional. Presenter intent interfaces remain, as in the user's `IMainMenuPresenter` example.
- Do not copy SoulsLikeTemplate's `IBaseUi`, component searches, empty `IStartable.Start()`, redundant visibility flags or resolver-based factory behavior.
- User decisions 2026-10-13: `UiPlacement` service owns the `Type → parent` map; only screen interfaces removed (world-space views + shared HUD widgets stay); view intents only via presenter interface; route screens → `<Feature>RouteUiController` on `IUiRoute`; `FadeService` → `FadeUiController`; Loading in scope; folders + namespaces normalized now; delete `MainMenuModel`.

## Implementation

### Current infrastructure

| Source under Assets/Scripts | Observed behavior | Planned replacement |
|---|---|---|
| `BaseSystem/Ui/BaseUi.cs:8` | Serialized CanvasGroup and Show/Hide; generic subclass injects model at :44 | Minimal non-generic BaseUi; explicit view setup |
| `BaseSystem/Ui/UiController.cs:7` | UI service + cancel router, focus/unfocus | Preserve cancel behavior; add typed creation convenience |
| `BaseSystem/Ui/UiService.cs:41` | Enum creation, per-screen placement, CoreGame/Faction casts | Typed creation; parent from `UiPlacement` `Type → Transform` map; feature setup in controller |
| `BaseSystem/Ui/UiFactory.cs:6` | Zenject PlaceholderFactory<UiType, Transform, BaseUi> | Concrete typed factory |
| `BaseSystem/Ui/UiInstaller.cs:27` | Loads `<UiType>Ui`, instantiates under context, binds interfaces | Typed prefab creation; controller-owned view lifecycle |
| `Services/Repository/AddressableAssetService.cs:21` | LoadComponent validates the root via TryGetComponent | Retain loading-boundary validation; no dependency discovery or lookup on created UI |
| `Services/Repository/Data/AssetMappingData.cs:10` | One `assetMappings` dictionary, fallback to address | Reuse this mapping; source template's separate UI mapping API does not exist here |

- `UiType.cs`: 20 members; `Interaction` has no placement branch. The 19 other identities appear in `UiService.GetPlacement`.
- Generic `BaseUi<TModel>` currently has one consumer: `MiniMapUi`.
- Required view DI to replace: minimap model + `IPlayerColors`; `ShipUi` / `ShipGroupUi` → `IShipIconProvider`; `TooltipUi` → `TooltipIconData` + `TooltipSettings`.
- Existing creation helper `InstallerExtensions.InstantiatePrefabForInstall` delays activation and queues injectable descendants. UI must explicitly preserve setup ordering without depending on that helper.
- Verified 2026-10-13: all 19 `BaseUi` subclass names equal the current `"{UiType}Ui"` prefab key → `typeof(TUi).Name` needs no asset changes.
- `UiInstaller` binds each view's interfaces in a per-screen `GameObjectContext`. Only `MiniMapUi` uses that for Zenject lifecycle (`IInitializable`, `ILateDisposable`) → must become explicit controller calls.
- No UI prefab descendants use `[Inject]`; only `EndGameUi` has `Awake` (self-contained listener).
- Screen intent events to replace with presenters: `EndGameUi.ReturnToMenuRequested`, `MiniMapUi.OnCameraMoveRequested/OnMoveOrderRequested`; `HeroUi.Disposed` is lifecycle → controller teardown.
- `MainMenuModel`/`IMainMenuModel` are empty; `MainMenuUi` stores but never reads the model.
- View-interface test fakes: only `UnitActionsPresenterTests.FakeView : IUnitActionsView` (kept interface). `HeroUiControllerLifecycleTests` already uses the real prefab.
- Two unrelated `ISkirmishRouteNavigation` exist (`Entities/MainMenu/Skirmish/`, `Services/UiRouting/`); renaming the MainMenu one to `ISkirmishPresenter` removes the clash.

### Placement parity

| Concrete view(s) | Current parent / special behavior |
|---|---|
| CoreGameUi, MainMenuUi, SkirmishUi, ReinforcementUi, HeroUi | `defaultCanvas.transform`; CoreGame first sibling |
| EconomyUi | `economySlot` |
| FactionUi | `factionLayout.Viewport`; receives `factionLayout` |
| ShipUi, ShipGroupUi | `shipLayout.Content` |
| MiniMapUi | `miniMapSlot` |
| ShipBuildUi | `buildPipelineSlot` |
| FpsUi | `dynamicCanvas.transform` |
| UnitOrderFeedbackUi | `feedbackCanvas.transform` |
| CaptureSiteBuildUi | `contextualSlot` |
| EndGameUi | `endGameSlot` |
| PauseMenuUi, SettingsUi | `modalSlot` |
| TooltipUi | `tooltipSlot` |
| FadeUi | `fadeSlot` |

- `UiService.SetHudVisible` disables default/dynamic/feedback canvases and contextual slot; popup UI stays independent.
- CoreGame's `ContextualPopup` is reparented to `contextualSlot`.
- `SetContentLayout` / `SetContentVisible` switch faction vs ship content and group layouts; preserve these separate responsibilities.

### Feature conversion inventory

| Existing source group | Actions |
|---|---|
| `Entities/MainMenu/Main/` | MainRouteController → MainMenuUiController; IMainRouteNavigation → IMainMenuPresenter; remove IMainMenuUi, MainMenuModel, IMainMenuModel |
| `Entities/MainMenu/{Settings,Skirmish}/` | `<Feature>RouteUiController` + `I<Feature>Presenter`; keep `ISettingsRoute`/`ISkirmishRoute : IUiRoute` |
| `Entities/Menu/{Ui,Controller}/` | PauseMenuRouteUiController + IPauseMenuPresenter in one folder; remove IPauseMenuUiView |
| `Entities/CoreGameUi/{Ui,Controller}/`, `Entities/EndGameUi/{Ui,Controller}/` | Concrete CoreGameUi/EndGameUi; replace ICoreGameUi/IEndGameView |
| `Entities/Faction/Ui/`, `Entities/Economy/Ui/` | Faction/build/economy views and controllers together; retain business models outside UI triplets |
| `Entities/Reinforcement/` | Concrete ReinforcementUi; explicit lifecycle for SpawnShipUi and per-unit rows |
| `Entities/ShipUi/{Ui,Controller}/` | Ship/group/selection/ability UI; explicit icons; preserve reuse and per-item ownership |
| `Entities/MiniMap/{Ui,Controller,Presenter}/` | Main view/controller/intent together; explicit model/colors; preserve marker/fog presenters |
| `Entities/{Heroes,Fps,Tooltip,Fade}/` | Concrete views; correct subscriptions, overlays and teardown |
| `Entities/UnitOrderFeedback/{Ui,Controller}/` | Concrete view and feature colocation |
| `Entities/CaptureSites/`, `Services/CaptureSites/CaptureSiteBuildUiController.cs` | Move controller beside concrete capture-site UI |
| `Entities/Loading/` | Scene-placed in `Scenes/Loading.unity` (no UiService there). `LoadingView` + `LoadingData` empty; `LoadingController.Tick` only activates the loaded scene → LoadingUi + LoadingUiController; activation moves to non-UI class |
| `Entities/{UnitActions,CinematicCamera,SuperWeapon}/Controller/` | Adapt UI infrastructure consumers; preserve behavior without creating new screens |

- Remove UI-only widget/view contracts when their users migrate; preserve model observers and framework event handlers.
- `ITooltipHoverView` is a common cast on several views: use the existing explicit hover-component reference/event source.
- `FadeService` currently creates FadeUi lazily; it stays the feature entry point and delegates to new FadeUiController (user rule: UI only through `UiController`). Preserve lazy creation and the fade API used by battle startup.
- SoulsLike routes: `Ui/Navigation/IUiRoute` (Show/Hide) + `UiRouteStack`; Grace pairs `IGraceUiPresenter` (view intents) with `IGraceRouteNavigation`. EmpireAtWar `IRoute` (Open/Close) is the equivalent → rename `IUiRoute`.
- CaptureSiteBuildUiController is already presentation despite its Services location; do not classify it as a gameplay service violation.

### Source-template comparison

- `Ui/Base/BaseUi.cs`: useful CanvasGroup/Show/Hide pattern; template also defines IBaseUi and IsHidden/IsActive. Target does not need these duplicate contracts/states.
- `Ui/MainMenu/MainMenuUi.cs`: concrete view assigns presenter and forwards button events. Ensure reassignment/disposal cannot duplicate listeners; avoid copying its unused IStartable.
- `Ui/MainMenu/MainMenuUiController.cs`: useful typed creation + presenter ownership; preserve EmpireAtWar's route, tooltip and focus needs.
- `Services/Ui/UiFactory.cs`: generic creation is the useful API; reject its extra child scope, RegisterComponentInHierarchy and GetComponent extraction.
- `Services/Ui/UiService.cs`: useful typed forwarding; EmpireAtWar still requires multiple explicit parents/layers.
- All reference paths are relative to `F:/Private/SoulsLikeTemplate/Assets/Scripts/`; that project is read-only reference material.

## Files

- UI regressions: `Assets/Scripts/Tests/Editor/UiCanvasArchitectureTests.cs`, `UiCancelRouterTests.cs`, `UiPrefabScaleTests.cs`, `SettingsUiPrefabTests.cs`, `PauseMenuUiPrefabTests.cs`, `SkirmishUiPrefabTests.cs`.
- Ownership/reuse: `HeroUiControllerLifecycleTests.cs`, `PipelineViewLifecycleTests.cs`, `ShipSelectionReuseTests.cs`, `ShipReinforcementUiPrefabTests.cs`, `SerializedReferenceTests.cs`, tooltip tests.
- Read in full for planning: [[Rules/UI_UX_GUIDELINES]], [[Rules/UI_CODE_BUILD_GUIDE]], [[Architecture/PROJECT_ORGANIZATION]].
- Existing [[TODOs/Refactoring/Explicit_Component_Binding]] already removed UiInstaller's old post-instantiation GetComponent; do not repeat that outdated finding.
- [[TODOs/Refactoring/Injection_And_Guard_Conventions]] is broader than this UI plan; only take ownership of the dependencies touched here.

## TODO

- [x] Asset-reference inventory and interface-based tests inspected; 19 prefab keys and original placement references preserved.
- [x] Live source checked during implementation. Historical tables above describe the pre-rework baseline.
- 2026-10-13 implementation: typed `CreateUi<TUi>`, `UiPlacement` mapping, presenter intents, explicit minimap/Fps/fade teardown and isolated scene-placed LoadingUi. 33 moved scripts preserve GUIDs; obsolete contracts/models removed through Unity.
- Verification: 19 saved screen prefabs pass concrete-type/CanvasGroup/missing-script checks; compilation clean. Selected UI cases 76/76 across full run and focused fade rerun; full suite retains 47 failures outside scope.
- Runtime behavior acceptance remains unverified; see active plan gates. No Play Mode run.
