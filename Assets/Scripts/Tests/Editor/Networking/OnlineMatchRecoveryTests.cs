using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Networking.Online.Messages;
using kcp2k;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class OnlineMatchRecoveryTests
    {
        private string _directory;
        private ReconnectSessionStore _store;
        private OnlineMatchService _online;
        private Transport _previousTransport;
        private GameObject _transportObject;
        private bool _prepared;
        private bool _addedConnection;

        [SetUp]
        public void SetUp()
        {
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False, "Requires no running network session.");
            _directory = Path.Combine(Path.GetTempPath(), "OnlineMatchRecoveryTests-" + Guid.NewGuid().ToString("N"));
            _store = new ReconnectSessionStore(_directory);
            _online = new OnlineMatchService(null, new OnlineRecoveryProgress());
            Set("_recoveryStore", _store);
            _previousTransport = Transport.active;
            _prepared = true;
            _transportObject = new GameObject("Recovery test transport");
            _transportObject.SetActive(false);
            var transport = _transportObject.AddComponent<KcpTransport>();
            typeof(KcpTransport).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(transport, new KcpConfig());
            Transport.active = transport;
        }

        [TearDown]
        public void TearDown()
        {
            if (_addedConnection) NetworkServer.connections.Remove(7);
            if (_prepared) Transport.active = _previousTransport;
            if (_transportObject != null) UnityEngine.Object.DestroyImmediate(_transportObject);
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
            _prepared = false;
            _addedConnection = false;
        }

        [Test]
        public void AccidentalDisconnect_PreservesDiskRecordAndReturnsToMenu()
        {
            RememberSession();
            SetProperty("Role", OnlineRole.Client);
            SetProperty("IsInBattle", true);
            int exits = 0;
            _online.HostLeft += () => exits++;

            Invoke("OnClientDisconnect");

            Assert.That(_online.Role, Is.EqualTo(OnlineRole.None));
            Assert.That(_online.LastError, Is.EqualTo(OnlineError.ConnectionLost));
            Assert.That(_online.HasRecovery, Is.True);
            Assert.That(_store.TryLoad(out ReconnectSessionRecord restored, out _), Is.True);
            Assert.That(restored.Credential, Is.EqualTo("private-credential"));
            Assert.That(exits, Is.EqualTo(1));
            Invoke("OnClientDisconnect");
            Assert.That(exits, Is.EqualTo(1), "A duplicate disconnect cannot exit another scene.");
        }

        [Test]
        public void LeaveFromRecoveryMenu_ClearsDiskAndMemory()
        {
            RememberSession();
            Get<List<(NetworkConnectionToClient Connection, float At)>>("_pendingKicks")
                .Add((new NetworkConnectionToClient(7), float.MaxValue));
            _online.Leave();
            Assert.That(_online.HasRecovery, Is.False);
            Assert.That(_store.TryLoad(out _, out _), Is.False);
            Assert.That(Get<List<(NetworkConnectionToClient Connection, float At)>>("_pendingKicks"), Is.Empty,
                "Delayed disconnects from the previous host must not target a replacement session.");
        }

        [Test]
        public void ApplicationQuitFromRecoveryMenu_KeepsDiskRecordForNextLaunch()
        {
            RememberSession();

            _online.LateDispose();

            Assert.That(_store.TryLoad(out ReconnectSessionRecord restored, out _), Is.True,
                "Closing the game is not an explicit Leave; the next launch offers Rejoin previous match.");
            Assert.That(restored.Credential, Is.EqualTo("private-credential"));
        }

        [TestCase("127.0.0.1", 7777, true)]
        [TestCase("127.0.0.2", 7777, false)]
        [TestCase("127.0.0.1", 7778, false)]
        public void JoinByIp_UsesCredentialOnlyForSavedEndpoint(string address, int port, bool recovers)
        {
            RememberSession();
            var transport = _transportObject.GetComponent<KcpTransport>();
            typeof(KcpTransport).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(transport, null);
            transport.Port = (ushort)port;
            var manager = _transportObject.AddComponent<OnlineNetworkManager>();
            manager.autoCreatePlayer = false;
            manager.dontDestroyOnLoad = false;
            manager.runInBackground = false;
            Set("_root", _transportObject);
            Set("_manager", manager);
            Set("_kcp", transport);
            LobbyHelloMessage? hello = null;
            void Capture(NetworkDiagnostics.MessageInfo info)
            {
                if (info.message is LobbyHelloMessage message) hello = message;
            }

            NetworkDiagnostics.OutMessageEvent += Capture;
            try
            {
                _online.JoinByIp(address);
                var state = typeof(NetworkClient).GetField("connectState", BindingFlags.Static | BindingFlags.NonPublic);
                state.SetValue(null, Enum.Parse(state.FieldType, "Connected"));
                Invoke("SendHello");

                Assert.That(_online.IsReconnecting, Is.EqualTo(recovers));
                Assert.That(_store.TryLoad(out _, out _), Is.EqualTo(recovers));
                Assert.That(hello.HasValue, Is.True);
                Assert.That(hello.Value.MatchId, Is.EqualTo(recovers ? "match" : string.Empty));
                Assert.That(hello.Value.Credential, Is.EqualTo(recovers ? "private-credential" : string.Empty));
            }
            finally
            {
                NetworkDiagnostics.OutMessageEvent -= Capture;
                manager.StopClient();
                NetworkManager.ResetStatics();
            }
        }

        [TestCase(10UL, false)]
        [TestCase(20UL, true)]
        public void SteamJoinDuringBattle_IgnoresCurrentLobbyAndReservesSeatOnSwitch(ulong lobbyId, bool switches)
        {
            RememberSession();
            SetProperty("Role", OnlineRole.Client);
            SetProperty("IsInBattle", true);
            Set("_clientLobby", 10UL);
            Set("_root", _transportObject);
            Set("_manager", _transportObject.AddComponent<OnlineNetworkManager>());
            int exits = 0;
            _online.HostLeft += () => exits++;

            Assert.That(Invoke("BeginSteamJoin", lobbyId), Is.EqualTo(switches));

            Assert.That(_online.IsInBattle, Is.EqualTo(!switches));
            Assert.That(Get<bool>("_awaitingSteamHost"), Is.EqualTo(switches));
            Assert.That(exits, Is.EqualTo(switches ? 1 : 0));
            Assert.That(_online.HasRecovery, Is.True);
            Assert.That(_store.TryLoad(out _, out _), Is.True);
        }

        [Test]
        public void LeaveLobby_FromAnotherHost_KeepsSavedBattle()
        {
            RememberSession();
            SetProperty("Role", OnlineRole.Client);
            Set("_manager", _transportObject.AddComponent<OnlineNetworkManager>());

            _online.LeaveLobby();

            Assert.That(_online.Role, Is.EqualTo(OnlineRole.None));
            Assert.That(_online.HasRecovery, Is.True);
            Assert.That(_store.TryLoad(out ReconnectSessionRecord restored, out _), Is.True);
            Assert.That(restored.Credential, Is.EqualTo("private-credential"));
            Assert.That(_online.RecoveryStatus, Does.Contain("Reconnect"));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void LeaveLobby_FromSavedBattleSession_DismissesRecovery(bool recovering, bool inBattle)
        {
            RememberSession();
            SetProperty("Role", OnlineRole.Client);
            SetProperty("IsInBattle", inBattle);
            Set("_recovering", recovering);
            Set("_manager", _transportObject.AddComponent<OnlineNetworkManager>());

            _online.LeaveLobby();

            Assert.That(_online.Role, Is.EqualTo(OnlineRole.None));
            Assert.That(_online.HasRecovery, Is.False);
            Assert.That(_store.TryLoad(out _, out _), Is.False);
        }

        [Test]
        public void SteamJoinToSavedLobby_WithAnotherAccount_RejectsAndClearsRecovery()
        {
            const string HOST = "76561198000000001";
            var record = new ReconnectSessionRecord { Transport = ReconnectTransport.Steam, Host = HOST,
                SteamId = 99UL, MatchId = "match", Credential = "private-credential", SlotIndex = 1 };
            _store.Save(record);
            Set("_recovery", record);
            SetProperty("Role", OnlineRole.Client);
            Set("_awaitingSteamHost", true);
            Set("_root", _transportObject);
            Set("_manager", _transportObject.AddComponent<OnlineNetworkManager>());

            Invoke("JoinSteamHost", HOST);

            Assert.That(_online.LastError, Is.EqualTo(OnlineError.ReconnectRejected));
            Assert.That(_online.Role, Is.EqualTo(OnlineRole.None));
            Assert.That(_online.HasRecovery, Is.False);
            Assert.That(_store.TryLoad(out _, out _), Is.False);
        }

        [Test]
        public void HelloAfterHostLeft_DoesNotCreateMember()
        {
            Invoke("OnHello", new NetworkConnectionToClient(7, "127.0.0.1"), new LobbyHelloMessage
                { Version = Application.version, Protocol = OnlineMatchService.LOBBY_PROTOCOL_VERSION, Name = "Late client" });
            Assert.That(_online.Members, Is.Empty);
            Assert.That(Get<List<(NetworkConnectionToClient Connection, float At)>>("_pendingKicks"), Is.Empty);
        }

        [Test]
        public void MatchCompletion_ClearsRecoveryAndReservation()
        {
            RememberSession();
            Get<ReconnectReservations>("_reservations").MarkDisconnected(1, 0f);
            _online.MarkMatchEnded();
            Assert.That(_online.HasRecovery, Is.False);
            Assert.That(_store.TryLoad(out _, out _), Is.False);
            Assert.That(Get<ReconnectReservations>("_reservations").Expired(float.MaxValue), Is.Empty);
        }

        [Test]
        public void AcceptedLobbyState_CancelsConnectionTimeout()
        {
            SetProperty("Role", OnlineRole.Client);
            Set("_connectDeadline", 1f);
            Invoke("OnLobbyState", new LobbyStateMessage { Members = Array.Empty<LobbyMember>() });
            Assert.That(Get<float>("_connectDeadline"), Is.Zero,
                "The connect timeout must not disconnect a player waiting in an accepted lobby.");
        }

        [TestCase("wrong-match", "private-credential")]
        [TestCase("match", "wrong-credential")]
        public void InvalidRejoinHello_DoesNotChangeOriginalOwnership(string match, string credential)
        {
            OnlineLobby lobby = HostBattle();
            string original = lobby.GetReconnectCredential(1);
            if (credential == "private-credential") credential = original;
            Invoke("OnHello", new NetworkConnectionToClient(7, "127.0.0.1"), new LobbyHelloMessage
            {
                Version = Application.version, Protocol = OnlineMatchService.LOBBY_PROTOCOL_VERSION, MatchId = match,
                Credential = credential, Name = "Client"
            });
            Assert.That(lobby.TryGetMember(1, out LobbyMember member), Is.True);
            Assert.That(member.Connected, Is.True);
            Assert.That(lobby.GetReconnectCredential(1), Is.EqualTo(original));
            Assert.That(lobby.TryGetMember(7, out _), Is.False);
        }

        [Test]
        public void ExpiredLoadingReservation_RevokesOwnershipAndSchedulesDisconnect()
        {
            OnlineLobby lobby = HostBattle(7);
            string credential = lobby.GetReconnectCredential(7);
            NetworkServer.connections.Add(7, new NetworkConnectionToClient(7, "127.0.0.1"));
            _addedConnection = true;
            Get<ReconnectReservations>("_reservations").MarkDisconnected(1, Time.unscaledTime - 301f);
            int abandoned = 0;
            _online.PlayerAbandoned += _ => abandoned++;

            Invoke("ExpireReservations");

            Assert.That(lobby.TryGetMember(7, out _), Is.False);
            Assert.That(Get<Dictionary<string, OnlineError>>("_revokedCredentials")[credential],
                Is.EqualTo(OnlineError.ReconnectExpired));
            Assert.That(Get<List<(NetworkConnectionToClient Connection, float At)>>("_pendingKicks").Count,
                Is.EqualTo(1));
            Assert.That(abandoned, Is.EqualTo(1));
        }

        [Test]
        public void ExplicitClientLeave_RevokesCredentialAndCommitsTakeoverImmediately()
        {
            OnlineLobby lobby = HostBattle(7);
            string credential = lobby.GetReconnectCredential(7);
            var connection = new LeavingConnection(7);
            var reservations = Get<ReconnectReservations>("_reservations");
            reservations.MarkDisconnected(1, Time.unscaledTime);
            Get<HashSet<int>>("_loaded").Add(7);
            int abandoned = 0;
            _online.PlayerAbandoned += _ => abandoned++;

            Invoke("OnClientLeave", connection, new LobbyLeaveMessage());

            Assert.That(connection.Disconnected, Is.True);
            Assert.That(lobby.TryGetMember(7, out _), Is.False);
            Assert.That(Get<HashSet<int>>("_loaded"), Has.No.Member(7));
            Assert.That(Get<Dictionary<string, OnlineError>>("_revokedCredentials")[credential],
                Is.EqualTo(OnlineError.ReconnectRejected));
            Assert.That(reservations.Expired(float.MaxValue), Is.Empty);
            Assert.That(abandoned, Is.EqualTo(1));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void SynchronizationAck_RequiresLoadedMemberAndBegunBattle(bool loaded, bool begun)
        {
            HostBattle();
            SetProperty("HasBegun", begun);
            var reservations = Get<ReconnectReservations>("_reservations");
            reservations.MarkDisconnected(1, Time.unscaledTime);
            if (loaded) Get<HashSet<int>>("_loaded").Add(1);

            Invoke("OnClientSynchronized", new NetworkConnectionToClient(1, "127.0.0.1"),
                new BattleSynchronizedMessage());

            Assert.That(reservations.Expired(float.MaxValue).Count, Is.EqualTo(loaded && begun ? 0 : 1));
        }

        private OnlineLobby HostBattle(int connection = 1)
        {
            SetProperty("Role", OnlineRole.Host);
            SetProperty("IsInBattle", true);
            Set("_matchId", "match");
            OnlineLobby lobby = Get<OnlineLobby>("_lobby");
            lobby.Reset("Host", 0UL, 8);
            lobby.Add(connection, "Client", 0UL);
            lobby.AssignSlots(new Dictionary<int, int> { [0] = 0, [connection] = 1 });
            return lobby;
        }

        private void RememberSession()
        {
            var record = new ReconnectSessionRecord { Transport = ReconnectTransport.Kcp, Host = "127.0.0.1",
                Port = 7777, MatchId = "match", Credential = "private-credential", SlotIndex = 1 };
            _store.Save(record);
            Set("_recovery", record);
        }

        private object Invoke(string method, params object[] arguments) => typeof(OnlineMatchService)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_online, arguments);
        private T Get<T>(string field) => (T)typeof(OnlineMatchService)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_online);
        private void Set<T>(string field, T value) => typeof(OnlineMatchService)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_online, value);
        private void SetProperty<T>(string property, T value) => Set("<" + property + ">k__BackingField", value);

        private sealed class LeavingConnection : NetworkConnectionToClient
        {
            public bool Disconnected { get; private set; }
            public LeavingConnection(int connectionId) : base(connectionId) { }
            public override void Disconnect() => Disconnected = true;
        }
    }
}
