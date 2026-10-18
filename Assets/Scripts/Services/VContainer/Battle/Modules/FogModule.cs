using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.FogOfWar;
using EmpireAtWar.Services.Vision;
using global::VContainer;
using global::VContainer.Unity;
using UnityEngine;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class FogModule : IBattleModule
    {
        private readonly FogOfWarSystem _fog;
        public BattleFeatures Feature => BattleFeatures.Fog;
        public FogModule(FogOfWarSystem fog) => _fog = fog;

        public void ConfigureBattle(IContainerBuilder builder)
        {
            RegisterVision(builder, true);
            builder.RegisterComponent(_fog).AsImplementedInterfaces().AsSelf();
            builder.Register<MiniMapFogMask>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }

        public void ConfigureDisabled(IContainerBuilder builder)
        {
            RegisterVision(builder, false);
            _fog.gameObject.SetActive(false);
            builder.Register<NoFogOfWar>(Lifetime.Singleton).As<IFogOfWarSystem>();
            builder.Register<RevealedMiniMapFogMask>(Lifetime.Singleton).As<IMiniMapFogMask>();
        }

        public void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player, Transform playerRoot) { }

        private static void RegisterVision(IContainerBuilder builder, bool fogEnabled) =>
            builder.Register(resolver => new VisionService(resolver.Resolve<IPlayerRelations>(), fogEnabled),
                Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
    }
}
