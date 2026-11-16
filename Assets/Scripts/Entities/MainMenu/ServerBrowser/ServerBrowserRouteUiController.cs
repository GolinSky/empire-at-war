using System.Collections.Generic;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.ServerBrowser
{
    /// <summary>
    /// Join Game screen: lists open Steam lobbies and the player's saved battle, or joins by IP.
    /// </summary>
    public sealed class ServerBrowserRouteUiController : UiController, IServerBrowserRoute, IServerBrowserPresenter,
        IInitializable, ILateDisposable
    {
        private readonly OnlineMatchService _online;
        private readonly List<ServerBrowserRowState> _rows = new List<ServerBrowserRowState>();
        private ServerBrowserUi _ui;
        private bool _isOpen;
        private bool _hasRejoinRow;

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
            if (_online.IsReconnecting) _online.CancelReconnect();
            else if (_online.Role != OnlineRole.None) _online.LeaveLobby();
            Hide();
        }

        public void Refresh()
        {
            if (_online.Steam.IsInitialized) _online.Steam.RequestLobbyList();
            Render();
        }

        public void JoinLobby(int index)
        {
            if (_hasRejoinRow && index == 0)
            {
                _online.Reconnect();
                return;
            }

            int lobbyIndex = _hasRejoinRow ? index - 1 : index;
            _online.JoinListedLobby(_online.Steam.ListedLobbies[lobbyIndex]);
        }

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
            if (_online.IsInLobby || _online.IsInBattle) Hide();
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
            _hasRejoinRow = _online.CanRejoinFromServerBrowser;
            if (_hasRejoinRow) _rows.Add(new ServerBrowserRowState("Saved battle", "REJOIN", string.Empty));
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
            if (!_online.Steam.IsInitialized) return "Steam not detected: the lobby list needs Steam. Join a LAN / IP game by address below.";
            if (_online.Steam.IsListingLobbies) return "Searching for open lobbies...";
            if (_hasRejoinRow) return "Rejoin your saved battle, choose an open lobby, or join a host by IP.";
            return _rows.Count == 0 ? "No open lobbies found. Refresh, or join a host by IP."
                : "Choose a lobby to join, or join a host by IP.";
        }
    }
}
