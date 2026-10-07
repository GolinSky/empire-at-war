using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Entities.Fps;
using EmpireAtWar.SceneContext;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Services.Fade;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Controllers.MiniMap;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Presenters.Game;
using EmpireAtWar.Controllers.ShipUi;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CinematicCamera.Controller;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Entities.UnitOrderFeedback;
using EmpireAtWar.Entities.UnitActions.Controller;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.StationFacing;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Views.SpawnArea;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Mvc;
using UnityEngine;
using ViewComponents;
using Zenject;

public class SkirmishMainInstaller : MonoInstaller
{
    [SerializeField] private FogOfWarSystem fogOfWarSystem;
    [SerializeField] private SpawnAreaOverlay spawnAreaOverlay;
    [SerializeField] private ReinforcementZoneData reinforcementZoneData;
    [SerializeField] private UnitOrderSettings unitOrderSettings;
    [SerializeField] private TeamColorPalette teamColorPalette;

    [Inject] private IGameModelObserver GameModelObserver { get; }
    [Inject] private IAssetService AssetService { get; }

    public override void InstallBindings()
    {
        Container.BindScriptableObject<TooltipSettings>(AssetService);
        Container.BindScriptableObject<TooltipIconData>(AssetService);
        Container.Bind<TooltipTiming>().FromMethod(context =>
            context.Container.Resolve<TooltipSettings>().Timing).AsSingle();
        Container.Bind<ITooltipClock>()
            .To<TooltipClock>().AsSingle();
        Container.BindInterfacesAndSelfTo<TooltipModel>().AsSingle();
        Container.BindInterfacesAndSelfTo<TooltipService>().AsSingle().NonLazy();
        Container.BindInterfacesTo<TooltipUiController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<TooltipLifecyclePresenter>().AsSingle().NonLazy();
        Container.BindInterfacesTo<WorldTooltipPresenter>().AsSingle().NonLazy();
        Container.Bind<ReinforcementZoneData>().FromInstance(reinforcementZoneData).AsSingle();
        Container.BindScriptableObject<CaptureSiteData>(AssetService);
        Container.Bind<UnitOrderSettings>().FromInstance(unitOrderSettings).AsSingle();
        Container.Bind<IUnitOrderService>().To<UnitOrderService>().AsSingle();
        Container.Bind<UnitActionTargetingModel>().AsSingle();
        Container.BindInterfacesNonLazyExt<PlayerOrderInputHandler>();
        Container.BindScriptableObject<HardPointOverlayData>(AssetService);
        Container.BindInterfacesAndSelfTo<HardPointOverlayModel>().AsSingle();
        Container.BindInterfacesAndSelfTo<HardPointOverlayView>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesAndSelfTo<HardPointOverlayPresenter>().AsSingle().NonLazy();

        Container.BindInterfacesExt<AttackDataFactory>();
        Container.Bind<BattleVictoryModel>().AsSingle();
        Container.BindInterfacesExt<BattleVictoryService>();

        Container
            .BindInterfacesAndSelfTo<UiService>()
            .FromComponentInNewPrefab(AssetService.LoadPrefab(nameof(UiService)))
            .AsSingle()
            .NonLazy();
        Container
            .BindFactory<UiType, Transform, BaseUi, UiFactory>()
            .FromSubContainerResolve()
            .ByNewGameObjectInstaller<UiInstaller>();
        Container.BindInterfacesExt<UiCancelRouter>();
        Container.BindInterfacesTo<FadeService>().AsSingle();
        Container.Bind<FpsModel>().AsSingle();
        Container.BindInterfacesNonLazyExt<FpsUiController>();

        Container.BindInterfacesExt<EntityLocator>();
        Container.BindInterfacesExt<StationRegistry>();
        Container.BindInterfacesExt<SquadronRegistry>();
        Container.BindInterfacesExt<SquadronLauncher>();
        

        BindPlayers();
        
        Container.BindModel<MenuData>(AssetService);
        Container.Bind<SettingsModel>().AsSingle();
        Container.Bind<SettingsDraftEditor>().AsSingle();
        Container.Bind<KeyBindingEditor>().AsSingle();
        Container.BindInterfacesTo<SettingsRouteController>().AsSingle();
        Container.BindInterfacesNonLazyExt<PauseMenuRouteController>();
        
        Container.BindScriptableObject<ShipUiData>(AssetService);
        Container.BindScriptableObject<ShipAbilityCatalog>(AssetService);
        Container.Bind<IShipAbilityFactory>().To<ShipAbilityFactory>().AsSingle();
        Container.BindInterfacesAndSelfTo<ShipAbilityService>().AsSingle().NonLazy();
        Container.BindScriptableObject<SuperWeaponData>(AssetService);
        Container.Bind<SuperWeaponTargetingModel>().AsSingle();
        Container.BindInterfacesTo<SuperWeaponOriginRegistry>().AsSingle();
        Container.BindInterfacesExt<SuperWeaponFireService>();
        Container.BindInterfacesAndSelfTo<ShipUiModel>().AsSingle();
        Container.BindInterfacesNonLazyExt<ShipUiController>();
        Container.BindInterfacesNonLazyExt<UnitOrderFeedbackUiController>();
        Container.BindInitializableExecutionOrder<PlayerOrderInputHandler>(-100);
        
        Container.BindInterfacesAndSelfTo<StationFacingService>().AsSingle().NonLazy();
        Container.BindInterfacesExt<ShipSpawnClearance>();
        Container.BindInterfacesExt<ShipSpawnPoints>();
        Container.Bind<IReinforcementSpawnRule>().To<ReinforcementSpawnRule>().AsSingle();
        Container.Bind<IStructureSpawnClearance>().To<StructureSpawnClearance>().AsSingle()
            .WithArguments(new[]
            {
                AssetService.LoadComponent<BoxCollider>("DefendPlatformView"),
                AssetService.LoadComponent<BoxCollider>("MiningFacilityView")
            });
        Container.BindModel<MiniMapData>(AssetService);
        Container.BindInterfacesNonLazyExt<MiniMapController>();
        Container.BindInterfacesAndSelfTo<ReinforcementZoneMiniMapPresenter>()
            .AsSingle()
            .NonLazy();
        Container.BindInitializableExecutionOrder<ReinforcementZoneMiniMapPresenter>(100);
        Container.BindInterfacesAndSelfTo<CaptureSiteMiniMapPresenter>()
            .AsSingle()
            .NonLazy();
        Container.BindInitializableExecutionOrder<CaptureSiteMiniMapPresenter>(100);
        Container.BindInterfacesTo<BaseMiniMapPresenter>().AsSingle().NonLazy();
        Container.BindInterfacesTo<MiniMapFogMask>().AsSingle();
        
        Container.BindInterfacesAndSelfTo<CinematicCameraModel>().AsSingle();
        Container.BindInterfacesExt<CinematicCameraPresenter>();
        Container.BindInterfacesTo<BattleStartupSequence>().AsSingle();
        Container.BindInterfacesNonLazyExt<SkirmishOrchestrator>();
        Container.BindInterfacesNonLazyExt<CoreGameUiController>();
        Container.BindInterfacesNonLazyExt<UnitActionsPresenter>();
        Container.BindInitializableExecutionOrder<CoreGameUiController>(-200);
        Container.BindInitializableExecutionOrder<UnitActionsPresenter>(100);
        
        Container
            .BindModel<FactionCatalog>(AssetService)
            .BindModel<StationLevelData>(AssetService)
            .BindModel<MiningFacilityCatalog>(AssetService)
            .BindModel<DefendPlatformCatalog>(AssetService)
            .BindModel<SuperWeaponCatalog>(AssetService)
            .BindModel<WeaponsData>(AssetService)
            .BindModel<DamageMatrixData>(AssetService)
            .BindModel<LayerData>(AssetService);
        Container.BindInterfacesAndSelfTo<LayerService>().AsSingle();

        Container.BindScriptableObject<ShipsData>(AssetService);

        Container
            .BindInterfacesExt<UnitRequestFactory>();

        Container.Bind<IVisionService>().To<VisionService>().AsSingle();
        Container.Bind<ISpawnBlockerService>().To<SpawnBlockerService>().AsSingle();
        Container.BindInterfacesAndSelfTo<FogOfWarSystem>().FromInstance(fogOfWarSystem).AsSingle();
        Container.BindInterfacesTo<SpawnAreaOverlay>().FromInstance(spawnAreaOverlay).AsSingle();

    }

