---
category: Refactoring
status: in-progress
created: 2026-10-13
updated: 2026-10-13
---
# Zenject to VContainer — Research

## Goal

- Source evidence and migration traps for [[Done/Refactoring/Zenject_To_VContainer_Migration]].
- Snapshot 2026-10-13, HEAD `88635ce69`; live source inspected with Serena, exact-text scans and Graphify orientation. Graph is advisory.

## Important Values

### Verification

- Final EditMode: 1,182/1,229 passed; same 47 baseline failed test names, zero new failures; 11 migration fixtures pass.
- StandaloneWindows64: BuildResult Succeeded; 3,291,193,252 B; 708.005 s; 4 warnings and one reported error in unchanged planet shader (`MotionVectors`, `DOTS_INSTANCING_ON`, `ObjectSpaceNormal`).
- Final imported asset audit: MissingScripts / LegacyComponents / ZenjectDependencies all empty. Nine GUID-preserving file moves and 1,561 retained asset GUIDs verified.
- User post-cutover smoke test passed 2026-10-13: direct Battle entry, two menu/battle cycles, multiple AI, production, reinforcement, capture builds, abilities, pooled squadrons, settings and audio.
- Remaining acceptance: matched startup/spawn/teardown/frame timing capture. No runtime performance claim.

### Baseline inventory

- Unity: `6000.4.7f1` from `ProjectSettings/ProjectVersion.txt`.
- Reference VContainer: `1.19.0`, from SoulsLikeTemplate manifest and installed `package.json`; not a claim about the latest release.
- Baseline Zenject was vendored under `Assets/Plugins/Zenject/`; cutover removed it. VContainer `1.19.0` is pinned in the EmpireAtWar manifest.
- Broad source scan: 205 `Assets/Scripts/**/*.cs` files matched Zenject/container/context vocabulary, including tests; this is a scope indicator, not 205 confirmed runtime violations.
- Runtime composition audit: **3 direct DI Resolve call sites**, **16 FromComponent(s)InHierarchy call sites**, **2 FromComponentOn rebinding sites**, **1 shared injectable-descendant scan helper**.
- Counts refer to authored call sites, not prefab instances or every lookup executed internally by the vendor library.

## Implementation

### Confirmed direct DI resolution

| Source under Assets/Scripts | Location / dependency | Replacement |
|---|---|---|
| `Entities/BaseEntity/EntityFeatureBindings.cs:62` | WeaponModel factory resolves DamageMatrixData then CopyAccuracy | Explicit factory/registration input; preserve separate accuracy copy per WeaponModel |
| `Services/SceneContext/MainMenu/MainMenuInstaller.cs:22` | TooltipSettings → Timing via context.Container.Resolve | Load/receive settings explicitly; register its Timing value |
| `Services/SceneContext/Skirmish/SkirmishMainInstaller.cs:82` | Same tooltip timing lookup | Same explicit composition |

### Confirmed hierarchy binding sites

| Source under Assets/Scripts | Lines | Components / count |
|---|---|---|
| `Entities/BaseEntity/EntityFeatureBindings.cs` | 35, 44, 55, 64, 88 | Selection, Health, Radar, Weapon, conditional FogVisibility — 5 |
| `Entities/Ship/ShipInstaller.cs` | 60, 67, 87, 92 | ShipMove, optional Hangar, AudioShip, local AudioDialog — 4 |
| `Entities/Squadron/SquadronInstaller.cs` | 67, 74, 75, 76, 79 | Optional SFoils, Health, Flight, Icon, local AudioDialog — 5 |
| `Entities/SpaceStation/SpaceStationInstaller.cs` | 51 | Hangar — 1 |
| `Services/SceneContext/Skirmish/SkirmishServiceInstaller.cs` | 88 | CameraService — 1 |

