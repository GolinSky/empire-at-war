using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Global strategy chooses the objective and task-force receivers; ship states and
    /// the tactical brain execute it. Capture routes use Attack-Move, base defense uses
    /// Guard, and Hunt is only the fallback when a fleet target is unknown.
    /// Ships already retreating are not re-ordered, so their slots stay stable.
    /// </summary>
    public sealed class EnemyTaskForceExecutor
    {
        private const float CAPTURE_TARGET_EPSILON_SQUARED = 1f;
        private readonly IUnitOrderService _orders;
        private readonly List<IShipEntity> _captureShips = new List<IShipEntity>();
        private readonly List<FormationPoint> _positions = new List<FormationPoint>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<FormationPoint> _destinations = new List<FormationPoint>();
        private readonly Dictionary<IShipEntity, FormationPoint> _battleOffsets =
            new Dictionary<IShipEntity, FormationPoint>();
        private GameEntity _battleTarget;
        private Vector3 _captureTarget;
        private bool _hasCaptureTarget;

        public EnemyTaskForceExecutor(IUnitOrderService orders) => _orders = orders;

        public void Execute(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            switch (decision.State)
            {
                case EnemyStrategicState.CaptureZone:
                    _captureShips.Clear();
                    _captureShips.AddRange(context.Ships);
                    _captureShips.Sort((a, b) =>
                        b.NavigationSpeed.CompareTo(a.NavigationSpeed));
                    int captureCount = Math.Min(decision.CommittedShipCount,
                        _captureShips.Count);
                    // Compact slots are recomputed from current positions, so re-issuing
                    // to ships already attack-moving would reset their engagement.
                    bool isSameCaptureTarget = _hasCaptureTarget &&
                        (_captureTarget - context.CaptureTarget).sqrMagnitude <=
                        CAPTURE_TARGET_EPSILON_SQUARED;
                    _captureTarget = context.CaptureTarget;
                    _hasCaptureTarget = true;
                    List<GameEntity> captureReceivers = isSameCaptureTarget
                        ? ResolveWithout(context, _captureShips, captureCount,
                            ShipOrderType.AttackMove)
                        : Resolve(context, _captureShips, captureCount);
                    if (captureReceivers.Count > 0)
                        _orders.IssueAttackMove(captureReceivers, context.CaptureTarget);
                    StopRemaining(context, _captureShips, captureCount);
                    return;
                case EnemyStrategicState.HuntFleet:
                    IssueAttackOrHunt(context, decision.CommittedShipCount,
                        context.EnemyFleetTarget);
                    return;
                case EnemyStrategicState.AssaultBase:
                    IssueAttackOrHunt(context, decision.CommittedShipCount,
                        context.EnemyBaseTarget, allowHunt: false);
                    return;
                case EnemyStrategicState.DefendBase:
                    if (context.OwnBase == null)
                    {
                        StopRemaining(context, context.Ships, 0);
                        return;
                    }
                    int guardCount = Math.Min(decision.CommittedShipCount,
                        context.Ships.Count);
                    _orders.IssueGuard(Resolve(context, context.Ships, guardCount),
                        context.OwnBase, BattleOffsets(context.Ships, guardCount,
                            context.OwnBase));
                    StopRemaining(context, context.Ships, guardCount);
                    return;
                case EnemyStrategicState.RetreatValue:
                    // Slots are recomputed from current positions; re-issuing would
                    // re-path ships that are already on their way.
                    List<GameEntity> retreatReceivers = ResolveWithout(context,
                        context.Ships, context.Ships.Count, ShipOrderType.Retreat);
                    if (retreatReceivers.Count > 0) _orders.IssueRetreat(retreatReceivers);
                    return;
                case EnemyStrategicState.Hold:
                case EnemyStrategicState.RebuildFleet:
                    StopRemaining(context, context.Ships, 0);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(decision.State));
            }
        }

        private void IssueAttackOrHunt(EnemyStrategicContext context,
            int committed, GameEntity target, bool allowHunt = true)
        {
            int count = Math.Min(committed, context.Ships.Count);
            List<GameEntity> receivers = Resolve(context, context.Ships, count);
            if (target != null)
                _orders.IssueAttack(receivers, target,
                    BattleOffsets(context.Ships, count, target));
            else if (allowHunt) _orders.IssueHunt(receivers);
            else
            {
                StopRemaining(context, context.Ships, 0);
                return;
            }
            StopRemaining(context, context.Ships, count);
        }

        private List<Vector3> BattleOffsets(IReadOnlyList<IShipEntity> ships,
            int count, GameEntity target)
        {
            bool rebuild = _battleTarget != target;
            for (int i = 0; i < count; i++)
                rebuild |= !_battleOffsets.ContainsKey(ships[i]);
            if (rebuild)
            {
                _battleOffsets.Clear();
                _positions.Clear();
                _radii.Clear();
                for (int i = 0; i < count; i++)
                {
                    _positions.Add(new FormationPoint(ships[i].WorldPosition.x,
                        ships[i].WorldPosition.z));
                    _radii.Add(ships[i].NavigationRadius);
                }
                BattleFormationModel.CalculateDestinations(_positions, _radii,
                    default, _destinations);
                for (int i = 0; i < count; i++)
                    _battleOffsets.Add(ships[i], _destinations[i]);
            }
            _battleTarget = target;
            List<Vector3> offsets = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                FormationPoint offset = _battleOffsets[ships[i]];
                offsets.Add(new Vector3(offset.X, 0f, offset.Z));
            }
            return offsets;
        }

        private static List<GameEntity> Resolve(EnemyStrategicContext context,
            IReadOnlyList<IShipEntity> ships, int count)
        {
            List<GameEntity> receivers = new List<GameEntity>(count);
            for (int i = 0; i < count; i++)
                receivers.Add(context.Receivers[ships[i]]);
            return receivers;
        }

        private static List<GameEntity> ResolveWithout(EnemyStrategicContext context,
            IReadOnlyList<IShipEntity> ships, int count, ShipOrderType runningOrder)
        {
            List<GameEntity> receivers = new List<GameEntity>(count);
            for (int i = 0; i < count; i++)
                if (ships[i].CurrentOrder != runningOrder)
                    receivers.Add(context.Receivers[ships[i]]);
            return receivers;
        }

        private void StopRemaining(EnemyStrategicContext context,
            IReadOnlyList<IShipEntity> ships, int start)
        {
            List<GameEntity> idleCandidates = new List<GameEntity>();
            for (int i = start; i < ships.Count; i++)
                if (ships[i].CurrentOrder != ShipOrderType.None)
                    idleCandidates.Add(context.Receivers[ships[i]]);
            if (idleCandidates.Count > 0) _orders.IssueStop(idleCandidates);
        }
    }
}
