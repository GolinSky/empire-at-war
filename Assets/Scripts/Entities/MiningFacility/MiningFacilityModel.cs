using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class MiningFacilityModel : IModel, IMiningFacilityModelObserver
    {
        public float BaseIncome { get; }

        public MiningFacilityModel(float baseIncome)
        {
            BaseIncome = baseIncome;
        }
    }
}