- These Zenject APIs hide the hierarchy search even though installer code does not directly call GetComponentsInChildren.
- No direct GetComponentInChildren/GetComponentsInChildren calls were found in the scanned project-owned runtime source directories; the installer wrappers above are the injection issue.
- `BaseSystem/DependencyInjection/InstallerExtensions.cs:36–38`: ZenUtilInternal.GetInjectableMonoBehavioursUnderGameObject + QueueForInject scans descendants; used by dynamic entity and UI instantiation. Replace with explicit references and a defined activation sequence.
- `Services/SceneContext/ViewInstallers/Base/ViewComponentsInstaller.cs:20–24`: explicit component already exists, but FromComponentOn searches its GameObject again.
- `Services/SceneContext/ViewInstallers/Base/ModelDependencyInstaller.cs:21–25`: same unnecessary re-lookup of an explicit view model.
- `DynamicEntityInstaller.cs:46–48`: FromResolveGetter obtains entity.transform; register the held entity's transform directly with the existing semantic key.
- `EntityFeatureBindings.cs:34,54` and `ShipInstaller.cs:85`: FromResolve aliases; expose one model registration through its observer interfaces.
- `PlayerCoreInstaller.cs:43–47`: writes IReinforcementService into the parent via FromSubContainerResolve/ByInstance; remove the unnecessary upward export after verifying callers.
- `MainMenuInstaller`, `SkirmishMainInstaller`, `GameUnitsInstaller`: FromSubContainerResolve is a factory/export mechanism, not counted among the 3 direct Resolve calls; all must still be replaced.

### Review findings (2026-10-13)

| Pattern | Sites under Assets/Scripts | Problem → Phase 2 fix |
|---|---|---|
| `LazyInject<T>` | `Ship.cs:67` IEntity; `Squadron.cs:70,73,74` IEntity + IRespawnable/IDespawnable lists; `HangarComponent.cs:38` carrier IEntity; `SkirmishOrchestrator.cs:27,28` notifiers; `EnemyStructurePlacementService.cs:40` IMapModelObserver | Cycle / wrong ownership boundary → single owner, one-way deps |
| `[InjectOptional]` | `BaseSystem/MonoComponent.cs:18` TModel (base of all entity components); `HardPoint.cs:50` IFogVisibilityFacade; `Ship.cs:98`, `Squadron.cs:127` IAudioDialogShipComponent | VContainer 1.19.0 has no optional injection → explicit capability |
| `IInstantiator` | `ShipAbilityFactory.cs`, `ShipAbilitySettings.CreateAbility`, ~20 `Abilities/*/*Settings.cs` | Container as instantiator; `IObjectResolver.Instantiate` is the same locator → explicit construction |
| `FromComponentInNewPrefab` (GameObject) | `MainMenuInstaller:29`, `SkirmishMainInstaller:107`, `SkirmishServiceInstaller:44,53`, `ViewDependencyBuilder:20` | Searches new instance hierarchy → typed component prefab |
| Keyed IDs | 12 `[Inject(Id=…)]`/`WithId`; 9 `ViewTransform` consumers, `MonoComponentInstaller:20`, `DynamicEntityInstaller:46`, `SkirmishMainInstaller:234` | Convert to `[Key]`/`.Keyed` at cutover |
| Consumer-less export | `SkirmishMainInstaller:233` `IEnemyService.WithId(slot.Id)` NonLazy | Exists only to force AI sub-container creation → explicit per-slot scope |
| `[Inject]` | 76 non-test sites | Namespace swap at cutover |

- Lazy-injection observations: Ship uses entity only for `Id`, ability facade slots and audio init (`Ship.cs:86,128–129`). Squadron uses entity for SFX calls (`:247,500`); its respawn/despawn lists include `Entity` (`Entity : IRespawnable, IDespawnable`) → cycle Squadron ↔ Entity. Hangar uses carrier only for `squadron.Guard(_carrier.Value, …)` (`:165`).
- `HangarComponent` injecting its carrier also violates AGENTS entity rule (components never inject their owner).
- Phase 2 order rationale: all fixes are valid under Zenject, testable with existing EditMode tests, and remove the patterns VContainer cannot express.

