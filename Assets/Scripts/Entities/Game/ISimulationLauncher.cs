namespace EmpireAtWar.Entities.Game
{
    public interface ISimulationLauncher
    {
        SimulationSetup Current { get; }
        void Launch(SimulationSetup setup);
    }
}
