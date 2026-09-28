# Graph Report - empire-at-war  (2026-09-23)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 4837 nodes · 10725 edges · 223 communities (213 shown, 10 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 1009 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `713cf822`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- EmpireAtWar.Models.Factions
- MiningFacility
- EmpireAtWar.Entities.BaseEntity
- EmpireAtWar.Models.Health
- IShipNavigationAgent
- PipelineView
- IEntity
- ShipMoveComponent
- EmpireAtWar.Entities.Game
- FactionType
- FakeShip
- ShipNavigationServiceTests
- AttackSequenceDiagnostics
- MarqueeRectangle
- SelectionEntry
- .Tick
- SkirmishOrhestrator
- CameraService
- EmpireAtWar.Mvc
- .Run
- MenuController
- IHardPointModel
- .ScheduledBuildFailure_RefundsOnceAndDoesNotBlockLaterBuilds
- EnemyProductionStrategy
- RadarContact
- ShipMoveModel
- SceneService
- ShipMovementTweenPlayer
- EconomyService
- EnemyFactionData
- ShipAIBrain
- ShipUiController
- SelectionComponent
- AudioShipComponent
- ShipBezierRoute
- BattleResult
- SelectionContext
- ReinforcementZonesSystem
- Ship
- EmpireAtWar.Entities.BaseEntity.EntityCommands
- .InstallBindings
- SkirmishMainInstaller.cs
- MenuUiController
- @InputComponent_Generated
- IInitializable
- HealthComponent
- ShipData
- FakeTarget
- WeaponType
- ReinforcementZoneView
- PopupUi
- FormationPoint
- IShipMoveComponent
- SkirmishPopupUi
- EnemyStrategicSnapshot
- MiniMapData
- MiniMapMarker
- SelectionService
- Transform
- IHealthModelObserver
- AudioService
- FactionUi
- IShipEntity
- InputService
- UiType
- .InstallBindings
- WeaponHardPointView
- EnemyAiDifficulty
- EnemyProductionSnapshot
- PlayerFactionModel
- ISceneService
- EntityComponentData
- AttackSequenceState
- FakeEntity
- EnemyUnitLimitModel
- ReinforcementService
- StationCombatPresenter
- IAssetService
- HardPointView
- EnemyUnitCommander
- UnitSpawnView
- FakeShipMoveComponent
- BaseUi
- EmpireAtWar.Extentions
- EnemyFactionController
- FactionUnitUi
- GameData
- MarkView
- ReinforcementModel
- ReinforcementZoneData
- CameraInputBindingsTests
- DynamicEntityInstaller
- IHealthState
- TurretView
- HealthOverlayView
- RadarModel
- SceneReference
- ILateDisposable
- .PreferredShipAtLimit_SelectsAnotherAvailableShip
- .InstallBindings
- TouchMapActions
- BattlePerformanceCapture
- PlayerShipCommand
- LaserGun
- RadarComponent
- CombatAttackCoordinator.cs
- LoadingController
- MiniMapUi
- InvalidOperationException
- LayerKey
- FogOfWarSystem
- CheatServiceTests
- ShipAiSnapshot
- BattlePerformanceCaptureMetadata
- IAttackCommand
- MiniMapController
- DependencyBuilder
- StaticViewInstaller
- HealthModel
- BaseTurretView
- LaserTurretView
- CameraMarkData
- SpawnShipUi
- EnemyStrategicContext
- Transform
- HealthOverlayPresenter
- IEntitySelectionCommand
- FactionService
- CheatPresenter
- .BindComponents
- BattleVictoryModel
- ReinforcementUi
- IReinforcementUi
- ReinforcementZoneModel
- BattlePerformanceCaptureReport
- UiCanvasArchitectureTests
- AddressableAssetService
- CameraData
- ViewComponent
- CoreGameUi
- PlayerFactionData
- MiningFacilityData
- SpaceStationInstaller
- EnemyStructurePlacementServiceTests
- InstallerExtensions
- FakeReinforcementZonesSystem
- Vector3
- IModelObserver
- MonoComponent
- MonoBehaviour
- IUiService
- CheatView
- CountingMapModel
- .InstallBindings_ResolvesPlayerModelWithSelectedFaction
- ViewComponent
- HardPointModel
- ITickable
- PureModel
- .InstallBindings
- .CalculateSoftVisibility
- WeaponModel
- IPlayerFactionModelObserver
- ShipType
- MarkData
- EnemyTaskForceExecutor
- ITouchMapActions
- .Build
- RadarComponentStub
- OperationalEntityLocator
- EmpireAtWar.Ui.Popups
- HardPointAdapter
- ShieldView
- SelectionCommand
- RandomVector3
- IMapModelObserver
- HealthDataStub
- View
- EntityInstaller
- EnemyAiDifficultyProfile
- EnemyStrategicState
- PlanetData
- .InstallBindings
- ReinforcementZonePresenter
- SettingsPopupUi
- IHardPointView
- .RemoveObserver
- IMiniMapModelObserver
- ShipInstaller
- ShipUiData
- CameraFrustumProjection
- EnemyStructurePlacementService
- HealthCommand
- IObservableProperty
- .CalculateDestinations
- IShipMovementMediator
- SafeAreaHelper
- ModelDependency
- .Write_IncludesJobPhasesWorkloadAndSourceMetadata
- .SetUp
- FakeGameModel
- TurretType
- FrameworkComponent
- IHealthData
- IReinforcementPresenter
- ISpawnShipUi
- IReinforcementModelObserver
- FakeShipMoveData
- .OnStateUpdated
- HardPointType
- CollectionUtility
- ProductionQueueItem
- Entry
- .StartRecorder
- GameTimeMode
- .AnimateShields
- DotweenExtensions_v2.cs
- PlayerType
- InputType
- MapStub
- .ShipReinforcementUiPrefab_IsConfiguredCorrectly
- IVisitor
- ISelectableView.cs
- IEnemyAiDebugInfo

## God Nodes (most connected - your core abstractions)
1. `EmpireAtWar.Mvc` - 151 edges
2. `EmpireAtWar.Models.Factions` - 114 edges
3. `IEntity` - 104 edges
4. `ShipMoveComponent` - 63 edges
5. `RadarContact` - 55 edges
6. `EmpireAtWar.Entities.BaseEntity` - 54 edges
7. `AttackSequenceDiagnostics` - 51 edges
8. `Ship` - 49 edges
9. `HealthComponent` - 49 edges
10. `SkirmishOrhestrator` - 44 edges

## Surprising Connections (you probably didn't know these)
- `CancelSequence()` --references--> `WeaponHardPointView`  [EXTRACTED]
  Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs → Assets/Scripts/Components/ViewComponents/Health/WeaponHardPointView.cs
- `Controller` --implements--> `IController`  [EXTRACTED]
  Assets/Scripts/Components/BaseSystem/Controllers.cs → Assets/Scripts/Components/BaseSystem/IController.cs
- `IController` --inherits--> `IFrameworkObject`  [EXTRACTED]
  Assets/Scripts/Components/BaseSystem/IController.cs → Assets/Scripts/Components/BaseSystem/IFrameworkObject.cs
- `Ship` --implements--> `IController`  [EXTRACTED]
  Assets/Scripts/Entities/Ship/Ship.cs → Assets/Scripts/Components/BaseSystem/IController.cs
- `IModel` --inherits--> `IModelObserver`  [EXTRACTED]
  Assets/Scripts/Components/BaseSystem/IModel.cs → Assets/Scripts/Components/BaseSystem/IModelObserver.cs

