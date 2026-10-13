---
category: Refactoring
status: in-progress
created: 2026-10-13
updated: 2026-10-13
---
# Zenject to VContainer Migration

## Goal

- First remove every lookup-style and boundary-violating injection pattern while still on Zenject; then replace Zenject composition, factories and lifecycle with VContainer.
- Preserve project → scene → player → entity ownership, pooled lifetimes, gameplay behavior and serialized asset references.
- End with zero project-authored container resolution, hierarchy discovery, lazy injection or optional injection.

## Decision

- **Chosen:** Zenject-side cleanup first (Phase 2), then explicit LifetimeScopes, constructor-injected typed factories, serialized component references and narrow registration extensions inspired by SoulsLikeTemplate.
- **Why cleanup first:** most migration risk is architectural, not library-specific. Fixing it on Zenject keeps existing tests valid, splits the work into small verifiable steps and shrinks the final cutover to scope/namespace/lifecycle changes.
- **Lazy injection = architecture defect.** Every `LazyInject<T>` marks a dependency cycle or wrong ownership boundary. Fix the boundary; never replace with `Func<T>`, resolving delegates, post-construction setters that hide the same cycle, or VContainer `Lazy<T>`.
- **Optional injection = undeclared capability.** VContainer 1.19.0 has no `[InjectOptional]` (only `[Inject]`, `[Key]`). Make each capability explicit in data/authoring; required dependencies stay required.
- **Factories:** keep named typed factories with typed `Create(...)` signatures (Ship, Squadron, …) on a minimal `BaseFactory`. Matches reference `BaseFactory`; no PlaceholderFactory arity generics, no resolver field.
- **Prerequisite:** [[Done/Refactoring/UI_Service_Refactoring|UI Rework Before VContainer]] complete and verified before Phase 3+. Phase 2 may start independently when approved; creating this plan does not authorize implementation.
- Baseline package: VContainer `1.19.0`, matching the reference. Pin it; verify against Unity `6000.4.7f1` before cutover.
- Do not copy the reference BaseFactory's `resolver.Resolve<LifetimeScope>()`, UiFactory's hierarchy scan, CoreScope's resolving build callback, or entity installers' `RegisterComponentInHierarchy` / `GetComponentInChildren`.
- No general Zenject compatibility layer. Stage VContainer work in isolated fixtures until a complete root can switch.

## Rules

- No project-authored `Resolve` / `TryResolve` / keyed resolution / `IInstantiator` / `IObjectResolver.Instantiate` in scopes, callbacks, extensions, factories, settings or injection methods.
- No `GetComponent*`, `Find*`, `RegisterComponentInHierarchy`, `FromComponent(s)InHierarchy`, `FromComponentOn`, `FromComponentInNewPrefab` with an untyped GameObject, or descendant auto-inject scans as dependency discovery.
- No `LazyInject<T>`, `Lazy<T>` or `Func<T>` used to defer a cyclic dependency.
- No `[InjectOptional]`. A capability flag/catalog/authored reference decides optional composition, never a failed lookup.
- Use `builder.RegisterComponent(explicitReference)`; list multiple components explicitly.
- Pure C# → constructor injection. Unity components → explicit serialized references and one method-injection/setup path.
- Factories receive dependencies directly; scoped factories receive the owning `LifetimeScope`, not `IObjectResolver`.
- Entity rules remain: components never inject their owner or command siblings; only the entity class connects components. Keep `IEntity` / facade boundaries and one owner per entity.
- Preserve keyed own-transform `EntityBindType.ViewTransform`; per-player identity; shared read-only data. No UI screen enum returns.

## Implementation

### 1. Inventory and baseline

- [x] Inventory Zenject types/attributes, contexts/installers, injection IDs, aliases, optional/lazy dependencies, factory signatures, collections and lifecycle order.
- [x] Record scene/prefab/context GUID references, Addressable keys, build scenes and asmdef dependencies. `Temp/vcontainer-asset-inventory.json`: 1,623 assets, 3 build scenes; serialized source references in `Temp/vcontainer-scope-references.json`.
- [x] Capture applicable tests and two consecutive battle lifecycles; record existing failures separately. Baseline EditMode: 1,586/1,633 passed, 47 failures; user verified the UI prerequisite's two cycles.
- [x] Verify: every binding has an owning scope, lifetime, initialization owner and disposal owner. Audit details in [[TODOs/Refactoring/Zenject_To_VContainer_Migration - Research|research]].

### 2. Zenject-side cleanup (first implementation phase)

- Zenject cleanup checkpoint compiled before cutover; full tests retained the 47 baseline failures. Cutover was applied coherently after the user's UI acceptance.

