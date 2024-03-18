using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Controllers.Map;
using EmpireAtWar.Controllers.Menu;
using EmpireAtWar.Controllers.Navigation;
using EmpireAtWar.Controllers.Planet;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Controllers.ShipUi;
using EmpireAtWar.Controllers.SkirmishCamera;
using EmpireAtWar.Controllers.Terrain;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Models.Menu;
using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Models.Planet;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Models.Terrain;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Views;
using EmpireAtWar.Views.Economy;
using EmpireAtWar.Views.Factions;
using EmpireAtWar.Views.Game;
using EmpireAtWar.Views.Map;
using EmpireAtWar.Views.Menu;
using EmpireAtWar.Views.NavigationUiView;
using EmpireAtWar.Views.Planet;
using EmpireAtWar.Views.Reinforcement;
using EmpireAtWar.Views.SkirmishCamera;
using EmpireAtWar.Views.Terrain;
using LightWeightFramework.Components.Repository;
using UnityEngine;
using Zenject;

public class SkirmishSingleEntityInstaller : MonoInstaller
{
    [SerializeField] private NavigationUiView navigationUiView;
    [SerializeField] private TerrainView terrainView;
    [SerializeField] private SkirmishCameraView cameraView;
    [SerializeField] private ShipUiView shipUiView;
    [SerializeField] private FactionUiView factionUiView;
    [SerializeField] private SkirmishGameView gameView;
    [SerializeField] private PlanetView planetView;
    [SerializeField] private ReinforcementView reinforcementView;
    [SerializeField] private MapView mapView;
    [SerializeField] private MenuView menuView;
    [SerializeField] private EconomyView economyView;
    
    [Inject]
    private IGameModelObserver GameModelObserver { get; }
    
    [Inject]
    private IRepository Repository { get; }
    
    public override void InstallBindings()
    {
        Container.Bind<FactionType>().WithId(PlayerType.Player).FromMethod(GetPlayerFactionType);
        Container.Bind<FactionType>().WithId(PlayerType.Opponent).FromMethod(GetEnemyFactionType);
        
        Container
            .BindModel<FactionsModel>(Repository)
            .BindModel<WeaponDamageModel>(Repository)
            .BindModel<ProjectileModel>(Repository)
            .BindModel<LayerModel>(Repository)
            .BindModel<DamageCalculationModel>(Repository);

        Container.BindInterfaces<UnitRequestFactory>();

        Container
            .BindModel<EconomyModel>(Repository)
            .BindInterfaces<EconomyController>()
            .BindViewFromInstance(economyView);
        
        Container
            .BindModel<MenuModel>(Repository)
            .BindInterfaces<MenuController>()
            .BindViewFromInstance(menuView);
        
        Container
            .BindModel<MapModel>(Repository)
            .BindInterfaces<MapController>()
            .BindViewFromInstance(mapView);
        
        Container
            .BindModel<NavigationModel>(Repository)
            .BindInterfaces<NavigationController>()
            .BindViewFromInstance(navigationUiView);
        
        Container
            .BindModel<SkirmishCameraModel>(Repository)
            .BindInterfaces<SkirmishCameraController>()
            .BindViewFromInstance(cameraView);
        
        Container
            .BindModel<TerrainModel>(Repository)
            .BindInterfaces<TerrainController>()
            .BindViewFromInstance(terrainView);

        Container
            .BindModel<ShipUiModel>(Repository)
            .BindInterfaces<ShipUiController>()
            .BindViewFromInstance(shipUiView);
        
        Container
            .BindModel<PlayerFactionModel>(Repository)
            .BindInterfaces<FactionController>()
            .BindViewFromInstance(factionUiView);
        
        Container
            .BindModel<SkirmishGameModel>(Repository)
            .BindInterfaces<SkirmishGameController>()
            .BindViewFromInstance(gameView);
        
        Container
            .BindModel<PlanetModel>(Repository)
            .BindInterfaces<PlanetController>()
            .BindViewFromInstance(planetView);
        
        Container
            .BindModel<ReinforcementModel>(Repository)
            .BindInterfaces<ReinforcementController>()
            .BindViewFromInstance(reinforcementView);
        
        Container.BindInterfaces<PurchaseMediator>();
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