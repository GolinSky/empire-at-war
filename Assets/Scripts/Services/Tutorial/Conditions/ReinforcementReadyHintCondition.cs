using EmpireAtWar.Models.Reinforcement;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met while built units wait in the reinforcement reserve.</summary>
    public sealed class ReinforcementReadyHintCondition : IHintCondition
    {
        private readonly IReinforcementModelObserver _reinforcements;

        public bool IsMet => _reinforcements.HasReserve;

        public ReinforcementReadyHintCondition(IReinforcementModelObserver reinforcements)
        {
            _reinforcements = reinforcements;
        }

        public void Dispose()
        {
        }
    }
}
