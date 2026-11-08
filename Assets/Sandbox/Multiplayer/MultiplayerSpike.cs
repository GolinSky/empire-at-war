using kcp2k;
using Mirror;
using Mirror.FizzySteam;
using UnityEngine;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Phase 0 spike driver: KCP (direct IP) or FizzySteamworks (Steam lobby) host/client,
    // 20 Hz tick replication, command round-trip and bandwidth / dead-reckoning measurement. OnGUI only.
    public sealed class MultiplayerSpike : MonoBehaviour
    {
        private const int PANEL_WIDTH = 380;
        private const float KIBIBYTE = 1024f;

        [SerializeField] private SpikeNetworkManager networkManager;
        [SerializeField] private KcpTransport kcpTransport;
        [SerializeField] private FizzySteamworks steamTransport;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SpikeUnitRenderer unitRenderer;

        [Header("Host Settings")]
        [SerializeField] private int unitsPerSide = 100;
        [SerializeField] private SpikeSyncMode syncMode = SpikeSyncMode.DeadReckoning;
        [SerializeField] private float deadReckoningThreshold = 2f;
        [SerializeField] private SnapshotInterpolationSettings interpolationSettings = new SnapshotInterpolationSettings { bufferTimeMultiplier = 3 };

        private readonly SpikeSteamLobby _steam = new SpikeSteamLobby();
        private SpikeHostSimulation _host;
        private SpikeClientReplica _replica;
        private SpikeBandwidthMeter _meter;
        private string _address = "localhost";
        private string _lobbyIdInput = string.Empty;
        private Vector2 _friendScroll;
        private string _message = string.Empty;
        private ushort _commandSequence;
        private double _commandSentAt;
        private uint _awaitedTick;
        private double _awaitedTickTime;
        private double _ackLatency;
        private double _visibleLatency;
        private bool _awaitingVisible;

        private void Awake()
        {
            networkManager.ServerStarted += OnServerStarted;
            networkManager.ServerStopped += OnServerStopped;
            networkManager.ServerClientConnected += OnServerClientConnected;
            networkManager.ServerClientDisconnected += OnServerClientDisconnected;
            networkManager.ClientStarted += OnClientStarted;
            networkManager.ClientStopped += OnClientStopped;
            _steam.HostLobbyReady += hostId => _message = $"Lobby ready. Host SteamID {hostId}. Use Invite.";
            _steam.JoinedHost += OnJoinedSteamHost;
            _steam.Failed += reason => _message = reason;
            _meter = new SpikeBandwidthMeter(Time.unscaledTimeAsDouble);
        }

        private void Start()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "+connect_lobby") >= 0 && InitializeSteam())
                _steam.TryJoinFromLaunchArgs();
        }

        private void Update()
        {
            _steam.Tick();
            _meter.Update(Time.unscaledTimeAsDouble);
            if (_host != null)
                _host.Update(Time.deltaTime);

            if (_replica == null)
                return;

            _replica.Update(Time.unscaledDeltaTime);
            unitRenderer.Render(_replica);
            if (_awaitingVisible && _awaitedTickTime > 0 && _replica.RenderTime >= _awaitedTickTime)
            {
                _visibleLatency = Time.unscaledTimeAsDouble - _commandSentAt;
                _awaitingVisible = false;
            }
        }

        private void OnDestroy()
        {
            _meter.Dispose();
            _steam.Shutdown();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, PANEL_WIDTH, Screen.height - 20), GUI.skin.box);
            if (!NetworkServer.active && !NetworkClient.active)
                DrawOfflinePanel();
            else
                DrawSessionPanel();

            if (_message.Length > 0)
                GUILayout.Label(_message);
            GUILayout.EndArea();

            Event current = Event.current;
            bool outsidePanel = current.mousePosition.x > PANEL_WIDTH + 20;
            if (current.type == EventType.MouseDown && current.button == 1 && outsidePanel && _replica != null)
            {
                Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(current.mousePosition.x, Screen.height - current.mousePosition.y, 0f));
                SendMoveCommand(new Vector2(world.x, world.z));
                current.Use();
            }
        }

        private void DrawOfflinePanel()
        {
            GUILayout.Label("Host settings");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Units per side", GUILayout.Width(120));
            if (int.TryParse(GUILayout.TextField(unitsPerSide.ToString()), out int units))
                unitsPerSide = Mathf.Clamp(units, 1, 1000);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("DR threshold (m)", GUILayout.Width(120));
            if (float.TryParse(GUILayout.TextField(deadReckoningThreshold.ToString("0.##")), out float threshold))
                deadReckoningThreshold = Mathf.Max(0.1f, threshold);
            GUILayout.EndHorizontal();
            if (GUILayout.Button($"Sync mode: {syncMode}"))
                syncMode = syncMode == SpikeSyncMode.FullSnapshot ? SpikeSyncMode.DeadReckoning : SpikeSyncMode.FullSnapshot;

            GUILayout.Space(8);
            GUILayout.Label($"Direct IP (KCP, port {kcpTransport.port})");
            if (GUILayout.Button("Host (KCP)"))
            {
                networkManager.UseTransport(kcpTransport);
                networkManager.StartHost();
            }

            GUILayout.BeginHorizontal();
            _address = GUILayout.TextField(_address);
            if (GUILayout.Button("Join (KCP)", GUILayout.Width(110)))
            {
                networkManager.UseTransport(kcpTransport);
                networkManager.networkAddress = _address;
                networkManager.StartClient();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label($"Steam (App ID 480): {_steam.Status}");
            if (GUILayout.Button("Host Steam lobby") && InitializeSteam())
            {
                networkManager.UseTransport(steamTransport);
                networkManager.StartHost();
                _steam.CreateLobby();
            }

            if (!_steam.IsInitialized && GUILayout.Button("Init Steam (to accept invites)"))
                InitializeSteam();

            if (!_steam.IsInitialized) return;
            if (_steam.HasPendingInvite && GUILayout.Button($"Accept invite from {_steam.PendingInviteFrom}"))
                _steam.AcceptInvite();
            GUILayout.BeginHorizontal();
            _lobbyIdInput = GUILayout.TextField(_lobbyIdInput);
            if (GUILayout.Button("Join lobby ID", GUILayout.Width(110)) && !_steam.TryJoinById(_lobbyIdInput))
                _message = "Lobby ID must be a number.";
            GUILayout.EndHorizontal();
        }

        private void DrawSessionPanel()
        {
            string role = NetworkServer.active ? (NetworkClient.active ? "Host" : "Server") : "Client";
            GUILayout.Label($"{role} via {Transport.active.GetType().Name}");
            if (_steam.InLobby && NetworkServer.active)
                DrawSteamInvites();
            if (GUILayout.Button("Stop"))
                StopSession();

            GUILayout.Space(6);
            if (_host != null)
            {
                SpikeDeadReckoningStats stats = _host.Stats;
                GUILayout.Label($"Tick {_host.CurrentTick}  units {_host.UnitCount}  mode {_host.Mode}  chunk {_host.UnitsPerChunk} units");
                GUILayout.Label($"Keyframes/tick {stats.KeyframesPerTick:0.0}  clients {NetworkServer.connections.Count - 1}");
                GUILayout.Label($"DR error capital mean {stats.MeanError(SpikeUnitKind.Capital):0.00} m  max {stats.MaxError(SpikeUnitKind.Capital):0.00} m");
                GUILayout.Label($"DR error squadron mean {stats.MeanError(SpikeUnitKind.Squadron):0.00} m  max {stats.MaxError(SpikeUnitKind.Squadron):0.00} m");
                GUILayout.Label($"Per client {_meter.PerClientBytesPerSecond / KIBIBYTE:0.00} KiB/s  peak {_meter.PeakPerClientBytesPerSecond / KIBIBYTE:0.00}");
                GUILayout.Label($"Host upload {_meter.UploadBytesPerSecond / KIBIBYTE:0.00} KiB/s");
                if (GUILayout.Button("Reset stats"))
                {
                    stats.Reset();
                    _meter.ResetPeaks();
                }
            }

            if (_replica != null)
            {
                GUILayout.Space(6);
                GUILayout.Label($"Side {_replica.Side}  last tick {_replica.LastReceivedTick}  chunks {_replica.ReceivedChunks}");
                GUILayout.Label($"RTT {NetworkTime.rtt * 1000:0} ms  buffer {_replica.TimelineBufferCount}  delay {(NetworkTime.time - _replica.RenderTime) * 1000:0} ms");
                GUILayout.Label($"Down {_meter.InBytesPerSecond / KIBIBYTE:0.00} KiB/s  peak {_meter.PeakInBytesPerSecond / KIBIBYTE:0.00}");
                GUILayout.Label($"Command #{_commandSequence}: ack {_ackLatency * 1000:0} ms, visible {_visibleLatency * 1000:0} ms");
                GUILayout.Label("Right-click the map to move your side.");
            }
        }

        private void DrawSteamInvites()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Lobby ID {_steam.LobbyId}");
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
                GUIUtility.systemCopyBuffer = _steam.LobbyId.ToString();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Invite friends (Steam overlay)"))
                _steam.InviteFriends();

            GUILayout.Label("Online friends (direct invite):");
            _friendScroll = GUILayout.BeginScrollView(_friendScroll, GUILayout.Height(120));
            foreach (SpikeSteamFriend friend in _steam.RefreshOnlineFriends())
            {
                if (GUILayout.Button($"Invite {friend.Name}"))
                    _steam.InviteFriend(friend);
            }
            GUILayout.EndScrollView();
            GUILayout.Label(_steam.Status);
        }

        private bool InitializeSteam()
        {
            if (!_steam.TryInitialize())
            {
                _message = _steam.Status;
                return false;
            }

            steamTransport.enabled = true;
            return true;
        }

        private void OnJoinedSteamHost(string hostSteamId)
        {
            networkManager.UseTransport(steamTransport);
            networkManager.networkAddress = hostSteamId;
            networkManager.StartClient();
        }

        private void StopSession()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
                networkManager.StopHost();
            else if (NetworkClient.active)
                networkManager.StopClient();
            else
                networkManager.StopServer();
            _steam.Leave();
        }

        private void SendMoveCommand(Vector2 target)
        {
            _commandSequence++;
            _commandSentAt = Time.unscaledTimeAsDouble;
            _awaitingVisible = false;
            NetworkClient.Send(new SpikeCommandMessage
            {
                Sequence = _commandSequence,
                LastReceivedTick = _replica.LastReceivedTick,
                Side = _replica.Side,
                TargetX = SpikeQuantization.EncodePosition(target.x),
                TargetZ = SpikeQuantization.EncodePosition(target.y)
            });
        }

        private void OnServerStarted()
        {
            _host = new SpikeHostSimulation(unitsPerSide, syncMode, deadReckoningThreshold);
            NetworkServer.RegisterHandler<SpikeCommandMessage>(_host.EnqueueCommand);
        }

        private void OnServerStopped()
        {
            NetworkServer.UnregisterHandler<SpikeCommandMessage>();
            _host = null;
        }

        private void OnServerClientConnected(NetworkConnectionToClient connection)
        {
            byte side = _host.AssignSide(connection);
            connection.Send(_host.CreateWelcome(side));
            _host.ForceFullResend();
        }

        private void OnServerClientDisconnected(NetworkConnectionToClient connection) => _host.ReleaseSide(connection);

        private void OnClientStarted()
        {
            NetworkClient.RegisterHandler<SpikeWelcomeMessage>(welcome => _replica = new SpikeClientReplica(welcome, interpolationSettings));
            NetworkClient.RegisterHandler<SpikeTickMessage>(OnTickMessage);
            NetworkClient.RegisterHandler<SpikeCommandAckMessage>(OnCommandAck);
        }

        private void OnClientStopped()
        {
            NetworkClient.UnregisterHandler<SpikeWelcomeMessage>();
            NetworkClient.UnregisterHandler<SpikeTickMessage>();
            NetworkClient.UnregisterHandler<SpikeCommandAckMessage>();
            _replica = null;
        }

        private void OnTickMessage(SpikeTickMessage message)
        {
            if (_replica == null)
                return;

            _replica.OnTick(message);
            if (_awaitingVisible && _awaitedTickTime <= 0 && message.Tick >= _awaitedTick)
                _awaitedTickTime = message.Time;
        }

        private void OnCommandAck(SpikeCommandAckMessage ack)
        {
            if (ack.Sequence != _commandSequence)
                return;

            _ackLatency = Time.unscaledTimeAsDouble - _commandSentAt;
            _message = ack.Accepted ? string.Empty : $"Command #{ack.Sequence} rejected by host";
            _awaitedTick = ack.AppliedTick;
            _awaitedTickTime = 0;
            _awaitingVisible = ack.Accepted;
        }
    }
}
