using EmpireAtWar.Entities.Game;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public interface IBattleModule
    {
        BattleFeatures Feature { get; }
        void ConfigureBattle(IContainerBuilder builder);
        void ConfigurePlayer(IContainerBuilder builder, BattlePlayer player);
    }
}
