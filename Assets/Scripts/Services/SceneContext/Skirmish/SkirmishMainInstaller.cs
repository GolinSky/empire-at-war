using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.SceneContext;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Player;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
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
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Services.StationFacing;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Mvc;
using UnityEngine;
using ViewComponents;
using Zenject;

public class SkirmishMainInstaller : MonoInstaller
{
    [SerializeField] private FogOfWarSystem fogOfWarSystem;
    [SerializeField] private ReinforcementZoneData reinforcementZoneData;
    [SerializeField] private UnitOrderSettings unitOrderSettings;
    [SerializeField] private TeamColorPalette teamColorPalette;
    [Inject] private IGameModelObserver GameModelObserver { get; }
    [Inject] private IAssetService Repository { get; }

    public override void InstallBindings()
    {
        Container.Bind<ReinforcementZoneData>().FromInstance(reinforcementZoneData).AsSingle();
        Container.BindScriptableObject<CaptureSiteData>(Repository);
        Container.Bind<UnitOrderSettings>().FromInstance(unitOrderSettings).AsSingle();
        Container.Bind<IUnitOrderService>().To<UnitOrderService>().AsSingle();
        Container.Bind<UnitActionTargetingModel>().AsSingle();
        Container.BindInterfacesNonLazyExt<PlayerOrderInputHandler>();
        Container.BindScriptableObject<HardPointOverlayData>(Repository);
        Container.BindInterfacesAndSelfTo<HardPointOverlayModel>().AsSingle();
        Container.BindInterfacesAndSelfTo<HardPointOverlayView>().FromNewComponentOnNewGameObject().AsSingle();
        Container.BindInterfacesAndSelfTo<HardPointOverlayPresenter>().AsSingle().NonLazy();

        Container.BindInterfacesExt<AttackDataFactory>();
        Container.Bind<BattleVictoryModel>().AsSingle();
        Container.BindInterfacesExt<BattleVictoryService>();

        Container
            .BindInterfacesAndSelfTo<UiService>()
            .FromComponentInNewPrefab(Repository.LoadPrefab(nameof(UiService)))
            .AsSingle()
            .NonLazy();
        Container
            .BindFactory<UiType, Transform, BaseUi, UiFactory>()
            .FromSubContainerResolve()
            .ByNewGameObjectInstaller<UiInstaller>();

        Container.BindInterfacesExt<EntityLocator>();
        Container.BindInterfacesExt<SquadronLauncher>();
        

        BindPlayers();
        
        Container.BindModel<MenuData>(Repository);
        Container.BindInterfacesNonLazyExt<MenuController>();
        
        Container.BindScriptableObject<ShipUiData>(Repository);
        Container.BindScriptableObject<ShipAbilityCatalog>(Repository);
        Container.Bind<IShipAbilityFactory>().To<ShipAbilityFactory>().AsSingle();
        Container.BindInterfacesAndSelfTo<ShipAbilityService>().AsSingle().NonLazy();
        Container.BindScriptableObject<SuperWeaponData>(Repository);
        Container.Bind<SuperWeaponTargetingModel>().AsSingle();
        Container.BindInterfacesTo<SuperWeaponOriginRegistry>().AsSingle();
        Container.BindInterfacesExt<SuperWeaponFireService>();
        Container.BindInterfacesAndSelfTo<ShipUiModel>().AsSingle();
        Container.BindInterfacesNonLazyExt<ShipUiController>();
        Container.BindInterfacesNonLazyExt<UnitOrderFeedbackUiController>();
        Container.BindInitializableExecutionOrder<PlayerOrderInputHandler>(-100);
        
        Container.BindInterfacesAndSelfTo<StationFacingService>().AsSingle().NonLazy();
        Container.BindModel<MiniMapData>(Repository);
        Container.BindInterfacesNonLazyExt<MiniMapController>();
        Container.BindInterfacesAndSelfTo<ReinforcementZoneMiniMapPresenter>()
            .AsSingle()
            .NonLazy();
        Container.BindInitializableExecutionOrder<ReinforcementZoneMiniMapPresenter>(100);
        Container.BindInterfacesAndSelfTo<CaptureSiteMiniMapPresenter>()
            .AsSingle()
            .NonLazy();
        Container.BindInitializableExecutionOrder<CaptureSiteMiniMapPresenter>(100);
        
        Container.BindInterfacesAndSelfTo<SkirmishSessionModel>().AsSingle();
        Container.BindInterfacesAndSelfTo<CinematicCameraModel>().AsSingle();
        Container.BindInterfacesExt<CinematicCameraPresenter>();
        Container.BindInterfacesNonLazyExt<SkirmishOrchestrator>();
        Container.BindInterfacesNonLazyExt<CoreGameUiController>();
        Container.BindInterfacesNonLazyExt<UnitActionsPresenter>();
        Container.BindInitializableExecutionOrder<CoreGameUiController>(-200);
        Container.BindInitializableExecutionOrder<UnitActionsPresenter>(100);
        
        Container
            .BindModel<FactionsData>(Repository)
            .BindModel<WeaponsData>(Repository)
            .BindModel<DamageMatrixData>(Repository)
            .BindModel<LayerData>(Repository);
        Container.BindInterfacesAndSelfTo<LayerService>().AsSingle();

        Container.BindScriptableObject<ShipsData>(Repository);

        Container
            .BindInterfacesExt<UnitRequestFactory>();

        Container.BindEntityExt(fogOfWarSystem);

    }

    private void BindPlayers()
    {
        PlayerRoster roster = new PlayerRoster(GameModelObserver.Players);
        Container.Bind(typeof(IPlayerRoster), typeof(IPlayerRelations)).FromInstance(roster).AsSingle();
        Container.Bind<ILocalPlayer>().To<LocalPlayer>().AsSingle();
        Container.Bind<IPlayerRegistry>().To<PlayerRegistry>().AsSingle();
        Container.Bind<TeamColorPalette>().FromInstance(teamColorPalette).AsSingle();
        Container.BindInterfacesTo<TeamColorService>().AsSingle();

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
