using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.SpawnBlocking;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public static class MapRegistrations
    {
        public static void Register(IContainerBuilder builder, MapGenerationSettings settings,
            MapLayoutView mapLayoutView, ReinforcementZonesSystem reinforcementZonesSystem)
        {
            builder.RegisterInstance(settings);
            builder.RegisterComponent(mapLayoutView).AsImplementedInterfaces().AsSelf();
            builder.Register<MapModel>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<BattleMapLoader>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(reinforcementZonesSystem).AsImplementedInterfaces().AsSelf();
            builder.Register<ReinforcementZonesSaveSource>(Lifetime.Singleton).As<IBattleSaveSource>();
            builder.Register<PlanetFactory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<PlanetSpawner>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<AsteroidSpawnBlockers>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
