using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Round 5, E9: an AI order is sent again only for a new target or a destination moved over 50.</summary>
    [Category("EnemyAi")]
    public sealed class AiOrderDeduplicatorTests
    {
        [Test]
        public void SamePoint_IsNotSentAgainWhileTheOrderRuns()
        {
            var orders = new RecordingOrders();
            var filter = new AiOrderDeduplicator(orders);
            var ship = new OrderedEntity(UnitOrderType.AttackMove);

            filter.IssueAttackMove(new IEntity[] { ship }, new Vector3(100f, 0f, 0f));
            filter.IssueAttackMove(new IEntity[] { ship }, new Vector3(100f + 0.8f * AiOrderDeduplicator.SAME_POINT_DISTANCE, 0f, 0f));

            Assert.That(orders.Issued, Is.EqualTo(new[] { "AttackMove:1" }));

            filter.IssueAttackMove(new IEntity[] { ship }, new Vector3(100f + 2f * AiOrderDeduplicator.SAME_POINT_DISTANCE, 0f, 0f));
            Assert.That(orders.Issued, Is.EqualTo(new[] { "AttackMove:1", "AttackMove:1" }));
        }

        [Test]
        public void Attack_IsSentAgainOnlyForANewTarget()
        {
            var orders = new RecordingOrders();
            var filter = new AiOrderDeduplicator(orders);
            var ship = new OrderedEntity(UnitOrderType.Attack);
            var first = new OrderedEntity(UnitOrderType.None);
            var second = new OrderedEntity(UnitOrderType.None);

            filter.IssueAttack(new IEntity[] { ship }, first);
            filter.IssueAttack(new IEntity[] { ship }, first);
            filter.IssueAttack(new IEntity[] { ship }, second);

            Assert.That(orders.Issued, Is.EqualTo(new[] { "Attack:1", "Attack:1" }));
        }

        [Test]
        public void EndedMove_IsRepairedAtOnceButEndedAttackOnTheSameTargetWaits()
        {
            var orders = new RecordingOrders();
            var filter = new AiOrderDeduplicator(orders);
            var stopped = new OrderedEntity(UnitOrderType.None);
            var target = new OrderedEntity(UnitOrderType.None);

            filter.IssueAttackMove(new IEntity[] { stopped }, Vector3.zero);
            filter.IssueAttackMove(new IEntity[] { stopped }, Vector3.zero);
            filter.IssueAttack(new IEntity[] { stopped }, target);
            filter.IssueAttack(new IEntity[] { stopped }, target);

            Assert.That(orders.Issued, Is.EqualTo(new[] { "AttackMove:1", "AttackMove:1", "Attack:1" }));
        }

        [Test]
        public void OnlyNewReceivers_GetARepeatedOrder()
        {
            var orders = new RecordingOrders();
            var filter = new AiOrderDeduplicator(orders);
            var sent = new OrderedEntity(UnitOrderType.Move);
            var fresh = new OrderedEntity(UnitOrderType.None);

            filter.IssueMove(new IEntity[] { sent }, Vector3.one);
            filter.IssueMove(new IEntity[] { sent, fresh }, Vector3.one);

            Assert.That(orders.Issued, Is.EqualTo(new[] { "Move:1", "Move:1" }));
            Assert.That(orders.Receivers[1], Is.EqualTo(new IEntity[] { fresh }));
        }

        [Test]
        public void AnotherOrder_ResetsTheMemory()
        {
            var orders = new RecordingOrders();
            var filter = new AiOrderDeduplicator(orders);
            var ship = new OrderedEntity(UnitOrderType.AttackMove);

            filter.IssueAttackMove(new IEntity[] { ship }, Vector3.zero);
            filter.IssueStop(new IEntity[] { ship });
            filter.IssueAttackMove(new IEntity[] { ship }, Vector3.zero);

            Assert.That(orders.Issued, Is.EqualTo(new[] { "AttackMove:1", "Stop:1", "AttackMove:1" }));
        }

        // W66: a ship fighting a warship that hit it on its attack-move keeps that fight; only a retreat reaches it.
        [Test]
        public void ShipEngagedEnRoute_KeepsItsFightUntilARetreat()
        {
            var orders = new RecordingOrders();
            var filter = new AiOrderDeduplicator(orders);
            var engaged = new OrderedEntity(UnitOrderType.AttackMove) { IsEngagingEnRoute = true };
            var free = new OrderedEntity(UnitOrderType.None);

            filter.IssueAttackMove(new IEntity[] { engaged, free }, Vector3.zero);
            filter.IssueStop(new IEntity[] { engaged });
            filter.IssueGuard(new IEntity[] { engaged, free }, free);
            filter.IssueRetreat(new IEntity[] { engaged }, Vector3.one);

            Assert.That(orders.Issued, Is.EqualTo(new[] { "AttackMove:1", "Guard:1", "Retreat:1" }));
            Assert.That(orders.Receivers[0], Is.EqualTo(new IEntity[] { free }));
            Assert.That(orders.Receivers[1], Is.EqualTo(new IEntity[] { free }));
            Assert.That(orders.Receivers[2], Is.EqualTo(new IEntity[] { engaged }));
        }

        private sealed class RecordingOrders : IUnitOrderService
        {
            public List<string> Issued { get; } = new List<string>();
            public List<IReadOnlyList<IEntity>> Receivers { get; } = new List<IReadOnlyList<IEntity>>();

            public event Action<UnitOrder> OrderIssued { add { } remove { } }

            private void Record(string order, IReadOnlyList<IEntity> receivers)
            {
                Issued.Add($"{order}:{receivers.Count}");
                Receivers.Add(receivers);
            }

            public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation) =>
                Record("Move", receivers);
            public void IssueMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) =>
                Record("Move", receivers);
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, MovePace pace = MovePace.Formation) => Record("Attack", receivers);
            public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets) =>
                Record("Attack", receivers);
            public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId, MovePace pace = MovePace.Formation) =>
                Record("HardPointAttack", receivers);
            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation) => Record("AttackMove", receivers);
            public void IssueAttackMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations) =>
                Record("AttackMove", receivers);
            public void IssueStop(IReadOnlyList<IEntity> receivers) => Record("Stop", receivers);
            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly) => Record("Guard", receivers);
            public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly, IReadOnlyList<Vector3> offsets) =>
                Record("Guard", receivers);
            public void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints) =>
                Record("WaypointMove", receivers);
            public void IssueHunt(IReadOnlyList<IEntity> receivers) => Record("Hunt", receivers);
            public void IssueRetreat(IReadOnlyList<IEntity> receivers) => Record("Retreat", receivers);
            public void IssueRetreat(IReadOnlyList<IEntity> receivers, Vector3 point) => Record("Retreat", receivers);
        }

        private sealed class OrderedEntity : IEntity, IUnitOrderObserverFacade
        {
            private static long _nextId;

            public long Id { get; } = ++_nextId;
            public IHealthModelObserver HealthModel => null;
            public PlayerId Owner => TestPlayers.Enemy;
            public UnitOrderType CurrentOrder { get; }
            public IEntity AttackTarget => null;
            public bool IsEngagingEnRoute { get; set; }

            public OrderedEntity(UnitOrderType currentOrder) => CurrentOrder = currentOrder;

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                TryGetFacade(out TFacade facade);
                return facade;
            }

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