## Import Cycles
- None detected.

## Communities (223 total, 10 thin omitted)

### Community 0 - "EmpireAtWar.Models.Factions"
Cohesion: 0.08
Nodes (20): EmpireAtWar.Entities.EnemyFaction.Controllers, EmpireAtWar.Services.Cheats, EmpireAtWar.Controllers.Factions, EmpireAtWar.Services.ReinforcementZones, EmpireAtWar.Controllers.Economy, EmpireAtWar.Models.Factions, EmpireAtWar.Models.Reinforcement, EmpireAtWar.Entities.EnemyFaction.Models (+12 more)

### Community 1 - "MiningFacility"
Cohesion: 0.05
Nodes (44): IController, IModel, ObservableList, IRadarComponent, Enemies, IReadOnlyList, EntityComponentLifecycle, IEntityLifecycle (+36 more)

### Community 2 - "EmpireAtWar.Entities.BaseEntity"
Cohesion: 0.06
Nodes (15): EmpireAtWar.Services.Layer, EmpireAtWar.Components.Movement.Formation, EmpireAtWar.Tests.Movement, EmpireAtWar.Entities.Ship.Mediator, EmpireAtWar.Patterns.StateMachine, EmpireAtWar.Services.UnitDeathAnimation, EmpireAtWar.Entities.Ship.StateMachine, EmpireAtWar.Utils.Random (+7 more)

### Community 3 - "EmpireAtWar.Models.Health"
Cohesion: 0.07
Nodes (22): Construct(), CombatAttackCoordinator, Inject, OnDestroy(), Release(), TargetCandidate, ProfilerMarker, BattleProfilerMarkers (+14 more)

### Community 4 - "IShipNavigationAgent"
Cohesion: 0.06
Nodes (32): Vector2, Vector2Range, IMapObstacleContactProvider, Dictionary, IReadOnlyList, List, Vector3, IShipNavigationAgent (+24 more)

### Community 5 - "PipelineView"
Cohesion: 0.05
Nodes (29): UnitRequest, ProductionQueueSnapshot, Count, RemainingBuildTime, UnitRequest, Action, CanvasGroup, Dictionary (+21 more)

### Community 6 - "IEntity"
Cohesion: 0.05
Nodes (36): PlayerType, Entity, HealthModel, Id, Model, PlayerType, IEntity, HealthModel (+28 more)

### Community 7 - "ShipMoveComponent"
Cohesion: 0.08
Nodes (25): Ease, Inject, IReadOnlyList, LineRenderer, List, NumericsQuaternion, NumericsVector3, PlayerType (+17 more)

### Community 8 - "EmpireAtWar.Entities.Game"
Cohesion: 0.06
Nodes (18): GameMode, Skirmish, Test, FleeStateTests, EmpireAtWar.Ui.Base, EmpireAtWar.Presenters.Reinforcement, EmpireAtWar.Views.Reinforcement, EmpireAtWar.Models.SkirmishGame (+10 more)

### Community 9 - "FactionType"
Cohesion: 0.09
Nodes (27): ClipType, Index, Inject, ITimer, PlayerType, Vector3, AudioDialogShipComponent, IAudioDialogShipComponent (+19 more)

### Community 10 - "FakeShip"
Cohesion: 0.07
Nodes (33): List, PlayerType, ShipType, Test, Transform, Vector3, EnemyUnitCommanderTests, FakeEntity (+25 more)

### Community 11 - "ShipNavigationServiceTests"
Cohesion: 0.11
Nodes (21): GameObject, IReadOnlyList, LineRenderer, SetUp, Test, Vector2, Vector3, FakeAgent (+13 more)

### Community 12 - "AttackSequenceDiagnostics"
Cohesion: 0.05
Nodes (28): AttackSequenceDiagnostics, ActiveSequences, AppliedImpacts, BatchTargetSelectionAttempts, BatchTargetSelectionTicks, CancelledImpacts, CandidateRebuilds, CandidateRebuildTicks (+20 more)

### Community 13 - "MarqueeRectangle"
Cohesion: 0.08
Nodes (24): MarqueePoint, X, Y, MarqueeRectangle, Height, MaxX, MaxY, MinX (+16 more)

### Community 14 - "SelectionEntry"
Cohesion: 0.08
Nodes (28): SelectionEntry, Command, Entity, ICollection, List, Vector2, ISelectionQuery, MarqueeCandidate (+20 more)

### Community 15 - ".Tick"
Cohesion: 0.07
Nodes (19): IBaseState, IReadOnlyList, Vector2, HealthCommand, HealthModel, WeaponComponent, LazyInject, Vector3 (+11 more)

### Community 16 - "SkirmishOrhestrator"
Cohesion: 0.09
Nodes (17): IGameCommand, ICoreGameCommand, Dictionary, GameTimeMode, INotifier, LazyInject, List, SkirmishOrhestrator (+9 more)

### Community 17 - "CameraService"
Cohesion: 0.10
Nodes (19): Camera, Ease, IReadOnlyList, Plane, RaycastHit, Vector2, Vector3, CameraService (+11 more)

### Community 18 - "EmpireAtWar.Mvc"
Cohesion: 0.07
Nodes (11): EmpireAtWar.Services.Player, EmpireAtWar.Models.Audio, EmpireAtWar.Entities.SpaceStation, EmpireAtWar.Mvc, EmpireAtWar.Ship, EmpireAtWar.Components.Ship.Audio, EmpireAtWar.SceneContext.Skirmish, EmpireAtWar.Services.Audio (+3 more)

### Community 19 - ".Run"
Cohesion: 0.10
Nodes (24): NativeArray, AttackDueJob, Input, float4, NativeArray, quaternion, Input, Result (+16 more)

### Community 20 - "MenuController"
Cohesion: 0.09
Nodes (15): List, IUserStateNotifier, MenuController, UserNotifierState, ExitGame, InGame, InMenu, IPauseMenuPresenter (+7 more)

### Community 21 - "IHardPointModel"
Cohesion: 0.07
Nodes (22): HardPointType, List, AttackData, HealthCommand, IsDestroyed, Units, HardPointType, Transform (+14 more)

### Community 22 - ".ScheduledBuildFailure_RefundsOnceAndDoesNotBlockLaterBuilds"
Cohesion: 0.09
Nodes (17): Vector3, IEnemyStructurePlacementService, Action, BindingFlags, CustomCoroutine, ICollection, Test, TimerPoolService (+9 more)

### Community 23 - "EnemyProductionStrategy"
Cohesion: 0.11
Nodes (22): DefendPlatformType, Xq6, DefendPlatformUnitRequest, MiningFacilityUnitRequest, ShipType, ShipUnitRequest, FactionData, ShipType (+14 more)

### Community 24 - "RadarContact"
Cohesion: 0.13
Nodes (16): ArgumentNullException, Vector3, RadarContact, IsShip, Position, Radius, IReadOnlyList, Vector2 (+8 more)

### Community 25 - "ShipMoveModel"
Cohesion: 0.08
Nodes (29): IShipMoveData, BodyRotationMaxAngle, Height, HyperSpaceDuration, NavigationRadius, RotationSpeed, Speed, NumericsQuaternion (+21 more)

### Community 26 - "SceneService"
Cohesion: 0.08
Nodes (24): PlanetType, Coruscant, Kamino, Dictionary, DictionaryWrapper, Scene, ISceneModelObserver, SceneData (+16 more)

