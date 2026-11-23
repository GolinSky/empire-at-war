using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// AI order filter (round 5, E9): a move, attack-move or attack is sent again to a ship only when its target
    /// changes or its destination moves more than <see cref="SAME_POINT_DISTANCE"/>. A ship whose move or attack-move
    /// ended gets it again at once (order repair); an attack on the same target that ended waits
    /// <see cref="REISSUE_SECONDS"/>, which breaks Attack → None loops. Other orders pass through and reset the ship's
    /// memory.
    /// </summary>
    public sealed class AiOrderDeduplicator : IUnitOrderService
    {
        public const float SAME_POINT_DISTANCE = 50f;
        public const float REISSUE_SECONDS = 10f;
        private const int PRUNE_SIZE = 256;

        private readonly IUnitOrderService _orders;
        private readonly Dictionary<IEntity, (UnitOrderType Type, IEntity Target, Vector3 Point, float At)> _issued =
            new Dictionary<IEntity, (UnitOrderType, IEntity, Vector3, float)>();
        private readonly List<IEntity> _receivers = new List<IEntity>();
        private readonly List<Vector3> _destinations = new List<Vector3>();

        private bool _resent;

        public AiOrderDeduplicator(IUnitOrderService orders) => _orders = orders;

        public event Action<UnitOrder> OrderIssued
        {
            add => _orders.OrderIssued += value;
            remove => _orders.OrderIssued -= value;
        }

        public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation)
        {
            if (Filter(receivers, UnitOrderType.Move, null, point))
                using (Tag()) _orders.IssueMove(_receivers.ToArray(), point, pace);
        }

        public void IssueMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations)
        {
            if (Filter(receivers, destinations, UnitOrderType.Move))
                using (Tag()) _orders.IssueMove(_receivers.ToArray(), _destinations.ToArray());
        }

        public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, MovePace pace = MovePace.Formation)
        {
            if (Filter(receivers, UnitOrderType.Attack, target, default))
                using (Tag()) _orders.IssueAttack(_receivers.ToArray(), target, pace);
        }

        public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets)
        {
            Forget(receivers);
            using (Tag()) _orders.IssueAttack(receivers, target, offsets);
        }

        public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId,
            MovePace pace = MovePace.Formation)
        {
            Forget(receivers);
            using (Tag()) _orders.IssueHardPointAttack(receivers, target, hardPointId, pace);
        }

        public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation)
        {
            if (Filter(receivers, UnitOrderType.AttackMove, null, point))
                using (Tag()) _orders.IssueAttackMove(_receivers.ToArray(), point, pace);
        }

        public void IssueAttackMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations)
        {
            if (Filter(receivers, destinations, UnitOrderType.AttackMove))
                using (Tag()) _orders.IssueAttackMove(_receivers.ToArray(), _destinations.ToArray());
        }

        public void IssueStop(IReadOnlyList<IEntity> receivers)
        {
            Forget(receivers);
            using (Tag()) _orders.IssueStop(receivers);
        }

        public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly)
        {
            Forget(receivers);
            using (Tag()) _orders.IssueGuard(receivers, friendly);
        }

        public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly, IReadOnlyList<Vector3> offsets)
        {
            Forget(receivers);
            using (Tag()) _orders.IssueGuard(receivers, friendly, offsets);
        }

        public void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints)
        {
            Forget(receivers);
            using (Tag()) _orders.IssueWaypointMove(receivers, waypoints);
        }

        public void IssueHunt(IReadOnlyList<IEntity> receivers)
        {
            Forget(receivers);
            using (Tag()) _orders.IssueHunt(receivers);
        }

        public void IssueRetreat(IReadOnlyList<IEntity> receivers)
        {
            Forget(receivers);
            using (Tag()) _orders.IssueRetreat(receivers);
        }

        public void IssueRetreat(IReadOnlyList<IEntity> receivers, Vector3 point)
        {
            Forget(receivers);
            using (Tag()) _orders.IssueRetreat(receivers, point);
        }

        // Rows of orders passed on are tagged; a same order resent after it ended is the filter's own repair.
        private ShipOrderTelemetry.IssuerScope Tag()
        {
            ShipOrderTelemetry.IssuerScope scope = ShipOrderTelemetry.Issue(_resent ? "dedup" : "executor");
            _resent = false;
            return scope;
        }

        private bool Filter(IReadOnlyList<IEntity> receivers, UnitOrderType type, IEntity target, Vector3 point)
        {
            float now = Time.time;
            Prune();
            _receivers.Clear();
            foreach (IEntity receiver in receivers)
            {
                if (IsRepeat(receiver, type, target, point, now)) continue;
                _issued[receiver] = (type, target, point, now);
                _receivers.Add(receiver);
            }
            return _receivers.Count > 0;
        }

        private bool Filter(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations, UnitOrderType type)
        {
            float now = Time.time;
            Prune();
            _receivers.Clear();
            _destinations.Clear();
            for (int i = 0; i < receivers.Count; i++)
            {
                if (IsRepeat(receivers[i], type, null, destinations[i], now)) continue;
                _issued[receivers[i]] = (type, null, destinations[i], now);
                _receivers.Add(receivers[i]);
                _destinations.Add(destinations[i]);
            }
            return _receivers.Count > 0;
        }

        // Lost ships never get another order; their entries go once the memory grows.
        private void Prune()
        {
            if (_issued.Count < PRUNE_SIZE) return;
            _receivers.Clear();
            foreach (IEntity receiver in _issued.Keys)
                if (receiver.HealthModel.IsDestroyed) _receivers.Add(receiver);
            foreach (IEntity receiver in _receivers) _issued.Remove(receiver);
        }

        private bool IsRepeat(IEntity receiver, UnitOrderType type, IEntity target, Vector3 point, float now)
        {
            if (!_issued.TryGetValue(receiver, out var last) || last.Type != type) return false;
            bool same = type == UnitOrderType.Attack
                ? last.Target == target
                : (new Vector2(last.Point.x - point.x, last.Point.z - point.z)).sqrMagnitude <=
                  SAME_POINT_DISTANCE * SAME_POINT_DISTANCE;
            if (!same) return false;
            bool stillRunning = receiver.TryGetFacade(out IUnitOrderObserverFacade orders) &&
                                orders.CurrentOrder == type;
            bool repeat = stillRunning || type == UnitOrderType.Attack && now - last.At < REISSUE_SECONDS;
            if (!repeat) _resent = true;
            return repeat;
        }

        private void Forget(IReadOnlyList<IEntity> receivers)
        {
            foreach (IEntity receiver in receivers) _issued.Remove(receiver);
        }
    }
}
