using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    /// <summary>Registers one game mode's battle services: the local player view, outcome rule, station facing,
    /// and the mode's director and HUD.</summary>
    public interface IBattleMode
    {
        void ConfigureBattle(IContainerBuilder builder);
    }
}
