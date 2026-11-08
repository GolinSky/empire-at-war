using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Services.Networking.Online;
using NUnit.Framework;
using Steamworks;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class SteamPlatformLifecycleTests
    {
        [Test]
        public void Leave_BeforeLobbyCompletes_ClearsHostAndPendingJoin()
        {
            var steam = new SteamPlatform();
            Set(steam, "_isHost", true);
            Set(steam, "_requestedLobby", new CSteamID(10));

            steam.Leave();

            Assert.That(Get<bool>(steam, "_isHost"), Is.False, "A former host must receive direct invites again.");
            Assert.That(steam.IsJoiningLobby, Is.False);
        }

        [Test]
        public void CancelledLobbyCreationFailure_DoesNotReplaceCurrentStatus()
        {
            var steam = new SteamPlatform();
            Set(steam, "_isHost", true);
            int failures = 0;
            steam.Failed += _ => failures++;
            steam.Leave();

            typeof(SteamPlatform).GetMethod("OnLobbyCreated", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(steam, new object[] { new LobbyCreated_t { m_eResult = EResult.k_EResultFail }, true });

            Assert.That(failures, Is.Zero);
            Assert.That(steam.InLobby, Is.False);
        }

        [Test]
        public void CancelledJoinCompletion_DoesNotPublishFailureOrConnect()
        {
            var steam = new SteamPlatform();
            CSteamID lobby = new CSteamID(10);
            AddRequest(steam, lobby);
            Set(steam, "_requestedLobby", lobby);
            int failures = 0;
            int joins = 0;
            steam.Failed += _ => failures++;
            steam.JoinedHost += _ => joins++;
            steam.Leave();

            Complete(steam, lobby, EChatRoomEnterResponse.k_EChatRoomEnterResponseError, true);

            Assert.That(failures, Is.Zero);
            Assert.That(joins, Is.Zero);
            Assert.That(steam.IsJoiningLobby, Is.False);
            Assert.That(Requests(steam), Is.Empty);
        }

        [Test]
        public void SupersededJoinFailure_DoesNotInvalidateNewTarget()
        {
            var steam = new SteamPlatform();
            CSteamID oldLobby = new CSteamID(10);
            CSteamID newLobby = new CSteamID(20);
            AddRequest(steam, oldLobby);
            Set(steam, "_requestedLobby", newLobby);
            int failures = 0;
            steam.Failed += _ => failures++;

            Complete(steam, oldLobby, EChatRoomEnterResponse.k_EChatRoomEnterResponseFull, false);

            Assert.That(failures, Is.Zero);
            Assert.That(steam.IsJoiningLobby, Is.True);
            Assert.That(Get<CSteamID>(steam, "_requestedLobby"), Is.EqualTo(newLobby));
        }

        [Test]
        public void RejoinSamePendingLobby_ReusesOriginalOperation()
        {
            var steam = new SteamPlatform();
            CSteamID lobby = new CSteamID(10);
            AddRequest(steam, lobby);
            CallResult<LobbyEnter_t> original = Requests(steam)[lobby];
            steam.JoinStarting += steam.Leave;

            steam.Join(lobby);
            steam.Leave();
            steam.Join(lobby);

            Assert.That(steam.IsJoiningLobby, Is.True);
            Assert.That(Requests(steam).Count, Is.EqualTo(1));
            Assert.That(Requests(steam)[lobby], Is.SameAs(original));
            Complete(steam, lobby, EChatRoomEnterResponse.k_EChatRoomEnterResponseError, true);
        }

        [Test]
        public void StaleSuccessForAcceptedLobby_DoesNotLeaveOrConnectAgain()
        {
            var steam = new SteamPlatform();
            CSteamID lobby = new CSteamID(10);
            AddRequest(steam, lobby);
            Set(steam, "_lobby", lobby);
            int joins = 0;
            steam.JoinedHost += _ => joins++;

            Complete(steam, lobby, EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess, false);

            Assert.That(steam.LobbyId, Is.EqualTo(10UL));
            Assert.That(joins, Is.Zero);
        }

        [TestCase(false, OnlineError.LobbyFull)]
        [TestCase(true, OnlineError.ConnectionFailed)]
        public void CurrentJoinFailure_ClearsPendingAndReportsReason(bool ioFailure, OnlineError expected)
        {
            var steam = new SteamPlatform();
            CSteamID lobby = new CSteamID(10);
            AddRequest(steam, lobby);
            Set(steam, "_requestedLobby", lobby);
            OnlineError error = OnlineError.None;
            steam.Failed += reason => error = reason;

            Complete(steam, lobby, EChatRoomEnterResponse.k_EChatRoomEnterResponseFull, ioFailure);

            Assert.That(error, Is.EqualTo(expected));
            Assert.That(steam.IsJoiningLobby, Is.False);
        }

        private static void AddRequest(SteamPlatform steam, CSteamID lobby) =>
            Requests(steam).Add(lobby, CallResult<LobbyEnter_t>.Create());

        private static Dictionary<CSteamID, CallResult<LobbyEnter_t>> Requests(SteamPlatform steam) =>
            Get<Dictionary<CSteamID, CallResult<LobbyEnter_t>>>(steam, "_lobbyJoins");

        private static void Complete(SteamPlatform steam, CSteamID lobby, EChatRoomEnterResponse response, bool ioFailure) =>
            typeof(SteamPlatform).GetMethod("OnLobbyEntered", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(steam, new object[] { lobby, new LobbyEnter_t
                    { m_ulSteamIDLobby = lobby.m_SteamID, m_EChatRoomEnterResponse = (uint)response }, ioFailure });

        private static T Get<T>(SteamPlatform steam, string field) =>
            (T)typeof(SteamPlatform).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(steam);

        private static void Set<T>(SteamPlatform steam, string field, T value) =>
            typeof(SteamPlatform).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(steam, value);
    }
}
