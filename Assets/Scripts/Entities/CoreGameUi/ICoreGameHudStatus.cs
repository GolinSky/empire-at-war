namespace EmpireAtWar.Entities.CoreGameUi
{
    public interface ICoreGameHudStatus
    {
        void SetProductionStatus(string faction, int level);
        void SetPopulationCapacity(int current, int max);
    }
}
