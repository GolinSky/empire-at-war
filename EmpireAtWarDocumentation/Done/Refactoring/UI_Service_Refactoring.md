---
category: Refactoring
status: done
updated: 2026-10-13
completed: 2026-10-13
---
# UI Rework Before VContainer

## Goal

- Complete under Zenject before [[TODOs/Refactoring/Zenject_To_VContainer_Migration|Zenject → VContainer migration]].
- Every screen = concrete `<Feature>Ui` + `I<Feature>Presenter` + `<Feature>UiController`, colocated in one feature folder.
- Remove `UiType`, screen-interface casts and per-screen Zenject subcontainers; preserve current UI behavior.

## Decision

- **Chosen:** SoulsLikeTemplate typed UI/controller pattern (`BaseUi`, `UiController.CreateUi<TUi>()`, `IUiRoute`-style routes).
- **Why:** UI simplification lands first; the DI migration then only changes composition/lifetimes.
- **Screen interfaces removed:** `IMainMenuUi`, `ISettingsUi`, `ISkirmishUi`, `IPauseMenuUiView`, `ICoreGameUi`, `IEndGameView`, `IEconomyUi`, `IFactionUi`, `IFactionView`, `IShipBuildUi`, `IReinforcementUi`, `IShipUi`, `IShipGroupUi`, `IMiniMapView`, `IHeroUi`, `IFpsUi`, `ITooltipUi`, `IFadeUi`, `IUnitOrderFeedbackUi`, `ICaptureSiteBuildUi`, `ITooltipHoverView`. No `IBaseUi`.
- **Interfaces kept:** world-space/entity views (`IShieldView`, `IHealthOverlayView`, `IIonStunView`, `IIonPulseView`, `ISFoilsView`, `IHardPointOverlayView`, `ISelectableView`, `IReinforcementZoneView`, `IMarqueeSelectionView`, `ICaptureSiteView`, `IImpactEffectView`, `ICheatView`); shared HUD widgets (`IUnitActionsView`, `ISuperWeaponsView`, `ISpawnShipUi`) unless listed above; presenter, route, model-observer, cancel and Unity event-handler contracts. `IUiService`/`IUiPlacement` are infrastructure.
- **View → controller:** always through `I<Feature>Presenter`. Screen views expose no intent events (`EndGameUi.ReturnToMenuRequested`, `MiniMapUi.OnCameraMoveRequested/OnMoveOrderRequested` → presenter calls; `HeroUi.Disposed` → controller-owned teardown). Child widgets report to their parent view.
- **Placement:** dedicated `UiPlacement` (`IUiPlacement`) owns the single `Type → parent` mapping. `UiService.CreateUi<TUi>()` asks it; controllers never pick slots.
- **Routes:** route screens are a navigation layer on top of the UI system → `<Feature>RouteUiController`. `IRoute` → `IUiRoute` in `BaseSystem/Ui/Navigation/` (reference: SoulsLike `Ui/Navigation/IUiRoute.cs`, `UiRouteStack.cs`).
- **Fade:** `FadeService` stays the feature entry point (`IFadeService` API unchanged) and delegates to new `FadeUiController`, which owns `FadeUi`. Same rule for every screen: UI only via `UiController`.
- **Loading:** in scope (it is UI). Scene-placed `LoadingUi : BaseUi` + `LoadingUiController`; scene activation is not UI.
- **Folders + namespaces:** merge `Ui/` + `Controller/` triplets now and normalize namespaces to the feature folder.
- User-requested concrete views/colocation override [[Rules/UI_CODE_BUILD_GUIDE]] and [[Architecture/PROJECT_ORGANIZATION]] for UI triplets; update both during implementation.
- **Avoid:** screen enum, per-screen type switch in `UiService`, controllers knowing slots, view-interface casts, `Resolve`/`GetComponent*` in composition.

## Rules

- `BaseUi : MonoBehaviour`: serialized `CanvasGroup`, `IsVisible`, `Show()`/`Hide()`; non-generic; no `[Inject]`.
- `UiController`: `protected TUi CreateUi<TUi>() where TUi : BaseUi` + existing cancel focus behavior.
- Prefab key = `typeof(TUi).Name` (already equals current `"{UiType}Ui"` key for all 19 screens → no asset/address changes).
- Creation order: create → `AssignPresenter(this)` / pass data → initialize listeners → `Show()`. `AssignPresenter` is single-call; listeners removed once in teardown.
- Controller owns view setup, model subscriptions, show/hide and disposal. Account for scene destruction before controller disposal.
- Passive views (no user intent: Fade, Fps, Loading, Tooltip, UnitOrderFeedback) get no presenter interface.

