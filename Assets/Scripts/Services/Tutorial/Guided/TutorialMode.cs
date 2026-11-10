using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.StationFacing;
using EmpireAtWar.Services.VContainer;
using global::VContainer;

namespace EmpireAtWar.Services.Tutorial.Guided
{
    /// <summary>
    /// Guided tutorial battle: offline only, never saved, won by destroying the enemy station.
    /// Registers no save services; <see cref="BattleSetupSave.Supports"/> already refuses this mode.
    /// </summary>
    public sealed class TutorialMode : IBattleMode
    {
        public void ConfigureBattle(IContainerBuilder builder)
        {
            builder.Register<IMatchSession>(resolver => OfflineSession.ForHuman(resolver.Resolve<IPlayerRoster>()),
                Lifetime.Singleton);
            builder.Register<LocalPlayer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<BattleVictoryModel>(Lifetime.Singleton).As<IBattleOutcomeRule>().AsSelf();
            builder.Register<StationFacingService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
