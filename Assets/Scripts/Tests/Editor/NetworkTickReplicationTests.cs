using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.UnitOrders;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class NetworkTickReplicationTests
    {
        private const float TOLERANCE = 0.01f;

        [Test]
        public void TickClock_RunsAtTwentyHertzRegardlessOfFrameTime()
        {
            NetworkTickClock clock = new NetworkTickClock();
            int ticks = 0;
            for (int frame = 0; frame < 60; frame++) ticks += clock.Advance(1f / 60f);
            ticks += clock.Advance(0.001f);

            Assert.That(ticks, Is.EqualTo(NetworkTickSettings.NETWORK_TICK_RATE));
            Assert.That(clock.Tick, Is.EqualTo((uint)NetworkTickSettings.NETWORK_TICK_RATE));
        }

        [Test]
        public void Keyframe_RoundTripsWithinQuantization()
        {
            EntityMotion motion = new EntityMotion(new Vector3(-4321.3f, -40.2f, 3999.9f), 271.4f, 37.3f, -42f, 3.1f);

            EntityMotion decoded = EntityKeyframeCodec.Decode(EntityKeyframeCodec.Encode(7, motion));

            Assert.That(Vector3.Distance(decoded.Position, motion.Position), Is.LessThanOrEqualTo(0.25f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(decoded.Heading, motion.Heading)), Is.LessThan(0.01f));
            Assert.That(decoded.Speed, Is.EqualTo(motion.Speed).Within(0.25f));
            Assert.That(decoded.TurnRate, Is.EqualTo(motion.TurnRate).Within(0.5f));
            Assert.That(decoded.ClimbRate, Is.EqualTo(motion.ClimbRate).Within(0.125f));
        }

        [Test]
        public void Motion_ExtrapolatesStraightLinesAndArcs()
        {
            EntityMotion straight = new EntityMotion(Vector3.zero, 90f, 10f, 0f, 1f).Extrapolate(2f);
            Assert.That(Vector3.Distance(straight.Position, new Vector3(20f, 2f, 0f)), Is.LessThan(TOLERANCE));

            // Half a circle at 90°/s for 2 s: radius = 10 / (π/2), ends 2r to the side, facing back.
            EntityMotion arc = new EntityMotion(Vector3.zero, 0f, 10f, 90f, 0f).Extrapolate(2f);
            float radius = 10f / (90f * Mathf.Deg2Rad);
            Assert.That(Vector3.Distance(arc.Position, new Vector3(2f * radius, 0f, 0f)), Is.LessThan(TOLERANCE));
            Assert.That(Mathf.DeltaAngle(arc.Heading, 180f), Is.EqualTo(0f).Within(TOLERANCE));
        }

        [Test]
        public void Motion_FromSamples_DerivesRates()
        {
            EntityMotion motion = EntityMotion.FromSamples(Vector3.zero, 350f, new Vector3(3f, 1f, 4f), 10f, 0.5f);

            Assert.That(motion.Speed, Is.EqualTo(10f).Within(TOLERANCE));
            Assert.That(motion.TurnRate, Is.EqualTo(40f).Within(TOLERANCE));
            Assert.That(motion.ClimbRate, Is.EqualTo(2f).Within(TOLERANCE));
        }

        [Test]
        public void DeadReckoning_SendsFirst_SkipsPredictable_ResendsOnDriftAndRefresh()
        {
            DeadReckoningSender sender = new DeadReckoningSender();
            EntityMotion cruising = new EntityMotion(Vector3.zero, 0f, 10f, 0f, 0f);

            Assert.That(sender.TryCreateKeyframe(1, cruising, 0.0, out _), Is.True, "First sighting is always sent.");
            Assert.That(sender.TryCreateKeyframe(1, cruising.Extrapolate(0.5f), 0.5, out _), Is.False,
                "A unit following its prediction is not resent.");

            EntityMotion swerved = new EntityMotion(cruising.Extrapolate(0.6f).Position + Vector3.right * 3f, 0f, 10f, 0f, 0f);
            Assert.That(sender.TryCreateKeyframe(1, swerved, 0.6, out _), Is.True, "Drift over 2 m is corrected.");

            Assert.That(sender.TryCreateKeyframe(1, swerved.Extrapolate(1.0f), 1.6, out _), Is.True,
                "A moving unit refreshes every second.");

            EntityMotion parked = new EntityMotion(Vector3.one, 0f, 0f, 0f, 0f);
            Assert.That(sender.TryCreateKeyframe(2, parked, 0.0, out _), Is.True);
            Assert.That(sender.TryCreateKeyframe(2, parked, 2.0, out _), Is.False, "Idle units refresh every 3 s.");
            Assert.That(sender.TryCreateKeyframe(2, parked, 3.0, out _), Is.True);
        }

        [Test]
        public void Chunker_KeepsChunksUnderTheBudget_AndAlwaysSendsOne()
        {
            Assert.That(KeyframeChunker.Split(new List<EntityKeyframe>()), Has.Count.EqualTo(1));

            List<EntityKeyframe> keyframes = new List<EntityKeyframe>();
            for (ushort i = 0; i < 200; i++) keyframes.Add(new EntityKeyframe { Index = i });
            List<EntityKeyframe[]> chunks = KeyframeChunker.Split(keyframes);

            Assert.That(chunks, Has.Count.EqualTo(Mathf.CeilToInt(200f / KeyframeChunker.KEYFRAMES_PER_CHUNK)));
            foreach (EntityKeyframe[] chunk in chunks)
                Assert.That(KeyframeChunker.HEADER_BYTES + chunk.Length * EntityKeyframeCodec.ENCODED_BYTES,
                    Is.LessThanOrEqualTo(NetworkTickSettings.CHUNK_BYTE_BUDGET));
        }

        [Test]
        public void NetIndexMap_ReusesReleasedIndices()
        {
            NetIndexMap map = new NetIndexMap();
            ushort first = map.Assign(100);
            ushort second = map.Assign(200);
            map.Release(100);

            Assert.That(map.Assign(300), Is.EqualTo(first));
            Assert.That(map.TryGetEntityId(second, out long id) && id == 200, Is.True);
        }

        [Test]
        public void CommandMessages_RoundTripThroughMirrorSerialization()
        {
            PlayerCommandMessage order = new PlayerCommandMessage
            {
                Kind = PlayerCommandKind.UnitOrder,
                LastReceivedTick = 42,
                UnitOrder = new UnitOrderCommand(TestPlayers.Enemy, UnitActionId.WaypointMove, new long[] { 5, 9 },
                    Vector3.up, 77, 3, new[] { Vector3.left, Vector3.forward })
            };
            PlayerCommandMessage deploy = new PlayerCommandMessage
            {
                Kind = PlayerCommandKind.DeployReinforcement,
                DeployReinforcement = new DeployReinforcementCommand(TestPlayers.Human, UnitRequestKind.Squadron, 4,
                    new[] { Vector3.one })
            };

            PlayerCommandMessage readOrder = RoundTrip(order);
            PlayerCommandMessage readDeploy = RoundTrip(deploy);

            Assert.That(readOrder.LastReceivedTick, Is.EqualTo(42));
            Assert.That(readOrder.UnitOrder.Issuer, Is.EqualTo(TestPlayers.Enemy));
            Assert.That(readOrder.UnitOrder.ReceiverIds, Is.EqualTo(new long[] { 5, 9 }));
            Assert.That(readOrder.UnitOrder.TargetId, Is.EqualTo(77));
            Assert.That(readOrder.UnitOrder.Waypoints, Is.EqualTo(new[] { Vector3.left, Vector3.forward }));
            Assert.That(readDeploy.DeployReinforcement.Kind, Is.EqualTo(UnitRequestKind.Squadron));
            Assert.That(readDeploy.DeployReinforcement.Slots, Is.EqualTo(new[] { Vector3.one }));
            Assert.That(HostCommandReceiver.IssuerOf(readDeploy), Is.EqualTo(TestPlayers.Human));
        }

        [Test]
        public void OrderFeedback_RoundTripsResolvedPointTargetAndWaypoints()
        {
            var target = new FakeEntity(77, null);
            var waypoints = new[] { Vector3.left, Vector3.forward };
            var accepted = new UnitOrder(UnitActionId.Attack, TestPlayers.Enemy, new Vector3(15f, 2f, 90f),
                target, waypoints, 3);
            var message = new OrderFeedbackMessage(accepted);
            waypoints[0] = Vector3.zero;
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.Write(message);
            using NetworkReaderPooled reader = NetworkReaderPool.Get(writer.ToArraySegment());
            OrderFeedbackMessage decoded = reader.Read<OrderFeedbackMessage>();

            Assert.That(decoded.Action, Is.EqualTo(accepted.Action));
            Assert.That(decoded.Issuer, Is.EqualTo(TestPlayers.Enemy));
            Assert.That(decoded.Point, Is.EqualTo(accepted.Point));
            Assert.That(decoded.TargetId, Is.EqualTo(77));
            Assert.That(decoded.TargetHardPointId, Is.EqualTo(3));
            Assert.That(decoded.Waypoints, Is.EqualTo(new[] { Vector3.left, Vector3.forward }));
        }

        [Test]
        public void HostReplicator_SpawnsMovesAndDespawns_AndClientBufferFollows()
        {
            GameObject root = new GameObject("NetworkedShip");
            try
            {
                EntityLocator entities = new EntityLocator();
                using HostTickReplicator host = new HostTickReplicator(entities);
                HostTickOutput output = new HostTickOutput();
                ClientReplicaBuffer client = new ClientReplicaBuffer();
                FakeEntity ship = new FakeEntity(11, root.transform);
                entities.AddEntity(ship);

                double time = 0;
                host.Advance(NetworkTickSettings.TICK_INTERVAL, time, output);
                Assert.That(output.Spawns, Has.Count.EqualTo(1));
                Assert.That(output.Spawns[0].EntityId, Is.EqualTo(11));
                Assert.That(output.Keyframes, Is.Not.Empty);
                Deliver(output, client, time);

                for (int tick = 1; tick <= 10; tick++)
                {
                    root.transform.position += Vector3.forward * 0.5f;
                    time += NetworkTickSettings.TICK_INTERVAL;
                    host.Advance(NetworkTickSettings.TICK_INTERVAL, time, output);
                    Deliver(output, client, time);
                }

                Assert.That(client.TrySample(11, out EntityMotion sampled), Is.True);
                Assert.That(sampled.Position.z, Is.GreaterThan(0f), "The client sees the ship moving forward.");

                entities.RemoveEntity(ship);
                host.Advance(0f, time, output);
                Assert.That(output.Despawns, Has.Count.EqualTo(1));
                Deliver(output, client, time);
                Assert.That(client.TrySample(11, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void Deliver(HostTickOutput output, ClientReplicaBuffer client, double time)
        {
            foreach (EntitySpawnMessage spawn in output.Spawns) client.OnSpawn(spawn, time);
            foreach (EntityDespawnMessage despawn in output.Despawns) client.OnDespawn(despawn);
            foreach (EntityKeyframesMessage chunk in output.Keyframes) client.OnKeyframes(chunk, time);
            client.Update(NetworkTickSettings.TICK_INTERVAL);
        }

        private static PlayerCommandMessage RoundTrip(PlayerCommandMessage message)
        {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.WritePlayerCommandMessage(message);
            using NetworkReaderPooled reader = NetworkReaderPool.Get(writer.ToArraySegment());
            return reader.ReadPlayerCommandMessage();
        }

        private sealed class DestroyedHealth : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }
            public EmpireAtWar.Components.Ship.Health.ShipClass ShipClass => default;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 0f;
            public float HullPercentage => 0f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => true;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => false;
            public bool HasLiveHardPoints => false;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;
            public IHardPointModel[] GetShipUnits(EmpireAtWar.Components.Ship.Health.HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }

        private sealed class FakeEntity : IEntity, IEntityTransformFacade
        {
            public FakeEntity(long id, Transform transform)
            {
                Id = id;
                Transform = transform;
            }

            public long Id { get; }
            public IHealthModelObserver HealthModel => new DestroyedHealth();
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade =>
                TryGetFacade(out TFacade facade) ? facade : throw new NotSupportedException();

            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                if (this is TFacade self)
                {
                    facade = self;
                    return true;
                }

                facade = default;
                return false;
            }
        }
    }
}
