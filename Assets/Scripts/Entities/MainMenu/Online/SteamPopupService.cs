using System;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    public sealed class SteamPopupService : ISteamPopupService
    {
        private readonly IUiService _uiService;
        private readonly IPopupService _popups;
        private readonly IConfirmPopupService _confirmPopups;
        private readonly SteamPlatform _steam;
        private readonly ITooltipService _tooltips;
        private PopupHandle _invitePicker;

        public SteamPopupService(IUiService uiService, IPopupService popupService,
            IConfirmPopupService confirmPopups, OnlineMatchService online, ITooltipService tooltips)
        {
            _uiService = uiService;
            _popups = popupService;
            _confirmPopups = confirmPopups;
            _steam = online.Steam;
            _tooltips = tooltips;
        }

        public PopupHandle OpenInvitePicker()
        {
            if (!_popups.IsOpen(_invitePicker))
                _invitePicker = _popups.Open(new SteamInvitePopupUiController(_uiService, _steam, _tooltips));

            return _invitePicker;
        }

        public void CloseInvitePicker(PopupHandle handle) => _popups.Close(handle);

        public void ShowInviteReceived(string from, Action<ConfirmPopupResult> answered) =>
            _confirmPopups.ShowAccept(new AcceptPopupContent(
                title: "Game invite",
                message: $"{from} invited you to join their online lobby.",
                acceptLabel: "Join",
                declineLabel: "Later"), answered);
    }
}
