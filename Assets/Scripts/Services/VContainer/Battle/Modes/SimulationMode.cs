using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.StationFacing;
using global::VContainer;

namespace EmpireAtWar.Services.VContainer
{
    public sealed class SimulationMode : IBattleMode
    {
        private readonly BattleDefinition _battle;
        public SimulationMode(BattleDefinition battle) => _battle = battle;

        public void ConfigureBattle(IContainerBuilder builder)
        {
            builder.Register<SimulationLocalPlayer>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterInstance(new SimulationModel(_battle.Simulation));
            builder.Register<SimulationOutcomeRule>(Lifetime.Singleton).As<IBattleOutcomeRule>();
            builder.Register<StationFacingService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SimulationResetService>(Lifetime.Singleton);
            builder.Register<SimulationTargeting>(Lifetime.Singleton);
            builder.Register<SimulationDirector>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SimulationDraft>(Lifetime.Singleton);
            builder.Register<SimulationBattleLauncher>(Lifetime.Singleton).As<ISimulationLauncher>();
            builder.Register<SimulationSetupUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<SimulationHudUiController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