### Community 27 - "ShipMovementTweenPlayer"
Cohesion: 0.10
Nodes (14): Action, Ease, LineRenderer, Quaternion, Sequence, Transform, Vector3, ShipMovementTweenPlayer (+6 more)

### Community 28 - "EconomyService"
Cohesion: 0.09
Nodes (14): EconomyModel, Money, ITimer, List, UnitRequest, EconomyService, Income, IEconomyProvider (+6 more)

### Community 29 - "EnemyFactionData"
Cohesion: 0.06
Nodes (33): Dictionary, FactionType, ShipType, EnemyFactionData, CurrentLevel, DefendPlatforms, FactionsModel, FactionType (+25 more)

### Community 30 - "ShipAIBrain"
Cohesion: 0.09
Nodes (25): Vector3, ShipAIBrain, IsFleeing, PlayerType, Test, Transform, Vector3, FakeEntity (+17 more)

### Community 31 - "ShipUiController"
Cohesion: 0.08
Nodes (23): Controller, Id, Model, IShipUiCommand, List, TouchPhase, Vector2, ShipUiController (+15 more)

### Community 32 - "SelectionComponent"
Cohesion: 0.07
Nodes (20): ISelectionCommand, Canvas, Image, PlayerType, SelectionType, SelectionComponent, PlayerType, WorldPosition (+12 more)

### Community 33 - "AudioShipComponent"
Cohesion: 0.09
Nodes (20): AudioClip, AudioSource, Inject, ITimer, TimerPoolService, AudioShipComponent, AssetReferenceT, AudioClip (+12 more)

### Community 34 - "ShipBezierRoute"
Cohesion: 0.12
Nodes (18): Vector3, ShipBezierPath, Vector3, CubicBezierSegment, P0, P1, P2, P3 (+10 more)

### Community 35 - "BattleResult"
Cohesion: 0.07
Nodes (23): Action, INotifier, EndGamePresenter, Button, TMP_Text, EndGameUi, IEndGameView, PlanetType (+15 more)

### Community 36 - "SelectionContext"
Cohesion: 0.08
Nodes (24): IReadOnlyList, List, SelectionContext, Count, Entities, Entity, HasSelectable, PlayerType (+16 more)

### Community 37 - "ReinforcementZonesSystem"
Cohesion: 0.15
Nodes (9): Dictionary, IReadOnlyList, List, PlayerType, ShipType, Vector3, IReinforcementZonesSystem, ReinforcementZonesSystem (+1 more)

### Community 38 - "Ship"
Cohesion: 0.08
Nodes (25): IComponent, IFrameworkObject, Id, ISelectionComponent, WorldPosition, IAudioShipComponent, IWeaponComponent, AttackDistance (+17 more)

### Community 39 - "EmpireAtWar.Entities.BaseEntity.EntityCommands"
Cohesion: 0.10
Nodes (12): EmpireAtWar.Components.Ship.Health.Overlay, EmpireAtWar.Components.Selection.Marquee, EmpireAtWar.Entities.BaseEntity.EntityCommands, EmpireAtWar.Models.ShipUi, EmpireAtWar.Commands.ShipUi, EmpireAtWar.Services.InputService, EmpireAtWar.Controllers.ShipUi, EmpireAtWar.Views (+4 more)

### Community 40 - ".InstallBindings"
Cohesion: 0.12
Nodes (18): IChainHandler, BasePurchaseProcessor, EnemyPurchaseProcessor, IBuildShipChain, IEnemyPurchaseProcessor, IPurchaseProcessor, PurchaseProcessor, FactionData (+10 more)

### Community 41 - "SkirmishMainInstaller.cs"
Cohesion: 0.11
Nodes (12): EmpireAtWar.Views.ReinforcementZones, EmpireAtWar.Models.ReinforcementZones, EmpireAtWar.Controllers.MiniMap, EmpireAtWar.Entities.Map, EmpireAtWar.Services.StationFacing, EmpireAtWar.Views.MiniMap, ViewComponents, EmpireAtWar.Models.MiniMap (+4 more)

### Community 42 - "MenuUiController"
Cohesion: 0.11
Nodes (9): IMenuUiPresenter, MainMenuOrchestrator, MenuUiController, IMenuUiModel, MenuUiModel, Button, IMenuUiView, MenuUiView (+1 more)

### Community 43 - "@InputComponent_Generated"
Cohesion: 0.07
Nodes (22): IEnumerable, IEnumerator, InputAction, InputActionAsset, List, @InputComponent_Generated, asset, bindingMask (+14 more)

### Community 44 - "IInitializable"
Cohesion: 0.08
Nodes (21): ICommand, IService, Service, Id, IMiningFacilityCommand, ISpaceStationCommand, FactionType, LazyInject (+13 more)

### Community 45 - "HealthComponent"
Cohesion: 0.08
Nodes (23): Coroutine, HardPointType, HealthModel, Inject, ITimer, List, PlayerType, Transform (+15 more)

### Community 46 - "ShipData"
Cohesion: 0.07
Nodes (31): FloatRange, ParticleSystem, ShipType, Vector3, IShipData, DeathExplosionVfx, MinMoveCoefficient, ShipData (+23 more)

### Community 47 - "FakeTarget"
Cohesion: 0.10
Nodes (25): Action, CombatAttackCoordinator, HardPointType, StringBuilder, Test, TestCase, Transform, Vector3 (+17 more)

### Community 48 - "WeaponType"
Cohesion: 0.08
Nodes (26): Vector3, ProjectileData, Color, Delay, DelayBetweenShots, ShotsPerSalvo, Size, TurretType (+18 more)

### Community 49 - "ReinforcementZoneView"
Cohesion: 0.11
Nodes (21): Canvas, CanvasScaler, Color, Image, MeshRenderer, PlayerType, TMP_Text, Vector3 (+13 more)

### Community 50 - "PopupUi"
Cohesion: 0.13
Nodes (15): IPopupCommand, Button, CanvasGroup, PopupUi, Transform, PopupUiFacade, Dictionary, IPopupService (+7 more)

### Community 51 - "FormationPoint"
Cohesion: 0.17
Nodes (10): IList, IReadOnlyList, List, FormationModel, FormationPoint, X, Z, TestCase (+2 more)

### Community 52 - "IShipMoveComponent"
Cohesion: 0.12
Nodes (11): Vector3, IShipMoveComponent, CurrentPosition, HyperSpaceDuration, IsBlocked, IsMoving, NavigationRadius, NavigationSpeed (+3 more)

### Community 53 - "SkirmishPopupUi"
Cohesion: 0.11
Nodes (13): Button, FactionType, PlanetType, TMP_Dropdown, TMP_Text, SkirmishPopupUi, GameCommand, BattleVictoryCondition (+5 more)

### Community 54 - "EnemyStrategicSnapshot"
Cohesion: 0.16
Nodes (14): EnemyStrategicDecisionModel, EnemyStrategicSnapshot, Difficulty, EnemyShipCount, EnemyShipsNearOwnBase, HasCaptureTarget, HasEnemyBaseTarget, HasOwnBase (+6 more)

### Community 55 - "MiniMapData"
Cohesion: 0.09
Nodes (24): MarkType, Camera, DefendPlatform, EnemyBase, EnemyMining, MiningFacility, PlayerBase, PlayerMining (+16 more)

### Community 56 - "MiniMapMarker"
Cohesion: 0.13
Nodes (12): PlayerType, MiniMapMarker, MarkType, Relation, Visible, WorldDiameter, PlayerType, SelectionType (+4 more)

