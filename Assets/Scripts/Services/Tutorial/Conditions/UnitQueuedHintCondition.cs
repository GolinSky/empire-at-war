using System.Collections.Generic;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class UnitQueuedHintCondition : IHintCondition
    {
        private readonly IPlayerFactionModelObserver _faction;
        private readonly int _minimumCount;

        public bool IsMet { get; private set; }

        public UnitQueuedHintCondition(IPlayerFactionModelObserver faction, int minimumCount)
        {
            _faction = faction;
            _minimumCount = minimumCount;
            _faction.OnProductionChanged += HandleProductionChanged;
        }

        private void HandleProductionChanged(IReadOnlyList<ProductionQueueSnapshot> queue)
        {
            if (queue.Count >= _minimumCount) IsMet = true;
        }

        public void Dispose() => _faction.OnProductionChanged -= HandleProductionChanged;
    }
}
