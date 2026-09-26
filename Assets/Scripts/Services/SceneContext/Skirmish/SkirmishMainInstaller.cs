using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
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
using EmpireAtWar.Entities.Map;
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
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
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
    [Inject] private IGameModelObserver GameModelObserver { get; }
    [Inject] private IAssetService Repository { get; }

    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<ReinforcementZonesSystem>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container.Bind<ReinforcementZoneData>().FromInstance(reinforcementZoneData).AsSingle();
        Container.BindInterfacesAndSelfTo<CaptureSitesSystem>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container.BindScriptableObject<CaptureSiteData>(Repository);
        Container.Bind<UnitOrderSettings>().FromInstance(unitOrderSettings).AsSingle();
        Container.Bind<IUnitOrderService>().To<UnitOrderService>().AsSingle();
        Container.Bind<UnitActionTargetingModel>().AsSingle();
        Container.BindInterfacesNonLazyExt<PlayerOrderInputHandler>();

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
        
        
        //todo: use GameModelObserver.PlayerFactionType directly
        Container.Bind<FactionType>().WithId(PlayerType.Player).FromMethod(GetPlayerFactionType);
        Container.Bind<FactionType>().WithId(PlayerType.Opponent).FromMethod(GetEnemyFactionType);
        
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
        
        //todo: merge map model with minimap 
        Container.BindModel<MapData>(Repository);
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

    private FactionType GetPlayerFactionType()
    {
        return GameModelObserver.PlayerFactionType;
    }

    private FactionType GetEnemyFactionType()
    {
        return GameModelObserver.EnemyFactionType;
    }
}
