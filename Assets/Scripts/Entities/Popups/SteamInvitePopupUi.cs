using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.Popups
{
    public sealed class SteamInvitePopupUi : BaseUi
    {
        [SerializeField] private SteamInviteView inviteView;

        [SerializeField] private TooltipHoverView tooltipHover;

        public TooltipHoverView TooltipHover => tooltipHover;
        public SteamInviteView InviteView => inviteView;
    }
}
