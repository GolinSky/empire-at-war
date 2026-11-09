using EmpireAtWar.Entities.ShipUi;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class AutoPowerToggledHintCondition : IHintCondition
    {
        private readonly IAutoPowerToggleObserver _toggle;

        public bool IsMet { get; private set; }

        public AutoPowerToggledHintCondition(IAutoPowerToggleObserver toggle)
        {
            _toggle = toggle;
            _toggle.AutoPowerToggled += HandleToggled;
        }

        private void HandleToggled() => IsMet = true;

        public void Dispose() => _toggle.AutoPowerToggled -= HandleToggled;
    }
}
