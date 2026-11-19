# Test inventory

Static source inventory: **2026-11-14**. No tests were run.

## Scope and counts

- `Assets/Scripts/Tests/Editor`: **310 fixture source files** named `*Tests.cs`; compiled into the predefined Editor assembly (no local `.asmdef`).
- `Assets/Scripts/Tests`: **345 C# files** total, including support classes, benchmark implementations, and fixture wrappers.
- Benchmark coverage: two abstract test bases, instantiated by four concrete Editor/player fixture wrappers in two separate assemblies.
- Tooling coverage: five Python test modules and three PowerShell regression scripts.
- Counts describe source files, not discovered/runnable NUnit cases. Parameterized fixtures, `[TestCase]`, `[TestCaseSource]`, `[Values]`, inheritance, and explicit selection change runtime counts.
- Source scan found no `[Ignore]`, `Assert.Ignore`, or `Assert.Inconclusive` in the project test tree. Benchmark setup uses `Assume.That(BurstCompiler.IsEnabled, ...)`, which can prevent benchmark execution when Burst is disabled.

## Fixture categories

Counts are fixture source files carrying a category in the class header. A file can belong to multiple categories or provide multiple fixture instances; totals are not additive. Folder names do not always match categories.

| Category | Fixture files |
|---|---:|
| `Abilities` | 14 |
| `Audio` | 14 |
| `Balance` | 12 |
| `BattleInput` | 31 |
| `Capture` | 11 |
| `Combat` | 25 |
| `Core` | 26 |
| `Economy` | 12 |
| `EnemyAi` | 19 |
| `GameFlow` | 17 |
| `Health` | 9 |
| `Map` | 15 |
| `MapExhaustive` | 2 |
| `MapGeneration` | 3 |
| `MapSpaceDust` | 1 |
| `MusicReplacement` | 2 |
| `Navigation` | 24 |
| `NavigationExhaustive` | 1 |
| `Networking` | 6 |
| `Ships` | 21 |
| `Simulation` | 6 |
| `Squadrons` | 13 |
| `Ui` | 29 |
| `Vision` | 6 |

## Explicit suites

- `Map/MapLayoutGeneratorTests.cs`: quick `MapGeneration` and exhaustive `MapExhaustive` fixture instances; both explicit.
- `Map/MapArchetypeGenerationTests.cs`: quick `MapGeneration` and exhaustive `MapExhaustive` fixture instances; both explicit.
- `Navigation/ShipArchetypeRouteSweepTests.cs`: quick `Navigation` and exhaustive `NavigationExhaustive` fixture instances; both explicit.
- `MapGeneration` also appears on a non-explicit fixture; category membership alone does not imply an explicit test.
- Follow `AGENTS.md` for execution authorization and exact category selection. Inventory does not authorize running tests.

## Editor fixture files

All paths below are relative to `Assets/Scripts/Tests/Editor/`.

### Abilities (14)

- [AutoPowerRulesTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/AutoPowerRulesTests.cs)
- [CheatServiceTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/CheatServiceTests.cs)
- [HarmonicBombTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/HarmonicBombTests.cs)
- [PowerToMainBatteriesTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/PowerToMainBatteriesTests.cs)
- [ShieldAbilityMigrationTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/ShieldAbilityMigrationTests.cs)
- [ShipAbilityAudioTransitionTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/ShipAbilityAudioTransitionTests.cs)
- [ShipAbilityCatalogTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/ShipAbilityCatalogTests.cs)
- [ShipAbilityFactoryTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/ShipAbilityFactoryTests.cs)
- [ShipAbilityPerformanceTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/ShipAbilityPerformanceTests.cs)
- [ShipAbilityServiceTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/ShipAbilityServiceTests.cs)
- [ShipAbilitySubsystemAvailabilityTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/ShipAbilitySubsystemAvailabilityTests.cs)
- [ShipAbilitySymmetryTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/ShipAbilitySymmetryTests.cs)
- [SuperWeaponTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/SuperWeaponTests.cs)
- [TractorBeamSettingsTests.cs](../../Assets/Scripts/Tests/Editor/Abilities/TractorBeamSettingsTests.cs)

### AloImport (1)

- [AloEditorOperationTests.cs](../../Assets/Scripts/Tests/Editor/AloImport/AloEditorOperationTests.cs)

### Audio (14)

