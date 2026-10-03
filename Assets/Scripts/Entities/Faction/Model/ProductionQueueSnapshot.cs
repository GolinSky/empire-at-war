using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Models.Factions
{
    public sealed class ProductionQueueSnapshot
    {
        public UnitRequest UnitRequest { get; }
        public int Count { get; }
        public float RemainingBuildTime { get; }

        public ProductionQueueSnapshot(
            UnitRequest unitRequest,
            float remainingBuildTime,
            int count)
        {
            UnitRequest = unitRequest;
            Count = count;
            RemainingBuildTime = remainingBuildTime;
        }
    }
}
