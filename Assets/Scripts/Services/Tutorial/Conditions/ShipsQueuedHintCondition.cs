using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// Met when the player has ordered a number of ships and squadrons since the step started. Builds that finish
    /// still count; cancelled ones do not. The card shows the progress through the [count] token.
    /// </summary>
    public sealed class ShipsQueuedHintCondition : IHintCondition, IHintTextSource
    {
        private const string COUNT_TOKEN = "count";

        private readonly IPlayerFactionModelObserver _faction;
        private readonly int _count;
        private readonly int _queuedAtStart;
        private int _queued;
        private int _completed;

        public bool IsMet { get; private set; }

        private int Ordered => _queued + _completed - _queuedAtStart;

        public ShipsQueuedHintCondition(IPlayerFactionModelObserver faction, int count)
        {
            _faction = faction;
            _count = count;
            _queuedAtStart = CountShips(_faction.GetProductionQueueSnapshots());
            _queued = _queuedAtStart;
            _faction.OnProductionChanged += HandleProductionChanged;
            _faction.OnUnitCompleted += HandleUnitCompleted;
        }

        public bool TryGetText(string token, out string text)
        {
            text = token == COUNT_TOKEN ? $"{System.Math.Min(System.Math.Max(Ordered, 0), _count)}/{_count}" : null;
            return text != null;
        }

        private void HandleProductionChanged(IReadOnlyList<ProductionQueueSnapshot> queue)
        {
            _queued = CountShips(queue);
            if (Ordered >= _count) IsMet = true;
        }

        private void HandleUnitCompleted(UnitRequest request)
        {
            if (IsShip(request)) _completed++;
        }

        private static int CountShips(IReadOnlyList<ProductionQueueSnapshot> queue)
        {
            int ships = 0;
            foreach (ProductionQueueSnapshot item in queue)
                if (IsShip(item.UnitRequest)) ships += item.Count;
            return ships;
        }

        private static bool IsShip(UnitRequest request) => request is ShipUnitRequest || request is SquadronUnitRequest;

        public void Dispose()
        {
            _faction.OnProductionChanged -= HandleProductionChanged;
            _faction.OnUnitCompleted -= HandleUnitCompleted;
        }
    }
}
