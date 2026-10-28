using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using System.Reflection;
using System.Linq;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyUnitCommanderTests
    {
        [Test]
        public void ReserveSquadrons_AreTrackedOnceAndRejoinAfterPoolReuse()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EntityLocator locator = new EntityLocator();
            FakeSquadron reserve = new FakeSquadron(1, TestPlayers.Enemy);
            FakeSquadron friendly = new FakeSquadron(2, TestPlayers.Human);
            locator.AddEntity(reserve);
            locator.AddEntity(friendly);
            UnitCombatProfileCatalog catalog = new UnitCombatProfileCatalog(null, null, null, null);
            UnitCombatProfile profile = new UnitCombatProfile(ShipClass.Fighter, 1f, 0f,
                Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(), Array.Empty<UnitCombatProfile>());
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            profiles.Add(UnitTypeId.Squadron(SquadronType.Vulture), profile);
            EnemySquadronCommander commander = new EnemySquadronCommander(null, null, locator, roster,
                null, catalog, null, new TeamIntelRegistry(), roster.Get(TestPlayers.Enemy));
            MethodInfo discover = typeof(EnemySquadronCommander).GetMethod("TrackOwnedSquadrons",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo track = typeof(EnemySquadronCommander).GetMethod("Track",
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(ISquadron), typeof(SquadronType) }, null);
            var tracked = (Dictionary<ISquadron, CommandedSquadron>)typeof(EnemySquadronCommander)
                .GetField("_squadrons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(commander);
            try
            {
                discover.Invoke(commander, null);
                discover.Invoke(commander, null);
                track.Invoke(commander, new object[] { reserve, SquadronType.Vulture });
                Assert.That(tracked.Keys, Is.EquivalentTo(new[] { reserve }));
                Assert.That(tracked[reserve].Profile, Is.SameAs(profile));
                Assert.That(reserve.ReleaseListeners, Is.EqualTo(1));

                locator.RemoveEntity(reserve);
                reserve.Release();
                Assert.That(tracked, Is.Empty);
                Assert.That(reserve.ReleaseListeners, Is.Zero);
                locator.AddEntity(reserve);
                discover.Invoke(commander, null);
                Assert.That(tracked.Keys, Is.EquivalentTo(new[] { reserve }));
                Assert.That(reserve.ReleaseListeners, Is.EqualTo(1));
            }
            finally { commander.LateDispose(); }
            Assert.That(reserve.ReleaseListeners, Is.Zero);
        }

        [Test]
        public void HangarSquadrons_KeepEscortingTheirCarrier_WhileStationPurchasesTakeOrders()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            EntityLocator locator = new EntityLocator();
            FakeSquadron hangar = new FakeSquadron(1, TestPlayers.Enemy) { CurrentOrder = UnitOrderType.Guard };
            locator.AddEntity(hangar);
            UnitCombatProfileCatalog catalog = new UnitCombatProfileCatalog(null, null, null, null);
            UnitCombatProfile profile = new UnitCombatProfile(ShipClass.Fighter, 1f, 0f,
                Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>(), Array.Empty<UnitCombatProfile>());
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            profiles.Add(UnitTypeId.Squadron(SquadronType.Vulture), profile);
            EnemySquadronCommander commander = new EnemySquadronCommander(null, null, locator, roster,
                null, catalog, null, new TeamIntelRegistry(), roster.Get(TestPlayers.Enemy));
            var tracked = (Dictionary<ISquadron, CommandedSquadron>)typeof(EnemySquadronCommander)
                .GetField("_squadrons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(commander);
            MethodInfo issueOrder = typeof(EnemySquadronCommander).GetMethod("IssueOrder",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FakeSquadron purchased = new FakeSquadron(2, TestPlayers.Enemy);
            try
            {
                typeof(EnemySquadronCommander).GetMethod("TrackOwnedSquadrons",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(commander, null);
                typeof(EnemySquadronCommander).GetMethod("Track", BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(ISquadron), typeof(SquadronType) }, null)
                    .Invoke(commander, new object[] { purchased, SquadronType.Vulture });

                Assert.That(tracked[hangar].IsHangarLaunched, Is.True);
                Assert.That(tracked[purchased].IsHangarLaunched, Is.False);

                issueOrder.Invoke(commander, new object[] { hangar });
                Assert.That(hangar.OrdersReceived, Is.Zero, "A guarding hangar squadron must stay with its carrier.");
            }
            finally { commander.LateDispose(); }
        }

        private sealed class FakeSquadron : IEntity, ISquadron, IUnitTypeFacade, IUnitOrderObserverFacade
        {
            public event Action Released;
            public UnitOrderType CurrentOrder { get; set; }
            public int OrdersReceived { get; private set; }
            public int ReleaseListeners => Released == null ? 0 : Released.GetInvocationList().Length;
            public long Id { get; }
            public long EntityId => Id;
            public PlayerId Owner { get; }
            public IHealthModelObserver HealthModel { get; } = new FakeHealthModel(null);
            public UnitTypeId UnitTypeId => UnitTypeId.Squadron(SquadronType.Vulture);
            public Vector3 WorldPosition => Vector3.zero;
            public FakeSquadron(long id, PlayerId owner) { Id = id; Owner = owner; }
            public void Guard(IEntity friendly, Vector3 offset) => OrdersReceived++;
            public void AttackMoveTo(Vector3 position) => OrdersReceived++;
            public void Hunt() => OrdersReceived++;
            public void Release() => Released();
            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade => (TFacade)(object)this;
            public bool TryGetFacade<TFacade>(out TFacade facade) where TFacade : IEntityFacade
            {
                if (this is TFacade found) { facade = found; return true; }
                facade = default;
                return false;
            }
        }

        [Test]
        public void IntelGatherer_DoesNotReportCloakedShipInVisibleTerrain()
        {
            GameObject view = new GameObject("Cloaked target");
            try
            {
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                EntityLocator locator = new EntityLocator();
                FakeEntity target = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 1);
                target.Modifiers.SetCloaked(true);
                locator.AddEntity(target);
                TeamIntelRegistry registry = new TeamIntelRegistry();
                VisionService vision = new VisionService(roster, fogEnabled: false);

                new HostileIntelGatherer(locator, vision, roster, registry, owner).Tick();
                Assert.That(registry.Get(owner.Team).Sightings, Is.Empty);

                float lastSeen = Time.time - 5f;
                registry.Get(owner.Team).Report(new HostileSighting(target.Id,
                    UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    new FormationPoint(100f, 0f), 1f, 0f, 1f, lastSeen));
                new HostileIntelGatherer(locator, vision, roster, registry, owner).Tick();
                Assert.That(registry.Get(owner.Team).Sightings.Single().SeenAt, Is.EqualTo(lastSeen));
                Assert.That(registry.Get(owner.Team).Sightings.Single().Position.X, Is.EqualTo(100f));

                target.Modifiers.SetCloaked(false);
                new HostileIntelGatherer(locator, vision, roster, registry, owner).Tick();
                Assert.That(registry.Get(owner.Team).Sightings.Count, Is.EqualTo(1));
                Assert.That(registry.Get(owner.Team).Sightings.Single().Position.X, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [TestCase("FindLocalThreat")]
        [TestCase("FindClosestKnownShip")]
        public void StrategicTarget_CloakedContactDoesNotHideValidAlternative(string selector)
        {
            GameObject view = new GameObject("Targets");
            try
            {
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                EntityLocator locator = new EntityLocator();
                FakeEntity first = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 1);
                FakeEntity second = new FakeEntity(new FakeHealthModel(view.transform), TestPlayers.Human, 2);
                locator.AddEntity(first);
                locator.AddEntity(second);
                TeamIntelRegistry registry = new TeamIntelRegistry();
                HostileIntelModel intel = registry.Get(owner.Team);
                intel.Report(new HostileSighting(1, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    new FormationPoint(10f, 0f), 1f, 0f, 1f, 0f));
                intel.Report(new HostileSighting(2, UnitTypeId.Ship(ShipType.Venator), TestPlayers.Human,
                    new FormationPoint(20f, 0f), 1f, 0f, 1f, 0f));
                EnemyStrategicContextBuilder builder = new EnemyStrategicContextBuilder(
                    null, null, null, locator, null, roster, null, registry,
                    new VisionService(roster, fogEnabled: false), owner, null);
                MethodInfo find = typeof(EnemyStrategicContextBuilder).GetMethod(selector,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                object[] args = { Vector3.zero, 0f };
                Assert.That(find.Invoke(builder, args), Is.SameAs(first));
                // Build stores this result between decisions. Exercise the same cached-target path.
                typeof(EnemyStrategicContextBuilder).GetField("_localThreat",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, first);

                first.Modifiers.SetCloaked(true);
                Assert.That(find.Invoke(builder, args), Is.SameAs(second));
                second.Modifiers.SetCloaked(true);
                Assert.That(find.Invoke(builder, args), Is.Null);
                first.Modifiers.SetCloaked(false);
                Assert.That(find.Invoke(builder, args), Is.SameAs(first));
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [TestCase("FindClosestKnownShip")]
        [TestCase("FindLocalThreat")]
        [TestCase("FindRaidTarget")]
        public void RememberedTargets_RequireCurrentVisibilityIncludingCachedPick(string selector)
        {
            GameObject targetView = new GameObject("Target");
            GameObject scoutView = new GameObject("Scout");
            try
            {
                targetView.transform.position = Vector3.right * 10f;
                PlayerRoster roster = TestPlayers.CreateDuel();
                PlayerSlot owner = roster.Get(TestPlayers.Enemy);
                EntityLocator locator = new EntityLocator();
                FakeEntity target = new FakeEntity(new FakeHealthModel(targetView.transform), TestPlayers.Human, 1)
                    { IsUnit = selector != "FindRaidTarget" };
                locator.AddEntity(target);
                TeamIntelRegistry registry = new TeamIntelRegistry();
                if (target.IsUnit)
                    registry.Get(owner.Team).Report(new HostileSighting(1, UnitTypeId.Ship(ShipType.Venator),
                        TestPlayers.Human, new FormationPoint(10f, 0f), 1f, 0f, 1f, 0f));
                VisionService vision = new VisionService(roster);
                vision.Register(owner.Id, scoutView.transform, 20f);
                EnemyStrategicContextBuilder builder = new EnemyStrategicContextBuilder(
                    null, null, null, locator, null, roster, null, registry, vision, owner, null);
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo find = typeof(EnemyStrategicContextBuilder).GetMethod(selector, flags);
                object[] args = { Vector3.zero, 0f };
                Assert.That(find.Invoke(builder, args), Is.SameAs(target));
                typeof(EnemyStrategicContextBuilder).GetField("_localThreat", flags).SetValue(builder, target);
                typeof(EnemyStrategicContextBuilder).GetField("_raidTarget", flags).SetValue(builder, target);

                // Last-seen position remains visible, but the target has moved outside team vision.
                targetView.transform.position = Vector3.right * 100f;
                Assert.That(find.Invoke(builder, args), Is.Null);
                scoutView.transform.position = targetView.transform.position;
                // EditMode assertions share a frame; registration refreshes the per-frame vision snapshot.
                vision.Register(owner.Id, scoutView.transform, 20f);
                Assert.That(find.Invoke(builder, args), Is.SameAs(target));
                vision.Unregister(scoutView.transform);
                Assert.That(find.Invoke(builder, args), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetView);
                UnityEngine.Object.DestroyImmediate(scoutView);
            }
        }

        [Test]
        public void CaptureZone_CommitsFastestShipAndStopsOnlyBusyRemainder()
        {
            FakeShip slow = new FakeShip(id: 1, position: Vector3.zero) { NavigationSpeed = 1f,
                CurrentOrder = UnitOrderType.Attack };
            FakeShip fast = new FakeShip(id: 2, position: Vector3.right) { NavigationSpeed = 4f };
            FakeOrderService orders = new FakeOrderService();
            EnemyStrategicContext context = Context(new IShipEntity[] { slow, fast });

            new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                state: EnemyStrategicState.CaptureZone, committedShipCount: 1, reason: "test"), context);

            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(orders.Calls[0].Receivers[0].Id, Is.EqualTo(fast.EntityId));
            Assert.That(orders.Calls[1].Action, Is.EqualTo(UnitActionId.Stop));
            Assert.That(orders.Calls[1].Receivers[0].Id, Is.EqualTo(slow.EntityId));
        }

        [Test]
        public void HuntFleet_PreservesSurvivorOffsetsAcrossDecisions()
        {
            FakeShip first = new FakeShip(id: 1, position: Vector3.zero);
            FakeShip second = new FakeShip(id: 2, position: Vector3.zero);
            FakeShip third = new FakeShip(id: 3, position: Vector3.zero);
            GameObject view = new GameObject("Target");
            FakeEntity target = new FakeEntity(id: 4, side: TestPlayers.Human,
                health: new FakeHealthModel(view.transform));
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            try
            {
                executor.Execute(new EnemyStrategicDecision(
                    state: EnemyStrategicState.HuntFleet, committedShipCount: 3, reason: "test"),
                    Context(new IShipEntity[] { first, second, third }, target));
                Vector3 secondOffset = orders.Calls[0].Offsets[1];
                Vector3 thirdOffset = orders.Calls[0].Offsets[2];
                orders.Calls.Clear();

                executor.Execute(new EnemyStrategicDecision(
                    state: EnemyStrategicState.HuntFleet, committedShipCount: 2, reason: "test"),
                    Context(new IShipEntity[] { second, third }, target));

                Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.Attack));
                Assert.That(orders.Calls[0].Offsets[0], Is.EqualTo(secondOffset));
                Assert.That(orders.Calls[0].Offsets[1], Is.EqualTo(thirdOffset));
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [Test]
        public void DefendBase_GuardsCommittedShipsAndLeavesIdleShipUntouched()
        {
            FakeShip defender = new FakeShip(id: 1, position: Vector3.zero);
            FakeShip idle = new FakeShip(id: 2, position: Vector3.right);
            GameObject view = new GameObject("Station");
            FakeEntity station = new FakeEntity(id: 3, side: TestPlayers.Enemy,
                health: new FakeHealthModel(view.transform));
            FakeOrderService orders = new FakeOrderService();
            try
            {
                new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                    state: EnemyStrategicState.DefendBase, committedShipCount: 1, reason: "test"),
                    Context(new IShipEntity[] { defender, idle }, ownBase: station));
                Assert.That(orders.Calls.Count, Is.EqualTo(1));
                Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.Guard));
                Assert.That(orders.Calls[0].Target, Is.SameAs(station));
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }

        [Test]
        public void RetreatValue_IssuesRetreatForEntireFleetWithoutOwnBase()
        {
            FakeOrderService orders = new FakeOrderService();
            new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                state: EnemyStrategicState.RetreatValue, committedShipCount: 1, reason: "test"),
                Context(new IShipEntity[] { new FakeShip(id: 1, position: Vector3.zero),
                    new FakeShip(id: 2, position: Vector3.right) }));
            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.Retreat));
            Assert.That(orders.Calls[0].Receivers.Count, Is.EqualTo(2));
        }

        [Test]
        public void RetreatValue_DoesNotReissueRetreatToRetreatingShips()
        {
            FakeShip retreating = new FakeShip(id: 1, position: Vector3.zero)
                { CurrentOrder = UnitOrderType.Retreat };
            FakeShip fresh = new FakeShip(id: 2, position: Vector3.right);
            FakeOrderService orders = new FakeOrderService();

            new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                state: EnemyStrategicState.RetreatValue, committedShipCount: 2, reason: "test"),
                Context(new IShipEntity[] { retreating, fresh }));

            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Receivers.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Receivers[0].Id, Is.EqualTo(fresh.EntityId));
        }

        [Test]
        public void RetreatValue_ArrivalDoesNotReissueUntilStrategyChanges()
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicContext context = Context(new IShipEntity[] { ship });
            EnemyStrategicDecision retreat = new EnemyStrategicDecision(
                state: EnemyStrategicState.RetreatValue, committedShipCount: 1, reason: "test");
            executor.Execute(retreat, context);
            orders.Calls.Clear();
            executor.Execute(retreat, context);
            Assert.That(orders.Calls, Is.Empty);
            executor.Execute(new EnemyStrategicDecision(
                state: EnemyStrategicState.Hold, committedShipCount: 0, reason: "test"), context);
            executor.Execute(retreat, context);
            Assert.That(orders.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void CaptureZone_ArrivalStaysAssignedAndReinforcementSharesNewGroup()
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero);
            FakeShip reinforcement = new FakeShip(id: 2, position: Vector3.right);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicDecision decision = new EnemyStrategicDecision(
                state: EnemyStrategicState.CaptureZone, committedShipCount: 2, reason: "test");
            executor.Execute(decision, Context(new IShipEntity[] { ship }));
            orders.Calls.Clear();
            executor.Execute(decision, Context(new IShipEntity[] { ship }));
            Assert.That(orders.Calls, Is.Empty);
            executor.Execute(decision, Context(new IShipEntity[] { ship, reinforcement }));
            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Receivers.Count, Is.EqualTo(2));
        }

        [Test]
        public void CaptureZone_SameTargetSkipsShipsAlreadyAttackMoving()
        {
            FakeShip ship = new FakeShip(id: 1, position: Vector3.zero);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicDecision decision = new EnemyStrategicDecision(
                state: EnemyStrategicState.CaptureZone, committedShipCount: 1, reason: "test");
            executor.Execute(decision, Context(new IShipEntity[] { ship }));
            ship.CurrentOrder = UnitOrderType.AttackMove;
            orders.Calls.Clear();

            executor.Execute(decision, Context(new IShipEntity[] { ship }));

            Assert.That(orders.Calls, Is.Empty);
        }

        private static EnemyStrategicContext Context(IReadOnlyList<IShipEntity> ships,
            IEntity fleetTarget = null, IEntity ownBase = null)
        {
            Dictionary<IShipEntity, IEntity> receivers = new Dictionary<IShipEntity, IEntity>();
            foreach (IShipEntity ship in ships)
                receivers.Add(ship, new FakeEntity(id: ship.EntityId, side: TestPlayers.Enemy, health: null));
            return new EnemyStrategicContext(snapshot: default, ships: ships,
                captureTarget: new Vector3(55f, 0f, -55f), enemyFleetTarget: fleetTarget, enemyBaseTarget: null, ownBase: ownBase, receivers: receivers);
        }

        private sealed class FakeShip : IShipEntity
        {
            public PlayerId Owner => TestPlayers.Enemy;
            public Vector3 WorldPosition { get; }
            public float NavigationRadius => 5f;
            public float NavigationSpeed { get; set; } = 1f;
            public long EntityId { get; }
            public UnitOrderType CurrentOrder { get; set; }

            public FakeShip(Vector3 position, long id) { EntityId = id; WorldPosition = position; }
        }

        private sealed class FakeOrderService : IUnitOrderService
        {
            public event Action<UnitOrder> OrderIssued { add { } remove { } }

            public List<OrderCall> Calls { get; } = new List<OrderCall>();

            public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point) =>
                Record(UnitActionId.Move, receivers, point);

            public void IssueMove(IReadOnlyList<IEntity> receivers,
                IReadOnlyList<Vector3> destinations) =>
                Record(UnitActionId.Move, receivers,
                    destinations.Count > 0 ? destinations[0] : default);

            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target) =>
                Record(UnitActionId.Attack, receivers, target: target);

            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target,
                IReadOnlyList<Vector3> offsets) =>
                Record(UnitActionId.Attack, receivers, target: target, offsets: offsets);

            public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target,
                int hardPointId) =>
                Record(UnitActionId.Attack, receivers, target: target);

            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point) =>
                Record(UnitActionId.AttackMove, receivers, point);

            public void IssueStop(IReadOnlyList<IEntity> receivers) =>
                Record(UnitActionId.Stop, receivers);

            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly) =>
                Record(UnitActionId.Guard, receivers, target: friendly);

            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly,
                IReadOnlyList<Vector3> offsets) =>
                Record(UnitActionId.Guard, receivers, target: friendly, offsets: offsets);

            public void IssueWaypointMove(IReadOnlyList<IEntity> receivers,
                IReadOnlyList<Vector3> waypoints) =>
                Record(UnitActionId.WaypointMove, receivers);

            public void IssueHunt(IReadOnlyList<IEntity> receivers) =>
                Record(UnitActionId.Hunt, receivers);

            public void IssueRetreat(IReadOnlyList<IEntity> receivers) =>
                Record(UnitActionId.Retreat, receivers);

            private void Record(UnitActionId action, IReadOnlyList<IEntity> receivers,
                Vector3 point = default, IEntity target = null,
                IReadOnlyList<Vector3> offsets = null) =>
                Calls.Add(new OrderCall(action: action, receivers: receivers, point: point, target: target, offsets: offsets));
        }

        private sealed class OrderCall
        {
            public UnitActionId Action { get; }
            public IReadOnlyList<IEntity> Receivers { get; }
            public Vector3 Point { get; }
            public IEntity Target { get; }
            public IReadOnlyList<Vector3> Offsets { get; }

            public OrderCall(IReadOnlyList<IEntity> receivers, IEntity target,
                IReadOnlyList<Vector3> offsets, UnitActionId action, Vector3 point)
            {
                Action = action;
                Receivers = receivers;
                Point = point;
                Target = target;
                Offsets = offsets;
            }
        }

        private sealed class FakeEntity : IEntity, ICombatModifiersFacade, IUnitTypeFacade
        {
            public bool IsUnit { get; set; } = true;
            public CombatModifiers Modifiers { get; } = new CombatModifiers();
            public UnitTypeId UnitTypeId => UnitTypeId.Ship(ShipType.Venator);
            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }

            public FakeEntity(IHealthModelObserver health, PlayerId side, long id)
            { Id = id; Owner = side; HealthModel = health; }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand command)
                where TCommand : IEntityFacade
            {
                if (!IsUnit && typeof(TCommand) == typeof(IUnitTypeFacade))
                { command = default; return false; }
                if (this is TCommand self) { command = self; return true; }
                if (HealthModel is TCommand transformFacade) { command = transformFacade; return true; }
                command = default;
                return false;
            }
        }

        private sealed class FakeHealthModel : IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }
            public bool HasShields => true;

            public FakeHealthModel(Transform transform) { Transform = transform; }

            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
