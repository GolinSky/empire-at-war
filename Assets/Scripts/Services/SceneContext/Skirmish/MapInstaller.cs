using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.SceneContext.Skirmish
{
    /// <summary>
    /// Binds the battlefield services. The map itself is generated and built by
    /// <see cref="BattleMapLoader"/> during the battle startup.
    /// </summary>
    public sealed class MapInstaller : MonoInstaller
    {
        [SerializeField] private MapGenerationSettings settings;
        [SerializeField] private MapLayoutView mapLayoutView;
        [SerializeField] private ReinforcementZonesSystem reinforcementZonesSystem;
        [SerializeField] private CaptureSitesSystem captureSitesSystem;

        public override void InstallBindings()
        {
            Container.Bind<MapGenerationSettings>().FromInstance(settings).AsSingle();
            Container.Bind<MapLayoutView>().FromInstance(mapLayoutView).AsSingle();
            Container.Bind(typeof(IMapModelObserver), typeof(MapModel)).To<MapModel>().AsSingle();
            Container.BindInterfacesTo<BattleMapLoader>().AsSingle();

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
