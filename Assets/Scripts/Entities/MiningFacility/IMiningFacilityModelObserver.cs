using EmpireAtWar.Ship;

namespace EmpireAtWar.Entities.MiningFacility
{
    public interface IMiningFacilityModelObserver : IUnitModelObserver
    {
        float BaseIncome { get; }
    }
}
