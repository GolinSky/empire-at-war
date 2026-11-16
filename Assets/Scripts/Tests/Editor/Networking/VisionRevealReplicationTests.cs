using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Vision;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class VisionRevealReplicationTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void RevealMessage_RoundTripsViewerEntityAndState(bool active)
        {
            var message = new VisionRevealMessage
                { Viewer = TestPlayers.Ally, EntityId = 9876543210L, Active = active };
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.Write(message);
            using NetworkReaderPooled reader = NetworkReaderPool.Get(writer.ToArraySegment());
            VisionRevealMessage decoded = reader.Read<VisionRevealMessage>();
            Assert.That(decoded.Viewer, Is.EqualTo(message.Viewer));
            Assert.That(decoded.EntityId, Is.EqualTo(message.EntityId));
            Assert.That(decoded.Active, Is.EqualTo(active));
        }

        [TestCase(4f, true)]
        [TestCase(5f, false)]
        public void ReconnectBaseline_UsesCurrentExposureBeforeNextVisionTick(float now, bool active)
        {
            float clock = 0f;
            var vision = new VisionService(TestPlayers.CreateTeamGame(), true, () => clock);
            vision.Reveal(TestPlayers.Human, 42, 5f);
            clock = now;
            var sender = new HostVisionRevealSender(vision, null, null, null);
            var messages = new List<VisionRevealMessage>();
            void Capture(NetworkDiagnostics.MessageInfo info)
            {
                if (info.message is VisionRevealMessage message) messages.Add(message);
            }

            Transport previousTransport = Transport.active;
            var transportObject = new GameObject("Vision baseline transport test");
            var connection = new NetworkConnectionToClient(71);
            NetworkDiagnostics.OutMessageEvent += Capture;
            try
            {
                var transport = transportObject.AddComponent<kcp2k.KcpTransport>();
                typeof(kcp2k.KcpTransport).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(transport, new kcp2k.KcpConfig());
                Transport.active = transport;
                sender.SendBaseline(connection);
                Assert.That(messages.Count, Is.EqualTo(active ? 1 : 0));
                if (active)
                {
                    Assert.That(messages[0].Viewer, Is.EqualTo(TestPlayers.Human));
                    Assert.That(messages[0].EntityId, Is.EqualTo(42));
                    Assert.That(messages[0].Active, Is.True);
                }
            }
            finally
            {
                NetworkDiagnostics.OutMessageEvent -= Capture;
                connection.Cleanup();
                Transport.active = previousTransport;
                Object.DestroyImmediate(transportObject);
            }
        }

        [Test]
        public void ReplicaExposure_PersistsUntilHostEndsIt_AndDespawnClearsReusedIds()
        {
            float clock = 0f;
            var vision = new VisionService(TestPlayers.CreateTeamGame(), true, () => clock);
            var entities = new EntityLocator();
            var entity = new Entity(null, entities, Array.Empty<IEntityFacade>(), TestPlayers.Enemy, 42,
                null, null);
            entities.AddEntity(entity);
            var receiver = new ClientVisionRevealReceiver(vision, entities);
            var reveals = new List<VisionReveal>();
            receiver.Initialize();
            try
            {
                Receive(receiver, true);
                clock = 100f;
                vision.Tick();
                vision.CopyActiveReveals(reveals);
                Assert.That(reveals.Count, Is.EqualTo(1), "Replica expiry is owned by the host.");

                Receive(receiver, false);
                vision.CopyActiveReveals(reveals);
                Assert.That(reveals, Is.Empty);

                Receive(receiver, true);
                entities.RemoveEntity(entity);
                entities.AddEntity(entity);
                vision.CopyActiveReveals(reveals);
                Assert.That(reveals, Is.Empty, "A pooled entity must not inherit its prior reveal.");

                entities.RemoveEntity(entity);
                Receive(receiver, true);
                vision.CopyActiveReveals(reveals);
                Assert.That(reveals, Is.Empty, "Late reveal messages for removed entities are ignored.");
            }
            finally
            {
                receiver.LateDispose();
            }
        }

        private static void Receive(ClientVisionRevealReceiver receiver, bool active) =>
            typeof(ClientVisionRevealReceiver).GetMethod("OnReveal", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(receiver, new object[] { new VisionRevealMessage
                    { Viewer = TestPlayers.Human, EntityId = 42, Active = active } });
    }
}
