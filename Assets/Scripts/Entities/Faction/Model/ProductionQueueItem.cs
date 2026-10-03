using System;
using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Models.Factions
{
    public sealed class ProductionQueueItem
    {
        public UnitRequest UnitRequest { get; }
        public float RemainingBuildTime { get; private set; }

        public ProductionQueueItem(UnitRequest unitRequest)
        {
            UnitRequest = unitRequest;
            RemainingBuildTime = unitRequest.FactionData.BuildTime;
        }

        public void Advance(float deltaTime)
        {
            RemainingBuildTime = Math.Max(0f, RemainingBuildTime - deltaTime);
        }
    }
}
