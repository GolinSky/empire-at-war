using System.Collections.Generic;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.ServerBrowser
{
    /// <summary>
    /// Join Game screen: lists open Steam lobbies of this version and joins one, or joins by IP. Entering the lobby
    /// closes this screen; the online route then shows the lobby.
    /// </summary>
    public sealed class ServerBrowserRouteUiController : UiController, IServerBrowserRoute, IServerBrowserPresenter,
        IInitializable, ILateDisposable
    {
        private readonly OnlineMatchService _online;
        private readonly List<ServerBrowserRowState> _rows = new List<ServerBrowserRowState>();
        private ServerBrowserUi _ui;
        private bool _isOpen;

        public ServerBrowserRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            OnlineMatchService online) : base(uiService, cancelRouter)
        {
            _online = online;
        }

        public void Initialize()
        {
            _online.Changed += OnOnlineChanged;
            _online.Steam.LobbyListReceived += Render;
        }

        public void LateDispose()
        {
            _online.Changed -= OnOnlineChanged;
            _online.Steam.LobbyListReceived -= Render;
            if (_ui != null) _ui.Dispose();
            _ui = null;
            Unfocus();
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<ServerBrowserUi>();
                _ui.AssignPresenter(this);
                _ui.Initialize();
            }

            _isOpen = true;
            _online.ClearError();
            Refresh();
            _ui.Show();
            Focus();
        }

        // Back while connecting cancels the join.
        public void Close()
        {
            if (_online.Role != OnlineRole.None) _online.Leave();
            Hide();
        }

        public void Refresh()
        {
            if (_online.Steam.IsInitialized) _online.Steam.RequestLobbyList();
            Render();
        }

        public void JoinLobby(int index) => _online.JoinListedLobby(_online.Steam.ListedLobbies[index]);

        public void JoinByIp(string address)
        {
            _online.ClearError();
            _online.JoinByIp(address.Trim());
        }

        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        private void OnOnlineChanged()
        {
            if (!_isOpen) return;
            if (_online.IsInLobby) Hide();
            else Render();
        }

        private void Hide()
        {
            _isOpen = false;
            _ui.Hide();
            Unfocus();
        }

        private void Render()
        {
            if (!_isOpen) return;
            bool connecting = _online.Role == OnlineRole.Client && !_online.IsInLobby;
            _rows.Clear();
            foreach (SteamLobbyListing lobby in _online.Steam.ListedLobbies)
            {
                string host = lobby.HostName.Length > 0 ? lobby.HostName : "Unknown host";
                _rows.Add(new ServerBrowserRowState(host, lobby.Mode.ToUpperInvariant(),
                    $"{lobby.Members} / {lobby.Capacity}"));
            }

            _ui.Render(_rows, Status(connecting), connecting, _online.Steam.IsInitialized);
        }

        private string Status(bool connecting)
        {
            if (_online.LastError != OnlineError.None) return OnlineErrorText.Describe(_online.LastError);
            if (connecting) return "Connecting to the host...";
            if (!_online.Steam.IsInitialized) return "Steam is not running: join a host by IP below.";
            if (_online.Steam.IsListingLobbies) return "Searching for open lobbies...";
            return _rows.Count == 0 ? "No open lobbies found. Refresh, or join a host by IP."
                : "Choose a lobby to join, or join a host by IP.";
        }
    }
}