- [AudioSettingsApplierTests.cs](../../Assets/Scripts/Tests/Editor/Audio/AudioSettingsApplierTests.cs)
- [FactionAnnouncementTests.cs](../../Assets/Scripts/Tests/Editor/Audio/FactionAnnouncementTests.cs)
- [FactionAudioAssetTests.cs](../../Assets/Scripts/Tests/Editor/Audio/FactionAudioAssetTests.cs)
- [FactionAudioDomainEventTests.cs](../../Assets/Scripts/Tests/Editor/Audio/FactionAudioDomainEventTests.cs)
- [HardPointDamageAudioTests.cs](../../Assets/Scripts/Tests/Editor/Audio/HardPointDamageAudioTests.cs)
- [MenuFactionAudioTests.cs](../../Assets/Scripts/Tests/Editor/Audio/MenuFactionAudioTests.cs)
- [MusicReplacementAssetTests.cs](../../Assets/Scripts/Tests/Editor/Audio/MusicReplacementAssetTests.cs)
- [MusicServiceTests.cs](../../Assets/Scripts/Tests/Editor/Audio/MusicServiceTests.cs)
- [ShipAmbienceJobTests.cs](../../Assets/Scripts/Tests/Editor/Audio/ShipAmbienceJobTests.cs)
- [ShipEngineAudioModelTests.cs](../../Assets/Scripts/Tests/Editor/Audio/ShipEngineAudioModelTests.cs)
- [ShipSfxBudgetTests.cs](../../Assets/Scripts/Tests/Editor/Audio/ShipSfxBudgetTests.cs)
- [ShipSfxDataCoverageTests.cs](../../Assets/Scripts/Tests/Editor/Audio/ShipSfxDataCoverageTests.cs)
- [ShipShieldAudioStateTests.cs](../../Assets/Scripts/Tests/Editor/Audio/ShipShieldAudioStateTests.cs)
- [SuperWeaponAudioTests.cs](../../Assets/Scripts/Tests/Editor/Audio/SuperWeaponAudioTests.cs)

### Balance (12)

- [BalanceCompareOrderTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceCompareOrderTests.cs)
- [BalanceCompareSelectionTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceCompareSelectionTests.cs)
- [BalanceCompareTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceCompareTests.cs)
- [BalanceDpsEstimatorTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceDpsEstimatorTests.cs)
- [BalanceDraftTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceDraftTests.cs)
- [BalanceDraftUsageTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceDraftUsageTests.cs)
- [BalanceHardpointsPopupTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceHardpointsPopupTests.cs)
- [BalanceInventoryTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceInventoryTests.cs)
- [BalanceLayoutTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceLayoutTests.cs)
- [BalancePersistenceTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalancePersistenceTests.cs)
- [BalancePrefabTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalancePrefabTests.cs)
- [BalanceSharedMountTests.cs](../../Assets/Scripts/Tests/Editor/Balance/BalanceSharedMountTests.cs)

### BattleInput (28)

- [BattleInputPrefabTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/BattleInputPrefabTests.cs)
- [CameraInputBindingsTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/CameraInputBindingsTests.cs)
- [CameraNavigationTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/CameraNavigationTests.cs)
- [CameraZoomTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/CameraZoomTests.cs)
- [CinemaCameraLayerTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/CinemaCameraLayerTests.cs)
- [CinematicCameraModelTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/CinematicCameraModelTests.cs)
- [InputBindingServiceTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/InputBindingServiceTests.cs)
- [InputLockServiceTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/InputLockServiceTests.cs)
- [MarkerRadialModelTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/MarkerRadialModelTests.cs)
- [MarqueeSelectionUtilityTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/MarqueeSelectionUtilityTests.cs)
- [ModalRouteInputTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/ModalRouteInputTests.cs)
- [PauseMenuUiPrefabTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/PauseMenuUiPrefabTests.cs)
- [PlayerCommandSinkTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/PlayerCommandSinkTests.cs)
- [PlayerOrderInputHandlerTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/PlayerOrderInputHandlerTests.cs)
- [PointerGesturesTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/PointerGesturesTests.cs)
- [PointerOverUiTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/PointerOverUiTests.cs)
- [SelectionInputTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/SelectionInputTests.cs)
- [SelectionMarkerTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/SelectionMarkerTests.cs)
- [SelectionQueryTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/SelectionQueryTests.cs)
- [ShipSelectionReuseTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/ShipSelectionReuseTests.cs)
- [TacticalMarkerRegistryTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/TacticalMarkerRegistryTests.cs)
- [UiCancelRouterTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/UiCancelRouterTests.cs)
- [UnitActionsPresenterTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/UnitActionsPresenterTests.cs)
- [UnitHighlightFacadeTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/UnitHighlightFacadeTests.cs)
- [UnitHighlightSettingsTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/UnitHighlightSettingsTests.cs)
- [UnitHoverServiceTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/UnitHoverServiceTests.cs)
- [UnitOrderModelTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/UnitOrderModelTests.cs)
- [UnitOrderServiceTests.cs](../../Assets/Scripts/Tests/Editor/BattleInput/UnitOrderServiceTests.cs)

### BattleSimulator (4)

- [BattleBindingTests.cs](../../Assets/Scripts/Tests/Editor/BattleSimulator/BattleBindingTests.cs)
- [BattleSimulationTests.cs](../../Assets/Scripts/Tests/Editor/BattleSimulator/BattleSimulationTests.cs)
- [BattleSummaryTests.cs](../../Assets/Scripts/Tests/Editor/BattleSimulator/BattleSummaryTests.cs)
- [BattleWorkflowTests.cs](../../Assets/Scripts/Tests/Editor/BattleSimulator/BattleWorkflowTests.cs)

### Capture (11)

