using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;
using Zenject;

namespace EmpireAtWar.Entities.Tooltip
{
    public sealed class TooltipLifecyclePresenter : ITickable
    {
        private readonly IHudVisibilityObserver _hud;
        private readonly ITooltipService _tooltips;

        public TooltipLifecyclePresenter(IHudVisibilityObserver hud, ITooltipService tooltips)
        { _hud = hud; _tooltips = tooltips; }

        public void Tick() { if (!_hud.IsHudVisible) _tooltips.HideAll(); }
    }
}
