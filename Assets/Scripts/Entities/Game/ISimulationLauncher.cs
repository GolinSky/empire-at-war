namespace EmpireAtWar.Entities.Game
{
    public interface ISimulationLauncher
    {
        SimulationSetup Current { get; }
        void Open();
        void Cancel();
        void Launch(SimulationSetup setup);
    }
}