### Naming

| Current | Target |
|---|---|
| `MainRouteController` + `IMainRouteNavigation` | `MainMenuUiController` + `IMainMenuPresenter` (root, not an `IUiRoute`) |
| `SettingsRouteController` + `ISettingsRouteNavigation` | `SettingsRouteUiController` + `ISettingsPresenter` |
| `SkirmishRouteController` + `ISkirmishRouteNavigation` (MainMenu) | `SkirmishRouteUiController` + `ISkirmishPresenter` |
| `PauseMenuRouteController` + `IPauseMenuRouteNavigation` | `PauseMenuRouteUiController` + `IPauseMenuPresenter` |
| `IRoute` (`Entities/MainMenu/IRoute.cs`) | `IUiRoute`; `ISettingsRoute`/`ISkirmishRoute`/`IPauseMenuRoute` keep names |
| other `<Feature>UiController` | keep; add `I<Feature>Presenter` only where the view has intents |

- Renaming `ISkirmishRouteNavigation` also removes its name clash with `Services/UiRouting/ISkirmishUiRoute.cs`'s `ISkirmishRouteNavigation`.

## Implementation

### 1. Baseline and ownership

- [x] Record source behavior and EditMode baseline for main menu, skirmish setup, settings, pause, HUD, tooltip, fade, loading and end-game; separate failures outside UI scope. Runtime capture remains in the manual gate.
- [x] Inventory per screen: child widgets, intents (buttons/events), `[Inject]` deps, Zenject lifecycle interfaces on views, owner, serialized/addressable refs. See [[Done/Refactoring/UI_Service_Refactoring - Research|research]].
- [x] Confirm `UiType.Interaction` has no prefab/caller; drop it.
- [x] Verify: each screen, child widget and dynamic row has one lifecycle owner.

### 2. Infrastructure

- [x] `UiPlacement : MonoBehaviour, IUiPlacement` on `Prefabs/Ui/UiService.prefab`: serialized canvases/slots/layouts moved from `UiService`; one `Dictionary<Type, Transform>` built once (19 entries, parity table in research). `Transform GetParent<TUi>()`; missing type → exception.
- [x] Expose only what owners need beyond parents: `ContextualSlot` (CoreGame popup reparent), `FactionLayout` (Faction content layout).
- [x] `UiService` keeps `SetHudVisible`, `SetContentLayout`, `SetContentVisible`, `IHudVisibilityObserver`; references `UiPlacement` explicitly.
- [x] `IUiService.CreateUi<TUi>() where TUi : BaseUi`; typed factory: `IAssetService.LoadComponent<TUi>(typeof(TUi).Name)` → `Object.Instantiate(prefab, parent, false)`. No subcontainer/`GameObjectContext`, no post-instantiation lookup. Preserve `AssetMappingData.assetMappings`.
- [x] Convert every caller to `CreateUi<TUi>()`; remove the transitional enum API after migration. Final integrated source compiles clean.
- [x] Remove `BaseUi<TModel>`. Move `MiniMapUi`'s Zenject `IInitializable.Initialize`/`ILateDisposable.LateDispose` to explicit `MiniMapUiController` calls (today they run only via the per-screen subcontainer — dropping it silently disables markers and leaks subscriptions/tweens).
- [x] Explicit deps: minimap model + `IPlayerColors`; `ShipUi`/`ShipGroupUi` ← `IShipIconProvider`; `TooltipUi` ← `TooltipIconData` + `TooltipSettings`.
- [x] Move CoreGame first-sibling + contextual popup reparent and Faction `SetContentLayout` from `UiService.CreateUi` into their controllers.
- [x] `IRoute` → `IUiRoute` in `BaseSystem/Ui/Navigation/`.
- [x] Verify: placement parity, setup-before-input, required Inspector refs, raycasts off when hidden, disposal without subcontainer.

### 3. Pilot: main menu + minimap

