using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Vision;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class HostVisionRevealSenderTests
    {
        private readonly List<VisionRevealMessage> _messages = new List<VisionRevealMessage>();
        private Dictionary<int, NetworkConnectionToClient> _previousConnections;
        private Transport _previousTransport;
        private GameObject _transportObject;
        private NetworkConnectionToClient _connection;
        private VisionService _vision;
        private NetworkTickService _ticks;
        private HostVisionRevealSender _sender;

        [SetUp]
        public void SetUp()
        {
            _previousConnections = new Dictionary<int, NetworkConnectionToClient>(NetworkServer.connections);
            _previousTransport = Transport.active;
            _transportObject = new GameObject("Vision sender transport test");
            var transport = _transportObject.AddComponent<kcp2k.KcpTransport>();
            typeof(kcp2k.KcpTransport).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(transport, new kcp2k.KcpConfig());
            Transport.active = transport;
            _connection = new NetworkConnectionToClient(71);
            NetworkServer.connections.Clear();
            NetworkServer.connections.Add(_connection.connectionId, _connection);
            var online = new OnlineMatchService(null, new OnlineRecoveryProgress(), new SteamPlatform(apiEnabled: false));
            typeof(OnlineMatchService).GetProperty("HasBegun").SetValue(online, true);
            ((HashSet<int>)typeof(OnlineMatchService).GetField("_loaded", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(online)).Add(_connection.connectionId);
            var entities = new EntityLocator();
            _vision = new VisionService(TestPlayers.CreateTeamGame(), true, () => 0f);
            _ticks = new NetworkTickService(entities, online);
            _sender = new HostVisionRevealSender(_vision, entities, online, _ticks);
            _messages.Clear();
            NetworkDiagnostics.OutMessageEvent += Capture;
            _sender.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _sender.LateDispose();
            _ticks.LateDispose();
            NetworkDiagnostics.OutMessageEvent -= Capture;
            _connection.Cleanup();
            NetworkServer.connections.Clear();
            foreach (var connection in _previousConnections)
                NetworkServer.connections.Add(connection.Key, connection.Value);
            Transport.active = _previousTransport;
            Object.DestroyImmediate(_transportObject);
        }

        [Test]
        public void Transitions_CoalescePerViewerAndEntityUntilSpawnFlush()
        {
            _vision.Reveal(TestPlayers.Human, 42, 5f);
            _vision.ForgetReveals(42);
            _vision.Reveal(TestPlayers.Human, 42, 5f);
            _vision.Reveal(TestPlayers.Human, 42, 5f);
            _vision.Reveal(TestPlayers.Enemy, 42, 5f);
            _vision.Reveal(TestPlayers.Human, 43, 5f);
            _vision.ForgetReveals(43);
            Assert.That(_messages, Is.Empty, "Transitions wait for NetworkTickService.SpawnsSent.");

            _ticks.Tick();

            Assert.That(_messages.Count, Is.EqualTo(3));
            Assert.That(_messages.Exists(m => m.Viewer == TestPlayers.Human && m.EntityId == 42 && m.Active), Is.True);
            Assert.That(_messages.Exists(m => m.Viewer == TestPlayers.Enemy && m.EntityId == 42 && m.Active), Is.True);
            Assert.That(_messages.Exists(m => m.Viewer == TestPlayers.Human && m.EntityId == 43 && !m.Active), Is.True);
            _messages.Clear();
            _ticks.Tick();
            Assert.That(_messages, Is.Empty, "A flushed transition is not sent twice.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Baseline_SupersedesQueuedTransitionsButAllowsLaterTransitions(bool activeAtBaseline)
        {
            _vision.Reveal(TestPlayers.Human, 42, 5f);
            if (!activeAtBaseline) _vision.ForgetReveals(42);
            _sender.SendBaseline(_connection);
            Assert.That(_messages.Count, Is.EqualTo(activeAtBaseline ? 1 : 0));
            _messages.Clear();

            _ticks.Tick();
            Assert.That(_messages, Is.Empty, "The baseline supersedes both queued reveal and hide transitions.");

            if (activeAtBaseline) _vision.ForgetReveals(42);
            else _vision.Reveal(TestPlayers.Human, 42, 5f);
            _ticks.Tick();
            Assert.That(_messages.Count, Is.EqualTo(1));
            Assert.That(_messages[0].Viewer, Is.EqualTo(TestPlayers.Human));
            Assert.That(_messages[0].EntityId, Is.EqualTo(42));
            Assert.That(_messages[0].Active, Is.EqualTo(!activeAtBaseline));
        }

        private void Capture(NetworkDiagnostics.MessageInfo info)
        {
            if (info.message is VisionRevealMessage message) _messages.Add(message);
        }
    }
}