- [CaptureControlTests.cs](../../Assets/Scripts/Tests/Editor/Capture/CaptureControlTests.cs)
- [CaptureRingViewTests.cs](../../Assets/Scripts/Tests/Editor/Capture/CaptureRingViewTests.cs)
- [CaptureSiteBuildUiTests.cs](../../Assets/Scripts/Tests/Editor/Capture/CaptureSiteBuildUiTests.cs)
- [CaptureSiteModelTests.cs](../../Assets/Scripts/Tests/Editor/Capture/CaptureSiteModelTests.cs)
- [CaptureSitePresenterTests.cs](../../Assets/Scripts/Tests/Editor/Capture/CaptureSitePresenterTests.cs)
- [CaptureStrengthBuilderTests.cs](../../Assets/Scripts/Tests/Editor/Capture/CaptureStrengthBuilderTests.cs)
- [CaptureStrengthSamplerTests.cs](../../Assets/Scripts/Tests/Editor/Capture/CaptureStrengthSamplerTests.cs)
- [DefenseSatelliteContractTests.cs](../../Assets/Scripts/Tests/Editor/Capture/DefenseSatelliteContractTests.cs)
- [ReinforcementZoneModelTests.cs](../../Assets/Scripts/Tests/Editor/Capture/ReinforcementZoneModelTests.cs)
- [ReinforcementZonesSystemTests.cs](../../Assets/Scripts/Tests/Editor/Capture/ReinforcementZonesSystemTests.cs)
- [TargetSelectionCaptureTests.cs](../../Assets/Scripts/Tests/Editor/Capture/TargetSelectionCaptureTests.cs)

### Combat (25)

- [AttackDueJobTests.cs](../../Assets/Scripts/Tests/Editor/Combat/AttackDueJobTests.cs)
- [AttackMoveEngagementTests.cs](../../Assets/Scripts/Tests/Editor/Combat/AttackMoveEngagementTests.cs)
- [AttackSequenceStateTests.cs](../../Assets/Scripts/Tests/Editor/Combat/AttackSequenceStateTests.cs)
- [BeamVisualsTests.cs](../../Assets/Scripts/Tests/Editor/Combat/BeamVisualsTests.cs)
- [BoltFlightSchedulingTests.cs](../../Assets/Scripts/Tests/Editor/Combat/BoltFlightSchedulingTests.cs)
- [BoltParticleSimulationTests.cs](../../Assets/Scripts/Tests/Editor/Combat/BoltParticleSimulationTests.cs)
- [CombatAttackCoordinatorDueTests.cs](../../Assets/Scripts/Tests/Editor/Combat/CombatAttackCoordinatorDueTests.cs)
- [CombatAttackCoordinatorSelectionTests.cs](../../Assets/Scripts/Tests/Editor/Combat/CombatAttackCoordinatorSelectionTests.cs)
- [CombatMatchupTests.cs](../../Assets/Scripts/Tests/Editor/Combat/CombatMatchupTests.cs)
- [CombatModifiersTests.cs](../../Assets/Scripts/Tests/Editor/Combat/CombatModifiersTests.cs)
- [HitAccumulatorTests.cs](../../Assets/Scripts/Tests/Editor/Combat/HitAccumulatorTests.cs)
- [OverkillTargetingTests.cs](../../Assets/Scripts/Tests/Editor/Combat/OverkillTargetingTests.cs)
- [ShieldSurfaceClipParityTests.cs](../../Assets/Scripts/Tests/Editor/Combat/ShieldSurfaceClipParityTests.cs)
- [ShotEffectPoolLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Combat/ShotEffectPoolLifecycleTests.cs)
- [TargetOriginCaptureTests.cs](../../Assets/Scripts/Tests/Editor/Combat/TargetOriginCaptureTests.cs)
- [TorpedoSalvoModifierTests.cs](../../Assets/Scripts/Tests/Editor/Combat/TorpedoSalvoModifierTests.cs)
- [WeaponCadenceTests.cs](../../Assets/Scripts/Tests/Editor/Combat/WeaponCadenceTests.cs)
- [WeaponCandidateTests.cs](../../Assets/Scripts/Tests/Editor/Combat/WeaponCandidateTests.cs)
- [WeaponFacingSolverTests.cs](../../Assets/Scripts/Tests/Editor/Combat/WeaponFacingSolverTests.cs)
- [WeaponLoadoutBakeTests.cs](../../Assets/Scripts/Tests/Editor/Combat/WeaponLoadoutBakeTests.cs)
- [WeaponRangeTests.cs](../../Assets/Scripts/Tests/Editor/Combat/WeaponRangeTests.cs)
- [WeaponTargetSelectionDistanceParityTests.cs](../../Assets/Scripts/Tests/Editor/Combat/WeaponTargetSelectionDistanceParityTests.cs)
- [WeaponTargetSelectionJobTests.cs](../../Assets/Scripts/Tests/Editor/Combat/WeaponTargetSelectionJobTests.cs)
- [WeaponTaxonomyTests.cs](../../Assets/Scripts/Tests/Editor/Combat/WeaponTaxonomyTests.cs)
- [WeaponTurnRateTests.cs](../../Assets/Scripts/Tests/Editor/Combat/WeaponTurnRateTests.cs)

### Core (25)

