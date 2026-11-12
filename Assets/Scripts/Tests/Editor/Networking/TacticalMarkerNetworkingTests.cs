using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.TacticalMarkers;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class TacticalMarkerNetworkingTests
    {
        [Test]
        public void HostBroadcastQueuesOnlyForLoadedAlliedPlayers()
        {
            Dictionary<int, NetworkConnectionToClient> previous = new Dictionary<int, NetworkConnectionToClient>(NetworkServer.connections);
            Transport previousTransport = Transport.active;
            GameObject transportObject = new GameObject("Marker transport test");
            RecordingConnection ally = new RecordingConnection(71);
            RecordingConnection enemy = new RecordingConnection(72);
            RecordingConnection unloaded = new RecordingConnection(73);
            RecordingConnection unknown = new RecordingConnection(74);
            try
            {
                kcp2k.KcpTransport transport = transportObject.AddComponent<kcp2k.KcpTransport>();
                // EditMode does not call Awake; batching only needs the packet-size configuration.
                typeof(kcp2k.KcpTransport).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(transport, new kcp2k.KcpConfig());
                Transport.active = transport;
                NetworkServer.connections.Clear();
                foreach (RecordingConnection connection in new[] { ally, enemy, unloaded, unknown })
                    NetworkServer.connections.Add(connection.connectionId, connection);
                OnlineMatchService online = new OnlineMatchService(null);
                HashSet<int> loaded = (HashSet<int>)typeof(OnlineMatchService)
                    .GetField("_loaded", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(online);
                loaded.UnionWith(new[] { 71, 72, 74 });
                PlayerDirectory players = new PlayerDirectory();
                players.Players.Add(71, TestPlayers.Ally);
                players.Players.Add(72, TestPlayers.Enemy);
                players.Players.Add(73, TestPlayers.Human);

                HostBroadcast.ToTeam(online, players, TestPlayers.CreateTeamGame(), TestPlayers.Human,
                    new TacticalMarkerMessage { Id = 1, Type = TacticalMarkerType.Attack, Point = Vector3.one });

                Assert.That(ally.HasQueuedMessages, Is.True);
                Assert.That(enemy.HasQueuedMessages, Is.False);
                Assert.That(unloaded.HasQueuedMessages, Is.False);
                Assert.That(unknown.HasQueuedMessages, Is.False);
            }
            finally
            {
                foreach (RecordingConnection connection in new[] { ally, enemy, unloaded, unknown }) connection.Cleanup();
                NetworkServer.connections.Clear();
                foreach (KeyValuePair<int, NetworkConnectionToClient> connection in previous)
                    NetworkServer.connections.Add(connection.Key, connection.Value);
                Transport.active = previousTransport;
                Object.DestroyImmediate(transportObject);
            }
        }

        [Test]
        public void HostRejectsForgedMarkerIssuerBeforeApplyingCommand()
        {
            MarkerHandler markerHandler = new MarkerHandler();
            PlayerCommandHandlers handlers = new PlayerCommandHandlers();
            handlers.RegisterTacticalMarkers(markerHandler);
            PlayerDirectory players = new PlayerDirectory();
            players.Players.Add(71, TestPlayers.Human);
            HostCommandReceiver receiver = new HostCommandReceiver(new AuthorityCommandSink(null, null, handlers), players);
            MethodInfo receive = typeof(HostCommandReceiver).GetMethod("Receive", BindingFlags.Instance | BindingFlags.NonPublic);
            RecordingConnection connection = new RecordingConnection(71);
            PlayerCommandMessage message = new PlayerCommandMessage
            {
                Kind = PlayerCommandKind.PlaceMarker,
                PlaceMarker = new PlaceMarkerCommand(TestPlayers.Enemy, TacticalMarkerType.Attack, Vector3.one)
            };
            receive.Invoke(receiver, new object[] { connection, message });
            Assert.That(markerHandler.Calls, Is.Zero);
            message.PlaceMarker = new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Attack, Vector3.one);
            receive.Invoke(receiver, new object[] { connection, message });
            Assert.That(markerHandler.Calls, Is.EqualTo(1));
        }

        [Test]
        public void CommandRoundTripPreservesIssuerAndTarget()
        {
            PlaceMarkerCommand command = new PlaceMarkerCommand(TestPlayers.Human, TacticalMarkerType.Capture,
                new Vector3(12, 0, 48), TacticalMarkerTarget.SiteId(7));
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.WritePlayerCommandMessage(new PlayerCommandMessage
                { Kind = PlayerCommandKind.PlaceMarker, PlaceMarker = command });
            using NetworkReaderPooled reader = NetworkReaderPool.Get(writer.ToArraySegment());
            PlayerCommandMessage message = reader.ReadPlayerCommandMessage();
            Assert.That(HostCommandReceiver.IssuerOf(message), Is.EqualTo(command.Issuer));
            Assert.That(message.PlaceMarker.Type, Is.EqualTo(command.Type));
            Assert.That(message.PlaceMarker.Point, Is.EqualTo(command.Point));
            Assert.That(message.PlaceMarker.TargetId, Is.EqualTo(command.TargetId));
        }

        private sealed class RecordingConnection : NetworkConnectionToClient
        {
            public RecordingConnection(int id) : base(id) { }
            public bool HasQueuedMessages => batches.Count > 0;
        }

        private sealed class PlayerDirectory : INetworkPlayerDirectory
        {
            public readonly Dictionary<int, PlayerId> Players = new Dictionary<int, PlayerId>();
            public bool TryGetPlayer(int connectionId, out PlayerId player) => Players.TryGetValue(connectionId, out player);
        }

        private sealed class MarkerHandler : ITacticalMarkerCommandHandler
        {
            public int Calls;
            public bool TryPlace(PlaceMarkerCommand command) { Calls++; return true; }
        }
    }
}
