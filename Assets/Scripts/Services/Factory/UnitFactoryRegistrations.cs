using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Ship;
using EmpireAtWar.Services.VContainer;
using VContainer;

namespace EmpireAtWar.Services.Factory
{
    public static class UnitFactoryRegistrations
    {
        public static void Register(IContainerBuilder builder)
        {
            builder.Register<EntityScopeOwnership>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ShipFactory>(Lifetime.Singleton);
            builder.Register<SquadronFactory>(Lifetime.Singleton);
            builder.Register<SpaceStationFactory>(Lifetime.Singleton);
            builder.Register<MiningFacilityFactory>(Lifetime.Singleton);
            builder.Register<AsteroidMiningFacilityFactory>(Lifetime.Singleton);
            builder.Register<DefendPlatformFactory>(Lifetime.Singleton);
            builder.Register<AsteroidDefendPlatformFactory>(Lifetime.Singleton);
        }
    }
}