- [AddressableHandleLoaderTests.cs](../../Assets/Scripts/Tests/Editor/Core/AddressableHandleLoaderTests.cs)
- [AssetMappingContextMenuTests.cs](../../Assets/Scripts/Tests/Editor/Core/AssetMappingContextMenuTests.cs)
- [AuditSharedOperationsTests.cs](../../Assets/Scripts/Tests/Editor/Core/AuditSharedOperationsTests.cs)
- [BattleAssetPreloaderTests.cs](../../Assets/Scripts/Tests/Editor/Core/BattleAssetPreloaderTests.cs)
- [BattlePerformanceCaptureReportTests.cs](../../Assets/Scripts/Tests/Editor/Core/BattlePerformanceCaptureReportTests.cs)
- [BuildInfoTests.cs](../../Assets/Scripts/Tests/Editor/Core/BuildInfoTests.cs)
- [CachedAssetServiceTests.cs](../../Assets/Scripts/Tests/Editor/Core/CachedAssetServiceTests.cs)
- [EntityBindingsTests.cs](../../Assets/Scripts/Tests/Editor/Core/EntityBindingsTests.cs)
- [EntityComponentLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Core/EntityComponentLifecycleTests.cs)
- [EntityFactoryLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Core/EntityFactoryLifecycleTests.cs)
- [EntityLocatorTests.cs](../../Assets/Scripts/Tests/Editor/Core/EntityLocatorTests.cs)
- [EntityOwnershipLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Core/EntityOwnershipLifecycleTests.cs)
- [LifecycleContractTests.cs](../../Assets/Scripts/Tests/Editor/Core/LifecycleContractTests.cs)
- [MetallicSmoothnessPackerTests.cs](../../Assets/Scripts/Tests/Editor/Core/MetallicSmoothnessPackerTests.cs)
- [PipelineViewLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Core/PipelineViewLifecycleTests.cs)
- [PlayerCoreInstallerTests.cs](../../Assets/Scripts/Tests/Editor/Core/PlayerCoreInstallerTests.cs)
- [PostTickLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Core/PostTickLifecycleTests.cs)
- [RealEntityCompositionTests.cs](../../Assets/Scripts/Tests/Editor/Core/RealEntityCompositionTests.cs)
- [SceneDataTests.cs](../../Assets/Scripts/Tests/Editor/Core/SceneDataTests.cs)
- [SerializedReferenceTests.cs](../../Assets/Scripts/Tests/Editor/Core/SerializedReferenceTests.cs)
- [SlowTickServiceTests.cs](../../Assets/Scripts/Tests/Editor/Core/SlowTickServiceTests.cs)
- [TestCategoryCoverageTests.cs](../../Assets/Scripts/Tests/Editor/Core/TestCategoryCoverageTests.cs)
- [UniqueIdGeneratorTests.cs](../../Assets/Scripts/Tests/Editor/Core/UniqueIdGeneratorTests.cs)
- [UtilityGeometryTests.cs](../../Assets/Scripts/Tests/Editor/Core/UtilityGeometryTests.cs)
- [VContainerCompositionTests.cs](../../Assets/Scripts/Tests/Editor/Core/VContainerCompositionTests.cs)

### Economy (12)

- [EconomyModelTests.cs](../../Assets/Scripts/Tests/Editor/Economy/EconomyModelTests.cs)
- [EconomyTuningTests.cs](../../Assets/Scripts/Tests/Editor/Economy/EconomyTuningTests.cs)
- [FactionServiceStationLossTests.cs](../../Assets/Scripts/Tests/Editor/Economy/FactionServiceStationLossTests.cs)
- [FactionUnitAvailabilityTests.cs](../../Assets/Scripts/Tests/Editor/Economy/FactionUnitAvailabilityTests.cs)
- [PlayerFactionProductionTests.cs](../../Assets/Scripts/Tests/Editor/Economy/PlayerFactionProductionTests.cs)
- [ProductionAccessTests.cs](../../Assets/Scripts/Tests/Editor/Economy/ProductionAccessTests.cs)
- [ReinforcementDeployableCountTests.cs](../../Assets/Scripts/Tests/Editor/Economy/ReinforcementDeployableCountTests.cs)
- [ReinforcementInteractionTests.cs](../../Assets/Scripts/Tests/Editor/Economy/ReinforcementInteractionTests.cs)
- [ReinforcementPreviewRotationTests.cs](../../Assets/Scripts/Tests/Editor/Economy/ReinforcementPreviewRotationTests.cs)
- [ReinforcementSpawnRuleTests.cs](../../Assets/Scripts/Tests/Editor/Economy/ReinforcementSpawnRuleTests.cs)
- [ShipReinforcementUiPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Economy/ShipReinforcementUiPrefabTests.cs)
- [UnitSpawnViewTests.cs](../../Assets/Scripts/Tests/Editor/Economy/UnitSpawnViewTests.cs)

### EnemyAi (20)

- [AllyAiMarkerDecisionTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/AllyAiMarkerDecisionTests.cs)
- [AllyAiMarkerObjectiveTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/AllyAiMarkerObjectiveTests.cs)
- [EnemyDecisionStabilityTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyDecisionStabilityTests.cs)
- [EnemyDefenseStrategyTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyDefenseStrategyTests.cs)
- [EnemyFactionControllerLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyFactionControllerLifecycleTests.cs)
- [EnemyPatrolMemoryTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyPatrolMemoryTests.cs)
- [EnemyProductionDecisionModelTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyProductionDecisionModelTests.cs)
- [EnemyRecoveryAnchorTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyRecoveryAnchorTests.cs)
- [EnemyRecoveryLatchTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyRecoveryLatchTests.cs)
- [EnemyShipAbilityControllerTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyShipAbilityControllerTests.cs)
- [EnemySquadronCaptureAssignmentsTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemySquadronCaptureAssignmentsTests.cs)
- [EnemyStationPressureTrackerTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyStationPressureTrackerTests.cs)
- [EnemyStrategicDecisionModelTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyStrategicDecisionModelTests.cs)
- [EnemyStructurePlacementServiceTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyStructurePlacementServiceTests.cs)
- [EnemyTaskForceSelectionModelTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyTaskForceSelectionModelTests.cs)
- [EnemyUnitCommanderTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyUnitCommanderTests.cs)
- [EnemyUnitLimitModelTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyUnitLimitModelTests.cs)
- [EnemyZoneAllocatorTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/EnemyZoneAllocatorTests.cs)
- [ForceCompositionBuilderTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/ForceCompositionBuilderTests.cs)
- [HostileIntelModelTests.cs](../../Assets/Scripts/Tests/Editor/EnemyAi/HostileIntelModelTests.cs)

### GameFlow (17)

- [AutosaveScheduleTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/AutosaveScheduleTests.cs)
- [BattleDefinitionTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/BattleDefinitionTests.cs)
- [BattleSaveRepositoryTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/BattleSaveRepositoryTests.cs)
- [BattleSaveServiceTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/BattleSaveServiceTests.cs)
- [BattleSetupSaveTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/BattleSetupSaveTests.cs)
- [BattleVictoryModelTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/BattleVictoryModelTests.cs)
- [BattleVictoryServiceTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/BattleVictoryServiceTests.cs)
- [GameControllerTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/GameControllerTests.cs)
- [JsonSettingsRepositoryTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/JsonSettingsRepositoryTests.cs)
- [MatchSessionTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/MatchSessionTests.cs)
- [PlayerSaveTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/PlayerSaveTests.cs)
- [SkirmishModelTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/SkirmishModelTests.cs)
- [SkirmishOrchestratorTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/SkirmishOrchestratorTests.cs)
- [SurvivalCoopTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/SurvivalCoopTests.cs)
- [SurvivalGameplayTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/SurvivalGameplayTests.cs)
- [SurvivalSpawnTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/SurvivalSpawnTests.cs)
- [UnitSaveTests.cs](../../Assets/Scripts/Tests/Editor/GameFlow/UnitSaveTests.cs)

### Health (9)

- [HardPointDestructionVfxPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Health/HardPointDestructionVfxPrefabTests.cs)
- [HardPointExplosionPoolTests.cs](../../Assets/Scripts/Tests/Editor/Health/HardPointExplosionPoolTests.cs)
- [HardPointMarkerViewTests.cs](../../Assets/Scripts/Tests/Editor/Health/HardPointMarkerViewTests.cs)
- [HealthModelTests.cs](../../Assets/Scripts/Tests/Editor/Health/HealthModelTests.cs)
- [HealthOverlayAnchorTests.cs](../../Assets/Scripts/Tests/Editor/Health/HealthOverlayAnchorTests.cs)
- [ShipExplosionPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Health/ShipExplosionPrefabTests.cs)
- [UnitWreckAssetDependencyTests.cs](../../Assets/Scripts/Tests/Editor/Health/UnitWreckAssetDependencyTests.cs)
- [WreckCutPlanTests.cs](../../Assets/Scripts/Tests/Editor/Health/WreckCutPlanTests.cs)
- [WreckMaterialSyncTests.cs](../../Assets/Scripts/Tests/Editor/Health/WreckMaterialSyncTests.cs)

### Map (18)

- [AsteroidFieldHeightTests.cs](../../Assets/Scripts/Tests/Editor/Map/AsteroidFieldHeightTests.cs)
- [BattleMapLoaderHazardTests.cs](../../Assets/Scripts/Tests/Editor/Map/BattleMapLoaderHazardTests.cs)
- [DecorAsteroidBuilderTests.cs](../../Assets/Scripts/Tests/Editor/Map/DecorAsteroidBuilderTests.cs)
- [HazardHeightTests.cs](../../Assets/Scripts/Tests/Editor/Map/HazardHeightTests.cs)
- [HazardServiceTests.cs](../../Assets/Scripts/Tests/Editor/Map/HazardServiceTests.cs)
- [HazardZoneTests.cs](../../Assets/Scripts/Tests/Editor/Map/HazardZoneTests.cs)
- [MapArchetypeGenerationTests.cs](../../Assets/Scripts/Tests/Editor/Map/MapArchetypeGenerationTests.cs)
- [MapArchetypeRuleTests.cs](../../Assets/Scripts/Tests/Editor/Map/MapArchetypeRuleTests.cs)
- [MapDecorationTests.cs](../../Assets/Scripts/Tests/Editor/Map/MapDecorationTests.cs)
- [MapLayoutGeneratorTests.cs](../../Assets/Scripts/Tests/Editor/Map/MapLayoutGeneratorTests.cs)
- [MapPlanetRosterTests.cs](../../Assets/Scripts/Tests/Editor/Map/MapPlanetRosterTests.cs)
- [MapSpaceDustTests.cs](../../Assets/Scripts/Tests/Editor/Map/MapSpaceDustTests.cs)
- [MapStationSlotTests.cs](../../Assets/Scripts/Tests/Editor/Map/MapStationSlotTests.cs)
- [MatchNebulaTests.cs](../../Assets/Scripts/Tests/Editor/Map/MatchNebulaTests.cs)
- [PlanetCatalogTests.cs](../../Assets/Scripts/Tests/Editor/Map/PlanetCatalogTests.cs)
- [PlanetObstaclePresentationTests.cs](../../Assets/Scripts/Tests/Editor/Map/PlanetObstaclePresentationTests.cs)
- [SpaceHazardPresentationTests.cs](../../Assets/Scripts/Tests/Editor/Map/SpaceHazardPresentationTests.cs)
- [StationAreaClearanceTests.cs](../../Assets/Scripts/Tests/Editor/Map/StationAreaClearanceTests.cs)

