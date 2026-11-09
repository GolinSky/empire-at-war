using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Reinforcement;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met while built units wait in the reserve; with several, while two or more of one kind wait.</summary>
    public sealed class ReinforcementReadyHintCondition : IHintCondition
    {
        private readonly IReinforcementModelObserver _reinforcements;
        private readonly bool _needsSeveral;

        public bool IsMet => _needsSeveral ? HasSeveralOfOneKind() : _reinforcements.HasReserve;

        public ReinforcementReadyHintCondition(IReinforcementModelObserver reinforcements, bool needsSeveral)
        {
            _reinforcements = reinforcements;
            _needsSeveral = needsSeveral;
        }

        private bool HasSeveralOfOneKind()
        {
            foreach (KeyValuePair<UnitRequest, int> entry in _reinforcements.Reserve)
                if (entry.Value >= 2) return true;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
