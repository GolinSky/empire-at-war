using EmpireAtWar.Entities.Game;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.FogOfWar;
using global::VContainer;
using global::VContainer.Unity;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class FogModule : IBattleModule
    {
        private readonly FogOfWarSystem _fog;
        public BattleFeatures Feature => BattleFeatures.Fog;
        public FogModule(FogOfWarSystem fog) => _fog = fog;

        public void ConfigureBattle(IContainerBuilder builder)
        {
            builder.RegisterComponent(_fog).AsImplementedInterfaces().AsSelf();
            builder.Register<MiniMapFogMask>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player) { }
    }
}
