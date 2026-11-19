using System;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Popups
{
    internal sealed class SteamInvitePopupUiController : IPopupController
    {
        private readonly IUiService _uiService;
        private readonly SteamPlatform _steam;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;
        private SteamInvitePopupUi _ui;
        private bool _isCovered;
        private SteamInvitePresenter _presenter;

        public SteamInvitePopupUiController(IUiService uiService, SteamPlatform steam, ITooltipService tooltipService)
        {
            _uiService = uiService;
            _steam = steam;
            _tooltips = new TooltipRequests(tooltipService);
        }

        public BaseUi Open(Action complete)
        {
            _ui = _uiService.CreateUi<SteamInvitePopupUi>();
            _presenter = new SteamInvitePresenter(_steam, _ui.InviteView, complete);
            _presenter.Initialize();
            _presenter.Open();
            _tooltipHover = new TooltipHoverSubscription(_ui.TooltipHover, HandleTooltipHover, _tooltips);
            _ui.Show();
            return _ui;
        }

        public void SetCovered(bool covered)
        {
            _isCovered = covered;
            if (covered) _tooltips.HideAll();
            _ui.InviteView.SetCovered(covered);
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => !_isCovered && _ui.IsVisible,
                () => new TooltipContent(title: (string)key, description: "Choose an online Steam friend to invite to the lobby."));

        public void Cancel() => _presenter.Close();

        public void Dispose(bool dismissed)
        {
            _tooltipHover.Dispose();
            _presenter.Dispose();
        }
    }
}
