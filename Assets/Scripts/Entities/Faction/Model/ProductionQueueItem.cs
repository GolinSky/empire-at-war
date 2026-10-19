using System;
using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Models.Factions
{
    public sealed class ProductionQueueItem
    {
        public UnitRequest UnitRequest { get; }
        public float RemainingBuildTime { get; private set; }

        public ProductionQueueItem(UnitRequest unitRequest)
            : this(unitRequest, unitRequest.FactionData.BuildTime)
        {
        }

        public ProductionQueueItem(UnitRequest unitRequest, float remainingBuildTime)
        {
            UnitRequest = unitRequest;
            RemainingBuildTime = remainingBuildTime;
        }

        public void Advance(float deltaTime)
        {
            RemainingBuildTime = Math.Max(0f, RemainingBuildTime - deltaTime);
        }
    }
}
