using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
    /// the lobby (members, ready ticks, the host's setup snapshot), the battle start handshake and disconnect
    /// grace. Offline play never touches Mirror.
    /// </summary>
    public sealed class OnlineMatchService : IInitializable, ITickable, ILateDisposable, INetworkPlayerDirectory,
        IOnlineLocalSlot
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
        private MultiplexTransport _multiplex;
        private string _battleJson;
        private bool _leaving;
        private bool _inLobby;

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
        /// <summary>Host: a client asked to change its own Skirmish row.</summary>
        public event Action<int, LobbySlotEditMessage> SlotEditRequested;

        public OnlineRole Role { get; private set; }
        public OnlineError LastError { get; private set; }
        public SteamPlatform Steam => _steam;
        public IReadOnlyList<LobbyMember> Members => _lobby.Members;
        public int LocalConnectionId { get; private set; }
        public int LocalSlotIndex { get; private set; } = -1;
        /// <summary>The setup screen the host picked; clients learn it from the first lobby state.</summary>
        public LobbyMode Mode { get; private set; }
        /// <summary>The host setup screen's lobby snapshot (JSON); empty until the host publishes one.</summary>
        public string SetupJson { get; private set; } = string.Empty;
        /// <summary>Client: the host accepted this PC and sent the lobby; host: always once hosting.</summary>
        public bool IsInLobby => Role == OnlineRole.Host || _inLobby;
        public bool AllReady => _lobby.AllReady;
        public bool IsSteamLobby => _steam.InLobby;
        /// <summary>Host: the KCP port friends use with Join by IP.</summary>
        public ushort Port => _kcp.Port;
        public bool IsInBattle { get; private set; }
        public bool HasBegun { get; private set; }
        /// <summary>A restart is reloading the battle scene; the closing scene keeps the session.</summary>
        public bool IsReloading { get; private set; }
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
            _steam.JoinStarting += BeginSteamJoin;
            _steam.Failed += OnSteamFailed;
            // The Steam lobby (invite button) and in-game invites arrive later through Steam callbacks.
            _steam.HostLobbyReady += OnSteamLobbyReady;
            _steam.InviteReceived += NotifyChanged;
            _steam.AvatarLoaded += NotifyChanged;
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
                _lobby.RemoveAbandoned(entry.Key);
                BroadcastLobby();
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
            // The host listens on direct IP and, with Steam running, on the Steam relay at once.
            _multiplex = _root.AddComponent<MultiplexTransport>();
            _multiplex.transports = _steam.IsInitialized ? new Transport[] { _kcp, _fizzy } : new Transport[] { _kcp };
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

        /// <summary>Hosts a lobby reachable by IP and, with Steam running, by Steam invite.</summary>
        public void HostLobby(LobbyMode mode)
        {
            EnterOnline();
            ResetMatch(OnlineRole.Host);
            Mode = mode;
            _lobby.Reset(_steam.PersonaName, _steam.LocalSteamId);
            _manager.UseTransport(_multiplex);
            _manager.StartHost();
            if (_steam.IsInitialized) _steam.CreateLobby();
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
            _steam.ClearPresence();
            _leaving = false;
            ResetMatch(OnlineRole.None);
            Changed?.Invoke();
        }

        public void SetReady(bool ready) => NetworkClient.Send(new LobbyReadyMessage { Ready = ready });

        /// <summary>Client: asks the host to change this PC's Skirmish row.</summary>
        public void SendSlotEdit(int faction, int team, int color) =>
            NetworkClient.Send(new LobbySlotEditMessage { Faction = faction, Team = team, Color = color });

        /// <summary>Host: the setup screen changed; every client gets the new snapshot.</summary>
        public void PublishSetup(string setupJson)
        {
            if (setupJson == SetupJson) return;
            SetupJson = setupJson;
            BroadcastLobby();
        }

        /// <summary>The member leaves the lobby at once, so its row frees before the delayed disconnect.</summary>
        public void Kick(int connectionId)
        {
            if (!NetworkServer.connections.TryGetValue(connectionId, out NetworkConnectionToClient connection)) return;
            _lobby.Remove(connectionId);
            Reject(connection, OnlineError.Kicked);
            BroadcastLobby();
        }

        public bool TryGetLocalSlot(out PlayerId player)
        {
            player = PlayerId.None;
            if (!IsInBattle) return false;
            player = new PlayerId(LocalSlotIndex);
            return true;
        }

        /// <summary>Host: this PC's IPv4 addresses, shown so friends can Join by IP.</summary>
        public IReadOnlyList<string> LocalAddresses() => Dns.GetHostAddresses(Dns.GetHostName())
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .Select(address => address.ToString())
            .ToArray();

        public void ClearError()
        {
            LastError = OnlineError.None;
            Changed?.Invoke();
        }

        /// <summary>
        /// Host, Skirmish: every member already sits in a Human row; <paramref name="memberSlots"/> maps each
        /// member's connection id to its slot index in <paramref name="battle"/>.
        /// </summary>
        public void StartSkirmish(BattleDefinition battle, IReadOnlyDictionary<int, int> memberSlots)
        {
            _lobby.AssignSlots(memberSlots);
            StartBattle(battle);
        }

        /// <summary>Host, Survival: adds a co-op human per member (<see cref="OnlineLineup"/>), host first.</summary>
        public bool TryStartSurvival(BattleDefinition setup)
        {
            if (!OnlineLineup.TryFit(setup, _lobby.Members.Count, out BattleDefinition battle, out OnlineError error))
            {
                Fail(error);
                return false;
            }

            if (!_lobby.TryAssignSlots(battle.Slots)) throw new InvalidOperationException(
                "The fitted line-up must hold a Human slot for every lobby member.");
            StartBattle(battle);
            return true;
        }

        private void StartBattle(BattleDefinition battle)
        {
            ClearError();
            _battleJson = JsonUtility.ToJson(BattleSetupSave.From(battle));
            foreach (LobbyMember member in _lobby.Members)
            {
                if (member.ConnectionId == OnlineLobby.HOST_CONNECTION_ID) LocalSlotIndex = member.SlotIndex;
                else
                    NetworkServer.connections[member.ConnectionId].Send(
                        new BattleStartMessage { SetupJson = _battleJson, LocalSlotIndex = member.SlotIndex });
            }

            BeginBattleLoad();
            _gameCommand.StartBattle(battle);
        }

        /// <summary>
        /// Host, in battle: every connected member reloads the same battle with its slot; members still in their
        /// reconnect grace get it when they rejoin.
        /// </summary>
        public void RestartMatch(BattleDefinition battle)
        {
            if (Role != OnlineRole.Host || !IsInBattle)
                throw new InvalidOperationException("Only the host restarts an online battle.");
            _battleJson = JsonUtility.ToJson(BattleSetupSave.From(battle));
            foreach (LobbyMember member in _lobby.Members)
            {
                if (member.ConnectionId == OnlineLobby.HOST_CONNECTION_ID ||
                    !NetworkServer.connections.TryGetValue(member.ConnectionId, out NetworkConnectionToClient connection))
                    continue;
                connection.Send(new BattleStartMessage { SetupJson = _battleJson, LocalSlotIndex = member.SlotIndex });
            }

            IsReloading = true;
            BeginBattleLoad();
            _gameCommand.StartBattle(battle);
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
            _steam.SetStatus(Mode == LobbyMode.Survival ? "Survival co-op battle" : "Skirmish battle");
            IsReloading = false;
            HasBegun = true;
            BattleBegun?.Invoke();
        }

        /// <summary>Host: this remote client reported the battle scene loaded, so it has the battle handlers.
        /// Never true for the host's own local connection.</summary>
        public bool IsBattleLoaded(NetworkConnectionToClient connection) => _loaded.Contains(connection.connectionId);

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
            NetworkServer.RegisterHandler<LobbySlotEditMessage>(OnSlotEdit);
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
            NetworkClient.Send(new LobbyHelloMessage
                { Name = _steam.PersonaName, Version = Application.version, SteamId = _steam.LocalSteamId });
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
                connection.Send(new BattleStartMessage { SetupJson = _battleJson, LocalSlotIndex = slot });
                BroadcastLobby();
                return;
            }

            if (_lobby.IsFull)
            {
                Reject(connection, OnlineError.LobbyFull);
                return;
            }

            _lobby.Add(connection.connectionId, hello.Name, hello.SteamId);
            BroadcastLobby();
        }

        private void OnReady(NetworkConnectionToClient connection, LobbyReadyMessage message)
        {
            _lobby.SetReady(connection.connectionId, message.Ready);
            BroadcastLobby();
        }

        private void OnSlotEdit(NetworkConnectionToClient connection, LobbySlotEditMessage message) =>
            SlotEditRequested?.Invoke(connection.connectionId, message);

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
            Mode = message.Mode;
            SetupJson = message.SetupJson;
            _inLobby = true;
            if (!IsInBattle) _steam.SetStatus(LobbyStatus());
            Changed?.Invoke();
        }

        private void OnBattleStart(BattleStartMessage message)
        {
            BattleDefinition battle = JsonUtility.FromJson<BattleSetupSave>(message.SetupJson).Rebuild();
            // A start that arrives during a battle is the host's restart.
            IsReloading = IsInBattle;
            LocalSlotIndex = message.LocalSlotIndex;
            BeginBattleLoad();
            _gameCommand.StartBattle(battle);
        }

        private void OnClientDisconnect()
        {
            if (_leaving || Role != OnlineRole.Client) return;
            bool wasInBattle = IsInBattle;
            // No lobby state yet: the host was never reached.
            if (LastError == OnlineError.None) LastError = _inLobby ? OnlineError.HostLeft : OnlineError.ConnectionFailed;
            _steam.Leave();
            _steam.ClearPresence();
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

        private void BeginSteamJoin()
        {
            Leave();
            EnterOnline();
            ResetMatch(OnlineRole.Client);
            Changed?.Invoke();
        }

        private void OnSteamFailed(OnlineError error)
        {
            if (Role == OnlineRole.Client) Leave();
            Fail(error);
        }

        private void Reject(NetworkConnectionToClient connection, OnlineError reason)
        {
            connection.Send(new LobbyRejectedMessage { Reason = reason });
            _pendingKicks.Add((connection, Time.unscaledTime + KICK_DISCONNECT_DELAY));
        }

        private string LobbyStatus() => $"In {Mode} lobby {_lobby.Members.Count}/{OnlineLobby.MAX_MEMBERS}";

        private void BroadcastLobby()
        {
            if (Role == OnlineRole.Host && !IsInBattle) _steam.SetStatus(LobbyStatus());
            LobbyMember[] members = new LobbyMember[_lobby.Members.Count];
            for (int i = 0; i < members.Length; i++) members[i] = _lobby.Members[i];
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection == NetworkServer.localConnection) continue;
                connection.Send(new LobbyStateMessage
                {
                    Members = members, YourConnectionId = connection.connectionId, Mode = Mode, SetupJson = SetupJson
                });
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
            IsReloading = false;
            _inLobby = false;
            Mode = LobbyMode.None;
            SetupJson = string.Empty;
            LocalSlotIndex = -1;
            _loaded.Clear();
            _disconnectedAt.Clear();
            if (role != OnlineRole.None) LastError = OnlineError.None;
        }

        private void OnSteamLobbyReady(string hostSteamId) => NotifyChanged();

        private void NotifyChanged() => Changed?.Invoke();

        private void Fail(OnlineError error)
        {
            LastError = error;
            Changed?.Invoke();
        }
    }
}
