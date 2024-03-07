using EmpireAtWar.Commands.ShipUi;
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
using EmpireAtWar.Views.Factions;
using EmpireAtWar.Views.Game;
using EmpireAtWar.Views.Map;
using EmpireAtWar.Views.Menu;
using EmpireAtWar.Views.NavigationUiView;
using EmpireAtWar.Views.Planet;
using EmpireAtWar.Views.Reinforcement;
using EmpireAtWar.Views.SkirmishCamera;
using EmpireAtWar.Views.Terrain;
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
    
    [Inject]
    private IGameModelObserver GameModelObserver { get; }
    
    public override void InstallBindings()
    {
        Container.Bind<FactionType>().WithId(PlayerType.Player).FromMethod(GetPlayerFactionType);
        Container.Bind<FactionType>().WithId(PlayerType.Opponent).FromMethod(GetEnemyFactionType);
        
        Container
            .BindModel<FactionsModel>()
            .BindModel<WeaponDamageModel>()
            .BindModel<ProjectileModel>()
            .BindModel<LayerModel>()
            .BindModel<DamageCalculationModel>();

        Container.BindService<UnitRequestFactory>();
        
        Container
            .BindEntity<MenuController, MenuView, MenuModel>(menuView);
        
        Container
            .BindEntity<MapController, MapView, MapModel>(mapView);
        
        Container
            .BindEntity<NavigationController, NavigationUiView, NavigationModel>(navigationUiView);
        
        Container
            .BindEntity<SkirmishCameraController, SkirmishCameraView, SkirmishCameraModel>(cameraView);
            
        Container
            .BindEntity<TerrainController, TerrainView, TerrainModel>(terrainView);
        
        Container
            .BindEntity<ShipUiController, ShipUiView, ShipUiModel, ShipUiCommand>(shipUiView);
        
        Container
            .BindEntity<FactionController, FactionUiView, PlayerFactionModel>(factionUiView);
        
        Container
            .BindEntity<SkirmishGameController, SkirmishGameView, SkirmishGameModel>(gameView);
        
        Container
            .BindEntity<PlanetController, PlanetView, PlanetModel>(planetView);
        
        Container
            .BindEntity<ReinforcementController, ReinforcementView, ReinforcementModel>(reinforcementView);
        
        Container.BindService<PurchaseMediator>();
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