- [x] **Lazy injection — fix boundaries (7 sites).** For each: identify the cycle, choose the single owner, make dependencies flow one way, record the decision in research.
  - `Ship` `LazyInject<IEntity>`: uses only `Id`, ability facade slots and audio init → inject `long entityId` / ability slots directly or let the entity class drive audio init; Ship must not depend on the `Entity` that depends on it.
  - `Squadron` `LazyInject<IEntity>`, `LazyInject<List<IRespawnable>>`, `LazyInject<List<IDespawnable>>`: lists include `Entity` itself → one owner orchestrates despawn/respawn; preserve health-before-flight ordering. SFX calls take an id/handle, not `IEntity`.
  - `HangarComponent` `LazyInject<IEntity> _carrier`: component injects its owner (rule violation) → entity passes the guard target when launching, or hangar receives `IEntityTransformFacade`-level data only.
  - `SkirmishOrchestrator` lazy `INotifier<BattleResult>` / `IUserStateNotifier`: identify the provider↔orchestrator cycle; invert subscription ownership or extract a coordinator.
  - `EnemyStructurePlacementService` lazy `IMapModelObserver`: map availability/ownership problem → map model owned by the battle parent and constructed before AI player composition.
- [x] **Optional injection — explicit capabilities (4 sites).**
  - `MonoComponent<TModel>` `[InjectOptional] TModel` (base of all entity components): components needing a model receive it explicitly; components without a model do not inherit an optional model slot. Split base or move model to the concrete component.
  - `HardPoint` optional `IFogVisibilityFacade`: explicit local-team vs fogged capability (e.g. a no-hide fog visibility for the local team bound explicitly), not absence.
  - `Ship` / `Squadron` optional `IAudioDialogShipComponent`: local-owner capability decided by the installer from owner data and the authored reference.
- [x] **Container instantiation — ship abilities.** Replace `ShipAbilityFactory(IInstantiator)` + ~20 `*Settings.CreateAbility(IInstantiator)` with explicit construction: factory receives a defined ability dependency set; each settings class constructs its ability with `new` from that set. No container in settings.
- [x] **Direct Resolve (3 sites).** WeaponModel accuracy copy → explicit factory/registration input, one copy per WeaponModel; tooltip `Timing` (MainMenu + Skirmish) → load/receive settings explicitly.
- [x] **Hierarchy bindings (16 sites) → serialized references.** Author explicit component references on the typed view root or a focused binding component; installers bind `FromInstance(reference)`. Convert prefab/variants through Unity-aware tools; validate hangar, fog, voice audio and S-Foils variants.
- [x] **Hidden searches.** `FromComponentInNewPrefab` (MainMenu, SkirmishMain, SkirmishService ×2, `ViewDependencyBuilder`) → typed component prefab. `FromComponentOn` re-lookups (`ViewComponentsInstaller`, `ModelDependencyInstaller`) → `FromInstance`. `InstallerExtensions` descendant inject scan → explicit references plus defined activation order.
- [x] **Aliases and exports.** `FromResolve` aliases → one registration exposed as self/interfaces. `FromResolveGetter` transform → bind held entity transform directly with `ViewTransform`. Remove `PlayerCoreInstaller` upward `IReinforcementService` export after confirming only `ReinforcementUiController` consumes it. Replace consumer-less `IEnemyService.WithId(slot.Id)` NonLazy export with explicit per-AI-slot composition.
- [x] **Container-injected classes.** `PlanetSpawner` `DiContainer.InstantiatePrefab` → typed factory; inspect `SpaceStationFactory` `DiContainer` legacy code.
- [ ] Verify per step: compile, existing EditMode tests, user in-game check of affected units; static scan shows the pattern count at zero.

### 3. Prove minimal VContainer infrastructure

- [x] Confirm the UI prerequisite is done; re-read its research and acceptance evidence.
- [x] Pin VContainer; create project scopes under `Assets/Scripts/Services/VContainer/`; shared factory code under `Services/Factory/` only when shared.
- [x] Adapt `VContainerExt` into narrow registration helpers with explicit asset-service inputs. Root owns one asset service; no static service or static mapping cache.
- [x] Preserve shared data vs mutable clones: catalogs/ship data shared; `GameData` / per-battle `MiniMapData` keep copy semantics and explicit cleanup.
- [x] Prototype one typed entity factory: runtime parameters, explicit components, `ViewTransform` key via `[Key]`/`.Keyed`, lifecycle, teardown; return the held typed entity.
- [x] Verify: no Resolve/discovery/lazy/optional APIs needed; initial callbacks see configured dependencies; two instances share no entity state; collection registration and health-data decoration work.

