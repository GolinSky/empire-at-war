using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Views.ReinforcementZones;
using UnityEngine;
using Zenject;
using Random = System.Random;

namespace EmpireAtWar.SceneContext.Skirmish
{
    /// <summary>
    /// Generates and spawns the battlefield during binding, because navigation and minimap
    /// resolve the map obstacles before any Initialize runs.
    /// </summary>
    public sealed class MapInstaller : MonoInstaller
    {
        [SerializeField] private MapGenerationSettings settings;
        [SerializeField] private MapLayoutView mapLayoutView;
        [SerializeField] private ReinforcementZonesSystem reinforcementZonesSystem;
        [SerializeField] private CaptureSitesSystem captureSitesSystem;
        [Inject] private IGameModelObserver GameModel { get; }

        public override void InstallBindings()
        {
            MapLayout layout = new MapLayoutGenerator(settings, mapLayoutView.FeatureRadii).Generate(
                GameModel.MapSize,
                GameModel.Players,
                new Random());
            mapLayoutView.Build(layout);

            Container.Bind(typeof(IMapModelObserver), typeof(MapLayout)).FromInstance(layout).AsSingle();
            Container.Bind<MapGenerationSettings>().FromInstance(settings).AsSingle();
            Container.Bind<ReinforcementZoneView[]>().FromInstance(mapLayoutView.ZoneViews).AsSingle();
            Container.Bind<CaptureSiteView[]>().FromInstance(mapLayoutView.SiteViews).AsSingle();
            foreach (MapObstacle obstacle in mapLayoutView.Obstacles)
            {
                Container.Bind(typeof(IMapObstacleContactSource), typeof(IMiniMapObstacleSource))
                    .FromInstance(obstacle);
            }

            Container.BindInterfacesAndSelfTo<ReinforcementZonesSystem>()
                .FromInstance(reinforcementZonesSystem)
                .AsSingle();
            Container.BindInterfacesAndSelfTo<CaptureSitesSystem>()
                .FromInstance(captureSitesSystem)
                .AsSingle();
            Container.BindInterfacesTo<PlanetSpawner>().AsSingle().NonLazy();
        }
    }
}
