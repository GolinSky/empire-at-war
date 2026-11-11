using System;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Popups
{
    internal sealed class SteamInvitePopupUiController : UiController, IPopupController
    {
        private readonly SteamPlatform _steam;
        private SteamInvitePopupUi _ui;
        private SteamInvitePresenter _presenter;

        public SteamInvitePopupUiController(IUiService uiService, SteamPlatform steam) : base(uiService)
        {
            _steam = steam;
        }

        public BaseUi Open(Action complete)
        {
            _ui = CreateUi<SteamInvitePopupUi>();
            _presenter = new SteamInvitePresenter(_steam, _ui.InviteView, complete);
            _presenter.Initialize();
            _presenter.Open();
            _ui.Show();
            return _ui;
        }

        public void SetCovered(bool covered) => _ui.InviteView.SetCovered(covered);

        public void Cancel() => _presenter.Close();

        public void Dispose(bool dismissed) => _presenter.Dispose();
    }
}
