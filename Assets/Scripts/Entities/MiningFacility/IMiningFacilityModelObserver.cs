using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.MiningFacility
{
    public interface IMiningFacilityModelObserver : IModelObserver
    {
        float BaseIncome { get; }
    }
}
