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
    /// <see cref="REISSUE_SECONDS"/>, which breaks Attack → None loops. An attack with formation offsets is filtered
    /// the same way and also passes when the ship's slot moves (X6). An attack whose ship now attacks another target
    /// is not a repeat (X5). Other orders pass through and reset the ship's memory. A ship fighting a warship that hit
    /// it on its attack-move keeps that fight: every order except a retreat skips it, so its group keeps going without
    /// it (W66, D3).
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
        private readonly HashSet<IEntity> _kept = new HashSet<IEntity>();

        private bool _resent;

        public AiOrderDeduplicator(IUnitOrderService orders) => _orders = orders;

        /// <summary>Orders passed on, counted per receiver (W57 per-minute order rows).</summary>
        public int PassedOrders { get; private set; }

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
            if (Filter(receivers, destinations, UnitOrderType.Move, null))
                using (Tag()) _orders.IssueMove(_receivers.ToArray(), _destinations.ToArray());
        }

        public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, MovePace pace = MovePace.Formation)
        {
            if (Filter(receivers, UnitOrderType.Attack, target, default))
                using (Tag()) _orders.IssueAttack(_receivers.ToArray(), target, pace);
        }

        public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets)
        {
            if (!Filter(receivers, offsets, UnitOrderType.Attack, target)) return;
            using (Tag()) _orders.IssueAttack(_receivers.ToArray(), target, _destinations.ToArray());
        }

        public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId,
            MovePace pace = MovePace.Formation)
        {
            if (!Pass(receivers)) return;
            using (Tag()) _orders.IssueHardPointAttack(_receivers.ToArray(), target, hardPointId, pace);
        }

        public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point, MovePace pace = MovePace.Formation)
        {
            if (Filter(receivers, UnitOrderType.AttackMove, null, point))
                using (Tag()) _orders.IssueAttackMove(_receivers.ToArray(), point, pace);
        }

        public void IssueAttackMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations)
        {
            if (Filter(receivers, destinations, UnitOrderType.AttackMove, null))
                using (Tag()) _orders.IssueAttackMove(_receivers.ToArray(), _destinations.ToArray());
        }

        public void IssueStop(IReadOnlyList<IEntity> receivers)
        {
            if (!Pass(receivers)) return;
            using (Tag()) _orders.IssueStop(_receivers.ToArray());
        }

        public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly)
        {
            if (!Pass(receivers)) return;
            using (Tag()) _orders.IssueGuard(_receivers.ToArray(), friendly);
        }

        public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly, IReadOnlyList<Vector3> offsets)
        {
            if (!Pass(receivers, offsets)) return;
            using (Tag()) _orders.IssueGuard(_receivers.ToArray(), friendly, _destinations.ToArray());
        }

        public void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints)
        {
            if (!Pass(receivers)) return;
            using (Tag()) _orders.IssueWaypointMove(_receivers.ToArray(), waypoints);
        }

        public void IssueHunt(IReadOnlyList<IEntity> receivers)
        {
            if (!Pass(receivers)) return;
            using (Tag()) _orders.IssueHunt(_receivers.ToArray());
        }

        // A retreat reaches every ship, a fighting one too.
        public void IssueRetreat(IReadOnlyList<IEntity> receivers)
        {
            Forget(receivers);
            PassedOrders += receivers.Count;
            using (Tag()) _orders.IssueRetreat(receivers);
        }

        public void IssueRetreat(IReadOnlyList<IEntity> receivers, Vector3 point)
        {
            Forget(receivers);
            PassedOrders += receivers.Count;
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
            PassedOrders += _receivers.Count;
            return _receivers.Count > 0;
        }

        /// <param name="target">Attack target with per-ship formation offsets in <paramref name="destinations"/>; null
        /// for moves.</param>
        private bool Filter(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations, UnitOrderType type,
            IEntity target)
        {
            float now = Time.time;
            Prune();
            _receivers.Clear();
            _destinations.Clear();
            for (int i = 0; i < receivers.Count; i++)
            {
                if (IsRepeat(receivers[i], type, target, destinations[i], now, target != null)) continue;
                _issued[receivers[i]] = (type, target, destinations[i], now);
                _receivers.Add(receivers[i]);
                _destinations.Add(destinations[i]);
            }
            PassedOrders += _receivers.Count;
            return _receivers.Count > 0;
        }

        /// <summary>Orders that are not filtered: every receiver; resets their memory.</summary>
        private bool Pass(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> offsets = null)
        {
            _receivers.Clear();
            _destinations.Clear();
            for (int i = 0; i < receivers.Count; i++)
            {
                _issued.Remove(receivers[i]);
                _receivers.Add(receivers[i]);
                if (offsets != null) _destinations.Add(offsets[i]);
            }
            PassedOrders += _receivers.Count;
            return _receivers.Count > 0;
        }

        /// <summary>
        /// The last order this filter passed to <paramref name="receiver"/> was <paramref name="type"/> toward a point
        /// within <paramref name="tolerance"/> of <paramref name="point"/>; any other order passed since clears it.
        /// </summary>
        public bool WasLastSent(IEntity receiver, UnitOrderType type, Vector3 point, float tolerance) =>
            _issued.TryGetValue(receiver, out var last) && last.Type == type &&
            new Vector2(last.Point.x - point.x, last.Point.z - point.z).sqrMagnitude <= tolerance * tolerance;

        /// <summary>
        /// X5: forgets every ship not in <paramref name="receivers"/> (the group's ships now): a ship that left the
        /// group and returns is not filtered against orders it got before.
        /// </summary>
        public void RetainOnly(IEnumerable<IEntity> receivers)
        {
            _kept.Clear();
            foreach (IEntity receiver in receivers) _kept.Add(receiver);
            _receivers.Clear();
            foreach (IEntity receiver in _issued.Keys)
                if (!_kept.Contains(receiver)) _receivers.Add(receiver);
            foreach (IEntity receiver in _receivers) _issued.Remove(receiver);
            _receivers.Clear();
            _kept.Clear();
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

        /// <param name="comparePoint">An attack also compares its formation offset (X6).</param>
        private bool IsRepeat(IEntity receiver, UnitOrderType type, IEntity target, Vector3 point, float now,
            bool comparePoint = false)
        {
            if (!_issued.TryGetValue(receiver, out var last) || last.Type != type) return false;
            bool samePoint = (new Vector2(last.Point.x - point.x, last.Point.z - point.z)).sqrMagnitude <=
                             SAME_POINT_DISTANCE * SAME_POINT_DISTANCE;
            bool same = type == UnitOrderType.Attack ? last.Target == target && (!comparePoint || samePoint) : samePoint;
            if (!same) return false;
            bool hasOrders = receiver.TryGetFacade(out IUnitOrderObserverFacade orders);
            // X5: a ship now attacking another target (another issuer re-targeted it) still needs this attack.
            bool attackingOther = hasOrders && type == UnitOrderType.Attack &&
                                  orders.CurrentOrder == UnitOrderType.Attack && orders.AttackTarget != null &&
                                  orders.AttackTarget != target;
            bool stillRunning = hasOrders && orders.CurrentOrder == type && !attackingOther;
            bool repeat = stillRunning ||
                          type == UnitOrderType.Attack && !attackingOther && now - last.At < REISSUE_SECONDS;
            if (!repeat) _resent = true;
            return repeat;
        }

        private void Forget(IReadOnlyList<IEntity> receivers)
        {
            foreach (IEntity receiver in receivers) _issued.Remove(receiver);
        }
    }
}
