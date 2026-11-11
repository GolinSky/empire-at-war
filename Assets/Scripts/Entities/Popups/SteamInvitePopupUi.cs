using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.Popups
{
    public sealed class SteamInvitePopupUi : BaseUi
    {
        [SerializeField] private SteamInviteView inviteView;

        public SteamInviteView InviteView => inviteView;
    }
}
