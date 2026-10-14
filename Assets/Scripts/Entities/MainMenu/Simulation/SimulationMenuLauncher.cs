using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Game;

namespace EmpireAtWar.Entities.MainMenu.Simulation
{
    public sealed class SimulationMenuLauncher : ISimulationLauncher
    {
        private readonly IGameCommand _game;
        public SimulationMenuLauncher(IGameCommand game) => _game = game;
        public SimulationSetup Current => null;
        public void Open() { }
        public void Cancel() { }
        public void Launch(SimulationSetup setup) => _game.StartBattle(SimulationPreset.Create(setup).Build());
    }
}
