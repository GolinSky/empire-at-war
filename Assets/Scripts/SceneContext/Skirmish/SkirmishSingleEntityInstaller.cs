using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Commands.Terrain;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Navigation;
using EmpireAtWar.Controllers.Planet;
using EmpireAtWar.Controllers.ShipUi;
using EmpireAtWar.Controllers.SkirmishCamera;
using EmpireAtWar.Controllers.Terrain;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Game;
using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Models.Planet;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Models.Terrain;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Factions;
using EmpireAtWar.Views.Game;
using EmpireAtWar.Views.NavigationUiView;
using EmpireAtWar.Views.Planet;
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
  
    [SerializeField] private NavigationModel navigationModel;
    [SerializeField] private SkirmishCameraModel cameraModel;
    [SerializeField] private TerrainModel terrainModel;
    [SerializeField] private ShipUiModel shipUiModel; 
    [SerializeField] private PlayerFactionModel userfactionModel;
    [SerializeField] private GameModel gameModel;
    [SerializeField] private PlanetModel planetModel;
    
    [SerializeField] private NavigationUiView navigationUiView;
    [SerializeField] private TerrainView terrainView;
    [SerializeField] private SkirmishCameraView cameraView;
    [SerializeField] private ShipUiView shipUiView;
    [SerializeField] private FactionUiView factionUiView;
    [SerializeField] private GameView gameView;
    [SerializeField] private PlanetView planetView;
    
   [Inject]
   private IRepository Repository { get; }
  
    
    public override void InstallBindings()
    {
        Container.BindFactory<IModel, IMovable, SelectionComponent, SelectionFacade>()
            .AsSingle();
        
        Container.BindFactory<IModel, IHealthComponent, EnemySelectionComponent, EnemySelectionFacade>()
            .AsSingle();

        Container
            .BindInterfacesAndSelfTo<ProjectileModel>()
            .FromScriptableObject(Repository.Load<ProjectileModel>(nameof(ProjectileModel)))
            .AsSingle();
        
        Container
            .BindEntityNoCommand<NavigationController, NavigationUiView, NavigationModel>(
                navigationModel,
                navigationUiView);
        
        Container
            .BindEntityNoCommand<SkirmishCameraController, SkirmishCameraView, SkirmishCameraModel>(
                cameraModel,
                cameraView);
            
        Container
            .BindEntity<TerrainController, TerrainView, TerrainModel, TerrainCommand>(
                terrainModel,
            terrainView);
        
        Container
            .BindEntity<ShipUiController, ShipUiView, ShipUiModel, ShipUiCommand>(
                shipUiModel,
                shipUiView);
        
        Container
            .BindEntityNoCommand<FactionController, FactionUiView, PlayerFactionModel>(
                userfactionModel,
                factionUiView);
        
        Container
            .BindEntityNoCommand<GameController, GameView, GameModel>(
                Instantiate(gameModel),
                gameView);
        
        Container
            .BindEntityNoCommand<PlanetController, PlanetView, PlanetModel>(
                Instantiate(planetModel),
                planetView);
    }
}