### Navigation (24)

- [BattleAreaServiceTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/BattleAreaServiceTests.cs)
- [BattleFormationModelTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/BattleFormationModelTests.cs)
- [FleetCohesionTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/FleetCohesionTests.cs)
- [FormationModelTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/FormationModelTests.cs)
- [NavigateStateTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/NavigateStateTests.cs)
- [NavigationGridCellsTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/NavigationGridCellsTests.cs)
- [NavigationOptimizationParityTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/NavigationOptimizationParityTests.cs)
- [ShipArchetypeRouteSweepTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipArchetypeRouteSweepTests.cs)
- [ShipAvoidancePlannerTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipAvoidancePlannerTests.cs)
- [ShipDestinationRegistryTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipDestinationRegistryTests.cs)
- [ShipHeightTierTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipHeightTierTests.cs)
- [ShipHullFootprintTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipHullFootprintTests.cs)
- [ShipMoveModelTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipMoveModelTests.cs)
- [ShipNavigationServiceTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipNavigationServiceTests.cs)
- [ShipNavMeshTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipNavMeshTests.cs)
- [ShipPursuitTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipPursuitTests.cs)
- [ShipRotationKinematicsTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipRotationKinematicsTests.cs)
- [ShipRouteTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipRouteTests.cs)
- [ShipSpawnClearanceGridTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipSpawnClearanceGridTests.cs)
- [ShipStopTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipStopTests.cs)
- [ShipTangentRouteBuilderTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/ShipTangentRouteBuilderTests.cs)
- [SpawnAreaGridModelTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/SpawnAreaGridModelTests.cs)
- [StationFacingServiceTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/StationFacingServiceTests.cs)
- [VelocitySmoothingTests.cs](../../Assets/Scripts/Tests/Editor/Navigation/VelocitySmoothingTests.cs)

### Networking (6)

- [NetworkTickReplicationTests.cs](../../Assets/Scripts/Tests/Editor/Networking/NetworkTickReplicationTests.cs)
- [OnlineLobbyModeTests.cs](../../Assets/Scripts/Tests/Editor/Networking/OnlineLobbyModeTests.cs)
- [SteamPlatformLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Networking/SteamPlatformLifecycleTests.cs)
- [SuperWeaponCooldownReplicationTests.cs](../../Assets/Scripts/Tests/Editor/Networking/SuperWeaponCooldownReplicationTests.cs)
- [TacticalMarkerNetworkingTests.cs](../../Assets/Scripts/Tests/Editor/Networking/TacticalMarkerNetworkingTests.cs)
- [TacticalMarkerRepliesTests.cs](../../Assets/Scripts/Tests/Editor/Networking/TacticalMarkerRepliesTests.cs)

### Ships (21)

- [AllegianceShipTests.cs](../../Assets/Scripts/Tests/Editor/Ships/AllegianceShipTests.cs)
- [CisStationImportTests.cs](../../Assets/Scripts/Tests/Editor/Ships/CisStationImportTests.cs)
- [CorvetteLoadoutTests.cs](../../Assets/Scripts/Tests/Editor/Ships/CorvetteLoadoutTests.cs)
- [DefendPlatformTests.cs](../../Assets/Scripts/Tests/Editor/Ships/DefendPlatformTests.cs)
- [EmpressDefensePlatformCombatTests.cs](../../Assets/Scripts/Tests/Editor/Ships/EmpressDefensePlatformCombatTests.cs)
- [ExecutorShipTests.cs](../../Assets/Scripts/Tests/Editor/Ships/ExecutorShipTests.cs)
- [ForwardMainBatteryTests.cs](../../Assets/Scripts/Tests/Editor/Ships/ForwardMainBatteryTests.cs)
- [HardPointPlacementToolTests.cs](../../Assets/Scripts/Tests/Editor/Ships/HardPointPlacementToolTests.cs)
- [ISDIIShipTests.cs](../../Assets/Scripts/Tests/Editor/Ships/ISDIIShipTests.cs)
- [MC30cPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Ships/MC30cPrefabTests.cs)
- [ProcursatorShipTests.cs](../../Assets/Scripts/Tests/Editor/Ships/ProcursatorShipTests.cs)
- [RaiderCorvetteTests.cs](../../Assets/Scripts/Tests/Editor/Ships/RaiderCorvetteTests.cs)
- [ShipAssetIntegrityTests.cs](../../Assets/Scripts/Tests/Editor/Ships/ShipAssetIntegrityTests.cs)
- [ShipBattleMarkerAssetTests.cs](../../Assets/Scripts/Tests/Editor/Ships/ShipBattleMarkerAssetTests.cs)
- [ShipEngineHardpointTests.cs](../../Assets/Scripts/Tests/Editor/Ships/ShipEngineHardpointTests.cs)
- [ShipSpawnPointsTests.cs](../../Assets/Scripts/Tests/Editor/Ships/ShipSpawnPointsTests.cs)
- [ShipSystemHardpointTests.cs](../../Assets/Scripts/Tests/Editor/Ships/ShipSystemHardpointTests.cs)
- [TartanTests.cs](../../Assets/Scripts/Tests/Editor/Ships/TartanTests.cs)
- [TitanShipTests.cs](../../Assets/Scripts/Tests/Editor/Ships/TitanShipTests.cs)
- [UnitHelperMeshTests.cs](../../Assets/Scripts/Tests/Editor/Ships/UnitHelperMeshTests.cs)
- [UnitRenderStateComponentTests.cs](../../Assets/Scripts/Tests/Editor/Ships/UnitRenderStateComponentTests.cs)

