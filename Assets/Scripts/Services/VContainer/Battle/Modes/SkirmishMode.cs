using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.StationFacing;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class SkirmishMode : IBattleMode
    {
        public void ConfigureBattle(IContainerBuilder builder)
        {
            builder.Register<LocalPlayer>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<BattleSaveInput>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<BattleAutosave>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<BattleVictoryModel>(Lifetime.Singleton).As<IBattleOutcomeRule>().AsSelf();
            builder.Register<BattleVictorySaveSource>(Lifetime.Singleton).As<IBattleSaveSource>();
            builder.Register<StationFacingService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