### Community 57 - "SelectionService"
Cohesion: 0.14
Nodes (10): IObserver, IReadOnlyList, List, PlayerType, TouchPhase, Vector2, SelectionService, EnemySelectionContext (+2 more)

### Community 58 - "Transform"
Cohesion: 0.14
Nodes (18): Transform, Dictionary, Sequence, Vector3, IUnitDeathAnimationData, FallDownDirection, FallDownDuration, FallDownRotation (+10 more)

### Community 59 - "IHealthModelObserver"
Cohesion: 0.08
Nodes (22): IHealthComponent, Destroyed, HealthModelObserver, PlayerType, Transform, IHealthModelObserver, Armor, ArmorPercentage (+14 more)

### Community 60 - "AudioService"
Cohesion: 0.11
Nodes (11): AudioClip, AudioSource, List, Random, AudioService, IAudioService, AudioType, Dialog (+3 more)

### Community 61 - "FactionUi"
Cohesion: 0.14
Nodes (9): IUnitRequestFactory, List, SelectionType, Transform, FactionUi, IFactionUi, Transform, FactionUiController (+1 more)

### Community 62 - "IShipEntity"
Cohesion: 0.11
Nodes (18): IShipModelObserver, Vector3, IShipEntity, ModelObserver, NavigationRadius, NavigationSpeed, PlayerType, WorldPosition (+10 more)

### Community 63 - "InputService"
Cohesion: 0.13
Nodes (13): CallbackContext, TouchPhase, Vector2, InputService, CameraMove, CurrentTouchPhase, MapActions, SecondaryTouchPosition (+5 more)

### Community 64 - "UiType"
Cohesion: 0.09
Nodes (19): GameObject, Transform, UiFacade, GameObject, Inject, UiInstaller, Inject, UiType (+11 more)

### Community 65 - ".InstallBindings"
Cohesion: 0.10
Nodes (23): LayerData, DeadLayerMask, EnemyLayerMask, ObstacleLayerMask, PlayerLayerMask, Sprite, SharedSelectionData, SelectionSprite (+15 more)

### Community 66 - "WeaponHardPointView"
Cohesion: 0.10
Nodes (20): RocketLauncherHardPointView, CombatAttackCoordinator, FloatRange, Quaternion, WeaponType, WeaponHardPointView, DelayBetweenShots, Destroyed (+12 more)

### Community 67 - "EnemyAiDifficulty"
Cohesion: 0.18
Nodes (9): EnemyAiDifficulty, Easy, Hard, Medium, UltraHard, EnemyProductionDecisionModel, Test, TestCase (+1 more)

### Community 68 - "EnemyProductionSnapshot"
Cohesion: 0.08
Nodes (24): EnemyProductionCategory, Defense, Level, Mining, None, Ship, EnemyProductionSnapshot, CanBuildDefense (+16 more)

### Community 69 - "PlayerFactionModel"
Cohesion: 0.21
Nodes (12): LevelUnitRequest, Dictionary, Queue, UnitRequest, PlayerFactionModel, CurrentLevel, FactionType, SelectionType (+4 more)

### Community 70 - "ISceneService"
Cohesion: 0.10
Nodes (16): ArgumentException, FactionType, PlanetType, FactionType, PlanetType, GameController, ISceneService, IsSceneLoaded (+8 more)

### Community 71 - "EntityComponentData"
Cohesion: 0.09
Nodes (21): Inject, ISelectionModelObserver, IsSelected, SelectionModel, IsSelected, FloatRange, Vector3, EntityComponentData (+13 more)

### Community 72 - "AttackSequenceState"
Cohesion: 0.10
Nodes (10): AttackSequenceState, CurrentStatus, Generation, IsBusy, Status, Emitting, Ready, Released (+2 more)

### Community 73 - "FakeEntity"
Cohesion: 0.10
Nodes (18): Collider, RaycastHit, PlayerType, IViewEntity, Id, PlayerType, ViewEntity, Id (+10 more)

### Community 74 - "EnemyUnitLimitModel"
Cohesion: 0.22
Nodes (9): Dictionary, EnemyUnitLimitModel, CurrentUnitCapacity, ReleaseVersion, ShipOrdersCount, Test, EnemyUnitLimitModelTests, FakeRequest (+1 more)

### Community 75 - "ReinforcementService"
Cohesion: 0.14
Nodes (10): INotifier, ShipType, UnitRequest, Vector2, Vector3, ReinforcementService, SpawnType, DefendPlatform (+2 more)

### Community 76 - "StationCombatPresenter"
Cohesion: 0.12
Nodes (10): HardPointType, AttackDataFactory, IAttackDataFactory, AttackType, Base, MainTarget, ObservableList, StationCombatPresenter (+2 more)

### Community 77 - "IAssetService"
Cohesion: 0.11
Nodes (18): IAssetService, MainMenuInstaller, Repository, ProjectContextInstaller, SceneContext, EnemyCoreInstaller, Repository, SceneContext (+10 more)

### Community 78 - "HardPointView"
Cohesion: 0.11
Nodes (17): HardPointType, IObserver, List, Transform, Vector3, HardPointView, GameObject, HardPointType (+9 more)

### Community 79 - "EnemyUnitCommander"
Cohesion: 0.13
Nodes (13): EnemyStrategicDecision, CommittedShipCount, Reason, State, Dictionary, IReadOnlyList, Vector3, EnemyUnitCommander (+5 more)

### Community 80 - "UnitSpawnView"
Cohesion: 0.14
Nodes (12): Collider, Color, List, Material, MeshRenderer, Quaternion, Vector3, UnitSpawnView (+4 more)

### Community 81 - "FakeShipMoveComponent"
Cohesion: 0.10
Nodes (14): IReadOnlyList, Vector2, Vector3, FakeShipMoveComponent, CurrentPosition, HyperSpaceDuration, Id, IsBlocked (+6 more)

### Community 82 - "BaseUi"
Cohesion: 0.14
Nodes (8): CanvasGroup, BaseUi, Command, IsVisible, Model, Button, Image, ShipUi

### Community 83 - "EmpireAtWar.Extentions"
Cohesion: 0.11
Nodes (7): EntityBindType, ViewTransform, EmpireAtWar, EmpireAtWar.Extentions, EmpireAtWar.Services.IdGeneration, EmpireAtWar.SceneContext, EmpireAtWar.Presenters.MiniMap

### Community 84 - "EnemyFactionController"
Cohesion: 0.18
Nodes (11): Action, CustomCoroutine, Dictionary, PlayerType, ShipType, TimerPoolService, UnitRequest, Vector3 (+3 more)

### Community 85 - "FactionUnitUi"
Cohesion: 0.12
Nodes (11): UnitRequest, IFactionView, UnitRequest, Button, FactionData, Image, TextMeshProUGUI, UnitRequest (+3 more)

### Community 86 - "GameData"
Cohesion: 0.10
Nodes (20): FactionType, PlanetType, GameData, EnemyDifficulty, EnemyFactionType, GameMode, PlanetType, PlayerFactionType (+12 more)

### Community 87 - "MarkView"
Cohesion: 0.14
Nodes (10): Image, RectTransform, Sprite, Transform, Vector2, MarkView, IconImage, Vector2 (+2 more)

### Community 88 - "ReinforcementModel"
Cohesion: 0.13
Nodes (11): Dictionary, FactionData, ShipType, UnitRequest, ReinforcementModel, CapacityLeft, CurrentUnitCapacity, IsTrySpawning (+3 more)

