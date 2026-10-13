using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipLifecyclePresenter : ITickable
    {
        private readonly IHudVisibilityObserver _hud;
        private readonly ITooltipService _tooltipService;

        public TooltipLifecyclePresenter(IHudVisibilityObserver hud, ITooltipService tooltipService)
        { _hud = hud; _tooltipService = tooltipService; }

        public void Tick() { if (!_hud.IsHudVisible) _tooltipService.HideAll(); }
    }
}
