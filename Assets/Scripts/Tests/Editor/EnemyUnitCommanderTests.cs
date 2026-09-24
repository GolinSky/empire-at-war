using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyUnitCommanderTests
    {
        [Test]
        public void CaptureZone_CommitsFastestShipAndStopsOnlyBusyRemainder()
        {
            FakeShip slow = new FakeShip(1, Vector3.zero) { NavigationSpeed = 1f,
                CurrentOrder = ShipOrderType.Attack };
            FakeShip fast = new FakeShip(2, Vector3.right) { NavigationSpeed = 4f };
            FakeOrderService orders = new FakeOrderService();
            EnemyStrategicContext context = Context(new IShipEntity[] { slow, fast });

            new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                EnemyStrategicState.CaptureZone, 1, "test"), context);

            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(orders.Calls[0].Receivers[0].Id, Is.EqualTo(fast.EntityId));
            Assert.That(orders.Calls[1].Action, Is.EqualTo(UnitActionId.Stop));
            Assert.That(orders.Calls[1].Receivers[0].Id, Is.EqualTo(slow.EntityId));
        }

        [Test]
        public void HuntFleet_PreservesSurvivorOffsetsAcrossDecisions()
        {
            FakeShip first = new FakeShip(1, Vector3.zero);
            FakeShip second = new FakeShip(2, Vector3.zero);
            FakeShip third = new FakeShip(3, Vector3.zero);
            GameObject view = new GameObject("Target");
            FakeEntity target = new FakeEntity(4, PlayerType.Player,
                new FakeHealthModel(view.transform));
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            try
            {
                executor.Execute(new EnemyStrategicDecision(
                    EnemyStrategicState.HuntFleet, 3, "test"),
                    Context(new IShipEntity[] { first, second, third }, target));
                Vector3 secondOffset = orders.Calls[0].Offsets[1];
                Vector3 thirdOffset = orders.Calls[0].Offsets[2];
                orders.Calls.Clear();

                executor.Execute(new EnemyStrategicDecision(
                    EnemyStrategicState.HuntFleet, 2, "test"),
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
            FakeShip defender = new FakeShip(1, Vector3.zero);
            FakeShip idle = new FakeShip(2, Vector3.right);
            GameObject view = new GameObject("Station");
            FakeEntity station = new FakeEntity(3, PlayerType.Opponent,
                new FakeHealthModel(view.transform));
            FakeOrderService orders = new FakeOrderService();
            try
            {
                new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                    EnemyStrategicState.DefendBase, 1, "test"),
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
                EnemyStrategicState.RetreatValue, 1, "test"),
                Context(new IShipEntity[] { new FakeShip(1, Vector3.zero),
                    new FakeShip(2, Vector3.right) }));
            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Action, Is.EqualTo(UnitActionId.Retreat));
            Assert.That(orders.Calls[0].Receivers.Count, Is.EqualTo(2));
        }

        [Test]
        public void RetreatValue_DoesNotReissueRetreatToRetreatingShips()
        {
            FakeShip retreating = new FakeShip(1, Vector3.zero)
                { CurrentOrder = ShipOrderType.Retreat };
            FakeShip fresh = new FakeShip(2, Vector3.right);
            FakeOrderService orders = new FakeOrderService();

            new EnemyTaskForceExecutor(orders).Execute(new EnemyStrategicDecision(
                EnemyStrategicState.RetreatValue, 2, "test"),
                Context(new IShipEntity[] { retreating, fresh }));

            Assert.That(orders.Calls.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Receivers.Count, Is.EqualTo(1));
            Assert.That(orders.Calls[0].Receivers[0].Id, Is.EqualTo(fresh.EntityId));
        }

        [Test]
        public void CaptureZone_SameTargetSkipsShipsAlreadyAttackMoving()
        {
            FakeShip ship = new FakeShip(1, Vector3.zero);
            FakeOrderService orders = new FakeOrderService();
            EnemyTaskForceExecutor executor = new EnemyTaskForceExecutor(orders);
            EnemyStrategicDecision decision = new EnemyStrategicDecision(
                EnemyStrategicState.CaptureZone, 1, "test");
            executor.Execute(decision, Context(new IShipEntity[] { ship }));
            ship.CurrentOrder = ShipOrderType.AttackMove;
            orders.Calls.Clear();

            executor.Execute(decision, Context(new IShipEntity[] { ship }));

            Assert.That(orders.Calls, Is.Empty);
        }

        private static EnemyStrategicContext Context(IReadOnlyList<IShipEntity> ships,
            IEntity fleetTarget = null, IEntity ownBase = null)
        {
            Dictionary<IShipEntity, IEntity> receivers = new Dictionary<IShipEntity, IEntity>();
            foreach (IShipEntity ship in ships)
                receivers.Add(ship, new FakeEntity(ship.EntityId, PlayerType.Opponent, null));
            return new EnemyStrategicContext(default, ships,
                new Vector3(55f, 0f, -55f), fleetTarget, null, ownBase, receivers);
        }

        private sealed class FakeShip : IShipEntity
        {
            public FakeShip(long id, Vector3 position) { EntityId = id; WorldPosition = position; }
            public IShipModelObserver ModelObserver => null;
            public PlayerType PlayerType => PlayerType.Opponent;
            public Vector3 WorldPosition { get; }
            public float NavigationRadius => 5f;
            public float NavigationSpeed { get; set; } = 1f;
            public long EntityId { get; }
            public ShipOrderType CurrentOrder { get; set; }
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
                Calls.Add(new OrderCall(action, receivers, point, target, offsets));
        }

        private sealed class OrderCall
        {
            public OrderCall(UnitActionId action, IReadOnlyList<IEntity> receivers,
                Vector3 point, IEntity target, IReadOnlyList<Vector3> offsets)
            {
                Action = action;
                Receivers = receivers;
                Point = point;
                Target = target;
                Offsets = offsets;
            }
            public UnitActionId Action { get; }
            public IReadOnlyList<IEntity> Receivers { get; }
            public Vector3 Point { get; }
            public IEntity Target { get; }
            public IReadOnlyList<Vector3> Offsets { get; }
        }

        private sealed class FakeEntity : IEntity
        {
            public FakeEntity(long id, PlayerType side, IHealthModelObserver health)
            { Id = id; PlayerType = side; HealthModel = health; }
            public long Id { get; }
            public EmpireAtWar.Mvc.IModelObserver Model => null;
            public IHealthModelObserver HealthModel { get; }
            public PlayerType PlayerType { get; }
            public bool TryGetCommand<TCommand>(out TCommand command)
                where TCommand : IEntityCommand { command = default; return false; }
        }

        private sealed class FakeHealthModel : IHealthModelObserver
        {
            public FakeHealthModel(Transform transform) { Transform = transform; }
            public event Action OnDestroy;
            public event Action OnValueChanged;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Armor => 1f;
            public float ArmorPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerType PlayerType => PlayerType.Player;
            public Transform Transform { get; }
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
