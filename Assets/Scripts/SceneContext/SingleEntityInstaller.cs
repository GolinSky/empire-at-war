using EmpireAtWar.Commands.Terrain;
using EmpireAtWar.Controllers.Navigation;
using EmpireAtWar.Controllers.SkirmishCamera;
using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Controllers.Terrain;
using EmpireAtWar.Extentions;
using EmpireAtWar.Models.Navigation;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Models.Terrain;
using EmpireAtWar.Views.NavigationUiView;
using EmpireAtWar.Views.SkirmishCamera;
using EmpireAtWar.Views.SpaceStation;
using EmpireAtWar.Views.Terrain;
using UnityEngine;
using Zenject;

public class SingleEntityInstaller : MonoInstaller
{
    [SerializeField] private NavigationModel navigationModel;
    [SerializeField] private SkirmishCameraModel cameraModel;
    [SerializeField] private TerrainModel terrainModel;
    [SerializeField] private SpaceStationModel spaceStationModel;
    
    [SerializeField] private NavigationUiView navigationUiView;
    [SerializeField] private TerrainView terrainView;
    [SerializeField] private SkirmishCameraView cameraView;
    [SerializeField] private SpaceStationView spaceStationView;
    
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
            .BindEntityNoCommand<SpaceStationController, SpaceStationView, SpaceStationModel>(
            spaceStationModel,
            spaceStationView);
    }
}