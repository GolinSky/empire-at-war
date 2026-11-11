using System;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Services.Popups
{
    public sealed class ConfirmPopupService : IConfirmPopupService
    {
        private readonly IUiService _uiService;
        private readonly IPopupService _popups;

        public ConfirmPopupService(IUiService uiService, IPopupService popups)
        {
            _uiService = uiService;
            _popups = popups;
        }

        public void ShowAccept(AcceptPopupContent content, Action<ConfirmPopupResult> answered) =>
            _popups.Open(new AcceptPopupUiController(_uiService, content, answered));
    }
}
