using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Entities.BaseEntity.Orders;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
    public sealed class UnitOrderServiceTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private UnitOrderSettings _settings;
        private FakeLocator _locator;
        private FakeZones _zones;
        private UnitOrderService _unitOrderService;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            _locator = new FakeLocator();
            _zones = new FakeZones();
            _unitOrderService = CreateService();
        }

        private UnitOrderService CreateService(IFocusFireSlots focusFire = null)
        {
            EmptyObstacles obstacles = new EmptyObstacles();
            return new UnitOrderService(_locator, _zones, _settings, obstacles,
                new PaceClusterMergeService(_settings, obstacles), focusFire ?? UnlimitedFocusFireSlots.Instance);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject obj in _objects) UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        [TestCase(UnitActionId.Move)]
        [TestCase(UnitActionId.Attack)]
        [TestCase(UnitActionId.AttackMove)]
        [TestCase(UnitActionId.Stop)]
        [TestCase(UnitActionId.Guard)]
        [TestCase(UnitActionId.WaypointMove)]
        [TestCase(UnitActionId.Hunt)]
        [TestCase(UnitActionId.Retreat)]
        public void EveryAction_OnlyTouchesExplicitReceivers(UnitActionId action)
        {
            FakeEntity first = Entity(1, TestPlayers.Human);
            FakeEntity second = Entity(2, TestPlayers.Human);
            FakeEntity third = Entity(3, TestPlayers.Human);
            FakeEntity enemy = Entity(4, TestPlayers.Enemy);
            FakeEntity friendly = Entity(5, TestPlayers.Human);
            UnitOrder issued = default;
            bool fired = false;
            _unitOrderService.OrderIssued += order => { issued = order; fired = true; };

            Issue(action, new IEntity[] { second }, enemy, friendly);
            Assert.That(first.Command.CallCount, Is.Zero);
            Assert.That(second.Command.CallCount, Is.EqualTo(1));
            Assert.That(third.Command.CallCount, Is.Zero);
            Assert.That(fired, Is.True);
            Assert.That(issued.Issuer, Is.EqualTo(TestPlayers.Human));
            Assert.That(issued.Action, Is.EqualTo(action));

            first.Command.CallCount = 0;
            second.Command.CallCount = 0;
            third.Command.CallCount = 0;
            Issue(action, new IEntity[] { first, third }, enemy, friendly);
            Assert.That(first.Command.CallCount, Is.EqualTo(1));
            Assert.That(second.Command.CallCount, Is.Zero);
            Assert.That(third.Command.CallCount, Is.EqualTo(1));
        }

        [TestCase(UnitActionId.Move)]
        [TestCase(UnitActionId.Attack)]
        [TestCase(UnitActionId.AttackMove)]
        [TestCase(UnitActionId.Stop)]
        [TestCase(UnitActionId.Guard)]
        [TestCase(UnitActionId.WaypointMove)]
        [TestCase(UnitActionId.Hunt)]
        [TestCase(UnitActionId.Retreat)]
        public void ClientFeedback_PreservesAcceptedOrderWithoutExecutingCommands(UnitActionId action)
        {
            FakeEntity ship = Entity(1, TestPlayers.Human);
            FakeEntity enemy = Entity(2, TestPlayers.Enemy);
            FakeEntity friendly = Entity(3, TestPlayers.Human);
            _locator.AddEntity(ship);
            _locator.AddEntity(enemy);
            _locator.AddEntity(friendly);
            UnitOrder accepted = default;
            _unitOrderService.OrderIssued += order => accepted = order;
            Issue(action, new IEntity[] { ship }, enemy, friendly);
            ship.Command.CallCount = 0;

            var clientOrders = CreateService();
            var receiver = new ClientCombatEventReceiver(_locator, null, clientOrders);
            UnitOrder feedback = default;
            int notifications = 0;
            int attacks = 0;
            clientOrders.OrderIssued += order => { feedback = order; notifications++; };
            clientOrders.AttackIssued += (_, _, _) => attacks++;

            receiver.ReceiveOrderFeedback(new OrderFeedbackMessage(accepted));

            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(feedback.Action, Is.EqualTo(action));
            Assert.That(feedback.Issuer, Is.EqualTo(accepted.Issuer));
            Assert.That(feedback.Point, Is.EqualTo(accepted.Point));
            Assert.That(feedback.Target, Is.SameAs(accepted.Target));
            Assert.That(feedback.TargetHardPointId, Is.EqualTo(accepted.TargetHardPointId));
            Assert.That(feedback.Waypoints, Is.EqualTo(accepted.Waypoints));
            Assert.That(ship.Command.CallCount, Is.Zero);
            Assert.That(enemy.Command.CallCount, Is.Zero);
            Assert.That(friendly.Command.CallCount, Is.Zero);
            Assert.That(attacks, Is.Zero);
        }

        [Test]
        public void ClientFeedback_SkipsTargetRemovedBeforeDelivery()
        {
            FakeEntity enemy = Entity(2, TestPlayers.Enemy);
            var receiver = new ClientCombatEventReceiver(_locator, null, _unitOrderService);
            int notifications = 0;
            _unitOrderService.OrderIssued += _ => notifications++;

            receiver.ReceiveOrderFeedback(new OrderFeedbackMessage(
                new UnitOrder(UnitActionId.Attack, TestPlayers.Human, Vector3.zero, enemy)));

            Assert.That(notifications, Is.Zero);
        }

        [Test]
        public void UnsupportedReceiver_IsSkipped_IncludingMixedStationAttack()
        {
            FakeEntity ship = Entity(1, TestPlayers.Human);
            FakeEntity station = Entity(2, TestPlayers.Human,
                new[] { typeof(IFocusFireFacade), typeof(IStopFacade) });
            FakeEntity facility = Entity(3, TestPlayers.Human, Array.Empty<Type>());
            FakeEntity enemy = Entity(4, TestPlayers.Enemy);

            _unitOrderService.IssueAttack(new IEntity[] { ship, station, facility }, enemy);
            Assert.That(ship.Command.CallCount, Is.EqualTo(1));
            Assert.That(station.Command.CallCount, Is.EqualTo(1));
            Assert.That(facility.Command.CallCount, Is.Zero);
            Assert.That(station.Command.LastAction, Is.EqualTo(UnitActionId.Attack));
            _unitOrderService.IssueRetreat(new IEntity[] { ship, station, facility });
            Assert.That(ship.Command.CallCount, Is.EqualTo(2));
            Assert.That(station.Command.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void Move_UsesCompactFormationOrExplicitDestinations()
        {
            FakeEntity first = Entity(1, TestPlayers.Human);
            FakeEntity second = Entity(2, TestPlayers.Human);
            Vector3 destination = new Vector3(100f, 0f, 100f);
            _unitOrderService.IssueMove(new IEntity[] { first, second }, destination);
            Assert.That(first.Command.LastPoint, Is.Not.EqualTo(second.Command.LastPoint));
            Assert.That(Vector3.Distance(first.Command.LastPoint, second.Command.LastPoint),
                Is.GreaterThanOrEqualTo(10f));

            Vector3[] slots = { new Vector3(10f, 0f, 20f),
                new Vector3(30f, 0f, 40f) };
            _unitOrderService.IssueMove(new IEntity[] { first, second }, slots);
            Assert.That(first.Command.LastPoint, Is.EqualTo(slots[0]));
            Assert.That(second.Command.LastPoint, Is.EqualTo(slots[1]));
        }

        [Test]
        public void Save_LocalReplacementAttackRestoresAsIdle()
        {
            var order = new UnitOrderModel();
            order.Replace(UnitOrderType.Attack, holdAttackPosition: true);
            Assert.That(UnitOrderSave.From(order).Type, Is.EqualTo(UnitOrderType.None));

            order.Replace(UnitOrderType.Attack);
            Assert.That(UnitOrderSave.From(order).Type, Is.EqualTo(UnitOrderType.Attack));
        }

        [Test]
        public void AttackMove_SaveRoundTrip_RestoresOnlyMembersOfTheSameGroupTogether()
        {
            var restoredGroups = new Dictionary<string, AttackMoveEngagement>();
            var fleet = new AttackMoveEngagement();
            AttackMoveEngagement Restore(AttackMoveEngagement original)
            {
                var order = new UnitOrderModel();
                order.Replace(UnitOrderType.AttackMove, engagement: original);
                var loaded = JsonUtility.FromJson<UnitOrderSave>(JsonUtility.ToJson(UnitOrderSave.From(order)));
                FakeEntity ship = Entity(1, TestPlayers.Human);
                loaded.Apply(ship, _locator, restoredGroups);
                return ship.Command.LastEngagement;
            }

            AttackMoveEngagement first = Restore(fleet);
            Assert.That(first, Is.Not.SameAs(fleet));
            Assert.That(first.GroupId, Is.EqualTo(fleet.GroupId));
            Assert.That(Restore(fleet), Is.SameAs(first));
            Assert.That(Restore(new AttackMoveEngagement()), Is.Not.SameAs(first));
        }

        [Test]
        public void AttackMove_OldSaveWithoutGroup_RestoresIndependentEngagements()
        {
            var order = JsonUtility.FromJson<UnitOrderSave>($"{{\"type\":{(int)UnitOrderType.AttackMove}}}");
            var groups = new Dictionary<string, AttackMoveEngagement>();
            FakeEntity first = Entity(1, TestPlayers.Human);
            FakeEntity second = Entity(2, TestPlayers.Human);
            order.Apply(first, _locator, groups);
            order.Apply(second, _locator, groups);
            Assert.That(first.Command.LastEngagement, Is.Not.Null);
            Assert.That(second.Command.LastEngagement, Is.Not.Null.And.Not.SameAs(first.Command.LastEngagement));
        }

        [Test]
        public void AttackMove_ExplicitDestinationsShareEngagementAndSkipUnsupportedReceivers()
        {
            FakeEntity first = Entity(1, TestPlayers.Human);
            FakeEntity unsupported = Entity(2, TestPlayers.Human, Array.Empty<Type>());
            FakeEntity second = Entity(3, TestPlayers.Human);
            Vector3[] slots =
            {
                new Vector3(100f, 0f, 200f),
                Vector3.zero,
                new Vector3(300f, 0f, 400f)
            };
            UnitOrder issued = default;
            int eventCount = 0;
            _unitOrderService.OrderIssued += order => { issued = order; eventCount++; };

            _unitOrderService.IssueAttackMove(new IEntity[] { first, unsupported, second }, slots);

            Assert.That(first.Command.LastPoint, Is.EqualTo(slots[0]));
            Assert.That(second.Command.LastPoint, Is.EqualTo(slots[2]));
            Assert.That(unsupported.Command.CallCount, Is.Zero);
            Assert.That(first.Command.LastEngagement, Is.Not.Null);
            Assert.That(second.Command.LastEngagement, Is.SameAs(first.Command.LastEngagement));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(issued.Action, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(issued.Issuer, Is.EqualTo(TestPlayers.Human));
            Assert.That(issued.Point, Is.EqualTo(slots[0]));
        }

        [Test]
        public void GuardReservesTargetHull_AttackSlotPointsTowardEscortSide()
        {
            FakeEntity escort = Entity(1, TestPlayers.Human);
            FakeEntity target = Entity(2, TestPlayers.Human);
            escort.Command.WorldPosition = Vector3.forward * 200f;
            target.Command.NavigationRadius = 100f;
            _unitOrderService.IssueGuard(new IEntity[] { escort }, target);
            Assert.That(escort.Command.LastPoint.magnitude, Is.GreaterThan(105f));
            _unitOrderService.IssueGuard(new IEntity[] { escort }, target, new[] { Vector3.zero });
            Assert.That(escort.Command.LastPoint.magnitude, Is.GreaterThan(105f));
            _unitOrderService.IssueAttack(new IEntity[] { escort }, target);
            Assert.That(escort.Command.LastAction, Is.EqualTo(UnitActionId.Attack));
            Assert.That(Vector3.Distance(escort.Command.LastPoint.normalized, Vector3.forward),
                Is.LessThan(0.001f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_SlotsStayOnGroupSideInShipOrderWithHullClearance(bool mixedRadii)
        {
            const int SHIP_COUNT = 19;
            var ships = new FakeEntity[SHIP_COUNT];
            var receivers = new IEntity[SHIP_COUNT + 1];
            FakeEntity target = Entity(100, TestPlayers.Enemy);
            target.Command.NavigationRadius = 100f;
            for (int i = 0; i < SHIP_COUNT; i++)
            {
                ships[i] = Entity(i + 1, TestPlayers.Human);
                ships[i].Command.NavigationRadius = mixedRadii ? 2f + i % 4 : 5f;
                // A line of ships north of the target, west to east.
                ships[i].Command.WorldPosition = new Vector3((i - SHIP_COUNT / 2) * 50f, 0f, 1000f);
                receivers[i] = ships[i];
            }

            FakeEntity squadron = Entity(50, TestPlayers.Human, new[] { typeof(IAttackFacade) });
            receivers[SHIP_COUNT] = squadron;

            _unitOrderService.IssueAttack(receivers, target);

            Assert.That(squadron.Command.LastPoint, Is.EqualTo(Vector3.zero), "Squadrons get no ring slot.");
            for (int i = 0; i < SHIP_COUNT; i++)
            {
                Vector3 offset = ships[i].Command.LastPoint;
                Assert.That(offset.sqrMagnitude, Is.GreaterThan(0f));
                Assert.That(Vector3.Angle(offset, Vector3.forward), Is.LessThanOrEqualTo(60.001f),
                    "No slot lies behind the target.");
                if (i == 0) continue;
                Vector3 previous = ships[i - 1].Command.LastPoint;
                // West-to-east ships keep their order around the target, so legs do not cross.
                Assert.That(Vector3.SignedAngle(previous, offset, Vector3.up), Is.GreaterThan(0f));
                float contact = target.Command.NavigationRadius + Mathf.Max(5f, ships[i].Command.NavigationRadius);
                float clearance = ships[i].Command.NavigationRadius + ships[i - 1].Command.NavigationRadius;
                Assert.That(Vector3.Distance(previous.normalized * contact, offset.normalized * contact),
                    Is.GreaterThanOrEqualTo(clearance - 0.001f));
            }
        }

        [Test]
        public void Attack_TwoShipsGetNeighbouringSlotsNotOppositeEdgesOfTheArc()
        {
            FakeEntity target = Entity(100, TestPlayers.Enemy);
            target.Command.NavigationRadius = 100f;
            FakeEntity first = Entity(1, TestPlayers.Human);
            FakeEntity second = Entity(2, TestPlayers.Human);
            first.Command.WorldPosition = new Vector3(-20f, 0f, 1000f);
            second.Command.WorldPosition = new Vector3(20f, 0f, 1000f);

            _unitOrderService.IssueAttack(new IEntity[] { first, second }, target);

            Assert.That(Vector3.Angle(first.Command.LastPoint, second.Command.LastPoint), Is.LessThan(30f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_ShipsAndSquadronsShareOnePaceAndStationsGetNone(bool hardPoint)
        {
            FakeEntity target = Entity(100, TestPlayers.Enemy);
            FakeEntity ship = Entity(1, TestPlayers.Human);
            FakeEntity squadron = Entity(50, TestPlayers.Human, new[] { typeof(IAttackFacade) });
            FakeEntity station = Entity(60, TestPlayers.Human, new[] { typeof(IHardPointAttackFacade) });
            // Beyond link distance: a group already at its target is left unpaced.
            target.Health.Transform.position = new Vector3(0f, 0f, _settings.PaceTuning.LinkDistance * 2f);
            IEntity[] receivers = { ship, squadron, station };

            if (hardPoint) _unitOrderService.IssueHardPointAttack(receivers, target, 0);
            else _unitOrderService.IssueAttack(receivers, target);

            Assert.That(ship.Command.LastPace, Is.Not.Null);
            Assert.That(squadron.Command.LastPace, Is.SameAs(ship.Command.LastPace));
            Assert.That(station.Command.LastPace, Is.Null);
        }

        [Test]
        public void Attack_SingleMovingUnitGetsNoPace()
        {
            FakeEntity target = Entity(100, TestPlayers.Enemy);
            FakeEntity ship = Entity(1, TestPlayers.Human);
            FakeEntity station = Entity(60, TestPlayers.Human, new[] { typeof(IHardPointAttackFacade) });

            _unitOrderService.IssueHardPointAttack(new IEntity[] { ship, station }, target, 0);

            Assert.That(ship.Command.LastPace, Is.Null);
            Assert.That(station.Command.LastPace, Is.Null);
        }

        [Test]
        public void Retreat_UsesOwnStationThenDefaultZoneWhenStationDestroyed()
        {
            FakeEntity ship = Entity(1, TestPlayers.Enemy);
            FakeEntity station = Entity(2, TestPlayers.Enemy,
                Array.Empty<Type>(), new PlayerBaseFacade());
            station.Health.Transform.position = new Vector3(200f, 0f, 0f);
            _locator.AddEntity(station);
            _unitOrderService.IssueRetreat(new IEntity[] { ship });
            Assert.That(ship.Command.LastPoint.x, Is.LessThan(200f));
            station.Health.IsDestroyedValue = true;
            _unitOrderService.IssueRetreat(new IEntity[] { ship });
            Assert.That(ship.Command.LastPoint, Is.EqualTo(_zones.Center));
            Assert.That(_zones.LastSide, Is.EqualTo(TestPlayers.Enemy));
        }

        [Test]
        public void SpatialPaceClusters_MoveAttackAttackMoveClusterNearbyUnitsAndLooseMoveHasNoPace()
        {
            FakeEntity nearA = Entity(1, TestPlayers.Human);
            FakeEntity nearB = Entity(2, TestPlayers.Human);
            FakeEntity farC = Entity(3, TestPlayers.Human);
            FakeEntity enemy = Entity(4, TestPlayers.Enemy);
            nearA.Command.WorldPosition = new Vector3(0f, 0f, 0f);
            nearB.Command.WorldPosition = new Vector3(100f, 0f, 0f);
            farC.Command.WorldPosition = new Vector3(_settings.PaceTuning.LinkDistance * 3f, 0f, 0f);
            float farAway = _settings.PaceTuning.LinkDistance * 2f;
            enemy.Health.Transform.position = new Vector3(0f, 0f, farAway);
            IEntity[] receivers = { nearA, nearB, farC };

            UnitOrder issued = default;
            _unitOrderService.OrderIssued += order => issued = order;

            // Formation Move: near pair shares one cluster, far unit gets null.
            _unitOrderService.IssueMove(receivers, new Vector3(500f, 0f, 500f), MovePace.Formation);
            Assert.That(issued.IsLoose, Is.False);
            Assert.That(nearA.Command.LastPace, Is.Not.Null);
            Assert.That(nearA.Command.LastPace, Is.SameAs(nearB.Command.LastPace));
            Assert.That(farC.Command.LastPace, Is.Null);

            // Loose Move: no unit receives a pace cluster, and feedback flags IsLoose.
            _unitOrderService.IssueMove(receivers, new Vector3(500f, 0f, 500f), MovePace.Loose);
            Assert.That(issued.IsLoose, Is.True);
            Assert.That(nearA.Command.LastPace, Is.Null);
            Assert.That(nearB.Command.LastPace, Is.Null);
            Assert.That(farC.Command.LastPace, Is.Null);

            // AttackMove: near pair shares one cluster, far unit gets null.
            _unitOrderService.IssueAttackMove(receivers, new Vector3(0f, 0f, farAway));
            Assert.That(nearA.Command.LastPace, Is.Not.Null);
            Assert.That(nearA.Command.LastPace, Is.SameAs(nearB.Command.LastPace));
            Assert.That(farC.Command.LastPace, Is.Null);

            // Attack: near pair shares one cluster, far unit gets null.
            _unitOrderService.IssueAttack(receivers, enemy);
            Assert.That(nearA.Command.LastPace, Is.Not.Null);
            Assert.That(nearA.Command.LastPace, Is.SameAs(nearB.Command.LastPace));
            Assert.That(farC.Command.LastPace, Is.Null);

            // Loose AttackMove and Attack (double tap): no unit receives a pace cluster.
            _unitOrderService.IssueAttackMove(receivers, new Vector3(0f, 0f, farAway), MovePace.Loose);
            Assert.That(nearA.Command.LastPace, Is.Null);
            Assert.That(nearB.Command.LastPace, Is.Null);
            _unitOrderService.IssueAttack(receivers, enemy, MovePace.Loose);
            Assert.That(nearA.Command.LastPace, Is.Null);
            Assert.That(nearB.Command.LastPace, Is.Null);
        }

        [Test]
        public void AttackAndAttackMove_TargetNearCluster_LeaveThatClusterUnpaced()
        {
            float link = _settings.PaceTuning.LinkDistance;
            FakeEntity nearA = Entity(1, TestPlayers.Human);
            FakeEntity nearB = Entity(2, TestPlayers.Human);
            FakeEntity farA = Entity(3, TestPlayers.Human);
            FakeEntity farB = Entity(4, TestPlayers.Human);
            FakeEntity enemy = Entity(5, TestPlayers.Enemy);
            nearA.Command.WorldPosition = Vector3.zero;
            nearB.Command.WorldPosition = new Vector3(100f, 0f, 0f);
            farA.Command.WorldPosition = new Vector3(link * 4f, 0f, 0f);
            farB.Command.WorldPosition = new Vector3(link * 4f + 100f, 0f, 0f);
            Vector3 target = new Vector3(0f, 0f, link * 0.5f);
            enemy.Health.Transform.position = target;
            IEntity[] receivers = { nearA, nearB, farA, farB };

            _unitOrderService.IssueAttack(receivers, enemy);
            Assert.That(nearA.Command.LastPace, Is.Null);
            Assert.That(nearB.Command.LastPace, Is.Null);
            Assert.That(farA.Command.LastPace, Is.Not.Null);
            Assert.That(farA.Command.LastPace, Is.SameAs(farB.Command.LastPace));

            _unitOrderService.IssueAttackMove(receivers, target);
            Assert.That(nearA.Command.LastPace, Is.Null);
            Assert.That(nearB.Command.LastPace, Is.Null);
            Assert.That(farA.Command.LastPace, Is.Not.Null);
            Assert.That(farA.Command.LastPace, Is.SameAs(farB.Command.LastPace));
        }

        [Test]
        public void Attack_OverTheFocusFireCap_NearestShipsAttackAndTheRestAttackMoveToTheTarget()
        {
            CappedSlots slots = new CappedSlots { FreeShips = 2 };
            _unitOrderService = CreateService(slots);
            FakeEntity target = Entity(100, TestPlayers.Enemy);
            FakeEntity near = Entity(1, TestPlayers.Human);
            FakeEntity far = Entity(2, TestPlayers.Human);
            FakeEntity middle = Entity(3, TestPlayers.Human);
            near.Command.WorldPosition = new Vector3(0f, 0f, 100f);
            far.Command.WorldPosition = new Vector3(0f, 0f, 300f);
            middle.Command.WorldPosition = new Vector3(0f, 0f, 200f);
            var published = new List<UnitActionId>();
            _unitOrderService.OrderIssued += order => published.Add(order.Action);
            IReadOnlyList<IEntity> relayed = null;
            _unitOrderService.AttackIssued += (receivers, _, _) => relayed = receivers;

            _unitOrderService.IssueAttack(new IEntity[] { near, far, middle }, target);

            Assert.That(near.Command.LastAction, Is.EqualTo(UnitActionId.Attack));
            Assert.That(middle.Command.LastAction, Is.EqualTo(UnitActionId.Attack));
            Assert.That(far.Command.LastAction, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(Vector3.Distance(far.Command.LastPoint, target.Health.Transform.position), Is.LessThan(50f),
                "Overflow attack-moves to the target.");
            Assert.That(relayed, Is.EquivalentTo(new IEntity[] { near, middle }), "Only real attackers are relayed.");
            Assert.That(published, Is.EqualTo(new[] { UnitActionId.Attack, UnitActionId.AttackMove }),
                "Order feedback shows the overflow as attack-move.");
        }

        [Test]
        public void Attack_FullTarget_HolderKeepsItsSlotAndStationsStillAttack()
        {
            FakeEntity target = Entity(100, TestPlayers.Enemy);
            FakeEntity holder = Entity(1, TestPlayers.Human);
            FakeEntity nearer = Entity(2, TestPlayers.Human);
            FakeEntity station = Entity(60, TestPlayers.Human, new[] { typeof(IHardPointAttackFacade) });
            holder.Command.WorldPosition = new Vector3(0f, 0f, 500f);
            nearer.Command.WorldPosition = new Vector3(0f, 0f, 50f);
            CappedSlots slots = new CappedSlots { FreeShips = 0 };
            slots.Held.Add(holder.Id);
            _unitOrderService = CreateService(slots);

            _unitOrderService.IssueHardPointAttack(new IEntity[] { holder, nearer, station }, target, 0);

            Assert.That(holder.Command.LastAction, Is.EqualTo(UnitActionId.Attack));
            Assert.That(nearer.Command.LastAction, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(station.Command.LastAction, Is.EqualTo(UnitActionId.Attack), "Stations are exempt.");
        }

        [Test]
        public void Attack_ExplicitOffsetsStayWithTheirShipsWhenOneOverflows()
        {
            _unitOrderService = CreateService(new CappedSlots { FreeShips = 2 });
            FakeEntity target = Entity(100, TestPlayers.Enemy);
            FakeEntity first = Entity(1, TestPlayers.Human);
            FakeEntity overflow = Entity(2, TestPlayers.Human);
            FakeEntity third = Entity(3, TestPlayers.Human);
            first.Command.WorldPosition = new Vector3(0f, 0f, 100f);
            overflow.Command.WorldPosition = new Vector3(0f, 0f, 900f);
            third.Command.WorldPosition = new Vector3(0f, 0f, 200f);
            Vector3[] offsets = { Vector3.left * 10f, Vector3.forward * 10f, Vector3.right * 10f };

            _unitOrderService.IssueAttack(new IEntity[] { first, overflow, third }, target, offsets);

            Assert.That(first.Command.LastPoint, Is.EqualTo(offsets[0]));
            Assert.That(third.Command.LastPoint, Is.EqualTo(offsets[2]));
            Assert.That(overflow.Command.LastAction, Is.EqualTo(UnitActionId.AttackMove));
        }

        private FakeEntity Entity(long id, PlayerId side, Type[] commands = null,
            IEntityFacade role = null)
        {
            GameObject obj = new GameObject("Unit " + id);
            _objects.Add(obj);
            FakeEntity entity = new FakeEntity(id: id, side: side, health: new FakeHealth(obj.transform),
                role: role, commands: commands);
            return entity;
        }

        private void Issue(UnitActionId action, IReadOnlyList<IEntity> receivers,
            IEntity enemy, IEntity friendly)
        {
            Vector3 point = new Vector3(50f, 0f, 50f);
            switch (action)
            {
                case UnitActionId.Move: _unitOrderService.IssueMove(receivers, point); break;
                case UnitActionId.Attack: _unitOrderService.IssueAttack(receivers, enemy); break;
                case UnitActionId.AttackMove: _unitOrderService.IssueAttackMove(receivers, point); break;
                case UnitActionId.Stop: _unitOrderService.IssueStop(receivers); break;
                case UnitActionId.Guard: _unitOrderService.IssueGuard(receivers, friendly); break;
                case UnitActionId.WaypointMove:
                    _unitOrderService.IssueWaypointMove(receivers, new[] { point, point + Vector3.right });
                    break;
                case UnitActionId.Hunt: _unitOrderService.IssueHunt(receivers); break;
                case UnitActionId.Retreat: _unitOrderService.IssueRetreat(receivers); break;
            }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IEntityFacade _role;

            private readonly HashSet<Type> _commands;

            public long Id { get; }
            public PlayerId Owner { get; }
            public FakeHealth Health { get; }
            public IHealthModelObserver HealthModel => Health;
            public FakeCommand Command { get; }

            public FakeEntity(IEntityFacade role, FakeHealth health, Type[] commands,
                PlayerId side, long id)
            {
                Id = id;
                Owner = side;
                Health = health;
                _role = role;
                Command = new FakeCommand();
                _commands = commands == null ? null : new HashSet<Type>(commands);
            }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand command)
                where TCommand : IEntityFacade
            { if (HealthModel is TCommand transformFacade) { command = transformFacade; return true; }
                if (_role is TCommand roleFacade)
                {
                    command = roleFacade;
                    return true;
                }
                if ((_commands == null || _commands.Contains(typeof(TCommand))) && Command is TCommand allowed)
                {
                    command = allowed;
                    return true;
                }
                command = default;
                return false;
            }
        }

        private sealed class FakeCommand : IMoveFacade, IAttackFacade, IHardPointAttackFacade,
            IAttackMoveFacade, IStopFacade, IGuardFacade, IWaypointMoveFacade,
            IHuntFacade, IRetreatFacade, IFocusFireFacade, INavigationFootprintFacade
        {
            public int CallCount { get; set; }
            public UnitActionId LastAction { get; private set; }
            public Vector3 LastPoint { get; private set; }
            public AttackMoveEngagement LastEngagement { get; private set; }
            public PaceCluster LastPace { get; private set; }
            public Vector3 WorldPosition { get; set; }
            public float NavigationRadius { get; set; } = 5f;
            public float PaceSpeed { get; set; } = 30f;

            public void MoveTo(Vector2 point) => Record(UnitActionId.Move, point);

            public void MoveTo(Vector3 point, PaceCluster pace = null)
            { LastPace = pace; Record(UnitActionId.Move, point); }

            public void Attack(IEntity target, Vector3 offset, PaceCluster pace = null)
            { LastPace = pace; Record(UnitActionId.Attack, offset); }

            public void AttackHardPoint(IEntity target, int hardPointId, Vector3 offset, PaceCluster pace = null)
            { LastPace = pace; Record(UnitActionId.Attack, offset); }

            public void FocusFire(IEntity target) => Record(UnitActionId.Attack);

            public void AttackMoveTo(Vector3 point, AttackMoveEngagement engagement, PaceCluster pace = null)
            { LastEngagement = engagement; LastPace = pace; Record(UnitActionId.AttackMove, point); }

            public void Stop() => Record(UnitActionId.Stop);

            public void Guard(IEntity target, Vector3 offset) =>
                Record(UnitActionId.Guard, offset);

            public void MoveAlong(IReadOnlyList<Vector3> points) =>
                Record(UnitActionId.WaypointMove, points[0]);

            public void Hunt() => Record(UnitActionId.Hunt);

            public void Retreat(Vector3 point) =>
                Record(UnitActionId.Retreat, point);

            private void Record(UnitActionId action, Vector3 point = default)
            { CallCount++; LastAction = action; LastPoint = point; }
        }

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public bool IsDestroyedValue { get; set; }
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => IsDestroyedValue;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }
            public bool HasShields => true;

            public FakeHealth(Transform transform) { Transform = transform; }

            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }

        // Every FakeEntity counts as a ship (no unit type facade): only the ship pool matters here.
        private sealed class CappedSlots : IFocusFireSlots
        {
            public int FreeShips { get; set; }
            public HashSet<long> Held { get; } = new HashSet<long>();

            public event Action<IEntity> Changed { add { } remove { } }

            public int GetCap(IEntity target, bool isSquadron) => isSquadron ? FocusFireCaps.UNLIMITED : FreeShips + Held.Count;

            public int CountHolders(PlayerId team, IEntity target, bool isSquadron) => isSquadron ? 0 : Held.Count;

            public int FreeSlots(PlayerId team, IEntity target, bool isSquadron) => isSquadron ? int.MaxValue : FreeShips;

            public bool Holds(IEntity attacker, IEntity target) => Held.Contains(attacker.Id);

            public bool HasSlotFor(IEntity attacker, IEntity target) => Holds(attacker, target) || FreeShips > 0;

            public bool TryClaim(IEntity attacker, IEntity target) => throw new NotSupportedException();

            public void Release(long attackerId) => throw new NotSupportedException();
        }

        private sealed class FakeLocator : IEntityLocator
        {
            public event Action<IEntity> EntityAdded { add { } remove { } }

            public event Action<IEntity> EntityRemoved { add { } remove { } }

            public string Id => nameof(FakeLocator);
            private readonly Dictionary<long, IEntity> _entities = new Dictionary<long, IEntity>();
            public Dictionary<long, IEntity>.ValueCollection Entities => _entities.Values;

            public void AddEntity(IEntity entity) => _entities.Add(entity.Id, entity);

            public void RemoveEntity(IEntity entity) => _entities.Remove(entity.Id);

            public IEntity GetEntity(long id) => _entities.TryGetValue(id, out IEntity entity) ? entity : null;

            public bool TryGetEntity(long id, out IEntity entity)
            { entity = GetEntity(id); return entity != null; }

            public bool TryGetEntity(RaycastHit hit, out IEntity entity)
            { entity = null; return false; }

            public bool TryGetEntity(Collider collider, out IEntity entity)
            { entity = null; return false; }
        }

        private sealed class FakeZones : IReinforcementZonesSystem
        {
            public event Action OwnershipChanged { add { } remove { } }

            public Vector3 Center => new Vector3(-100f, 0f, 50f);
            public PlayerId LastSide { get; private set; }

            public bool TryGetDefaultZoneCenter(PlayerId side, out Vector3 point)
            { LastSide = side; point = Center; return true; }

            public int GetOwnedCapturableZoneCount(PlayerId side) => 0;

            public void CopyOwnedCapturableZoneBounds(PlayerId side, List<Bounds> dest)
                => dest.Clear();

            public bool TryGetDefaultZoneExitPosition(PlayerId side, Vector3 ship,
                float radius, out Vector3 point) { point = default; return false; }

            public bool TryGetCaptureTarget(PlayerId side, Vector3 origin,
                out Vector3 point) { point = default; return false; }
        }
    }
}
