using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Networking.Online.Messages;
using kcp2k;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class OnlineLobbyModeTests
    {
        [Test]
        public void BroadcastLobby_AllRowsClosed_SeatsEightMembersBeforeNotifyingWithoutUi()
        {
            OnlineMatchService online = CreateHost(out OnlineLobby lobby);
            var setup = new SkirmishModel();
            setup.BeginHostLobby(OnlineLobby.HOST_CONNECTION_ID, "Host");
            setup.SelectSlotOccupant(1, SkirmishSlotOccupant.Closed);
            setup.SelectMapSize(MapSize.Large);
            online.PublishSetup(JsonUtility.ToJson(setup.CaptureLobby()));
            int notifications = 0;
            online.Changed += () =>
            {
                notifications++;
                SkirmishLobbySnapshot snapshot = JsonUtility.FromJson<SkirmishLobbySnapshot>(online.SetupJson);
                var humans = snapshot.slots.Where(row => row.occupant == (int)SkirmishSlotOccupant.Human).ToArray();
                Assert.That(humans.Select(row => row.memberId),
                    Is.EquivalentTo(online.Members.Select(member => member.ConnectionId)));
                Assert.That(humans.Select(row => row.memberId).Distinct().Count(), Is.EqualTo(humans.Length));
                Assert.That(snapshot.mapSize, Is.EqualTo((int)MapSize.Large));
            };

            for (int i = 1; i < OnlineLobby.MAX_MEMBERS; i++)
            {
                Assert.That(lobby.IsFull, Is.False);
                typeof(OnlineMatchService).GetMethod("OnHello", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(online, new object[]
                    {
                        new NetworkConnectionToClient(i, i.ToString()), new LobbyHelloMessage
                        {
                            Version = Application.version, Protocol = OnlineMatchService.LOBBY_PROTOCOL_VERSION,
                            Name = $"Friend {i}"
                        }
                    });
            }

            Assert.That(notifications, Is.EqualTo(7));
            Assert.That(lobby.IsFull, Is.True);
        }

        [Test]
        public void BroadcastLobby_JoinAfterClosingSeat_PreservesChoicesAndReopensSeat()
        {
            OnlineMatchService online = CreateHost(out OnlineLobby lobby);
            var setup = new SkirmishModel();
            setup.BeginHostLobby(OnlineLobby.HOST_CONNECTION_ID, "Host");
            setup.SelectSlotOccupant(1, SkirmishSlotOccupant.Closed);
            setup.SelectSlotTeam(1, 3);
            setup.SelectSlotColor(1, 15);
            setup.SelectMapSize(MapSize.Large);
            online.PublishSetup(JsonUtility.ToJson(setup.CaptureLobby()));

            lobby.Add(7, "Friend", 77UL);
            Broadcast(online);
            SkirmishLobbySnapshot snapshot = JsonUtility.FromJson<SkirmishLobbySnapshot>(online.SetupJson);

            Assert.That(snapshot.slots[1].occupant, Is.EqualTo((int)SkirmishSlotOccupant.Human));
            Assert.That(snapshot.slots[1].memberId, Is.EqualTo(7));
            Assert.That(snapshot.slots[1].name, Is.EqualTo("Friend"));
            Assert.That(snapshot.slots[1].steamId, Is.EqualTo(77UL));
            Assert.That(snapshot.slots[1].team, Is.EqualTo(3));
            Assert.That(snapshot.slots[1].color, Is.EqualTo(15));
            Assert.That(snapshot.mapSize, Is.EqualTo((int)MapSize.Large));
            Assert.That(snapshot.slots.Skip(2).All(row => row.occupant == (int)SkirmishSlotOccupant.Closed), Is.True);
        }

        [Test]
        public void SwitchLobbyMode_BackToSkirmish_SeatsExistingMembersWithoutUi()
        {
            OnlineMatchService online = CreateHost(out OnlineLobby lobby);
            lobby.Add(7, "Friend", 77UL);
            online.SwitchLobbyMode(LobbyMode.Survival);

            online.SwitchLobbyMode(LobbyMode.Skirmish);

            SkirmishLobbySnapshot snapshot = JsonUtility.FromJson<SkirmishLobbySnapshot>(online.SetupJson);
            Assert.That(snapshot.slots.Where(row => row.occupant == (int)SkirmishSlotOccupant.Human)
                .Select(row => row.memberId), Is.EquivalentTo(new[] { OnlineLobby.HOST_CONNECTION_ID, 7 }));
        }

        [Test]
        public void OnHello_NinthMemberIsRejectedWithoutChangingEightSeatedPlayers()
        {
            OnlineMatchService online = CreateHost(out OnlineLobby lobby);
            for (int i = 1; i < OnlineLobby.MAX_MEMBERS; i++) lobby.Add(i, $"Friend {i}", (ulong)i);
            Broadcast(online);
            string setup = online.SetupJson;
            Transport previousTransport = Transport.active;
            var transportObject = new GameObject("Lobby capacity test transport");
            transportObject.SetActive(false);
            try
            {
                var transport = transportObject.AddComponent<KcpTransport>();
                typeof(KcpTransport).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(transport, new KcpConfig());
                Transport.active = transport;
                var connection = new NetworkConnectionToClient(8, "127.0.0.1");
                LogAssert.Expect(LogType.Log, "[Online] Connection 8 rejected: LobbyFull.");

                typeof(OnlineMatchService).GetMethod("OnHello", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(online, new object[]
                    {
                        connection, new LobbyHelloMessage
                        {
                            Version = Application.version, Protocol = OnlineMatchService.LOBBY_PROTOCOL_VERSION,
                            Name = "Ninth player"
                        }
                    });

                Assert.That(online.Members.Count, Is.EqualTo(8));
                Assert.That(lobby.TryGetMember(8, out _), Is.False);
                Assert.That(online.SetupJson, Is.EqualTo(setup));
                var pending = (List<(NetworkConnectionToClient Connection, float At)>)typeof(OnlineMatchService)
                    .GetField("_pendingKicks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(online);
                Assert.That(pending.Select(item => item.Connection), Is.EqualTo(new[] { connection }));
            }
            finally
            {
                Transport.active = previousTransport;
                Object.DestroyImmediate(transportObject);
            }
        }

        private static OnlineMatchService CreateHost(out OnlineLobby lobby)
        {
            Assert.That(NetworkServer.active || NetworkClient.active, Is.False, "Requires no running network session.");
            Assert.That(NetworkServer.connections, Is.Empty);
            var online = new OnlineMatchService(null, new OnlineRecoveryProgress(), new SteamPlatform(apiEnabled: false));
            typeof(OnlineMatchService).GetProperty(nameof(OnlineMatchService.Role)).SetValue(online, OnlineRole.Host);
            typeof(OnlineMatchService).GetProperty(nameof(OnlineMatchService.Mode)).SetValue(online, LobbyMode.Skirmish);
            lobby = (OnlineLobby)typeof(OnlineMatchService)
                .GetField("_lobby", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(online);
            lobby.Reset("Host", 0UL, OnlineLobby.MAX_MEMBERS);
            return online;
        }

        private static void Broadcast(OnlineMatchService online) => typeof(OnlineMatchService)
            .GetMethod("BroadcastLobby", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(online, null);

        [Test]
        public void SkirmishLobby_EightMembersFillCapacity()
        {
            var lobby = new OnlineLobby();
            lobby.Reset("Host", 0UL, OnlineLobby.MAX_MEMBERS);
            for (int i = 1; i < 8; i++)
            {
                Assert.That(lobby.IsFull, Is.False);
                lobby.Add(i, $"Player {i}", 0UL);
            }
            Assert.That(lobby.Members.Count, Is.EqualTo(8));
            Assert.That(lobby.IsFull, Is.True);
        }

        [Test]
        public void ChangeCapacity_AppliesLimitAndClearsClientReadyOnly()
        {
            var lobby = new OnlineLobby();
            lobby.Reset("Host", 0UL, OnlineLobby.MAX_MEMBERS);
            lobby.Add(1, "Client", 0UL);
            lobby.Add(2, "Other", 0UL);
            lobby.SetReady(1, true);
            lobby.SetReady(2, true);

            lobby.ChangeCapacity(OnlineLineup.MAX_SURVIVAL_HUMANS);

            Assert.That(lobby.Capacity, Is.EqualTo(OnlineLineup.MAX_SURVIVAL_HUMANS));
            Assert.That(lobby.IsFull, Is.True);
            Assert.That(lobby.Members[0].Ready, Is.True, "The host is always ready.");
            Assert.That(lobby.Members[1].Ready, Is.False);
            Assert.That(lobby.Members[2].Ready, Is.False);
            Assert.That(lobby.AllReady, Is.False);
        }

        [TestCase(LobbyMode.Skirmish)]
        [TestCase(LobbyMode.Survival)]
        public void SwitchLobbyMode_IsRefusedWhenNotHosting(LobbyMode mode)
        {
            var online = new OnlineMatchService(null, new OnlineRecoveryProgress(), new SteamPlatform(apiEnabled: false));

            Assert.That(online.CanSwitchLobbyMode(mode), Is.False);
            Assert.Throws<System.InvalidOperationException>(() => online.SwitchLobbyMode(mode));
        }
    }
}
