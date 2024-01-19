using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Commands.SpaceStation;
using EmpireAtWar.Commands.Terrain;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Navigation;
using EmpireAtWar.Controllers.ShipUi;
using EmpireAtWar.Controllers.SkirmishCamera;
using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Controllers.Terrain;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Models.Terrain;
using EmpireAtWar.Views.Factions;
using EmpireAtWar.Views.NavigationUiView;
using EmpireAtWar.Views.ShipUi;
using EmpireAtWar.Views.SkirmishCamera;
using EmpireAtWar.Views.SpaceStation;
using EmpireAtWar.Views.Terrain;
using UnityEngine;
using Zenject;

public class SkirmishSingleEntityInstaller : MonoInstaller
{
    [SerializeField] private NavigationModel navigationModel;
    [SerializeField] private SkirmishCameraModel cameraModel;
    [SerializeField] private TerrainModel terrainModel;
    [SerializeField] private SpaceStationModel spaceStationModel;
    [SerializeField] private ShipUiModel shipUiModel;
    [SerializeField] private FactionModel factionModel;

    
    [SerializeField] private NavigationUiView navigationUiView;
    [SerializeField] private TerrainView terrainView;
    [SerializeField] private SkirmishCameraView cameraView;
    [SerializeField] private SpaceStationView spaceStationView;
    [SerializeField] private ShipUiView shipUiView;
    [SerializeField] private FactionUiView factionUiView;

    
    public override void InstallBindings()
    {
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
            .BindEntity<SpaceStationController, SpaceStationView, SpaceStationModel, SpaceStationCommand>(
            spaceStationModel,
            spaceStationView);
        
        Container
            .BindEntity<ShipUiController, ShipUiView, ShipUiModel, ShipUiCommand>(
                shipUiModel,
                shipUiView);
        
        Container
            .BindEntity<FactionController, FactionUiView, FactionModel, FactionCommand>(
                Instantiate(factionModel),
                factionUiView);
    }
}