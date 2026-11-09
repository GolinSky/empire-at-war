using EmpireAtWar.Models.Reinforcement;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when a reinforcement is placed on the map: the used deployment capacity grows.</summary>
    public sealed class ReinforcementDeployedHintCondition : IHintCondition
    {
        private readonly IReinforcementModelObserver _reinforcements;
        private int _capacity;

        public bool IsMet { get; private set; }

        public ReinforcementDeployedHintCondition(IReinforcementModelObserver reinforcements)
        {
            _reinforcements = reinforcements;
            _capacity = _reinforcements.CurrentUnitCapacity;
            _reinforcements.OnCapacityChanged += HandleCapacityChanged;
        }

        private void HandleCapacityChanged(int capacity)
        {
            if (capacity > _capacity) IsMet = true;
            _capacity = capacity;
        }

        public void Dispose() => _reinforcements.OnCapacityChanged -= HandleCapacityChanged;
    }
}
