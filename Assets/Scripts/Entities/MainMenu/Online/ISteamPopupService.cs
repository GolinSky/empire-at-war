using System;
using EmpireAtWar.Services.Popups;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    public interface ISteamPopupService
    {
        PopupHandle OpenInvitePicker();
        void CloseInvitePicker(PopupHandle handle);
        void ShowInviteReceived(string from, Action<ConfirmPopupResult> answered);
    }
}