### Community 89 - "ReinforcementZoneData"
Cohesion: 0.23
Nodes (12): ReinforcementZoneData, CaptureSpeedPerNetShip, BindingFlags, GameObject, PlayerType, Test, TestCase, Vector2 (+4 more)

### Community 90 - "CameraInputBindingsTests"
Cohesion: 0.13
Nodes (10): Vector2, CameraPanSmoothing, InputAction, InputActionAsset, SetUp, Test, CameraInputBindingsTests, Test (+2 more)

### Community 91 - "DynamicEntityInstaller"
Cohesion: 0.12
Nodes (12): DiContainer, Inject, Vector3, DynamicEntityInstaller, Entity, EntityTransformParent, ModelPathPostfix, ModelPathPrefix (+4 more)

### Community 92 - "IHealthState"
Cohesion: 0.13
Nodes (14): DamageModel, DictionaryWrapper, DamageCalculationData, DamageModel, AccuracyCoefficient, DexterityCoefficient, DamageData, ArmorDamage (+6 more)

### Community 93 - "TurretView"
Cohesion: 0.10
Nodes (15): Transform, Action, BaseTurretView, Dictionary, HashSet, ProjectileEffectPool, TorpedoProjectileView, FloatRange (+7 more)

### Community 94 - "HealthOverlayView"
Cohesion: 0.18
Nodes (11): Canvas, CanvasScaler, Color, GraphicRaycaster, MPImage, RectTransform, Vector2, HealthOverlayView (+3 more)

### Community 95 - "RadarModel"
Cohesion: 0.10
Nodes (21): ObservableList, PlayerType, IRadarData, Delay, Distance, Range, IRadarModelObserver, Delay (+13 more)

### Community 96 - "SceneReference"
Cohesion: 0.12
Nodes (12): Rect, SceneReference, IsEmpty, SceneName, ScenePath, SceneReferencePropertyDrawer, GUIContent, ISerializationCallbackReceiver (+4 more)

### Community 97 - "ILateDisposable"
Cohesion: 0.16
Nodes (9): IEconomyModelObserver, Money, TextMeshProUGUI, Transform, EconomyUi, IEconomyUi, Transform, EconomyUiController (+1 more)

### Community 98 - ".PreferredShipAtLimit_SelectsAnotherAvailableShip"
Cohesion: 0.17
Nodes (11): UnitRequestFactory, BindingFlags, ShipType, Vector3, EconomyModelStub, Money, EnemyProductionStrategyTests, StateProviderStub (+3 more)

### Community 99 - ".InstallBindings"
Cohesion: 0.21
Nodes (9): Action, Coroutine, Func, IEnumerator, CoroutineService, Id, ICoroutineService, TimerPoolService (+1 more)

### Community 100 - "TouchMapActions"
Cohesion: 0.13
Nodes (14): TouchMapActions, @CameraDrag, @CameraMove, enabled, @PrimaryContact, @PrimaryPosition, @Scroll, @SecondaryPosition (+6 more)

### Community 101 - "BattlePerformanceCapture"
Cohesion: 0.18
Nodes (5): DateTime, BattlePerformanceCapture, IsCapturing, CombatWorkload, ProfilerRecorder

### Community 102 - "PlayerShipCommand"
Cohesion: 0.13
Nodes (16): Command, Controller, Ship, EnemyShipCommand, Ship, Vector2, Vector3, IShipCommand (+8 more)

### Community 103 - "LaserGun"
Cohesion: 0.14
Nodes (11): Color, GameObject, LineRenderer, Material, RaycastHit, Transform, LaserGun, LaserStage (+3 more)

### Community 104 - "RadarComponent"
Cohesion: 0.13
Nodes (10): Collider, HashSet, Inject, ITimer, List, Vector3, RadarComponent, Enemies (+2 more)

### Community 105 - "CombatAttackCoordinator.cs"
Cohesion: 0.16
Nodes (17): BeginSequence(), CancelSequence(), IWeaponPresenter, Quaternion, Vector3, WeaponComponent, WeaponType, DueEvent (+9 more)

### Community 106 - "LoadingController"
Cohesion: 0.13
Nodes (11): LoadingController, ILoadingModelObserver, LoadingData, LoadingInstaller, LoadingView, PlanetInstaller, EmpireAtWar.Models.Loading, EmpireAtWar.Controllers.Loading (+3 more)

### Community 107 - "MiniMapUi"
Cohesion: 0.15
Nodes (12): Dictionary, Image, List, PointerEventData, Rect, RectTransform, Transform, MiniMapUi (+4 more)

### Community 108 - "InvalidOperationException"
Cohesion: 0.27
Nodes (6): Dictionary, ShipDestinationRegistry, Test, ShipDestinationRegistryTests, Entry, InvalidOperationException

### Community 109 - "LayerKey"
Cohesion: 0.16
Nodes (10): LayerMask, LayerKey, Dead, Enemy, Obstacle, Player, Dictionary, GameObject (+2 more)

### Community 110 - "FogOfWarSystem"
Cohesion: 0.18
Nodes (12): Color, List, Material, Transform, Vector2, Vector3, FogOfWarSystem, VisionSource (+4 more)

### Community 111 - "CheatServiceTests"
Cohesion: 0.13
Nodes (12): EconomyData, IncomeDelay, StartMoneyAmount, PlayerType, ShipType, Vector3, ShipFacadeFactory, ArgumentOutOfRangeException (+4 more)

### Community 112 - "ShipAiSnapshot"
Cohesion: 0.14
Nodes (16): ShipAiDecision, Attack, Flee, Idle, Navigate, ShipAiDecisionModel, ShipAiSnapshot, HasAssignedTarget (+8 more)

### Community 113 - "BattlePerformanceCaptureMetadata"
Cohesion: 0.11
Nodes (18): BattlePerformanceCaptureMetadata, BuildGuid, DueBatchSize, DueThreshold, GraphicsDevice, Processor, Quality, Resolution (+10 more)

### Community 114 - "IAttackCommand"
Cohesion: 0.13
Nodes (15): Vector3, IAttackCommand, NavigationRadius, WorldPosition, IEntityCommand, Ship, Vector3, EnemyAttackShipCommand (+7 more)

### Community 115 - "MiniMapController"
Cohesion: 0.13
Nodes (13): CustomCoroutine, TimerPoolService, Vector3, IMiniMapCommand, MiniMapController, TouchPhase, Vector2, IInputService (+5 more)

### Community 116 - "DependencyBuilder"
Cohesion: 0.12
Nodes (10): DiContainer, DependencyBuilder, Container, PathToFile, IDependencyBuilder, GameObject, PrefabDependencyBuilder, DiContainer (+2 more)

### Community 117 - "StaticViewInstaller"
Cohesion: 0.14
Nodes (11): GameObject, GameUnitsInstaller, Repository, Transform, MonoComponentInstaller, View, StaticViewInstaller, Repository (+3 more)

### Community 118 - "HealthModel"
Cohesion: 0.14
Nodes (13): HealthModel, Armor, ArmorPercentage, Dexterity, HardPointModels, HasShields, HasUnits, IsDestroyed (+5 more)

### Community 119 - "BaseTurretView"
Cohesion: 0.15
Nodes (5): ITimer, Transform, BaseTurretView, IsBusy, LeaseId

### Community 120 - "LaserTurretView"
Cohesion: 0.17
Nodes (10): Color, LineRenderer, Material, RaycastHit, LaserStage, Finished, Growth, Hold (+2 more)

