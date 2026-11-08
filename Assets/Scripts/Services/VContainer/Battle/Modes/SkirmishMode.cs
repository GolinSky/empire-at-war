using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.StationFacing;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class SkirmishMode : IBattleMode
    {
        private readonly OnlineMatchService _online;
        public SkirmishMode(OnlineMatchService online) => _online = online;

        public void ConfigureBattle(IContainerBuilder builder)
        {
            builder.Register<IMatchSession>(resolver => _online.CreateSession(
                () => OfflineSession.ForHuman(resolver.Resolve<IPlayerRoster>())), Lifetime.Singleton);
            builder.Register<LocalPlayer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<BattleSaveInput>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<BattleAutosave>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<BattleVictoryModel>(Lifetime.Singleton).As<IBattleOutcomeRule>().AsSelf();
            builder.Register<BattleVictorySaveSource>(Lifetime.Singleton).As<IBattleSaveSource>();
            builder.Register<StationFacingService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
