using EmpireAtWar.Entities.Reinforcement;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the reinforcement panel opens, or is already open when the condition starts.</summary>
    public sealed class ReinforcementPanelHintCondition : IHintCondition
    {
        private readonly IReinforcementPanelObserver _panel;

        public bool IsMet { get; private set; }

        public ReinforcementPanelHintCondition(IReinforcementPanelObserver panel)
        {
            _panel = panel;
            IsMet = panel.IsOpen;
            _panel.Opened += HandleOpened;
        }

        private void HandleOpened() => IsMet = true;

        public void Dispose() => _panel.Opened -= HandleOpened;
    }
}