- [x] Main menu: `MainMenuUi` + `IMainMenuPresenter` + `MainMenuUiController` in `Entities/MainMenu/Main/`; update `MainMenuOrchestrator` and `MainMenuInstaller`.
- [x] Delete empty `MainMenuModel`/`IMainMenuModel` and the view's unused `SetModel`.
- [x] Minimap: proves explicit lifecycle + explicit deps + presenter replacing view events.
- [x] Verify: open/close/reopen creates no duplicate views/listeners; one action per click; Esc routes correctly; minimap markers/fog/colors/clicks work.

### 4. Routes

- [x] Settings, Skirmish, PauseMenu → `<Feature>RouteUiController` + `I<Feature>Presenter`; keep `I<Feature>Route : IUiRoute` for orchestrators.
- [x] Preserve return navigation, settings draft/apply/cancel/rebind, team/faction setup, tooltip cleanup.
- [x] Verify: route open/close/return and Esc from main menu and in-battle pause.

### 5. Gameplay UI, fade, loading

- [x] Convert CoreGame, EndGame (`IEndGamePresenter`), Economy, Faction/ShipBuild, Reinforcement, Ship/ShipGroup, Heroes, CaptureSiteBuild, UnitOrderFeedback, Fps, Tooltip to concrete views.
- [x] `TooltipHover` read from the concrete view; keep event source + presenter → `ITooltipService` flow. `TooltipService` remains sole show/hide authority.
- [x] Move `Services/CaptureSites/CaptureSiteBuildUiController.cs` beside `CaptureSiteBuildUi`.
- [x] Fade: new `FadeUiController` creates/owns `FadeUi` lazily (first request can come from another system's `Initialize`); `FadeService` delegates; keep completion/cancellation semantics.
- [x] Loading (`Scenes/Loading.unity`): `LoadingView`/`View<ILoadingModelObserver>` → scene-placed `LoadingUi : BaseUi` passed by serialized reference (Loading scene installer) to `LoadingUiController`; move `LoadingController.Tick` scene activation to a non-UI loading flow class; delete empty `LoadingData`/`ILoadingModelObserver` + asset if unused; replace `LoadingInstaller`. Legacy `View<T>`/`Controller<T>`/`StaticViewInstaller` stay (Planet, Game, EnemyFaction use them).
- [x] Loading must not reference `IUiService`, `UiPlacement`, `ITooltipService`, `IUiCancelRouter` or any MainMenu/Skirmish UI object: `UiService`, `TooltipService`, `UiCancelRouter`, `FadeService` are bound per scene in `MainMenuInstaller`/`SkirmishMainInstaller`, and `SceneService` loads Loading with `LoadSceneMode.Single` → those instances are destroyed (stale/missing refs — previously hit with old UI service refs).
- [x] No `ProjectContext` binding may cache `UiService`, `BaseUi` or a `UiController` across scene loads.
- [x] Remove screen interfaces/casts after their consumers and tests migrate.
- [x] Verify: selection/group reuse, reinforcement drag/cancel, construction/research, minimap, hero actions, pause/end-game, cinematic HUD restore, fade on battle start, loading → scene activation.

### 6. Folders, namespaces, cleanup

- [x] Merge split `Ui/` + `Controller/` (+ `Presenter/` for main view) into one feature folder: `Menu`, `CoreGameUi`, `EndGameUi`, `ShipUi`, `MiniMap`, `UnitOrderFeedback`. Models, shared components, Services, Editor, Tests stay separate.
- [x] Normalize namespaces to folder (e.g. `EmpireAtWar.Views.MiniMap`/`EmpireAtWar.Controllers.Menu` → `EmpireAtWar.Entities.<Feature>`). Moves via Unity-aware tooling; preserve `.meta` GUIDs; persist affected prefabs/scenes. Do not restructure `Assets/AddressableAssetsData`.
- [x] Remove `UiType`, `UiInstaller`, `UiFactory` placeholder bindings (`MainMenuInstaller.cs:33`, `SkirmishMainInstaller.cs:111`), `UiService.GetPlacement`.
- [x] Controllers stay registered in existing installers until the DI migration.
- [x] Update [[Rules/UI_CODE_BUILD_GUIDE]], [[Architecture/UI_REFACTORING_PLAYBOOK]] examples, triplet placement in [[Architecture/PROJECT_ORGANIZATION]].
- [x] Verify: no `UiType`, screen-interface casts, `[Inject]` in UI views, screen subcontainers or container/hierarchy lookups remain.

### 7. Completion gate

- [x] Compile/import clean; no missing scripts/references on affected prefabs/scenes.
- [x] Update/run EditMode UI tests (canvas, cancel router, prefab, settings, pause, skirmish, selection reuse, hero lifecycle, unit actions); replace `UiService` stubs returning `BaseUi` by `UiType`.
- [x] **User in-game smoke test** (agents do not enter Play Mode): MainMenu → Settings/Skirmish → Loading → Battle → Pause/Settings → EndGame → MainMenu; repeat a battle for stale state/listeners.
- [x] Only after the gate: mark done, move plan + research to `Done/Refactoring/`, repair backlog links, unblock migration.

## Edge Cases

- `UiService` places 19 screens across 4 canvases + slots → `UiPlacement` must keep every placement, not collapse to one parent.
- Each `CreateUi` today also spawns a `GameObjectContext` GameObject whose destruction disposes view-bound Zenject hooks → only `MiniMapUi` relies on that.
- `InstantiatePrefabForInstall` activated screens after injection; plain `Instantiate` runs `Awake` immediately. Only `EndGameUi.Awake` exists and is self-contained.
- `BaseUi.IsVisible` defaults `true` → new screens are visible/raycast-blocking until hidden; preserve unless a screen needs auto-hide.
- `Services/UiRouting` (`SkirmishUiRoutePosition`, `ISkirmishUiRoute`) is HUD-region visibility, not screen identity → out of scope; keep.
- Retain content-layout faction/ship/group switching, CoreGame contextual reparenting, first-sibling ordering.
- Scene change (`LoadSceneMode.Single`) → scene-bound UI services and views destroyed. Scene-placed UI (Loading) wires only to its own scene context; verify MainMenu → Loading → Battle → MainMenu → Battle leaves no stale `UiService`/view refs (`MissingReferenceException`).

## Files

- Infrastructure: `Assets/Scripts/BaseSystem/Ui/`; host prefab `Assets/Prefabs/Ui/UiService.prefab`.
- Inventory and tests: [[Done/Refactoring/UI_Service_Refactoring - Research]].
- Reference (read-only): `F:/Private/SoulsLikeTemplate/Assets/Scripts/Ui/{Base/BaseUi,Base/UiController,MainMenu/*,Navigation/IUiRoute,Navigation/UiRouteStack,Grace/*}.cs`.
- Related: [[TODOs/Refactoring/Explicit_Component_Binding]], [[TODOs/Features/Tooltip_System_Plan]], [[TODOs/Refactoring/Battle_Startup_Sequence_Plan]]; preserve their unfinished acceptance work.

## TODO

- 2026-10-13: implementation complete under Zenject. User confirmed the menu/settings → loading → battle → pause/end-game → menu flow twice, including HUD/minimap and drag/cancel behavior; VContainer prerequisite accepted. No agent Play Mode run.
- Verified: 33 Unity-aware script moves retained metadata/GUIDs; 19 screen prefabs have concrete components, assigned `CanvasGroup`, and no missing scripts. UI host/loading scene and affected assets imported, reserialized and saved; LoadingData mapping/address removed.
- Automated: 76 selected UI regression cases have passing results across full EditMode and focused fade rerun. Full run: 1,582/1,633 in 92.39 s; 4 new fade failures fixed and rerun 6/6 in 0.35 s. Remaining 47 failures concern balance/AI/ship assets and are outside UI scope. Raw local evidence: `Temp/ui-rework-full-tests.json`, `Temp/ui-rework-fade-tests.json`.
- Lifecycle fixes: explicit faction content parent; build-pipeline child listener cleanup; destroyed minimap/Fps wrappers release managed subscriptions; pending fade awaitables cancel even before DOTween's first update. Existing zero-scale FeedbackCanvas fixed; obsolete CoreGame reference test inventory updated to current placement architecture.
- Outcome: concrete typed UI/controller composition and explicit lifecycle are accepted; VContainer migration gate open.
- Existing large classes (CoreGame/Ship/Settings controllers and minimap view) were not split beyond this plan; focused extraction can be planned separately.
- Future tests: official `unity` CLI; save named dirty scenes, block on dirty untitled scenes, async + poll.