### Community 121 - "CameraMarkData"
Cohesion: 0.14
Nodes (10): IReadOnlyList, List, CameraMarkData, Vertices, X, Z, Vector2, CameraFootprintView (+2 more)

### Community 122 - "SpawnShipUi"
Cohesion: 0.15
Nodes (8): Color, Image, PointerEventData, TextMeshProUGUI, SpawnShipUi, UnitType, IBeginDragHandler, IDragHandler

### Community 123 - "EnemyStrategicContext"
Cohesion: 0.18
Nodes (12): IReadOnlyList, List, PlayerType, Vector3, EnemyStrategicContext, CaptureTarget, EnemyBaseTarget, EnemyFleetTarget (+4 more)

### Community 124 - "Transform"
Cohesion: 0.15
Nodes (10): Transform, Canvas, MPImage, Test, PauseMenuUiPrefabTests, GameObject, RectTransform, Test (+2 more)

### Community 126 - "IEntitySelectionCommand"
Cohesion: 0.12
Nodes (13): SelectionType, IEntitySelectionCommand, SelectionType, PlayerType, SelectionType, FakeEntity, HealthModel, Id (+5 more)

### Community 127 - "FactionService"
Cohesion: 0.20
Nodes (6): PlayerType, LazyInject, UnitRequest, FactionService, Income, IFactionService

### Community 128 - "CheatPresenter"
Cohesion: 0.22
Nodes (6): Dictionary, FactionData, List, ShipType, CheatPresenter, ICheatService

### Community 129 - ".BindComponents"
Cohesion: 0.15
Nodes (12): RadarModel, DefendPlatformData, ComponentData, RadarModel, DefendPlatform, HealthModel, Inject, PlayerType (+4 more)

### Community 130 - "BattleVictoryModel"
Cohesion: 0.24
Nodes (8): BattleOutcome, Draw, EnemyVictory, None, PlayerVictory, BattleVictoryModel, Test, BattleVictoryModelTests

### Community 131 - "ReinforcementUi"
Cohesion: 0.20
Nodes (8): Button, CanvasGroup, Dictionary, ShipType, TextMeshProUGUI, IReinforcementVisitor, ReinforcementUi, ISpawnShipUi

### Community 132 - "IReinforcementUi"
Cohesion: 0.17
Nodes (3): Transform, IReinforcementUi, Transform

### Community 133 - "ReinforcementZoneModel"
Cohesion: 0.27
Nodes (8): PlayerType, ReinforcementZoneModel, CaptureProgress, CapturingPlayer, IsContested, Owner, Test, ReinforcementZoneModelTests

### Community 134 - "BattlePerformanceCaptureReport"
Cohesion: 0.28
Nodes (3): DateTime, StringBuilder, BattlePerformanceCaptureReport

### Community 135 - "UiCanvasArchitectureTests"
Cohesion: 0.22
Nodes (8): Button, Canvas, CanvasGroup, CanvasScaler, GameObject, GraphicRaycaster, Test, UiCanvasArchitectureTests

### Community 136 - "AddressableAssetService"
Cohesion: 0.20
Nodes (7): AssetReference, GameObject, AddressableAssetService, DictionaryWrapper, AssetMappingData, EmpireAtWar.Repository.Data, EmpireAtWar.Repository

### Community 137 - "CameraData"
Cohesion: 0.13
Nodes (13): Data, FloatRange, CameraData, MaxMoveRangeY, MinMoveRangeX, PanAcceleration, PanDeceleration, PanSpeed (+5 more)

### Community 138 - "ViewComponent"
Cohesion: 0.15
Nodes (9): ViewComponent, InjectedModelObserver, Model, ModelObserver, View, ModelDependencyInstaller, EmpireAtWar.Views.ViewImpl, EmpireAtWar.ViewComponents (+1 more)

### Community 139 - "CoreGameUi"
Cohesion: 0.18
Nodes (8): Button, DictionaryWrapper, GameTimeMode, Image, MPImage, Sprite, Transform, CoreGameUi

### Community 140 - "PlayerFactionData"
Cohesion: 0.23
Nodes (6): FactionData, IEnumerable, KeyValuePair, ShipType, PlayerFactionData, FactionUnit

### Community 141 - "MiningFacilityData"
Cohesion: 0.15
Nodes (12): RadarModel, MiningFacilityData, ComponentData, Income, RadarModel, HealthModel, Inject, PlayerType (+4 more)

### Community 142 - "SpaceStationInstaller"
Cohesion: 0.15
Nodes (12): RadarModel, SpaceStationData, ComponentData, RadarModel, HealthModel, Inject, PlayerType, RadarModel (+4 more)

### Community 143 - "EnemyStructurePlacementServiceTests"
Cohesion: 0.26
Nodes (6): BoxCollider, TearDown, Test, EnemyStructurePlacementServiceTests, MapStub, ZonesStub

### Community 144 - "InstallerExtensions"
Cohesion: 0.19
Nodes (6): DiContainer, InstallerExtensions, Action, DiContainer, ModelDependencyBuilder, ConcreteIdArgConditionCopyNonLazyBinder

### Community 145 - "FakeReinforcementZonesSystem"
Cohesion: 0.26
Nodes (5): List, PlayerType, ShipType, Vector3, FakeReinforcementZonesSystem

### Community 146 - "Vector3"
Cohesion: 0.29
Nodes (6): List, PlayerType, ShipType, Vector3, ZonesStub, Centers

### Community 147 - "IModelObserver"
Cohesion: 0.16
Nodes (11): IModelObserver, IAudioShipDialogModelObserver, IAudioShipModelObserver, IDefendPlatformModelObserver, IMiningFacilityModelObserver, ShipType, IShipModelObserver, ShipType (+3 more)

### Community 148 - "MonoComponent"
Cohesion: 0.16
Nodes (9): Inject, IMonoComponent, MonoComponent, Id, Model, Test, EntityComponentLifecycleTests, TrackingComponent (+1 more)

### Community 149 - "MonoBehaviour"
Cohesion: 0.16
Nodes (7): TextMeshProUGUI, FpsCounter, LineRenderer, DrawCircle, Color, MapBoundaryView, MonoBehaviour

### Community 150 - "IUiService"
Cohesion: 0.22
Nodes (10): Canvas, Transform, IUiService, DefaultCanvasTransform, DynamicCanvasTransform, PopupCanvasTransform, UiService, DefaultCanvasTransform (+2 more)

### Community 151 - "CheatView"
Cohesion: 0.23
Nodes (5): IReadOnlyList, Rect, ShipType, CheatView, ICheatView

### Community 152 - "CountingMapModel"
Cohesion: 0.23
Nodes (10): PlayerType, Quaternion, IStationFacingService, StationFacingService, Test, Vector3, CountingMapModel, PositionRequestCount (+2 more)

### Community 153 - ".InstallBindings_ResolvesPlayerModelWithSelectedFaction"
Cohesion: 0.18
Nodes (9): Dictionary, GameObject, SceneContext, ScriptableObject, TestCase, PlayerCoreInstallerTests, TestAssetService, PlayerCoreInstaller (+1 more)

### Community 154 - "ViewComponent"
Cohesion: 0.23
Nodes (7): BaseView, ViewComponents, IView, ViewComponents, ViewComponent, ModelObserver, View

### Community 155 - "HardPointModel"
Cohesion: 0.17
Nodes (9): HardPointType, HardPointModel, Generation, HardPointType, Health, HealthPercentage, Id, IsDestroyed (+1 more)

