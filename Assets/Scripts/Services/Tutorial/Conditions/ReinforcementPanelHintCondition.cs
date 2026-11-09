using EmpireAtWar.Entities.Reinforcement;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the player opens the reinforcement panel.</summary>
    public sealed class ReinforcementPanelHintCondition : IHintCondition
    {
        private readonly IReinforcementPanelObserver _panel;

        public bool IsMet { get; private set; }

        public ReinforcementPanelHintCondition(IReinforcementPanelObserver panel)
        {
            _panel = panel;
            _panel.Opened += HandleOpened;
        }

        private void HandleOpened() => IsMet = true;

        public void Dispose() => _panel.Opened -= HandleOpened;
    }
}
