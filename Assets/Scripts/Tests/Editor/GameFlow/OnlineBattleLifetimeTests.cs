using System;
using System.IO;
using System.Reflection;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Utils;
using Mirror;
using NUnit.Framework;

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

        [Test]
        public void VoluntaryBattleExit_LeavesAndClearsRecovery()
        {
            Set("<Role>k__BackingField", OnlineRole.Host);
            _lifetime.LateDispose();
            Assert.That(_online.Role, Is.EqualTo(OnlineRole.None));
            Assert.That(_online.HasRecovery, Is.False);
            Assert.That(_store.TryLoad(out _, out _), Is.False);
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
