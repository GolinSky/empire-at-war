using System.Collections.Generic;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a scout (squadron, fighter, bomber or interceptor) enters the build queue.</summary>
    public sealed class ScoutQueuedHintCondition : IHintCondition
    {
        private readonly IPlayerFactionModelObserver _faction;
        private readonly ScoutUnits _scouts;

        public bool IsMet { get; private set; }

        public ScoutQueuedHintCondition(IPlayerFactionModelObserver faction, ScoutUnits scouts)
        {
            _faction = faction;
            _scouts = scouts;
            _faction.OnProductionChanged += HandleProductionChanged;
        }

        private void HandleProductionChanged(IReadOnlyList<ProductionQueueSnapshot> queue)
        {
            foreach (ProductionQueueSnapshot item in queue)
                if (_scouts.IsScout(item.UnitRequest)) IsMet = true;
        }

        public void Dispose() => _faction.OnProductionChanged -= HandleProductionChanged;
    }
}