### Community 156 - "ITickable"
Cohesion: 0.15
Nodes (7): Vector3, PlanetController, Transform, PlanetView, TimerPoolService, TimerPoolTick, ITickable

### Community 157 - "PureModel"
Cohesion: 0.18
Nodes (9): AnimationCurve, Dictionary, DictionaryWrapper, DamageModel, Distance, DistanceCurve, WeaponDamageData, DamageDictionary (+1 more)

### Community 158 - ".InstallBindings"
Cohesion: 0.21
Nodes (9): Collider, MapObstacle, Contact, CombatAttackCoordinator, IReadOnlyList, IMapObstacleContactSource, Contact, MapObstacleContactProvider (+1 more)

### Community 159 - ".CalculateSoftVisibility"
Cohesion: 0.27
Nodes (4): FogVisibilityModel, Test, FogVisibilityModelTests, EmpireAtWar.Models.FogOfWar

### Community 160 - "WeaponModel"
Cohesion: 0.18
Nodes (8): IWeaponContext, DelayBetweenAttack, IEnumerable, WeaponModel, DelayBetweenAttack, OptimalAttackRange, ProjectileModel, WeaponDamageModel

### Community 161 - "IPlayerFactionModelObserver"
Cohesion: 0.20
Nodes (7): FactionData, IReadOnlyList, SelectionType, IPlayerFactionModelObserver, CurrentLevel, FactionType, SelectionType

### Community 162 - "ShipType"
Cohesion: 0.17
Nodes (11): ShipType, Acclamator, Arquitens, HeavyDreadnought, Lucrehulk, Munificent, Providence, Recusant (+3 more)

### Community 163 - "MarkData"
Cohesion: 0.17
Nodes (10): Sprite, Vector3, IMarkData, Icon, Position, Sprite, Vector3, MarkData (+2 more)

### Community 164 - "EnemyTaskForceExecutor"
Cohesion: 0.38
Nodes (5): Dictionary, IReadOnlyList, List, Vector3, EnemyTaskForceExecutor

### Community 166 - ".Build"
Cohesion: 0.26
Nodes (9): IReadOnlyList, Vector3, ShipRoutePlan, Destination, Detour, IsStationary, Route, TurnDuration (+1 more)

### Community 167 - "RadarComponentStub"
Cohesion: 0.23
Nodes (8): ObservableList, Test, Vector3, DefendPlatformTests, RadarComponentStub, Enemies, Id, Position

### Community 168 - "OperationalEntityLocator"
Cohesion: 0.17
Nodes (7): Collider, IReadOnlyCollection, PlayerType, RaycastHit, OperationalEntityLocator, Entities, Id

### Community 169 - "EmpireAtWar.Ui.Popups"
Cohesion: 0.29
Nodes (4): PopupInstaller, EmpireAtWar.Services.Popup, EmpireAtWar.Commands.PopupCommands, EmpireAtWar.Ui.Popups

### Community 170 - "HardPointAdapter"
Cohesion: 0.18
Nodes (10): HardPointType, Transform, Vector3, HardPointAdapter, Generation, HardPointType, HealthPercentage, Id (+2 more)

### Community 171 - "ShieldView"
Cohesion: 0.20
Nodes (5): Material, MeshRenderer, Vector2, ShieldView, IsVisibleToCamera

### Community 172 - "SelectionCommand"
Cohesion: 0.18
Nodes (8): Vector3, ISelectionPositionProvider, WorldPosition, SelectionType, Vector3, SelectionCommand, SelectionType, WorldPosition

### Community 173 - "RandomVector3"
Cohesion: 0.20
Nodes (9): RandomValue, Max, Min, Random, Vector3, RandomFloat, Random, RandomVector3 (+1 more)

### Community 174 - "IMapModelObserver"
Cohesion: 0.20
Nodes (9): Dictionary, DictionaryWrapper, Vector3, IMapModelObserver, SizeRange, MapData, SizeRange, StationPositions (+1 more)

### Community 175 - "HealthDataStub"
Cohesion: 0.27
Nodes (9): Test, DamageCalculatorStub, HealthDataStub, Armor, Dexterity, ShieldRegenerateDelay, ShieldRegenerateValue, Shields (+1 more)

### Community 176 - "View"
Cohesion: 0.27
Nodes (5): View, Command, Model, ModelDependencies, ModelObserver

### Community 177 - "EntityInstaller"
Cohesion: 0.27
Nodes (4): EntityInstaller, IUniqueIdGenerator, UniqueIdGenerator, Component

### Community 178 - "EnemyAiDifficultyProfile"
Cohesion: 0.20
Nodes (9): EnemyAiDifficultyProfile, CommittedFleetRatio, DecisionInterval, DefenseThreatRatio, MinimumControlledZones, MinimumMiningFacilities, OutnumberedRetreatCount, RequiredAttackRatio (+1 more)

### Community 179 - "EnemyStrategicState"
Cohesion: 0.20
Nodes (10): EnemyStrategicState, AssaultBase, CaptureZone, DefendBase, Hold, HuntFleet, RebuildFleet, IEnemyAiStateProvider (+2 more)

### Community 180 - "PlanetData"
Cohesion: 0.24
Nodes (9): Vector3, IPlanetModelObserver, CloudRotation, PlanetRotation, PlanetData, CloudOrbitSpeed, CloudRotation, PlanetOrbitSpeed (+1 more)

### Community 181 - ".InstallBindings"
Cohesion: 0.29
Nodes (3): ReinforcementUiController, IReinforcementService, FactionType

### Community 182 - "ReinforcementZonePresenter"
Cohesion: 0.24
Nodes (7): PlayerType, Vector3, ReinforcementZonePresenter, Center, IsCapturable, Owner, Radius

### Community 183 - "SettingsPopupUi"
Cohesion: 0.22
Nodes (5): Button, TMP_Dropdown, SettingsPopupUi, SettingsCommand, EmpireAtWar.Services.Settings

### Community 184 - "IHardPointView"
Cohesion: 0.22
Nodes (9): HardPointType, Transform, Vector3, IHardPointView, HardPointType, Id, IsDestroyed, Position (+1 more)

### Community 186 - "IMiniMapModelObserver"
Cohesion: 0.22
Nodes (9): IReadOnlyList, IMiniMapModelObserver, CameraMark, EnemyBase, IsInputBlocked, MapRange, Markers, MarkViewPrefab (+1 more)

### Community 187 - "ShipInstaller"
Cohesion: 0.28
Nodes (7): Inject, PlayerType, Ship, ShipType, ShipInstaller, PrefabPathPostfix, PrefabPathPrefix

### Community 188 - "ShipUiData"
Cohesion: 0.31
Nodes (7): DictionaryWrapper, ShipType, Sprite, IShipUiModelObserver, ShipIcon, ShipUiData, ShipIcon

### Community 189 - "CameraFrustumProjection"
Cohesion: 0.25
Nodes (7): Camera, IReadOnlyList, List, Plane, Vector2, Vector3, CameraFrustumProjection

### Community 190 - "EnemyStructurePlacementService"
Cohesion: 0.33
Nodes (5): LazyInject, List, Queue, Vector3, EnemyStructurePlacementService

### Community 191 - "HealthCommand"
Cohesion: 0.29
Nodes (4): WeaponType, IHealthCommand, HealthCommand, IEntityCommand

### Community 192 - "IObservableProperty"
Cohesion: 0.29
Nodes (7): IObservableProperty, HasValue, Value, ObservableProperty, HasValue, Value, EmpireAtWar.Models

