using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Reinforcement;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class ReinforcementAvailableHintCondition : IHintCondition
    {
        private readonly IReinforcementModelObserver _reinforcements;

        public bool IsMet { get; private set; }

        public ReinforcementAvailableHintCondition(IReinforcementModelObserver reinforcements)
        {
            _reinforcements = reinforcements;
            _reinforcements.OnReinforcementAdded += HandleAdded;
        }

        private void HandleAdded(UnitRequest request) => IsMet = true;

        public void Dispose() => _reinforcements.OnReinforcementAdded -= HandleAdded;
    }
}