### Exclusions and boundaries

- `FighterManeuver.Resolve`, `EnemyTaskForceExecutor.Resolve`, `TooltipIconData.Resolve` and AddressableAssetService.ResolveKey are domain/asset functions, not DI resolution.
- Editor prefab inspections and tests use GetComponentsInChildren; do not report them as production injection violations.
- `EntityMediator.cs:75` uses GetComponentInParent on a physics-hit collider; outside this injection migration. Do not silently expand scope to gameplay hit lookup policy.
- `AddressableAssetService.LoadComponent` validates/extracts the component at the prefab root with TryGetComponent; keep this isolated asset-loading boundary, not runtime dependency assembly.
- VContainer internally resolves registrations; the rule applies to project-authored lookup/service-locator code. Do not modify vendor internals.

### Scope and factory coverage

| Current source | Required preservation |
|---|---|
| `Services/SceneContext/ProjectContextInstaller.cs` | Settings applier order, root asset service, copied GameData, timer/input/audio/scene lifetime |
| `Services/SceneContext/MainMenu/{MainMenuInstaller,MainMenuRouteInstaller}.cs` | Menu + settings/skirmish routes, concrete UI after prerequisite |
| `Entities/Loading/LoadingInstaller.cs`, `Services/SceneService/SceneService.cs` | Loading interstitial, Single scene switch, delayed async target load, explicit activation |
| `Services/SceneContext/Skirmish/{SkirmishMainInstaller,SkirmishServiceInstaller,MapInstaller}.cs` | Battle registries, map consumers, copied MiniMapData, fog, navigation/combat and lifecycle phases |
| `PlayerCoreInstaller.cs`, `AiPlayerInstaller.cs` | One PlayerSlot, economy/faction/production and own pool/factories per player |
| `GameUnitsInstaller.cs` | All seven typed dynamic factory registrations |
| `DynamicEntityInstallers/DynamicEntityInstaller.cs` | Shared data, typed view load, deferred activation, entity transform and EntityInstaller wiring |
| `Entities/BaseEntity/{EntityInstaller,EntityFeatureBindings}.cs` | Unique ID, entity/facades, health/selection/combat/fog composition |
| `Entities/Planet/{PlanetInstaller,PlanetSpawner}.cs` | Typed planet creation and planet lifetime |

| Factory | Existing Create arguments → result |
|---|---|
| ShipFactory | PlayerId, ShipType, Vector3 → Ship |
| SquadronFactory | PlayerId, SquadronType, Vector3, Quaternion → Squadron |
| SpaceStationFactory | PlayerId, FactionType, Vector3 → SpaceStation |
| MiningFacilityFactory / AsteroidMiningFacilityFactory | PlayerId, MiningFacilityType, Vector3 → MiningFacility |
| DefendPlatformFactory / AsteroidDefendPlatformFactory | PlayerId, DefendPlatformType, Vector3 → DefendPlatform |
| UiFactory after UI prerequisite | Generic concrete TUi + explicit Transform parent → TUi |

- Ship and Squadron roots are MonoBehaviours. A typed factory can return its held instantiated component; no child.Container.Resolve<TEntity>() is needed.
- Reference BaseFactory gets RootScope by resolving LifetimeScope. VContainer 1.19.0 registers its own LifetimeScope instance; constructor injection of that scope removes the lookup.
- A parent container cannot normally consume child-only registrations. Current runtime IReinforcementService consumer is ReinforcementUiController, registered in PlayerCoreInstaller; no scene consumer was found.
- Keep explicit caller/owner relationships; do not introduce a general cross-scope locator.

### Lifecycle traps

