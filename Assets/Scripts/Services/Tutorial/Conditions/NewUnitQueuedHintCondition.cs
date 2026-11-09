using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a unit unlocked by the latest station level enters the build queue.</summary>
    public sealed class NewUnitQueuedHintCondition : IHintCondition
    {
        private readonly IPlayerFactionModelObserver _faction;
        private readonly int _startLevel;

        public bool IsMet { get; private set; }

        public NewUnitQueuedHintCondition(IPlayerFactionModelObserver faction)
        {
            _faction = faction;
            _startLevel = faction.CurrentLevel;
            _faction.OnProductionChanged += HandleProductionChanged;
        }

        private void HandleProductionChanged(IReadOnlyList<ProductionQueueSnapshot> queue)
        {
            if (_faction.CurrentLevel <= _startLevel) return;
            foreach (ProductionQueueSnapshot item in queue)
                if ((item.UnitRequest is ShipUnitRequest || item.UnitRequest is SquadronUnitRequest) &&
                    item.UnitRequest.FactionData.AvailableLevel == _faction.CurrentLevel)
                    IsMet = true;
        }

        public void Dispose() => _faction.OnProductionChanged -= HandleProductionChanged;
    }
}
