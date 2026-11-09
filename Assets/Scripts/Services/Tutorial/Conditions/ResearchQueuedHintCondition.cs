using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Met when the player buys research (of one group, or any when null): queued research grows. Research already
    /// in the queue when the condition starts does not count.
    /// </summary>
    public sealed class ResearchQueuedHintCondition : IHintCondition
    {
        private readonly IPlayerFactionModelObserver _faction;
        private readonly bool? _isEconomy;
        private int _queued;

        public bool IsMet { get; private set; }

        public ResearchQueuedHintCondition(IPlayerFactionModelObserver faction, bool? isEconomy)
        {
            _faction = faction;
            _isEconomy = isEconomy;
            _queued = CountResearch(_faction.GetProductionQueueSnapshots());
            _faction.OnProductionChanged += HandleProductionChanged;
        }

        private void HandleProductionChanged(IReadOnlyList<ProductionQueueSnapshot> queue)
        {
            int queued = CountResearch(queue);
            if (queued > _queued) IsMet = true;
            _queued = queued;
        }

        private int CountResearch(IReadOnlyList<ProductionQueueSnapshot> queue)
        {
            int count = 0;
            foreach (ProductionQueueSnapshot item in queue)
                if (item.UnitRequest is ResearchUnitRequest research &&
                    (!_isEconomy.HasValue || ResearchGroup.IsEconomy(research.Key) == _isEconomy.Value))
                    count += item.Count;
            return count;
        }

        public void Dispose() => _faction.OnProductionChanged -= HandleProductionChanged;
    }
}