| Current mechanism | Migration requirement |
|---|---|
| All LazyInject sites (see Review findings) | Removed in Phase 2 by fixing boundaries; no Lazy/Func/setter substitutes |
| All InjectOptional sites (see Review findings) | Removed in Phase 2 via explicit capabilities; no silent fallback |
| IHealthData decoration by ResearchHealthData | Explicit base data → decorator dependency; no self-referential interface cycle |
| Lists of facades, mono-components, respawnables and settings appliers | Preserve identity, multiplicity and ordering; do not register the same object twice |
| Ship destroys context after Data.DestroyDelay | Preserve delay and one final scope disposal |
| Squadron disables/enables context during pooling | Keep scope for reuse, suppress scheduled work while pooled, dispose on final eviction/owner teardown |
| ILateDisposable | Explicit cleanup phase; no blind rename to IDisposable with changed order |
| RegisterInstance vs RegisterComponent | Instance registration does not provide automatic method injection/disposal; explicit component registration injects that component |
| Shared asset / cloned ScriptableObject | Shared read-only catalog ownership differs from runtime clone ownership |

### Observed execution ordering

- Initialization: CoreGameUiController `-200`; PlayerOrderInputHandler `-100`; reinforcement/capture-site minimap presenters and UnitActionsPresenter `100`.
- Entity initialization: HealthComponent `-100`; SquadronHealthComponent `-100`; SquadronFlightComponent `-90`.
- Late tick: FighterPoseBatch `-2000` → CombatAttackCoordinator `-1000` → default → ShipSfxService `1000` → ShipNavigationService `2000`.
- ShipSfxService also uses Tick `1000` and disposable order `-1000`; CaptureSiteBuildUiController uses late-dispose order `100`.
- Preserve the required relationships through a focused coordinator/phases; do not assume VContainer registration order reproduces every Zenject ordering hook.
- Battle startup should continue to use existing BattleState gating, not DI timing as gameplay state.

### Source-template adaptations

| Reference under F:/Private/SoulsLikeTemplate/Assets/Scripts | Keep | Change / reject |
|---|---|---|
| `Services/VContainer/{ProjectScope,SharedSceneScope,MainMenuScope,LoadingScope,CoreScope}.cs` | Small composition roots, explicit component fields | CoreScope's Resolve build callback; commented resolving fallbacks; singleton assumptions must match actual owner |
| `Services/VContainer/{CharacterScopeInstaller,EnemyScopeInstaller}.cs` | Entity-specific composition | RegisterComponentInHierarchy, GetComponentInChildren and capability detection by child searches |
| `Services/VContainer/EntityLifetimeScope.cs` | Explicit build boundary as a reference | Do not copy BuildOnce guard wrapper automatically; choose one build owner and verify activation order |
| `Utilities/Extensions/VContainerExt.cs` | Concise ScriptableObject registration | Static AddressableAssetService and static cached MappingData; use explicit inputs/current single mapping |
| `Services/Factory/BaseFactory.cs` | Common asset/scoped creation dependencies | Resolver field and Resolve<LifetimeScope>(); inject required values |
| `Services/Ui/UiFactory.cs` | Typed CreateUi API | Extra child scope + hierarchy registration + GetComponent; UI prerequisite already replaces these |

## Files