### 4. Scope hierarchy

| Target | Source composition | Ownership |
|---|---|---|
| `ProjectScope` | ProjectContextInstaller / ProjectContext prefab | One asset service, settings, input, audio/music, scene service, timers, IDs |
| `MainMenuScope` | MainMenuInstaller + MainMenuRouteInstaller | Menu/routes, tooltip and UI infrastructure |
| `LoadingScope` | LoadingInstaller / loading scene context | Loading presentation and target-scene activation |
| `SkirmishScope` | SkirmishMainInstaller + SkirmishServiceInstaller + map registrations | Battle/map state, shared registries, fog, navigation/combat, shared HUD |
| `LocalPlayerScope` | PlayerCoreInstaller | Local PlayerSlot, economy/faction/research, reinforcement, local UI, factories, squadron pool |
| `AiPlayerScope` per AI slot | AiPlayerInstaller | That AI's economy, production, strategy, factories and pool; created explicitly per slot |
| Entity scopes | Ship/Squadron/Station/Mining/Platform + asteroid variants | Spawn arguments, private runtime models, explicit entity/components/facades |
| Planet scope, if still needed | PlanetInstaller / PlanetSpawner | Explicit planet view/controller/data under the battle owner |

- `ProjectScope → MainMenuScope / LoadingScope / SkirmishScope`; battle → local/AI player scopes → entity scopes.
- No sibling scopes that resolve each other's services. Map dependencies stay in the battle scope.
- Preserve one instance per registration owner where Zenject used AsSingle; do not map every binding to `Lifetime.Scoped`.
- Preserve SceneService single-scene loading/activation and direct scene entry.
- Verify: player isolation, shared battle services access, return-to-menu teardown, no duplicate roots.

### 5. Factories

- [x] Keep typed Create signatures for Ship, Squadron, SpaceStation, MiningFacility, AsteroidMiningFacility, DefendPlatform, AsteroidDefendPlatform (see research table).
- [x] Minimal `BaseFactory`: injected `IAssetService` + owning `LifetimeScope`; no resolver.
- [x] Factory loads typed prefab, holds the instance, passes owner/type/pose/data into the child scope before build/activation (reference `EnemyFactory` flow without hierarchy scans).
- [x] Register the Phase 2 serialized references with `RegisterComponent`; explicit arrays for multi-bindings.
- [x] Keep the UI plan's typed UiFactory: no scope per screen.
- [x] Verify: correct owner, pose, data and destroy delay.

### 6. Lifecycle and registration semantics

- [x] Port entry points: plain C# IInitializable/ITickable/ILateTickable need entry-point dispatch, not only AsImplementedInterfaces.
- [x] One lifecycle owner for registered MonoBehaviours and EntityComponentLifecycle; no double init/tick/dispose.
- [x] Replace ILateDisposable and numeric execution orders with explicit phases/ordered coordinators where ordering matters; preserve frame-phase timing and reverse cleanup.
- [x] Preserve ResearchHealthData decoration and List binding multiplicity/order.
- [x] Verify init/tick/late-tick/teardown counters, player-registry registration, facade readiness, no graph cycles.

### 7. Pooling and ownership

- [x] Non-pooled entity releases its child scope once after death/destruction; remove GameObjectContext fields from Ship/Squadron.
- [x] Squadron pool return keeps instance/scope; only eviction or player/battle teardown disposes it.
- [x] Suppress scoped tick work while pooled.
- [x] Preserve health-before-flight respawn ordering, subscriptions, locator/facade identity, owner-keyed pools, cancelable delayed work.
- [x] Define ownership of externally created instances and cloned ScriptableObjects; `RegisterInstance` does not inject/dispose.
- [x] Verify spawn → despawn → respawn → final release for multiple owners; no double dispose, leaked ticks, stale selection or cross-player reuse.

### 8. Cutover and remove Zenject

- [x] Convert root/scene/player/entity composition and lifecycle signatures in one coordinated cutover; never two containers driving one instance.
- [x] Switch bootstrap, Loading/MainMenu/Battle/map contexts and Addressable wrapper prefabs; preserve GUIDs or update references explicitly.
- [x] Convert `[Inject(Id=…)]` / `WithId` (12 sites) to `[Key]` / `.Keyed`; `[Inject]` namespace swap (76 sites).
- [x] Migrate Zenject test fixtures/tooling; remove installers/attributes/context components and `Assets/Plugins/Zenject` after reference checks.
- [x] Audit asmdefs, defines, build scenes, Resources bootstrap and Editor tools; zero missing scripts. Do not restructure Addressables.
- [x] Update [[Architecture/PROJECT_ORGANIZATION]], UI composition docs and Zenject-specific rules.
- [x] Keep a recoverable pre-cutover checkpoint; revert the cutover coherently on failed acceptance.
- [x] Verify clean compile, imports/persistence, no project-owned Zenject references.

