using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Online
{
    /// <summary>
    /// Online entry screen and lobby routing: while this PC sits in a lobby, the lobby's setup screen is shown;
    /// when the session ends (left, kicked, host gone, connection failed) the entry screen returns with the reason.
    /// </summary>
    public sealed class OnlineRouteUiController : UiController, IOnlineRoute, IOnlinePresenter, IInitializable,
        ILateDisposable
    {
        private readonly OnlineMatchService _online;
        private readonly IOnlineSkirmishLobbyScreen _skirmishLobby;
        private readonly IOnlineSurvivalLobbyScreen _survivalLobby;
        private OnlineUi _ui;
        private bool _isOpen;
        private LobbyMode _shownLobby;

        public OnlineRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter, OnlineMatchService online,
            IOnlineSkirmishLobbyScreen skirmishLobby, IOnlineSurvivalLobbyScreen survivalLobby)
            : base(uiService, cancelRouter)
        {
            _online = online;
            _skirmishLobby = skirmishLobby;
            _survivalLobby = survivalLobby;
        }

        public void Initialize()
        {
            _online.Changed += Route;
            // A Steam join from the launch arguments may already be connecting.
            Route();
            // Back from a battle the host left: say why.
            if (_shownLobby == LobbyMode.None && _online.LastError != OnlineError.None) Open();
        }

        public void LateDispose()
        {
            _online.Changed -= Route;
            if (_ui != null) _ui.Dispose();
            _ui = null;
            Unfocus();
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<OnlineUi>();
                _ui.AssignPresenter(this);
                _ui.Initialize();
            }

            _isOpen = true;
            Render();
            _ui.Show();
            Focus();
        }

        public void Close()
        {
            // Back while connecting cancels the join.
            if (_online.Role != OnlineRole.None) _online.Leave();
            _isOpen = false;
            _ui.Hide();
            Unfocus();
        }

        public void HostSkirmish() => Host(LobbyMode.Skirmish);

        public void HostSurvival() => Host(LobbyMode.Survival);

        public void Join(string address)
        {
            _online.ClearError();
            _online.JoinByIp(address.Trim());
        }

        public void AcceptInvite()
        {
            _online.ClearError();
            _online.Steam.AcceptInvite();
        }

        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        private void Host(LobbyMode mode)
        {
            _online.ClearError();
            _online.HostLobby(mode);
        }

        private void Route()
        {
            if (_online.IsInBattle) return;
            LobbyMode lobby = _online.IsInLobby ? _online.Mode : LobbyMode.None;
            if (lobby != _shownLobby)
            {
                if (_shownLobby != LobbyMode.None) LobbyScreen(_shownLobby).Hide();
                LobbyMode left = _shownLobby;
                _shownLobby = lobby;
                if (lobby != LobbyMode.None)
                {
                    if (_isOpen) _ui.Hide();
                    Unfocus();
                    LobbyScreen(lobby).Show();
                }
                else if (left != LobbyMode.None)
                {
                    // Back on the entry screen with the reason the lobby ended.
                    Open();
                }
            }

            if (_isOpen && _shownLobby == LobbyMode.None) Render();
        }

        private IOnlineLobbyScreen LobbyScreen(LobbyMode mode) => mode switch
        {
            LobbyMode.Skirmish => _skirmishLobby,
            LobbyMode.Survival => _survivalLobby,
            _ => throw new System.ArgumentOutOfRangeException(nameof(mode), mode, "No lobby screen for this mode.")
        };

        private void Render()
        {
            bool connecting = _online.Role == OnlineRole.Client && !_online.IsInLobby;
            string status = _online.LastError != OnlineError.None ? OnlineErrorText.Describe(_online.LastError)
                : connecting ? "Connecting to the host..."
                : _online.Steam.IsInitialized
                    ? $"Steam: signed in as {_online.Steam.PersonaName}. Friends join your lobby from a Steam invite or by IP."
                    : "Steam is not running: hosting and joining work by IP only.";
            _ui.Render(status, connecting, _online.Steam.HasPendingInvite ? _online.Steam.PendingInviteFrom : string.Empty);
        }
    }
}
