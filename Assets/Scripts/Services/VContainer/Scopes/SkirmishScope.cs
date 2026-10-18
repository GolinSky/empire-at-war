using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.CinematicCamera.Controller;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Entities.CoreGameUi;
using EmpireAtWar.Entities.Fps;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Entities.UnitActions.Controller;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Entities.UnitOrderFeedback;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Fade;
using EmpireAtWar.Services.FogOfWar;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.UnitExplosion;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Weapon;
using EmpireAtWar.Views.SpawnArea;
using UnityEngine;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class SkirmishScope : AssetLifetimeScope
    {
        [SerializeField] private FogOfWarSystem fogOfWarSystem;
        [SerializeField] private SpawnAreaOverlay spawnAreaOverlay;
        [SerializeField] private ReinforcementZoneData reinforcementZoneData;
        [SerializeField] private UnitOrderSettings unitOrderSettings;
        [SerializeField] private TeamColorPalette teamColorPalette;
        [SerializeField] private ExplosionVfx fighterExplosion;
        [SerializeField] private ExplosionVfx hardPointExplosion;
        [SerializeField] private ImpactEffectView impactEffectPrefab;
        [SerializeField] private UnitExplosionView unitExplosionPrefab;
        [SerializeField] private Material rangeDebugLineMaterial;
        [SerializeField] private ShipSfxData shipSfxData;
        [SerializeField] private ShipSfxSources shipSfxSourcesPrefab;
        [SerializeField] private CameraService cameraService;
        [SerializeField] private MapGenerationSettings settings;
        [SerializeField] private MapLayoutView mapLayoutView;
        [SerializeField] private ReinforcementZonesSystem reinforcementZonesSystem;
        [SerializeField] private CaptureSitesSystem captureSitesSystem;

        private MiniMapData _miniMapData;

        public FactionCatalog Factions { get; private set; }
        public BattleDefinition Battle { get; private set; }
        public BattleModules Modules { get; private set; }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            Battle = ((ProjectScope)Parent).GameData.Battle;
            builder.RegisterInstance(Battle);
            Modules = new BattleModules(Battle, new SuperWeaponsModule(Assets), new EconomyModule(Assets),
                new CaptureSitesModule(Assets, captureSitesSystem), new FogModule(fogOfWarSystem));
            Modules.ConfigureBattle(builder);
            builder.Register<SkirmishStateModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            PlayerRoster roster = new PlayerRoster(((ProjectScope)Parent).GameData.Players);
            builder.RegisterInstance(roster).AsImplementedInterfaces().AsSelf();
            builder.Register<PlayerRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(teamColorPalette);
            builder.Register<TeamColorService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<PlayerColors>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<TeamIntelRegistry>(Lifetime.Singleton);
            Factions = Assets.Load<FactionCatalog>(nameof(FactionCatalog));
            builder.RegisterInstance(Factions).AsImplementedInterfaces().AsSelf();
            builder.Register<PlayerScopeBootstrap>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<BattleUnitSpawner>(Lifetime.Singleton);
            TooltipRegistrations.Register(builder, Assets);
            builder.Register<TooltipLifecyclePresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<WorldTooltipPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(reinforcementZoneData);
            builder.RegisterInstance(unitOrderSettings);
            builder.Register<UnitOrderService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UnitActionTargetingModel>(Lifetime.Singleton);
            builder.Register<PlayerOrderInputHandler>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            AssetRegistrations.RegisterShared<HardPointOverlayData>(builder, Assets);
            builder.Register<HardPointOverlayModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            GameObject overlayRoot = new GameObject(nameof(HardPointOverlayView));
            overlayRoot.transform.SetParent(transform, false);
            builder.RegisterComponent(overlayRoot.AddComponent<HardPointOverlayView>())
                .AsImplementedInterfaces().AsSelf();
            builder.Register<HardPointOverlayPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<AttackDataFactory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            // Mode services keep their entry-point order: after the player scopes, before the battle UI.
            BattleModes.Create(Battle, Assets).ConfigureBattle(builder);
            builder.Register<BattleVictoryService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            UiRegistrations.Register(builder, Assets, transform);
            builder.Register<FadeService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EmpireAtWar.Entities.Fade.FadeUiController>(Lifetime.Singleton)
                .AsImplementedInterfaces().AsSelf();
            builder.Register<FpsModel>(Lifetime.Singleton);
            builder.Register<FpsUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<EntityLocator>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<StationRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SquadronRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SquadronLauncher>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SquadronLaunchSlots>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            AssetRegistrations.RegisterShared<MenuData>(builder, Assets);
            builder.Register<SettingsModel>(Lifetime.Singleton);
            builder.Register<SettingsDraftEditor>(Lifetime.Singleton);
            builder.Register<KeyBindingEditor>(Lifetime.Singleton);
            builder.Register<SettingsRouteUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<PauseMenuRouteUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            AssetRegistrations.RegisterShared<ShipUiData>(builder, Assets);
            AssetRegistrations.RegisterShared<ShipAbilityCatalog>(builder, Assets);
            builder.Register<ShipAbilityDependencies>(Lifetime.Singleton);
            builder.Register<ShipAbilityFactory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShipAbilityService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SuperWeaponTargetingModel>(Lifetime.Singleton);
            builder.Register<SuperWeaponOriginRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShipUiModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShipUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UnitOrderFeedbackUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShipSpawnClearance>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShipSpawnPoints>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ReinforcementSpawnRule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<StructureSpawnClearance>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter(new[]
                {
                    Assets.LoadComponent<BoxCollider>("DefendPlatformView"),
                    Assets.LoadComponent<BoxCollider>("MiningFacilityView")
                });
            _miniMapData = Object.Instantiate(Assets.Load<MiniMapData>(nameof(MiniMapData)));
            builder.RegisterInstance(_miniMapData).AsImplementedInterfaces().AsSelf();
            builder.Register<MiniMapUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ReinforcementZoneMiniMapPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            if (Battle.Mode != GameMode.BattleSimulation)
                builder.Register<BaseMiniMapPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CinematicCameraModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CinematicCameraPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<BattleStartupSequence>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SkirmishOrchestrator>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SkirmishFlowSubscriptions>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CoreGameUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UnitActionsPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            BattleDataRegistrations.Register(builder, Assets);
            builder.Register<LayerService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<FighterExplosionPool>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter(fighterExplosion);
            builder.Register<HardPointExplosionPool>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()
                .WithParameter(hardPointExplosion);
            builder.Register<SlowTickService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SpawnBlockerService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(spawnAreaOverlay).AsImplementedInterfaces().AsSelf();
            SkirmishServiceRegistrations.Register(builder, Assets, transform, impactEffectPrefab,
                unitExplosionPrefab, rangeDebugLineMaterial, shipSfxData, shipSfxSourcesPrefab, cameraService);
            MapRegistrations.Register(builder, settings, mapLayoutView, reinforcementZonesSystem);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Application.isPlaying) Object.Destroy(_miniMapData);
            else Object.DestroyImmediate(_miniMapData);
        }
    }
}