### Simulation (2)

- [SimulationGameplayTests.cs](../../Assets/Scripts/Tests/Editor/Simulation/SimulationGameplayTests.cs)
- [SimulationMapNavigationTests.cs](../../Assets/Scripts/Tests/Editor/Simulation/SimulationMapNavigationTests.cs)

### Squadrons (13)

- [FighterCoordinationModifierTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/FighterCoordinationModifierTests.cs)
- [FighterExplosionPoolTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/FighterExplosionPoolTests.cs)
- [FighterFlightTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/FighterFlightTests.cs)
- [FighterPoseBatchTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/FighterPoseBatchTests.cs)
- [FighterViewTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/FighterViewTests.cs)
- [HangarModelTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/HangarModelTests.cs)
- [QuasarFireHangarTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/QuasarFireHangarTests.cs)
- [SquadronHealthModelTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/SquadronHealthModelTests.cs)
- [SquadronIconGroupingTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/SquadronIconGroupingTests.cs)
- [SquadronIconPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/SquadronIconPrefabTests.cs)
- [SquadronLauncherTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/SquadronLauncherTests.cs)
- [SquadronOrderTimingTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/SquadronOrderTimingTests.cs)
- [SquadronTargetSelectorTests.cs](../../Assets/Scripts/Tests/Editor/Squadrons/SquadronTargetSelectorTests.cs)

### Ui (29)

- [BattleNotificationFeedTests.cs](../../Assets/Scripts/Tests/Editor/Ui/BattleNotificationFeedTests.cs)
- [DisplayConfirmationCountdownTests.cs](../../Assets/Scripts/Tests/Editor/Ui/DisplayConfirmationCountdownTests.cs)
- [EndGameUiLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Ui/EndGameUiLifecycleTests.cs)
- [FadeUiControllerTests.cs](../../Assets/Scripts/Tests/Editor/Ui/FadeUiControllerTests.cs)
- [HeroUiControllerLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Ui/HeroUiControllerLifecycleTests.cs)
- [LoadingUiIsolationTests.cs](../../Assets/Scripts/Tests/Editor/Ui/LoadingUiIsolationTests.cs)
- [MainMenuUiControllerLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Ui/MainMenuUiControllerLifecycleTests.cs)
- [MarkerNotificationTests.cs](../../Assets/Scripts/Tests/Editor/Ui/MarkerNotificationTests.cs)
- [MiniMapUiLifecycleTests.cs](../../Assets/Scripts/Tests/Editor/Ui/MiniMapUiLifecycleTests.cs)
- [MPImageMeshTests.cs](../../Assets/Scripts/Tests/Editor/Ui/MPImageMeshTests.cs)
- [PlanetSelectionTests.cs](../../Assets/Scripts/Tests/Editor/Ui/PlanetSelectionTests.cs)
- [PopupServiceTests.cs](../../Assets/Scripts/Tests/Editor/Ui/PopupServiceTests.cs)
- [SettingsDataTests.cs](../../Assets/Scripts/Tests/Editor/Ui/SettingsDataTests.cs)
- [SettingsDraftEditorTests.cs](../../Assets/Scripts/Tests/Editor/Ui/SettingsDraftEditorTests.cs)
- [SettingsServiceTests.cs](../../Assets/Scripts/Tests/Editor/Ui/SettingsServiceTests.cs)
- [SettingsUiPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Ui/SettingsUiPrefabTests.cs)
- [ShipAutoPowerUiTests.cs](../../Assets/Scripts/Tests/Editor/Ui/ShipAutoPowerUiTests.cs)
- [SkirmishUiPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Ui/SkirmishUiPrefabTests.cs)
- [SliderInteractionTests.cs](../../Assets/Scripts/Tests/Editor/Ui/SliderInteractionTests.cs)
- [StationLevelViewTests.cs](../../Assets/Scripts/Tests/Editor/Ui/StationLevelViewTests.cs)
- [SteamInviteUiPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Ui/SteamInviteUiPrefabTests.cs)
- [SurvivalUiPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Ui/SurvivalUiPrefabTests.cs)
- [TeamColorViewPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Ui/TeamColorViewPrefabTests.cs)
- [TooltipPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Ui/TooltipPrefabTests.cs)
- [TooltipServiceTests.cs](../../Assets/Scripts/Tests/Editor/Ui/TooltipServiceTests.cs)
- [TooltipTriggerTests.cs](../../Assets/Scripts/Tests/Editor/Ui/TooltipTriggerTests.cs)
- [UiCanvasArchitectureTests.cs](../../Assets/Scripts/Tests/Editor/Ui/UiCanvasArchitectureTests.cs)
- [UiPrefabScaleTests.cs](../../Assets/Scripts/Tests/Editor/Ui/UiPrefabScaleTests.cs)
- [UnitActionsPrefabTests.cs](../../Assets/Scripts/Tests/Editor/Ui/UnitActionsPrefabTests.cs)

