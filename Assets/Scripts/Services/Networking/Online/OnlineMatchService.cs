using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking.Online.Messages;
using kcp2k;
using Mirror;
using Mirror.FizzySteam;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>
    /// Project-lifetime online coordinator: Steam, the Mirror manager (created only on <see cref="EnterOnline"/>),
    /// the lobby, the battle start handshake and disconnect grace. Offline play never touches Mirror.
    /// </summary>
    public sealed class OnlineMatchService : IInitializable, ITickable, ILateDisposable, INetworkPlayerDirectory
    {
        public const float RECONNECT_GRACE_SECONDS = 60f;
        private const float KICK_DISCONNECT_DELAY = 0.5f;

        private readonly IGameCommand _gameCommand;
        private readonly SteamPlatform _steam = new SteamPlatform();
        private readonly OnlineLobby _lobby = new OnlineLobby();
        private readonly HashSet<int> _loaded = new HashSet<int>();
        private readonly Dictionary<int, float> _disconnectedAt = new Dictionary<int, float>();
        private readonly List<(NetworkConnectionToClient Connection, float At)> _pendingKicks =
            new List<(NetworkConnectionToClient, float)>();
        private GameObject _root;
        private OnlineNetworkManager _manager;
        private KcpTransport _kcp;
        private FizzySteamworks _fizzy;
        private string _setupJson;
        private bool _leaving;
        private bool _forceStart;

        public OnlineMatchService(IGameCommand gameCommand)
        {
            _gameCommand = gameCommand;
        }

        public event Action Changed;
        /// <summary>Client: the host dropped during a battle; the battle returns to the menu.</summary>
        public event Action HostLeft;
        public event Action BattleBegun;
        /// <summary>Host: a client finished loading (start or rejoin); the battle sends it the baseline.</summary>
        public event Action<NetworkConnectionToClient> ClientLoaded;
        /// <summary>Host: a player stayed away past <see cref="RECONNECT_GRACE_SECONDS"/>; AI takes their slot.</summary>
        public event Action<PlayerId> PlayerAbandoned;

        public OnlineRole Role { get; private set; }
        public OnlineError LastError { get; private set; }
        public SteamPlatform Steam => _steam;
        public IReadOnlyList<LobbyMember> Members => _lobby.Members;
        public int LocalConnectionId { get; private set; }
        public int LocalSlotIndex { get; private set; } = -1;
        public bool IsInBattle { get; private set; }
        public bool HasBegun { get; private set; }
        public bool CanStart => Role == OnlineRole.Host && (_lobby.AllReady || _forceStart);

        public bool AllClientsLoaded
        {
            get
            {
                foreach (LobbyMember member in _lobby.Members)
                {
                    if (member.Connected && member.ConnectionId != OnlineLobby.HOST_CONNECTION_ID &&
                        !_loaded.Contains(member.ConnectionId)) return false;
                }

                return true;
            }
        }

        public void Initialize()
        {
            if (!_steam.TryInitialize()) return;
            _steam.JoinedHost += JoinSteamHost;
            _steam.Failed += Fail;
            _steam.TryJoinFromLaunchArgs();
        }

        public void Tick()
        {
            _steam.Tick();
            for (int i = _pendingKicks.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < _pendingKicks[i].At) continue;
                _pendingKicks[i].Connection.Disconnect();
                _pendingKicks.RemoveAt(i);
            }

            if (_disconnectedAt.Count == 0) return;
            foreach (KeyValuePair<int, float> entry in new List<KeyValuePair<int, float>>(_disconnectedAt))
            {
                if (Time.unscaledTime - entry.Value < RECONNECT_GRACE_SECONDS) continue;
                _disconnectedAt.Remove(entry.Key);
                PlayerAbandoned?.Invoke(new PlayerId(entry.Key));
            }
        }

        public void LateDispose()
        {
            Leave();
            _steam.Shutdown();
            if (_root != null) Object.Destroy(_root);
        }

        /// <summary>Creates the persistent network object; called when the player picks Online.</summary>
        public void EnterOnline()
        {
            if (_root != null) return;
            _root = new GameObject("OnlineNetwork");
            _root.SetActive(false);
            Object.DontDestroyOnLoad(_root);
            _kcp = _root.AddComponent<KcpTransport>();
            _fizzy = _root.AddComponent<FizzySteamworks>();
            _fizzy.enabled = _steam.IsInitialized;
            _manager = _root.AddComponent<OnlineNetworkManager>();
            _manager.autoCreatePlayer = false;
            _manager.maxConnections = OnlineLobby.MAX_MEMBERS;
            _manager.transport = _kcp;
            _manager.ServerStarted += RegisterServerHandlers;
            _manager.ServerClientDisconnected += OnServerDisconnect;
            _manager.ClientStarted += RegisterClientHandlers;
            _manager.ClientDisconnected += OnClientDisconnect;
            _root.SetActive(true);
        }

        public void HostLobby(bool viaSteam)
        {
            if (viaSteam && !_steam.IsInitialized)
            {
                Fail(OnlineError.SteamNotRunning);
                return;
            }

            EnterOnline();
            ResetMatch(OnlineRole.Host);
            _lobby.Reset(_steam.PersonaName);
            _manager.UseTransport(viaSteam ? _fizzy : _kcp);
            _manager.StartHost();
            if (viaSteam) _steam.CreateLobby();
            Changed?.Invoke();
        }

        public void JoinByIp(string address)
        {
            EnterOnline();
            ResetMatch(OnlineRole.Client);
            _manager.UseTransport(_kcp);
            _manager.networkAddress = address;
            _manager.StartClient();
            Changed?.Invoke();
        }

        public void Leave()
        {
            if (Role == OnlineRole.None) return;
            _leaving = true;
            if (Role == OnlineRole.Host) _manager.StopHost();
            else _manager.StopClient();
            _steam.Leave();
            _leaving = false;
            ResetMatch(OnlineRole.None);
            Changed?.Invoke();
        }

        public void SetReady(bool ready) => NetworkClient.Send(new LobbyReadyMessage { Ready = ready });

        public void ArmForceStart() => _forceStart = true;

        public void Kick(int connectionId)
        {
            if (NetworkServer.connections.TryGetValue(connectionId, out NetworkConnectionToClient connection))
                Reject(connection, OnlineError.Kicked);
        }

        public void ClearError()
        {
            LastError = OnlineError.None;
            Changed?.Invoke();
        }

        /// <summary>Host: assigns human slots, sends every client its slot and loads the battle locally.</summary>
        public bool TryStartMatch(BattleDefinition battle)
        {
            if (!CanStart || !_lobby.TryAssignSlots(battle.Slots)) return false;
            _setupJson = JsonUtility.ToJson(BattleSetupSave.From(battle));
            foreach (LobbyMember member in _lobby.Members)
            {
                if (member.ConnectionId == OnlineLobby.HOST_CONNECTION_ID) LocalSlotIndex = member.SlotIndex;
                else
                    NetworkServer.connections[member.ConnectionId].Send(
                        new BattleStartMessage { SetupJson = _setupJson, LocalSlotIndex = member.SlotIndex });
            }

            BeginBattleLoad();
            _gameCommand.StartBattle(battle);
            return true;
        }

        /// <summary>The battle scope's session; the offline one when no online match is loading.</summary>
        public IMatchSession CreateSession(Func<IMatchSession> offline) =>
            IsInBattle ? new OnlineSession(Role == OnlineRole.Host, new PlayerId(LocalSlotIndex)) : offline();

        /// <summary>The battle scene finished loading on this PC.</summary>
        public void NotifyLocalLoaded()
        {
            if (Role == OnlineRole.Client) NetworkClient.Send(new BattleLoadedMessage());
        }

        /// <summary>Host: clients have loaded (or Force Start); client: the host's begin message arrived.</summary>
        public void MarkBegun()
        {
            HasBegun = true;
            BattleBegun?.Invoke();
        }

        public bool TryGetPlayer(int connectionId, out PlayerId player)
        {
            player = PlayerId.None;
            if (!_lobby.TryGetMember(connectionId, out LobbyMember member) || member.SlotIndex < 0) return false;
            player = new PlayerId(member.SlotIndex);
            return true;
        }

        private void RegisterServerHandlers()
        {
            NetworkServer.RegisterHandler<LobbyHelloMessage>(OnHello);
            NetworkServer.RegisterHandler<LobbyReadyMessage>(OnReady);
            NetworkServer.RegisterHandler<BattleLoadedMessage>(OnClientLoaded);
        }

        private void RegisterClientHandlers()
        {
            NetworkClient.RegisterHandler<LobbyStateMessage>(OnLobbyState);
            NetworkClient.RegisterHandler<LobbyRejectedMessage>(message => Fail(message.Reason));
            NetworkClient.RegisterHandler<BattleStartMessage>(OnBattleStart);
            NetworkClient.RegisterHandler<BattleBeginMessage>(_ => MarkBegun());
            if (Role == OnlineRole.Client) NetworkClient.OnConnectedEvent += SendHello;
        }

        private void SendHello()
        {
            NetworkClient.OnConnectedEvent -= SendHello;
            NetworkClient.Send(new LobbyHelloMessage { Name = _steam.PersonaName, Version = Application.version });
        }

        private void OnHello(NetworkConnectionToClient connection, LobbyHelloMessage hello)
        {
            if (hello.Version != Application.version)
            {
                Reject(connection, OnlineError.VersionMismatch);
                return;
            }

            if (IsInBattle)
            {
                if (!_lobby.TryRebind(hello.Name, connection.connectionId, out int slot))
                {
                    Reject(connection, OnlineError.BattleInProgress);
                    return;
                }

                _disconnectedAt.Remove(slot);
                connection.Send(new BattleStartMessage { SetupJson = _setupJson, LocalSlotIndex = slot });
                BroadcastLobby();
                return;
            }

            if (_lobby.IsFull)
            {
                Reject(connection, OnlineError.LobbyFull);
                return;
            }

            _lobby.Add(connection.connectionId, hello.Name);
            BroadcastLobby();
        }

        private void OnReady(NetworkConnectionToClient connection, LobbyReadyMessage message)
        {
            _lobby.SetReady(connection.connectionId, message.Ready);
            BroadcastLobby();
        }

        private void OnClientLoaded(NetworkConnectionToClient connection, BattleLoadedMessage _)
        {
            _loaded.Add(connection.connectionId);
            ClientLoaded?.Invoke(connection);
            Changed?.Invoke();
        }

        private void OnServerDisconnect(NetworkConnectionToClient connection)
        {
            _loaded.Remove(connection.connectionId);
            if (IsInBattle && _lobby.TryGetMember(connection.connectionId, out LobbyMember member))
            {
                _lobby.MarkDisconnected(connection.connectionId);
                if (member.SlotIndex >= 0) _disconnectedAt[member.SlotIndex] = Time.unscaledTime;
            }
            else
            {
                _lobby.Remove(connection.connectionId);
            }

            BroadcastLobby();
        }

        private void OnLobbyState(LobbyStateMessage message)
        {
            _lobby.Replace(message.Members);
            LocalConnectionId = message.YourConnectionId;
            Changed?.Invoke();
        }

        private void OnBattleStart(BattleStartMessage message)
        {
            BattleDefinition battle = JsonUtility.FromJson<BattleSetupSave>(message.SetupJson).Rebuild();
            LocalSlotIndex = message.LocalSlotIndex;
            BeginBattleLoad();
            _gameCommand.StartBattle(battle);
        }

        private void OnClientDisconnect()
        {
            if (_leaving || Role != OnlineRole.Client) return;
            bool wasInBattle = IsInBattle;
            if (LastError == OnlineError.None) LastError = OnlineError.HostLeft;
            _steam.Leave();
            ResetMatch(OnlineRole.None);
            Changed?.Invoke();
            if (wasInBattle) HostLeft?.Invoke();
        }

        private void JoinSteamHost(string hostSteamId)
        {
            EnterOnline();
            _fizzy.enabled = true;
            ResetMatch(OnlineRole.Client);
            _manager.UseTransport(_fizzy);
            _manager.networkAddress = hostSteamId;
            _manager.StartClient();
            Changed?.Invoke();
        }

        private void Reject(NetworkConnectionToClient connection, OnlineError reason)
        {
            connection.Send(new LobbyRejectedMessage { Reason = reason });
            _pendingKicks.Add((connection, Time.unscaledTime + KICK_DISCONNECT_DELAY));
        }

        private void BroadcastLobby()
        {
            LobbyMember[] members = new LobbyMember[_lobby.Members.Count];
            for (int i = 0; i < members.Length; i++) members[i] = _lobby.Members[i];
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection == NetworkServer.localConnection) continue;
                connection.Send(new LobbyStateMessage { Members = members, YourConnectionId = connection.connectionId });
            }

            Changed?.Invoke();
        }

        private void BeginBattleLoad()
        {
            IsInBattle = true;
            HasBegun = false;
            _loaded.Clear();
        }

        private void ResetMatch(OnlineRole role)
        {
            Role = role;
            IsInBattle = false;
            HasBegun = false;
            _forceStart = false;
            LocalSlotIndex = -1;
            _loaded.Clear();
            _disconnectedAt.Clear();
            if (role != OnlineRole.None) LastError = OnlineError.None;
        }

        private void Fail(OnlineError error)
        {
            LastError = error;
            Changed?.Invoke();
        }
    }
}
