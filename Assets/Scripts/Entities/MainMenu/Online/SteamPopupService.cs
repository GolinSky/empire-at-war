using System;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    public sealed class SteamPopupService : ISteamPopupService
    {
        private readonly IUiService _uiService;
        private readonly IPopupService _popups;
        private readonly IConfirmPopupService _confirmPopups;
        private readonly SteamPlatform _steam;
        private PopupHandle _invitePicker;

        public SteamPopupService(IUiService uiService, IPopupService popupService,
            IConfirmPopupService confirmPopups, OnlineMatchService online)
        {
            _uiService = uiService;
            _popups = popupService;
            _confirmPopups = confirmPopups;
            _steam = online.Steam;
        }

        public PopupHandle OpenInvitePicker()
        {
            if (!_popups.IsOpen(_invitePicker))
                _invitePicker = _popups.Open(new SteamInvitePopupUiController(_uiService, _steam));

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
