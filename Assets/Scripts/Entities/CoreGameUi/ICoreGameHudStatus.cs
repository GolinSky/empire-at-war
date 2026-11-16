namespace EmpireAtWar.Entities.CoreGameUi
{
    public interface ICoreGameHudStatus
    {
        void SetProductionStatus(string faction, int level);
        void SetProductionMenusAvailable(bool available);
        void SetPopulationCapacity(int current, int max);
        void NotifyReinforcementReady();
    }
}
