using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Models.Factions
{
    public sealed class ProductionQueueSnapshot
    {
        public ProductionQueueSnapshot(
            UnitRequest unitRequest,
            int count,
            float remainingBuildTime)
        {
            UnitRequest = unitRequest;
            Count = count;
            RemainingBuildTime = remainingBuildTime;
        }

        public UnitRequest UnitRequest { get; }
        public int Count { get; }
        public float RemainingBuildTime { get; }
    }
}