### 9. Completion gate

- [x] Focused composition/entity/pool/UI lifecycle regressions and standalone build. EditMode 1,182/1,229; same 47 baseline failures; 11 migration tests pass. Play Mode/in-game checks remain user-owned.
- [ ] Smoke test (user, in-game): direct scene entry and MainMenu → Loading → Battle → MainMenu twice; local + multiple AI players, production, reinforcement, capture-site builds, ship abilities, pooled squadrons, settings, audio.
- [x] Static audit: zero Resolve, hierarchy discovery, lazy/optional injection, container instantiation, hidden container dependencies.
- [x] Every modified prefab/scene persisted and imported; no manual save.
- [ ] Compare spawn/startup/teardown and frame timings with baseline; measure spawn bursts (reinforcements, mass squadron launch) for per-entity scope build cost. No performance claims from library change alone.
- [ ] Mark done only after acceptance; move plan/research to Done and repair links.

## Files

- Audit locations, scope/factory mapping, source references, regression targets: [[TODOs/Refactoring/Zenject_To_VContainer_Migration - Research]].
- Reference root: `F:/Private/SoulsLikeTemplate/Assets/Scripts/`; `Services/VContainer/`, `Utilities/Extensions/VContainerExt.cs`, `Services/Factory/BaseFactory.cs`, `Services/Ui/UiFactory.cs`, `Entities/Enemy/EnemyFactory.cs`.
- Coordinate with [[TODOs/Refactoring/Injection_And_Guard_Conventions]] and [[TODOs/Refactoring/Explicit_Component_Binding]]; Phase 2 hierarchy work overlaps the latter.

## TODO

- Execution started 2026-10-13. User verified the UI prerequisite's two battle/menu cycles and authorized cutover.
- Baseline EditMode: 1,586/1,633 passed; 47 pre-existing failures; 90.36 s. Evidence: `Temp/vcontainer-baseline-test-result.json`.
- Final EditMode: 1,182/1,229 passed; exactly the same 47 failed test names, zero new failures; 84.37 s. All 11 migration tests pass. Vendor Zenject tests were removed with the plugin. Evidence: `Temp/vcontainer-full-test-result.json`, `Temp/vcontainer-final-tests.xml`.
- StandaloneWindows64 build succeeded: 3,291,193,252 B, 708.005 s, 4 warnings. Release-only WeaponComponent debug-field assignment corrected; Editor behavior unchanged. Evidence: `Temp/vcontainer-build-report.json`, `Temp/VContainerMigrationBuild/EmpireAtWar.exe`.
- Build diagnostic: unchanged `Assets/Art/Shaders/PlanetAutodeskInteractiveNoShadows.shadergraph`, MotionVectors + DOTS_INSTANCING_ON, invalid `ObjectSpaceNormal`. BuildResult is Succeeded with this reported shader error; shader fixes are outside migration scope.
- Final Unity asset audit: zero missing scripts, legacy components or Zenject dependencies; no import/serialization errors. Generated font/content-state/settings changes from the build were restored.
- Post-cutover gameplay acceptance and matched performance capture remain pending. Baseline frame/spawn timings were not captured for this migration; no performance improvement is claimed.
- Recoverable pre-cutover checkpoint: `8f1fa454a` plus pre-existing working-tree patch `Temp/pre-vcontainer-worktree.patch`; migration commit will exclude unrelated changes.
- VContainer `1.19.0` pinned; five scope script moves and context prefab moves preserved GUIDs. Seven legacy Addressable entity wrappers remain inert scopes; Addressables structure unchanged.
- Asset audit: runtime scenes/prefabs have zero missing scripts, legacy context components or Zenject dependencies. `Assets/Plugins/Zenject` removed through Unity.
- Real graph regression passed: root/battle + local/AI players, 14 entities through all seven factories, planet scope, keyed transform/base health, mutable state isolation, local-only voice, pool scope/identity reuse, owner teardown before battle buffers.
- Per-player EntityScopeOwnership releases active/pooled children synchronously; native GameObject destruction remains owned by the scope.
- GUID verification passed: 1,561 retained assets, nine file moves; build scenes and Addressable entries unchanged. Evidence: `Temp/vcontainer-guid-verification.json`.
- Unity operations follow AGENTS scene-safety, official CLI and asset-persistence rules; no agent Play Mode runs.