- Scenes: `Assets/Scenes/Loading.unity`, `MainMenuScene.unity`, `Battle/Battle.unity`, `Planets/Kamino/Kamino.unity`, `Planets/Corusant/Corusant.unity`.
- Bootstrap/context candidates: `Assets/Resources/{ProjectContext,SceneContext}.prefab`; `Assets/Prefabs/View/ZenjectContext/{SceneContext,PlayerContext,EnemyContext}.prefab`. Determine live references before removal.
- Factory wrappers: `Assets/Prefabs/View/{Ship,Squadron,SpaceStation,MiningFacility,AsteroidMiningFacility,DefendPlatform,AsteroidDefendPlatform}Installer.prefab`.
- Regressions: `PlayerCoreInstallerTests`, `EntityComponentLifecycleTests`, `HeroUiControllerLifecycleTests`, `EnemyFactionControllerLifecycleTests`, pooling tests, `SerializedReferenceTests`, `SceneDataTests`, UI gate tests.
- API sources checked: [explicit components](https://vcontainer.hadashikick.jp/registering/register-monobehaviour), [child scopes](https://vcontainer.hadashikick.jp/scoping/generate-child-with-code-first), [entry points](https://vcontainer.hadashikick.jp/integrations/entrypoint), [registration/instance ownership](https://vcontainer.hadashikick.jp/registering/register-type), [lifetimes](https://vcontainer.hadashikick.jp/scoping/lifetime-overview).
- Keyed injection: [current key documentation](https://vcontainer.hadashikick.jp/registering/register-with-keys); locally verified 1.19.0 `RegistrationBuilder.Keyed(object)` and `[Key(...)]`. The older Zenject comparison page's “WithId not supported” row is stale for this package.
- Local package evidence: `F:/Private/SoulsLikeTemplate/Library/PackageCache/jp.hadashikick.vcontainer@dcf16cda6a65/Runtime/{RegistrationBuilder.cs,Annotations/KeyAttribute.cs,Unity/LifetimeScope.cs,Unity/ContainerBuilderUnityExtensions.cs}`.
- Documentation fetched with Context7; package-source behavior checked where migration depends on it.

## TODO

- [x] Phase 2 ownership: Ship takes entityId + ability facade; Squadron publishes pool events and Entity owns locator registration; Hangar publishes launch events and Entity passes itself to Guard. SFX receives ShipSfxHandle instead of an owning IEntity.
- [x] Skirmish notifier cycle: PauseMenuRouteUiController/BattleVictoryService consume orchestrator BattleState while orchestrator subscribed to both. SkirmishFlowSubscriptions owns subscriptions; SkirmishStateModel publishes replayed state/speed, breaking camera/fog → orchestrator → startup dependency cycles.
- [x] AI placement: MapModel is battle-owned with no constructor dependencies; inject its observer directly. Layout availability stays gated by BattleState.
- [x] Explicit capability: local-owner voice is an authored component collection; local fog is VisibleFogFacade; enemy fog is the authored FogVisibilityComponent. A-Wing had no fog component; authored its renderer references using the same radius 0 as other fighters.
- [x] RealEntityCompositionTests passed: actual root/battle/player graph, seven factories × two owners + planet, keyed own Transform/base health, state isolation, explicit voice capability, pool identity and synchronous teardown.
- [x] Serialized inventory: 1,623 assets and 3 build scenes; root/scene/planet scope fields recorded before conversion. Final runtime asset audit reports zero missing scripts or Zenject dependencies.
- [x] Live source static audit: no project runtime DI Resolve, hierarchy binding, lazy/optional injection or container instantiation. Icon/steering APIs named Resolve are unrelated to DI.

## Decision

- Pinned VContainer registers the held concrete scope + LifetimeScope automatically; do not duplicate `RegisterInstance(this)`.
- Registration owners use Singleton within their scope. `ContainerLocal<IReadOnlyList<T>>` prevents parent callbacks from running in child coordinators.
- One ordered coordinator dispatches project lifecycle: initialization/ticks ascending stable order; regular cleanup reversed, then late cleanup reversed.
- IMonoComponent cleanup belongs only to EntityComponentLifecycle; exclude its dispose contracts from component registrations.
- ResearchHealthData consumes keyed base IHealthData (`EntityBindType.HealthData`); the unkeyed registration is the decorator.
- InputLockService initializes before startup locks and releases after their owners; InputActionsProvider owns the generated actions resource.
- EntityScopeOwnership belongs to each player (`Dispose = 1000`); releases held entity containers before player/battle buffers. EntityLifetimeScope unregisters on destruction. PlayerScopeBootstrap (`Dispose = 300`) owns players; PlanetFactory (`Dispose = 200`) owns planets.
- Test setup uses Battle's authored ReinforcementZoneData dependency, initializes the real MapModel before player startup, and simulates HealthOverlayView.Awake in EditMode. Memory settings + completed battle startup keep scene transitions and settings writes outside this fixture.