    private void BindPlayers()
    {
        PlayerRoster roster = new PlayerRoster(GameModelObserver.Players);
        Container.Bind(typeof(IPlayerRoster), typeof(IPlayerRelations)).FromInstance(roster).AsSingle();
        Container.Bind<ILocalPlayer>().To<LocalPlayer>().AsSingle();
        Container.Bind<IPlayerRegistry>().To<PlayerRegistry>().AsSingle();
        Container.Bind<TeamColorPalette>().FromInstance(teamColorPalette).AsSingle();
        Container.BindInterfacesTo<TeamColorService>().AsSingle();
        Container.Bind<IPlayerColors>().To<PlayerColors>().AsSingle();

        // Allied AIs share what their team has seen of hostile units.
        Container.Bind<TeamIntelRegistry>().AsSingle();

        // The human context is placed in the scene; every AI gets its own sub-container on a new GameObject.
        foreach (PlayerSlot slot in roster.Players)
        {
            if (!slot.IsAi)
            {
                continue;
            }

            Container.Bind<IEnemyService>()
                .WithId(slot.Id)
                .FromSubContainerResolve()
                .ByNewGameObjectMethod(subContainer => InstallAiPlayer(subContainer, slot))
                .WithGameObjectName($"AiPlayer{slot.Id.Index}")
                .AsCached()
                .NonLazy();
        }
    }

    private static void InstallAiPlayer(DiContainer subContainer, PlayerSlot slot)
    {
        subContainer.BindInstance(slot);
        AiPlayerInstaller.Install(subContainer);
    }
}
