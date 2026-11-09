using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a request of one kind (station level, research) enters the build queue, i.e. is bought.</summary>
    public sealed class QueuedRequestHintCondition : IHintCondition
    {
        private readonly IPlayerFactionModelObserver _faction;
        private readonly Type _requestType;

        public bool IsMet { get; private set; }

        public QueuedRequestHintCondition(IPlayerFactionModelObserver faction, Type requestType)
        {
            _faction = faction;
            _requestType = requestType;
            _faction.OnProductionChanged += HandleProductionChanged;
        }

        private void HandleProductionChanged(IReadOnlyList<ProductionQueueSnapshot> queue)
        {
            foreach (ProductionQueueSnapshot item in queue)
                if (_requestType.IsInstanceOfType(item.UnitRequest)) IsMet = true;
        }

        public void Dispose() => _faction.OnProductionChanged -= HandleProductionChanged;
    }
}
