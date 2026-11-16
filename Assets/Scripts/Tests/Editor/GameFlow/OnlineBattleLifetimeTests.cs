using System;
using System.IO;
using System.Reflection;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Networking.Online.Messages;
using EmpireAtWar.Utils;
using kcp2k;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
    public sealed class OnlineBattleLifetimeTests
    {
        private string _directory;
        private OnlineMatchService _online;
        private ReconnectSessionStore _store;
        private ReplayNotifier<BattleState> _state;
        private OnlineBattleLifetime _lifetime;
        private GameObject _network;

        [SetUp]
        public void SetUp()
        {
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False, "Requires no running network session.");
            _directory = Path.Combine(Path.GetTempPath(), "OnlineBattleLifetimeTests-" + Guid.NewGuid().ToString("N"));
            _store = new ReconnectSessionStore(_directory);
            _online = new OnlineMatchService(null, new OnlineRecoveryProgress());
            Set("_recoveryStore", _store);
            RememberSession();
            _state = new ReplayNotifier<BattleState>(BattleState.Running);
            _lifetime = new OnlineBattleLifetime(_online, _state);
            _lifetime.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_network != null) UnityEngine.Object.DestroyImmediate(_network);
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void DisconnectThenBattleDisposal_PreservesCrashRecovery()
        {
            Set("<Role>k__BackingField", OnlineRole.Client);
            Set("<IsInBattle>k__BackingField", true);
            typeof(OnlineMatchService).GetMethod("OnClientDisconnect", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_online, Array.Empty<object>());

            _lifetime.LateDispose();

            Assert.That(_online.Role, Is.EqualTo(OnlineRole.None));
            Assert.That(_online.HasRecovery, Is.True);
            Assert.That(_store.TryLoad(out _, out _), Is.True);
        }

        [Test]
        public void BattleEnded_ClearsRecoveryImmediately()
        {
            _state.Set(BattleState.Ended);
            Assert.That(_online.HasRecovery, Is.False);
            Assert.That(_store.TryLoad(out _, out _), Is.False);
            _lifetime.LateDispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VoluntaryClientExit_KeepsRecoveryWithoutLeaveMessage(bool battleBegun)
        {
            _network = new GameObject("Lifetime test manager");
            _network.SetActive(false);
            Set("_manager", _network.AddComponent<OnlineNetworkManager>());
            Set("<Role>k__BackingField", OnlineRole.Client);
            Set("<IsInBattle>k__BackingField", true);
            Set("<HasBegun>k__BackingField", battleBegun);
            Set("<RecoveryFromStartup>k__BackingField", !battleBegun);
            int exits = 0;
            _online.HostLeft += () => exits++;
            var stateField = typeof(NetworkClient).GetField("connectState", BindingFlags.Static | BindingFlags.NonPublic);
            PropertyInfo connectionProperty = typeof(NetworkClient).GetProperty(nameof(NetworkClient.connection));
            object previousState = stateField.GetValue(null);
            NetworkConnectionToServer previousConnection = NetworkClient.connection;
            int leaveMessages = 0;
            void Capture(NetworkDiagnostics.MessageInfo info)
            {
                if (info.message is LobbyLeaveMessage) leaveMessages++;
            }

            // A live connection: an explicit Leave would send LobbyLeaveMessage through it.
            stateField.SetValue(null, Enum.Parse(stateField.FieldType, "Connected"));
            connectionProperty.SetValue(null, new NetworkConnectionToServer());
            NetworkDiagnostics.OutMessageEvent += Capture;
            try { _lifetime.LateDispose(); }
            finally
            {
                NetworkDiagnostics.OutMessageEvent -= Capture;
                connectionProperty.SetValue(null, previousConnection);
                stateField.SetValue(null, previousState);
            }

            Assert.That(leaveMessages, Is.Zero, "Exit must not revoke the seat on the host.");
            Assert.That(_online.Role, Is.EqualTo(OnlineRole.None));
            Assert.That(_online.HasRecovery, Is.True);
            Assert.That(_store.TryLoad(out ReconnectSessionRecord record, out _), Is.True);
            Assert.That(record.Credential, Is.EqualTo("private-credential"));
            Assert.That(_online.RecoveryFromStartup, Is.False);
            Assert.That(_online.RecoveryStatus, Does.Contain("5-minute"));
            Assert.That(_online.LastError, Is.EqualTo(OnlineError.None));
            Assert.That(exits, Is.Zero);
        }

        [Test]
        public void VoluntaryHostExit_LeavesAndAnnouncesHostLeft()
        {
            var previousTransport = Transport.active;
            _network = new GameObject("Lifetime test transport");
            _network.SetActive(false);
            var transport = _network.AddComponent<KcpTransport>();
            typeof(KcpTransport).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(transport, new KcpConfig());
            const int REMOTE_ID = 7;
            OnlineError announcement = OnlineError.None;
            void Capture(NetworkDiagnostics.MessageInfo info)
            {
                if (info.message is LobbyRejectedMessage message) announcement = message.Reason;
            }

            Set("<Role>k__BackingField", OnlineRole.Host);
            Set("<IsInBattle>k__BackingField", true);
            Transport.active = transport;
            NetworkServer.connections.Add(REMOTE_ID, new NetworkConnectionToClient(REMOTE_ID));
            NetworkDiagnostics.OutMessageEvent += Capture;
            try { _lifetime.LateDispose(); }
            finally
            {
                NetworkDiagnostics.OutMessageEvent -= Capture;
                NetworkServer.connections.Remove(REMOTE_ID);
                Transport.active = previousTransport;
            }

            Assert.That(_online.Role, Is.EqualTo(OnlineRole.None));
            Assert.That(_online.HasRecovery, Is.False);
            Assert.That(_store.TryLoad(out _, out _), Is.False);
            Assert.That(announcement, Is.EqualTo(OnlineError.HostLeft));
        }

        [Test]
        public void RestartBattleDisposal_KeepsSessionAndUnsubscribesOldBattle()
        {
            Set("<Role>k__BackingField", OnlineRole.Host);
            Set("<IsReloading>k__BackingField", true);
            _lifetime.LateDispose();
            _state.Set(BattleState.Ended);
            Assert.That(_online.Role, Is.EqualTo(OnlineRole.Host));
            Assert.That(_online.HasRecovery, Is.True);
            Assert.That(_store.TryLoad(out _, out _), Is.True);
        }

        [Test]
        public void OldBattleEndingAndDisposal_CannotEndOrLeaveReplacementSession()
        {
            typeof(OnlineMatchService).GetMethod("ResetMatch", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_online, new object[] { OnlineRole.Host });
            RememberSession();

            _state.Set(BattleState.Ended);

            Assert.That(_online.HasRecovery, Is.True, "The old battle cannot end the replacement match.");
            Assert.That(_store.TryLoad(out _, out _), Is.True);
            _lifetime.LateDispose();
            Assert.That(_online.Role, Is.EqualTo(OnlineRole.Host), "The old scope cannot leave the replacement session.");
            Assert.That(_online.HasRecovery, Is.True);
        }

        private void RememberSession()
        {
            var record = new ReconnectSessionRecord { Transport = ReconnectTransport.Kcp, Host = "127.0.0.1",
                Port = 7777, MatchId = "match", Credential = "private-credential", SlotIndex = 1 };
            _store.Save(record);
            Set("_recovery", record);
        }

        private void Set<T>(string field, T value) => typeof(OnlineMatchService)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_online, value);
    }
}
