using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Reinforcement;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when one placement (Shift + drag) takes two or more units out of the reserve.</summary>
    public sealed class SeveralDeployedHintCondition : IHintCondition
    {
        private readonly IReinforcementModelObserver _reinforcements;
        private int _reserveAtLastPlacement;

        public bool IsMet { get; private set; }

        public SeveralDeployedHintCondition(IReinforcementModelObserver reinforcements)
        {
            _reinforcements = reinforcements;
            _reserveAtLastPlacement = CountReserve();
            _reinforcements.OnPlacementEnded += HandlePlacementEnded;
            _reinforcements.OnReinforcementAdded += HandleAdded;
        }

        private void HandleAdded(UnitRequest request) => _reserveAtLastPlacement = CountReserve();

        private void HandlePlacementEnded()
        {
            int reserve = CountReserve();
            if (_reserveAtLastPlacement - reserve >= 2) IsMet = true;
            _reserveAtLastPlacement = reserve;
        }

        private int CountReserve()
        {
            int count = 0;
            foreach (KeyValuePair<UnitRequest, int> entry in _reinforcements.Reserve) count += entry.Value;
            return count;
        }

        public void Dispose()
        {
            _reinforcements.OnPlacementEnded -= HandlePlacementEnded;
            _reinforcements.OnReinforcementAdded -= HandleAdded;
        }
    }
}
