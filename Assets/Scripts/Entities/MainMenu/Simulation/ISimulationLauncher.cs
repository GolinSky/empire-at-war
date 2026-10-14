using EmpireAtWar.Entities.Game;

namespace EmpireAtWar.Entities.MainMenu.Simulation
{
    public interface ISimulationLauncher
    {
        SimulationSetup Current { get; }
        void Open();
        void Cancel();
        void Launch(SimulationSetup setup);
    }
}
