using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Commands.Terrain;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Navigation;
using EmpireAtWar.Controllers.Planet;
using EmpireAtWar.Controllers.Reinforcement;
using EmpireAtWar.Controllers.ShipUi;
using EmpireAtWar.Controllers.SkirmishCamera;
using EmpireAtWar.Controllers.Terrain;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Models.Planet;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Models.Terrain;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Factions;
using EmpireAtWar.Views.Game;
using EmpireAtWar.Views.NavigationUiView;
using EmpireAtWar.Views.Planet;
using EmpireAtWar.Views.Reinforcement;
using EmpireAtWar.Views.ShipUi;
using EmpireAtWar.Views.SkirmishCamera;
using EmpireAtWar.Views.Terrain;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.Repository;
using Zenject;
using GameController = EmpireAtWar.Controllers.Game.GameController;

public class SkirmishSingleEntityInstaller : MonoInstaller
{
    [SerializeField] private NavigationUiView navigationUiView;
    [SerializeField] private TerrainView terrainView;
    [SerializeField] private SkirmishCameraView cameraView;
    [SerializeField] private ShipUiView shipUiView;
    [SerializeField] private FactionUiView factionUiView;
    [SerializeField] private GameView gameView;
    [SerializeField] private PlanetView planetView;
    [SerializeField] private ReinforcementView reinforcementView;
    
   [Inject]
   private IRepository Repository { get; }
  
    
    public override void InstallBindings()
    {
        Container.BindFactory<IModel, IMovable, SelectionComponent, SelectionFacade>()
            .AsSingle();
        
        Container.BindFactory<IModel, IHealthComponent, EnemySelectionComponent, EnemySelectionFacade>()
            .AsSingle();
        
        Container.BindModel<FactionsModel>();
        Container.BindModel<WeaponDamageModel>();
        Container.BindModel<ProjectileModel>();
        
        
        Container
            .BindEntity<NavigationController, NavigationUiView, NavigationModel>(
                navigationUiView);
        
        Container
            .BindEntity<SkirmishCameraController, SkirmishCameraView, SkirmishCameraModel>(
                cameraView);
            
        Container
            .BindEntity<TerrainController, TerrainView, TerrainModel, TerrainCommand>(
            terrainView);
        
        Container
            .BindEntity<ShipUiController, ShipUiView, ShipUiModel, ShipUiCommand>(
                shipUiView);
        
        Container
            .BindEntity<FactionController, FactionUiView, PlayerFactionModel>(
                factionUiView);
        
        Container
            .BindEntity<GameController, GameView, GameModel>(
                gameView);
        
        Container
            .BindEntity<PlanetController, PlanetView, PlanetModel>(
                planetView);
        
        Container
            .BindEntity<ReinforcementController, ReinforcementView, ReinforcementModel>(
                reinforcementView);
    }
}