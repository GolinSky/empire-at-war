using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
        public const float RECONNECT_GRACE_SECONDS = ReconnectReservations.GRACE_SECONDS;
        internal const int LOBBY_PROTOCOL_VERSION = 3;
        private const float CONNECT_TIMEOUT_SECONDS = 30f;
        private const float KICK_DISCONNECT_DELAY = 0.5f;

        private readonly IGameCommand _gameCommand;
        private readonly OnlineRecoveryProgress _recoveryProgress;
        private readonly SteamPlatform _steam = new SteamPlatform();
        private readonly OnlineLobby _lobby = new OnlineLobby();
        private readonly HashSet<int> _loaded = new HashSet<int>();
        private readonly ReconnectReservations _reservations = new ReconnectReservations();
        private readonly Dictionary<string, OnlineError> _revokedCredentials = new Dictionary<string, OnlineError>();
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
        private ReconnectSessionStore _recoveryStore;
        private ReconnectSessionRecord _recovery;
        private string _matchId;
        private bool _matchEnded;
        private bool _recovering;
        private bool _localLoaded;
        private bool _awaitingSteamHost;
        private float _connectDeadline;
        private float _recoveryDeadline;
        private int _shownRecoverySeconds = -1;
        private float _stopHostAt;
        private float _stopClientAt;
        private int _attempt;
        private ReconnectTransport _clientTransport;
        private string _clientHost;
        private ulong _clientLobby;

        public bool HasRecovery => _recovery != null;
        public bool RecoveryFromStartup { get; private set; }
        public bool IsReconnecting => _recovering && Role == OnlineRole.Client && !HasBegun;
        public string RecoveryStatus { get; private set; } = string.Empty;

        public OnlineMatchService(IGameCommand gameCommand, OnlineRecoveryProgress recoveryProgress)
        {
            _gameCommand = gameCommand;
            _recoveryProgress = recoveryProgress;
            _recoveryStore = new ReconnectSessionStore(Application.persistentDataPath);
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
        public int BattleGeneration { get; private set; }
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
            try
            {
                if (_recoveryStore.TryLoad(out _recovery, out string error))
                {
                    RecoveryFromStartup = true;
                    RecoveryStatus = "Rejoin previous match";
                }
                else if (!string.IsNullOrEmpty(error)) LastError = OnlineError.RecoveryRecordInvalid;
            }
            catch (IOException) { LastError = OnlineError.RecoveryStorageFailed; }
            catch (UnauthorizedAccessException) { LastError = OnlineError.RecoveryStorageFailed; }
            if (StartSteam()) _steam.TryJoinFromLaunchArgs();
        }

        /// <summary>Online screen: Steam started after the game, so Steam lobbies, invites and the Server Browser come back.</summary>
        public void RetrySteam()
        {
            if (Role != OnlineRole.None || _steam.IsInitialized)
                throw new InvalidOperationException("Steam can be retried only while it is off and outside a lobby.");
            LastError = StartSteam() ? OnlineError.None : OnlineError.SteamNotRunning;
            NotifyChanged();
        }

        private bool StartSteam()
        {
            if (!_steam.TryInitialize()) return false;
            _steam.JoinedHost += JoinSteamHost;
            _steam.JoinStarting += BeginSteamJoin;
            _steam.Failed += OnSteamFailed;
            // The Steam lobby (invite button) and in-game invites arrive later through Steam callbacks.
            _steam.HostLobbyReady += OnSteamLobbyReady;
            _steam.InviteReceived += NotifyChanged;
            _steam.AvatarLoaded += NotifyChanged;
            if (_root != null) ApplySteamTransport();
            return true;
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

            if (_stopHostAt > 0f && Time.unscaledTime >= _stopHostAt)
            {
                _stopHostAt = 0f;
                _manager.StopHost();
            }
            if (_stopClientAt > 0f && Time.unscaledTime >= _stopClientAt)
            {
                _stopClientAt = 0f;
                _manager.StopClient();
            }
            if (Role == OnlineRole.Client && !IsInBattle && _connectDeadline > 0f &&
                Time.unscaledTime >= _connectDeadline)
            {
                Fail(OnlineError.ConnectionFailed);
                DisconnectClient();
            }
            if (IsReconnecting && _recoveryDeadline > 0f)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(0f, _recoveryDeadline - Time.unscaledTime));
                // The countdown text changes once per second; rebuilding and notifying every frame only allocates.
                if (seconds != _shownRecoverySeconds)
                {
                    _shownRecoverySeconds = seconds;
                    RecoveryStatus = $"{(_localLoaded ? "Synchronizing" : "Loading battle")} - host allowed {seconds} s";
                    NotifyChanged();
                }
            }
            if (Role == OnlineRole.Host) ExpireReservations();
        }

        private void ExpireReservations()
        {
            if (_reservations.Count == 0) return;
            foreach (int slot in _reservations.Expired(Time.unscaledTime))
            {
                LobbyMember member = _lobby.Members.First(candidate => candidate.SlotIndex == slot);
                _revokedCredentials[_lobby.GetReconnectCredential(member.ConnectionId)] = OnlineError.ReconnectExpired;
                _reservations.Remove(slot);
                _lobby.RemoveAbandoned(slot);
                _loaded.Remove(member.ConnectionId);
                if (NetworkServer.connections.TryGetValue(member.ConnectionId, out NetworkConnectionToClient connection))
                    Reject(connection, OnlineError.ReconnectExpired);
                BroadcastLobby();
                PlayerAbandoned?.Invoke(new PlayerId(slot));
            }
        }

        public void LateDispose()
        {
            // Quitting the application is not an explicit Leave: the saved record survives for the next launch's
            // Rejoin previous match, and no LobbyLeaveMessage revokes the credential on the host.
            if (Role == OnlineRole.Host) Leave();
            else if (Role == OnlineRole.Client)
            {
                _leaving = true;
                _manager.StopClient();
                _leaving = false;
            }
            _steam.Shutdown();
            if (_root != null) Object.Destroy(_root);
        }

        /// <summary>Creates the persistent network object; called when the player picks Online.</summary>
        public void EnterOnline()
        {
            if (_stopHostAt > 0f)
            {
                _stopHostAt = 0f;
                _manager.StopHost();
            }
            if (_stopClientAt > 0f)
            {
                _stopClientAt = 0f;
                _manager.StopClient();
            }
            if (_root != null) return;
            _root = new GameObject("OnlineNetwork");
            _root.SetActive(false);
            Object.DontDestroyOnLoad(_root);
            _kcp = _root.AddComponent<KcpTransport>();
            _fizzy = _root.AddComponent<FizzySteamworks>();
            _multiplex = _root.AddComponent<MultiplexTransport>();
            ApplySteamTransport();
            _manager = _root.AddComponent<OnlineNetworkManager>();
            _manager.autoCreatePlayer = false;
            // Leave transport capacity for an authenticated rejoin before the old connection times out.
            _manager.maxConnections = OnlineLobby.MAX_MEMBERS * 2;
            _manager.transport = _kcp;
            _manager.ServerStarted += RegisterServerHandlers;
            _manager.ServerClientDisconnected += OnServerDisconnect;
            _manager.ClientStarted += RegisterClientHandlers;
            _manager.ClientDisconnected += OnClientDisconnect;
            _root.SetActive(true);
        }

        // A Steam lobby host listens on direct IP and the Steam relay at once; Steam may start after this object exists.
        private void ApplySteamTransport()
        {
            _fizzy.enabled = _steam.IsInitialized;
            _multiplex.transports = _steam.IsInitialized ? new Transport[] { _kcp, _fizzy } : new Transport[] { _kcp };
        }

        /// <summary>
        /// Hosts a Skirmish lobby reachable by IP; a <paramref name="steamLobby"/> (Steam required) is also listed in
        /// the Server Browser and takes Steam invites. The host picks the mode in the lobby (<see cref="SwitchLobbyMode"/>).
        /// </summary>
        public void HostLobby(bool steamLobby)
        {
            if (steamLobby && !_steam.IsInitialized)
                throw new InvalidOperationException("A Steam lobby needs Steam running.");
            Leave();
            EnterOnline();
            ResetMatch(OnlineRole.Host);
            _matchId = Guid.NewGuid().ToString("N");
            Mode = LobbyMode.Skirmish;
            int capacity = CapacityOf(Mode);
            _lobby.Reset(_steam.PersonaName, _steam.LocalSteamId, capacity);
            _manager.UseTransport(steamLobby ? _multiplex : _kcp);
            _manager.StartHost();
            if (steamLobby) _steam.CreateLobby(capacity, Mode.ToString());
            NotifyChanged();
        }

        /// <summary>Host, in the lobby: Survival seats fewer members, so it is refused while too many are in.</summary>
        public bool CanSwitchLobbyMode(LobbyMode mode) => Role == OnlineRole.Host && !IsInBattle &&
            mode != LobbyMode.None && _lobby.Members.Count <= CapacityOf(mode);

        /// <summary>Host: every member sees the new mode's setup screen and confirms ready again.</summary>
        public void SwitchLobbyMode(LobbyMode mode)
        {
            if (!CanSwitchLobbyMode(mode))
                throw new InvalidOperationException($"The lobby cannot switch to {mode} now.");
            if (mode == Mode) return;
            Mode = mode;
            SetupJson = string.Empty;
            _lobby.ChangeCapacity(CapacityOf(mode));
            if (_steam.IsInitialized) _steam.UpdateHostedLobby(_lobby.Capacity, mode.ToString());
            BroadcastLobby();
        }

        /// <summary>Server Browser: joins a listed Steam lobby; the join reports through <see cref="Changed"/>.</summary>
        public void JoinListedLobby(SteamLobbyListing lobby)
        {
            ClearError();
            _steam.Join(lobby.Id);
        }

        public void JoinByIp(string address)
        {
            Leave();
            StartClient(ReconnectTransport.Kcp, address, 0);
        }

        private void StartClient(ReconnectTransport transport, string host, ulong lobby)
        {
            EnterOnline();
            ResetMatch(OnlineRole.Client);
            _clientTransport = transport;
            _clientHost = host;
            _clientLobby = lobby;
            _connectDeadline = Time.unscaledTime + CONNECT_TIMEOUT_SECONDS;
            _manager.UseTransport(transport == ReconnectTransport.Steam ? _fizzy : _kcp);
            _manager.networkAddress = host;
            _manager.StartClient();
            NotifyChanged();
        }

        public void Reconnect()
        {
            if (!HasRecovery || IsReconnecting || Role != OnlineRole.None) return;
            if (_recovery.Transport == ReconnectTransport.Steam && !_steam.IsInitialized)
            {
                Fail(OnlineError.SteamNotRunning);
                return;
            }
            if (_recovery.Transport == ReconnectTransport.Steam && _recovery.SteamId != _steam.LocalSteamId)
            {
                ClearRecovery();
                Fail(OnlineError.ReconnectRejected);
                return;
            }
            _recovering = true;
            RecoveryStatus = "Connecting - checking session";
            _recoveryDeadline = 0f;
            EnterOnline();
            _kcp.Port = _recovery.Port;
            StartClient(_recovery.Transport, _recovery.Host, _recovery.LobbyId);
        }

        public void DismissRecovery()
        {
            bool wasInBattle = IsInBattle;
            Leave();
            if (LastError != OnlineError.RecoveryStorageFailed) LastError = OnlineError.None;
            NotifyChanged();
            if (wasInBattle) HostLeft?.Invoke();
        }

        public void Leave()
        {
            ClearRecovery();
            _attempt++;
            _awaitingSteamHost = false;
            _recovering = false;
            NetworkClient.OnConnectedEvent -= SendHello;
            _leaving = true;
            if (Role == OnlineRole.Host)
            {
                foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
                    if (connection != NetworkServer.localConnection)
                        connection.Send(new LobbyRejectedMessage { Reason = OnlineError.HostLeft });
                // Allow the reliable announcement to flush before closing the transport.
                _stopHostAt = Time.unscaledTime + KICK_DISCONNECT_DELAY;
            }
            else if (Role == OnlineRole.Client)
            {
                if (NetworkClient.isConnected)
                {
                    NetworkClient.Send(new LobbyLeaveMessage());
                    _stopClientAt = Time.unscaledTime + KICK_DISCONNECT_DELAY;
                }
                else _manager.StopClient();
            }
            _steam.Leave();
            _steam.ClearPresence();
            _leaving = false;
            ResetMatch(OnlineRole.None);
            NotifyChanged();
        }

        private void ClearRecovery()
        {
            _recovery = null;
            RecoveryFromStartup = false;
            RecoveryStatus = string.Empty;
            try { _recoveryStore.Clear(); }
            catch (IOException) { LastError = OnlineError.RecoveryStorageFailed; }
            catch (UnauthorizedAccessException) { LastError = OnlineError.RecoveryStorageFailed; }
        }

        public void MarkMatchEnded()
        {
            _matchEnded = true;
            _reservations.Clear();
            ClearRecovery();
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
            if (_lobby.TryGetMember(connectionId, out LobbyMember member))
            {
                _revokedCredentials[_lobby.GetReconnectCredential(connectionId)] = OnlineError.Kicked;
                _reservations.Remove(member.SlotIndex);
                if (IsInBattle) PlayerAbandoned?.Invoke(new PlayerId(member.SlotIndex));
            }
            _lobby.Remove(connectionId);
            _loaded.Remove(connectionId);
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
            NotifyChanged();
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
            if (_steam.IsInitialized) _steam.SetHostedLobbyInBattle(true);
            _battleJson = JsonUtility.ToJson(BattleSetupSave.From(battle));
            foreach (LobbyMember member in _lobby.Members)
            {
                if (member.ConnectionId == OnlineLobby.HOST_CONNECTION_ID) LocalSlotIndex = member.SlotIndex;
                else
                    NetworkServer.connections[member.ConnectionId].Send(
                        BattleStartFor(member.ConnectionId, member.SlotIndex));
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
                connection.Send(BattleStartFor(member.ConnectionId, member.SlotIndex));
            }

            _matchEnded = false;
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
            if (Role == OnlineRole.Client)
            {
                _localLoaded = true;
                _shownRecoverySeconds = -1;
                RecoveryStatus = "Synchronizing";
                NetworkClient.Send(new BattleLoadedMessage());
                NotifyChanged();
            }
        }

        /// <summary>Host: clients have loaded (or Force Start); client: the host's begin message arrived.</summary>
        public void MarkBegun()
        {
            _steam.SetStatus(Mode == LobbyMode.Survival ? "Survival co-op battle" : "Skirmish battle");
            IsReloading = false;
            HasBegun = true;
            if (Role == OnlineRole.Client)
            {
                NetworkClient.Send(new BattleSynchronizedMessage());
                _recovering = false;
                RecoveryFromStartup = false;
                RecoveryStatus = "Playing";
            }
            BattleBegun?.Invoke();
            NotifyChanged();
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
            NetworkServer.RegisterHandler<BattleSynchronizedMessage>(OnClientSynchronized);
            NetworkServer.RegisterHandler<LobbyLeaveMessage>(OnClientLeave);
        }

        private void OnClientLeave(NetworkConnectionToClient connection, LobbyLeaveMessage message)
        {
            if (Role != OnlineRole.Host || !_lobby.TryGetMember(connection.connectionId, out LobbyMember member)) return;
            _revokedCredentials[_lobby.GetReconnectCredential(connection.connectionId)] = OnlineError.ReconnectRejected;
            _lobby.Remove(connection.connectionId);
            _loaded.Remove(connection.connectionId);
            _reservations.Remove(member.SlotIndex);
            if (IsInBattle) PlayerAbandoned?.Invoke(new PlayerId(member.SlotIndex));
            connection.Disconnect();
            BroadcastLobby();
        }

        private void OnClientSynchronized(NetworkConnectionToClient connection, BattleSynchronizedMessage message)
        {
            if (Role != OnlineRole.Host) return;
            ExpireReservations();
            if (HasBegun && _loaded.Contains(connection.connectionId) &&
                TryGetPlayer(connection.connectionId, out PlayerId player))
                _reservations.Complete(player.Index);
        }

        private void RegisterClientHandlers()
        {
            int attempt = _attempt;
            NetworkClient.RegisterHandler<LobbyStateMessage>(message =>
            {
                if (attempt == _attempt && Role == OnlineRole.Client) OnLobbyState(message);
            });
            NetworkClient.RegisterHandler<LobbyRejectedMessage>(message =>
            {
                if (attempt != _attempt || Role != OnlineRole.Client) return;
                ClearRecovery();
                Fail(message.Reason);
                DisconnectClient();
            });
            NetworkClient.RegisterHandler<BattleStartMessage>(message =>
            {
                if (attempt == _attempt && Role == OnlineRole.Client) OnBattleStart(message);
            });
            NetworkClient.RegisterHandler<BattleBeginMessage>(_ =>
            {
                if (attempt == _attempt && Role == OnlineRole.Client && IsInBattle) MarkBegun();
            });
            if (Role == OnlineRole.Client) NetworkClient.OnConnectedEvent += SendHello;
        }

        private void SendHello()
        {
            NetworkClient.OnConnectedEvent -= SendHello;
            NetworkClient.Send(new LobbyHelloMessage
                { Name = _steam.PersonaName, Version = Application.version, SteamId = _steam.LocalSteamId,
                    Protocol = LOBBY_PROTOCOL_VERSION, MatchId = _recovering ? _recovery.MatchId : string.Empty,
                    Credential = _recovering ? _recovery.Credential : string.Empty });
        }

        private void OnHello(NetworkConnectionToClient connection, LobbyHelloMessage hello)
        {
            if (Role != OnlineRole.Host) return;
            if (hello.Version != Application.version || hello.Protocol != LOBBY_PROTOCOL_VERSION)
            {
                Reject(connection, OnlineError.VersionMismatch);
                return;
            }

            if (!string.IsNullOrEmpty(hello.Credential))
            {
                ExpireReservations();
                if (hello.MatchId != _matchId || _matchEnded || !IsInBattle)
                {
                    Reject(connection, OnlineError.MatchEnded);
                    return;
                }
                if (_revokedCredentials.TryGetValue(hello.Credential, out OnlineError revoked))
                {
                    Reject(connection, revoked);
                    return;
                }
                if (!_lobby.TryRebind(hello.Credential, VerifiedSteamId(connection), hello.Name,
                    connection.connectionId, out int slot, out int previousConnection))
                {
                    Reject(connection, OnlineError.ReconnectRejected);
                    return;
                }
                if (!_reservations.TryAccept(slot, Time.unscaledTime))
                    throw new InvalidOperationException("Expired reservations must be removed before rebind.");
                _loaded.Remove(previousConnection);
                if (previousConnection != connection.connectionId &&
                    NetworkServer.connections.TryGetValue(previousConnection, out NetworkConnectionToClient stale))
                    stale.Disconnect();
                Debug.Log($"[Online] Rejoin accepted for slot {slot}; remaining {_reservations.Remaining(slot, Time.unscaledTime):F0}s.");
                connection.Send(BattleStartFor(connection.connectionId, slot,
                    _reservations.Remaining(slot, Time.unscaledTime)));
                BroadcastLobby();
                return;
            }
            if (IsInBattle)
            {
                Reject(connection, OnlineError.BattleInProgress);
                return;
            }
            if (_lobby.TryGetMember(connection.connectionId, out _)) return;

            if (_lobby.IsFull)
            {
                Reject(connection, OnlineError.LobbyFull);
                return;
            }

            _lobby.Add(connection.connectionId, hello.Name, VerifiedSteamId(connection));
            BroadcastLobby();
        }

        private void OnReady(NetworkConnectionToClient connection, LobbyReadyMessage message)
        {
            if (Role != OnlineRole.Host || IsInBattle) return;
            _lobby.SetReady(connection.connectionId, message.Ready);
            BroadcastLobby();
        }

        private void OnSlotEdit(NetworkConnectionToClient connection, LobbySlotEditMessage message)
        {
            if (Role == OnlineRole.Host && !IsInBattle && _lobby.TryGetMember(connection.connectionId, out _))
                SlotEditRequested?.Invoke(connection.connectionId, message);
        }

        private void OnClientLoaded(NetworkConnectionToClient connection, BattleLoadedMessage message)
        {
            if (Role != OnlineRole.Host) return;
            ExpireReservations();
            if (!IsInBattle || !_lobby.TryGetMember(connection.connectionId, out _)) return;
            if (!_loaded.Add(connection.connectionId)) return;
            ClientLoaded?.Invoke(connection);
            NotifyChanged();
        }

        private void OnServerDisconnect(NetworkConnectionToClient connection)
        {
            if (Role != OnlineRole.Host) return;
            _loaded.Remove(connection.connectionId);
            if (IsInBattle && _lobby.TryGetMember(connection.connectionId, out LobbyMember member))
            {
                _lobby.MarkDisconnected(connection.connectionId);
                if (member.SlotIndex >= 0 && !_matchEnded) _reservations.MarkDisconnected(member.SlotIndex, Time.unscaledTime);
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
            _connectDeadline = 0f;
            if (!IsInBattle) _steam.SetStatus(LobbyStatus());
            NotifyChanged();
        }

        private void OnBattleStart(BattleStartMessage message)
        {
            _connectDeadline = 0f;
            _recoveryDeadline = _recovering ? Time.unscaledTime + message.RecoverySeconds : 0f;
            _shownRecoverySeconds = -1;
            _recovery = new ReconnectSessionRecord
            {
                Transport = _clientTransport, Host = _clientHost, Port = _kcp.Port, LobbyId = _clientLobby,
                SteamId = _clientTransport == ReconnectTransport.Steam ? _steam.LocalSteamId : 0UL,
                MatchId = message.MatchId, Credential = message.Credential, SlotIndex = message.LocalSlotIndex
            };
            try { _recoveryStore.Save(_recovery); }
            catch (IOException) { Fail(OnlineError.RecoveryStorageFailed); }
            catch (UnauthorizedAccessException) { Fail(OnlineError.RecoveryStorageFailed); }
            RecoveryStatus = "Loading battle";
            BattleDefinition battle = JsonUtility.FromJson<BattleSetupSave>(message.SetupJson).Rebuild();
            // A start that arrives during a battle is the host's restart.
            IsReloading = IsInBattle;
            LocalSlotIndex = message.LocalSlotIndex;
            BeginBattleLoad();
            NotifyChanged();
            _gameCommand.StartBattle(battle);
        }

        private void OnClientDisconnect()
        {
            if (_leaving || Role != OnlineRole.Client) return;
            bool wasInBattle = IsInBattle;
            NetworkClient.OnConnectedEvent -= SendHello;
            // No lobby state yet: the host was never reached.
            if (LastError == OnlineError.None) LastError = _inLobby || HasRecovery ? OnlineError.ConnectionLost : OnlineError.ConnectionFailed;
            _attempt++;
            _awaitingSteamHost = false;
            _recovering = false;
            if (HasRecovery) RecoveryStatus = "Reconnect to check the host's session, or Leave to dismiss it.";
            _steam.Leave();
            _steam.ClearPresence();
            ResetMatch(OnlineRole.None);
            NotifyChanged();
            if (wasInBattle) HostLeft?.Invoke();
        }

        private void DisconnectClient()
        {
            // Mirror's manager completes transport cleanup before this service resets its state.
            _manager.StopClient();
            if (Role == OnlineRole.Client) OnClientDisconnect();
        }

        private void JoinSteamHost(string hostSteamId)
        {
            if (!_awaitingSteamHost) return;
            _awaitingSteamHost = false;
            // Steam Join Game can reuse the saved credential, without matching a display name.
            _recovering = HasRecovery && _recovery.Transport == ReconnectTransport.Steam &&
                _recovery.Host == hostSteamId && _recovery.SteamId == _steam.LocalSteamId;
            if (!_recovering) ClearRecovery();
            if (_recovering) RecoveryStatus = "Connecting - checking session";
            StartClient(ReconnectTransport.Steam, hostSteamId, _steam.LobbyId);
        }

        private void BeginSteamJoin()
        {
            // An invite during a live battle must not transfer ownership under that battle's scope.
            if (IsInBattle)
            {
                Leave();
                HostLeft?.Invoke();
            }
            // Keep the record until the destination is known; a matching invite is a recovery attempt.
            ReconnectSessionRecord recovery = _recovery;
            _recovery = null;
            _leaving = true;
            if (Role == OnlineRole.Client) _manager.StopClient();
            else if (Role == OnlineRole.Host) _manager.StopHost();
            _leaving = false;
            _steam.Leave();
            _attempt++;
            _recovery = recovery;
            EnterOnline();
            ResetMatch(OnlineRole.Client);
            _awaitingSteamHost = true;
            _connectDeadline = Time.unscaledTime + CONNECT_TIMEOUT_SECONDS;
            NotifyChanged();
        }

        private void OnSteamFailed(OnlineError error)
        {
            Fail(error);
            if (Role == OnlineRole.Client) DisconnectClient();
        }

        // Fizzy returns the authenticated remote CSteamID; KCP returns a dotted/IPv6 address.
        // Never trust the SteamId supplied in LobbyHelloMessage.
        private static ulong VerifiedSteamId(NetworkConnectionToClient connection) =>
            ulong.TryParse(connection.address, out ulong steamId) ? steamId : 0UL;

        private BattleStartMessage BattleStartFor(int connectionId, int slot, float recoverySeconds = 0f) =>
            new BattleStartMessage
            {
                SetupJson = _battleJson, LocalSlotIndex = slot, MatchId = _matchId,
                Credential = _lobby.GetReconnectCredential(connectionId), RecoverySeconds = recoverySeconds
            };

        private void Reject(NetworkConnectionToClient connection, OnlineError reason)
        {
            Debug.Log($"[Online] Connection {connection.connectionId} rejected: {reason}.");
            connection.Send(new LobbyRejectedMessage { Reason = reason });
            _pendingKicks.Add((connection, Time.unscaledTime + KICK_DISCONNECT_DELAY));
        }

        private static int CapacityOf(LobbyMode mode) =>
            mode == LobbyMode.Survival ? OnlineLineup.MAX_SURVIVAL_HUMANS : OnlineLobby.MAX_MEMBERS;

        private string LobbyStatus() => $"In {Mode} lobby {_lobby.Members.Count}/{_lobby.Capacity}";

        private void BroadcastLobby()
        {
            if (Role == OnlineRole.Host && !IsInBattle) _steam.SetStatus(LobbyStatus());
            LobbyMember[] members = new LobbyMember[_lobby.Members.Count];
            for (int i = 0; i < members.Length; i++) members[i] = _lobby.Members[i];
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection == NetworkServer.localConnection || !_lobby.TryGetMember(connection.connectionId, out _)) continue;
                connection.Send(new LobbyStateMessage
                {
                    Members = members, YourConnectionId = connection.connectionId, Mode = Mode, SetupJson = SetupJson
                });
            }

            NotifyChanged();
        }

        private void BeginBattleLoad()
        {
            BattleGeneration++;
            IsInBattle = true;
            _localLoaded = false;
            HasBegun = false;
            _loaded.Clear();
        }

        private void ResetMatch(OnlineRole role)
        {
            BattleGeneration++;
            Role = role;
            IsInBattle = false;
            HasBegun = false;
            IsReloading = false;
            _inLobby = false;
            Mode = LobbyMode.None;
            SetupJson = string.Empty;
            LocalSlotIndex = -1;
            _loaded.Clear();
            _pendingKicks.Clear();
            _reservations.Clear();
            _revokedCredentials.Clear();
            _matchEnded = false;
            _connectDeadline = 0f;
            if (role != OnlineRole.None) LastError = OnlineError.None;
        }

        private void OnSteamLobbyReady(string hostSteamId) => NotifyChanged();

        private void NotifyChanged()
        {
            _recoveryProgress.Set(IsReconnecting, RecoveryStatus);
            Changed?.Invoke();
        }

        private void Fail(OnlineError error)
        {
            LastError = error;
            NotifyChanged();
        }
    }
}