### Vision (6)

- [FogDetectionEventTests.cs](../../Assets/Scripts/Tests/Editor/Vision/FogDetectionEventTests.cs)
- [FogVisibilityGridModelTests.cs](../../Assets/Scripts/Tests/Editor/Vision/FogVisibilityGridModelTests.cs)
- [FogVisibilityModelTests.cs](../../Assets/Scripts/Tests/Editor/Vision/FogVisibilityModelTests.cs)
- [RadarDetectionParityTests.cs](../../Assets/Scripts/Tests/Editor/Vision/RadarDetectionParityTests.cs)
- [RadarScanServiceTests.cs](../../Assets/Scripts/Tests/Editor/Vision/RadarScanServiceTests.cs)
- [VisionServiceTests.cs](../../Assets/Scripts/Tests/Editor/Vision/VisionServiceTests.cs)

## Benchmark fixtures and assemblies

- [EmpireAtWar.Tests.Benchmarks.asmdef](../../Assets/Scripts/Tests/Benchmarks/EmpireAtWar.Tests.Benchmarks.asmdef): player-compatible, `UNITY_INCLUDE_TESTS`, not auto-referenced.
- [EmpireAtWar.Tests.Benchmarks.Editor.asmdef](../../Assets/Scripts/Tests/Benchmarks/Editor/EmpireAtWar.Tests.Benchmarks.Editor.asmdef): Editor-only, `UNITY_INCLUDE_TESTS`, not auto-referenced.
- [BranchVariantParityTests.cs](../../Assets/Scripts/Tests/Benchmarks/BranchVariantParityTests.cs): abstract correctness tests for radar detection and target filtering.
- [BranchVariantBenchmarks.cs](../../Assets/Scripts/Tests/Benchmarks/BranchVariantBenchmarks.cs): abstract performance tests for the same kernels; requires Burst.
- [EditorBranchVariantParityTests.cs](../../Assets/Scripts/Tests/Benchmarks/Editor/EditorBranchVariantParityTests.cs): Editor correctness fixture.
- [EditorBranchVariantBenchmarks.cs](../../Assets/Scripts/Tests/Benchmarks/Editor/EditorBranchVariantBenchmarks.cs): Editor performance fixture.
- [PlayerBranchVariantParityTests.cs](../../Assets/Scripts/Tests/Benchmarks/PlayerBranchVariantParityTests.cs): player correctness fixture.
- [PlayerBranchVariantBenchmarks.cs](../../Assets/Scripts/Tests/Benchmarks/PlayerBranchVariantBenchmarks.cs): player performance fixture.
- Benchmark fixtures have no feature categories and are outside the main predefined Editor assembly category-coverage check.

## Tooling test files

- [Tools/AloImport/tests/test_converter.py](../../Tools/AloImport/tests/test_converter.py): 6 Python test method declarations.
- [Tools/AloImport/tests/test_jobs.py](../../Tools/AloImport/tests/test_jobs.py): 12 Python test method declarations.
- [Tools/AloImport/tests/test_recipe.py](../../Tools/AloImport/tests/test_recipe.py): 12 Python test method declarations.
- [Tools/AloImport/tests/test_source_audit.py](../../Tools/AloImport/tests/test_source_audit.py): 5 Python test method declarations.
- [Tools/Serena/tests/test_csharp_adapter.py](../../Tools/Serena/tests/test_csharp_adapter.py): 4 Python test method declarations.
- [Test-AssertPackagePins.ps1](Test-AssertPackagePins.ps1): standalone PowerShell regression script.
- [Test-AssertTestResults.ps1](Test-AssertTestResults.ps1): standalone PowerShell regression script.
- [Test-PrepareSnapshot.ps1](Test-PrepareSnapshot.ps1): standalone PowerShell regression script.

## Boundaries

- No additional NUnit/Unity test-bearing C# sources were found under `Assets` outside `Assets/Scripts/Tests`.
- `Packages/manifest.json` includes Unity Test Framework and Performance Testing dependencies. Downloaded package tests and third-party/vendor internals are not project-owned fixtures; package discovery depends on the installed dependency state and runner configuration.
- `Tools/Blender/**/Evidence/*Tests.json` and similar JSON files are captured evidence, not executable test suites. `Tools/Blender/Allegiance/CaptureTests.cs` is an evidence-capture script, not an NUnit fixture.
- `Tools/Jenkins/Assert-TestResults.ps1` validates a report; the regression suite for it is `Test-AssertTestResults.ps1`.
- See [README.md](README.md) and the live Jenkins pipeline/scripts for CI selection. This inventory alone does not establish what a Jenkins run selected.
