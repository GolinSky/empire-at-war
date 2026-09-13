using System;
using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Models.Factions
{
    public sealed class ProductionQueueItem
    {
        public ProductionQueueItem(UnitRequest unitRequest)
        {
            UnitRequest = unitRequest ?? throw new ArgumentNullException(nameof(unitRequest));
            RemainingBuildTime = unitRequest.FactionData.BuildTime;
        }

        public UnitRequest UnitRequest { get; }
        public float RemainingBuildTime { get; private set; }

        public void Advance(float deltaTime)
        {
            RemainingBuildTime = Math.Max(0f, RemainingBuildTime - deltaTime);
        }
    }
}
