namespace EmpireAtWar.Services.ShipNavigation
{
    /// <summary>A ship waiting for its turn in the per-frame route planning budget.</summary>
    public interface IShipPlanRequester
    {
        void ExecuteQueuedPlan();
    }
}