### Community 193 - ".CalculateDestinations"
Cohesion: 0.25
Nodes (5): IList, IReadOnlyList, BattleFormationModel, Test, BattleFormationModelTests

### Community 195 - "SafeAreaHelper"
Cohesion: 0.36
Nodes (3): Rect, RectTransform, SafeAreaHelper

### Community 196 - "ModelDependency"
Cohesion: 0.39
Nodes (4): View, ModelDependency, Model, View

### Community 197 - ".Write_IncludesJobPhasesWorkloadAndSourceMetadata"
Cohesion: 0.29
Nodes (4): CombatWorkload, Test, BattlePerformanceCaptureReportTests, EmpireAtWar.Tests.Timing

### Community 198 - ".SetUp"
Cohesion: 0.32
Nodes (4): GameObject, LayerMask, SetUp, LayersStub

### Community 199 - "FakeGameModel"
Cohesion: 0.25
Nodes (8): FactionType, FakeGameModel, EnemyDifficulty, EnemyFactionType, PlanetType, PlayerFactionType, StartingMoney, VictoryCondition

### Community 200 - "TurretType"
Cohesion: 0.29
Nodes (6): TurretType, Dual, Laser, Rocket, Single, Torpedo

### Community 201 - "FrameworkComponent"
Cohesion: 0.29
Nodes (5): BaseComponent, Model, FrameworkComponent, Id, EmpireAtWar.Components

### Community 202 - "IHealthData"
Cohesion: 0.29
Nodes (6): IHealthData, Armor, Dexterity, ShieldRegenerateDelay, ShieldRegenerateValue, Shields

### Community 205 - "IReinforcementModelObserver"
Cohesion: 0.33
Nodes (5): IReinforcementModelObserver, CapacityLeft, CurrentUnitCapacity, IsTrySpawning, MaxUnitCapacity

### Community 206 - "FakeShipMoveData"
Cohesion: 0.29
Nodes (7): FakeShipMoveData, BodyRotationMaxAngle, Height, HyperSpaceDuration, NavigationRadius, RotationSpeed, Speed

### Community 208 - "HardPointType"
Cohesion: 0.33
Nodes (5): HardPointType, Any, Engines, ShieldGenerator, Weapon

### Community 209 - "CollectionUtility"
Cohesion: 0.33
Nodes (4): List, Random, CollectionUtility, EmpireAtWar.Collections

### Community 210 - "ProductionQueueItem"
Cohesion: 0.33
Nodes (4): UnitRequest, ProductionQueueItem, RemainingBuildTime, UnitRequest

### Community 211 - "Entry"
Cohesion: 0.33
Nodes (6): Func, Entry, ActiveFinalPosition, CurrentPosition, NavigationRadius, PendingFinalPosition

### Community 212 - ".StartRecorder"
Cohesion: 0.33
Nodes (3): ProfilerMarker, ProfilerCategory, ProfilerRecorderOptions

### Community 213 - "GameTimeMode"
Cohesion: 0.40
Nodes (4): GameTimeMode, Common, Pause, SpeedUp

### Community 215 - "DotweenExtensions_v2.cs"
Cohesion: 0.40
Nodes (3): Sequence, DotweenExtensions_v2, EmpireAtWar.Utils

### Community 216 - "PlayerType"
Cohesion: 0.40
Nodes (4): PlayerType, None, Opponent, Player

### Community 217 - "InputType"
Cohesion: 0.40
Nodes (4): InputType, CameraInput, Selection, ShipInput

### Community 218 - "MapStub"
Cohesion: 0.40
Nodes (3): FactionType, MapStub, SizeRange

### Community 219 - ".ShipReinforcementUiPrefab_IsConfiguredCorrectly"
Cohesion: 0.40
Nodes (4): MonoBehaviour, MPImage, Test, ShipReinforcementUiPrefabTests

### Community 222 - "IEnemyAiDebugInfo"
Cohesion: 0.67
Nodes (3): IEnemyAiDebugInfo, LastDecision, LastSnapshot

## Knowledge Gaps
- **1052 isolated node(s):** `DueEvent`, `Input`, `FakeRequest`, `Enemies`, `Id` (+1047 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1744 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EmpireAtWar.Mvc` connect `EmpireAtWar.Mvc` to `EmpireAtWar.Models.Factions`, `MiningFacility`, `EmpireAtWar.Entities.BaseEntity`, `EmpireAtWar.Models.Health`, `EmpireAtWar.Entities.Game`, `CameraData`, `ViewComponent`, `AddressableAssetService`, `MarqueeRectangle`, `IModelObserver`, `MonoComponent`, `ViewComponent`, `PureModel`, `Ship`, `EmpireAtWar.Entities.BaseEntity.EntityCommands`, `EmpireAtWar.Ui.Popups`, `SkirmishMainInstaller.cs`, `MenuUiController`, `IInitializable`, `EntityInstaller`, `PlanetData`, `.InstallBindings`, `FrameworkComponent`, `IAssetService`, `EmpireAtWar.Extentions`, `PlayerShipCommand`, `LoadingController`?**
  _High betweenness centrality (0.082) - this node is a cross-community bridge._
- **Why does `IEntity` connect `IEntity` to `EmpireAtWar.Models.Factions`, `MiningFacility`, `EmpireAtWar.Entities.BaseEntity`, `FakeShip`, `SelectionEntry`, `.Tick`, `EmpireAtWar.Mvc`, `IModelObserver`, `ShipAIBrain`, `ShipUiController`, `SelectionComponent`, `EnemyTaskForceExecutor`, `SelectionContext`, `EmpireAtWar.Entities.BaseEntity.EntityCommands`, `RadarComponentStub`, `OperationalEntityLocator`, `IShipMoveComponent`, `SelectionService`, `IHealthModelObserver`, `IShipEntity`, `FakeEntity`, `StationCombatPresenter`, `EnemyUnitCommander`, `RadarModel`, `RadarComponent`, `IAttackCommand`, `EnemyStrategicContext`, `HealthOverlayPresenter`, `IEntitySelectionCommand`?**
  _High betweenness centrality (0.055) - this node is a cross-community bridge._
- **Why does `EmpireAtWar.Models.Factions` connect `EmpireAtWar.Models.Factions` to `EmpireAtWar.Entities.BaseEntity`, `EmpireAtWar.Models.Health`, `ShipType`, `EmpireAtWar.Entities.BaseEntity.EntityCommands`, `EmpireAtWar.Entities.Game`, `SkirmishMainInstaller.cs`, `FakeEntity`, `FactionType`, `EmpireAtWar.Mvc`, `EmpireAtWar.Extentions`, `CheatView`, `PlayerType`, `EnemyFactionData`?**
  _High betweenness centrality (0.054) - this node is a cross-community bridge._
- **Are the 70 inferred relationships involving `InvalidOperationException` (e.g. with `.HandleHealthChanged()` and `.Initialize()`) actually correct?**
  _`InvalidOperationException` has 70 INFERRED edges - model-reasoned connections that need verification._
- **What connects `DueEvent`, `Input`, `FakeRequest` to the rest of the system?**
  _1052 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `EmpireAtWar.Models.Factions` be split into smaller, more focused modules?**
  _Cohesion score 0.07942097026604068 - nodes in this community are weakly interconnected._
- **Should `MiningFacility` be split into smaller, more focused modules?**
  _Cohesion score 0.04596273291925466 - nodes in this community are weakly interconnected._