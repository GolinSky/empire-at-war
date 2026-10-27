using static EmpireAtWar.Utils.FormationConversion;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Global strategy chooses the objective and task-force receivers; ship states and
    /// the tactical brain execute it. Capture routes use Attack-Move, base defense uses
    /// Guard, and Hunt is only the fallback when a fleet target is unknown.
    /// Ships already retreating are not re-ordered, so their slots stay stable.
    /// Offensive orders go only to the task force's main body; stragglers (new builds, slow ships) guard its
    /// anchor ship until they have joined it, so the fleet arrives together instead of one ship at a time.
    /// </summary>
    public sealed class EnemyTaskForceExecutor
    {
        private const float CAPTURE_TARGET_EPSILON_SQUARED = 1f;

        private readonly IUnitOrderService _unitOrderService;
        private GameEntity _battleTarget;

        private readonly List<IShipEntity> _captureShips = new List<IShipEntity>();
        private readonly List<FormationPoint> _positions = new List<FormationPoint>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<FormationPoint> _destinations = new List<FormationPoint>();
        private readonly Dictionary<IShipEntity, FormationPoint> _battleOffsets =
            new Dictionary<IShipEntity, FormationPoint>();

        private Vector3 _captureTarget;

        private bool _hasCaptureTarget;
        private EnemyStrategicState _lastState;
        private readonly HashSet<IShipEntity> _assignedShips = new HashSet<IShipEntity>();
        private readonly List<IShipEntity> _body = new List<IShipEntity>();
        private readonly List<IShipEntity> _stragglers = new List<IShipEntity>();
        private readonly List<FormationPoint> _cohesionPoints = new List<FormationPoint>();
        private readonly HashSet<IShipEntity> _bodyMembers = new HashSet<IShipEntity>();
        private readonly Dictionary<IShipEntity, IShipEntity> _rallyAnchors = new Dictionary<IShipEntity, IShipEntity>();
        private readonly List<IShipEntity> _lostShips = new List<IShipEntity>();

        public EnemyTaskForceExecutor(IUnitOrderService unitOrderService) => _unitOrderService = unitOrderService;

        /// <summary>Ships committed with the main body by the last offensive order; 0 for other states.</summary>
        public int BodyCount => _body.Count;

        /// <summary>Committed ships still rallying to the main body after the last offensive order.</summary>
        public int RallyingCount => _stragglers.Count;

        public void Execute(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            if (_lastState != decision.State)
            {
                _assignedShips.Clear();
                _rallyAnchors.Clear();
                _hasCaptureTarget = false;
            }
            _lastState = decision.State;
            _body.Clear();
            _stragglers.Clear();
            _assignedShips.RemoveWhere(ship => !context.Receivers.ContainsKey(ship));
            _bodyMembers.RemoveWhere(ship => !context.Receivers.ContainsKey(ship));
            ForgetLostRallies(context);
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
                    bool captureChanged = !isSameCaptureTarget;
                    IShipEntity captureAnchor = SplitBody(_captureShips, captureCount);
                    foreach (IShipEntity ship in _body)
                        captureChanged |= !_assignedShips.Contains(ship) ||
                            (ship.CurrentOrder != UnitOrderType.None &&
                             ship.CurrentOrder != UnitOrderType.AttackMove);
                    if (captureChanged)
                        _unitOrderService.IssueAttackMove(Resolve(context, _body, _body.Count), context.CaptureTarget);
                    _assignedShips.Clear();
                    _assignedShips.UnionWith(_body);
                    RallyStragglers(context, captureAnchor);
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
                case EnemyStrategicState.EngageLocal:
                    IssueAttackOrHunt(context, decision.CommittedShipCount,
                        context.LocalThreatTarget, allowHunt: false);
                    return;
                case EnemyStrategicState.RaidStructures:
                    IssueAttackOrHunt(context, decision.CommittedShipCount,
                        context.RaidTarget, allowHunt: false);
                    return;
                case EnemyStrategicState.DefendBase:
                    if (context.OwnBase == null)
                    {
                        StopRemaining(context, context.Ships, 0);
                        return;
                    }
                    int guardCount = Math.Min(decision.CommittedShipCount,
                        context.Ships.Count);
                    _unitOrderService.IssueGuard(Resolve(context, context.Ships, guardCount),
                        context.OwnBase, BattleOffsets(context.Ships, guardCount,
                            context.OwnBase));
                    StopRemaining(context, context.Ships, guardCount);
                    return;
                case EnemyStrategicState.RetreatValue:
                    // Slots are recomputed from current positions; re-issuing would
                    // re-path ships that are already on their way.
                    List<GameEntity> retreatReceivers = ResolveWithout(context,
                        context.Ships, context.Ships.Count, UnitOrderType.Retreat);
                    if (retreatReceivers.Count > 0) _unitOrderService.IssueRetreat(retreatReceivers);
                    foreach (IShipEntity ship in context.Ships) _assignedShips.Add(ship);
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
            if (target == null && !allowHunt)
            {
                StopRemaining(context, context.Ships, 0);
                return;
            }
            IShipEntity anchor = SplitBody(context.Ships, count);
            List<GameEntity> receivers = Resolve(context, _body, _body.Count);
            if (target != null)
                _unitOrderService.IssueAttack(receivers, target,
                    BattleOffsets(_body, _body.Count, target));
            else _unitOrderService.IssueHunt(receivers);
            RallyStragglers(context, anchor);
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
                    _positions.Add(ToPoint(ships[i].WorldPosition));
                    _radii.Add(ships[i].NavigationRadius);
                }
                BattleFormationModel.CalculateDestinations(_positions, _radii,
                    default, _destinations,
                    target.TryGetFacade(out EmpireAtWar.Entities.BaseEntity.EntityFacades.INavigationFootprintFacade footprint)
                        ? footprint.NavigationRadius : 0f);
                for (int i = 0; i < count; i++)
                    _battleOffsets.Add(ships[i], _destinations[i]);
            }
            _battleTarget = target;
            List<Vector3> offsets = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                FormationPoint offset = _battleOffsets[ships[i]];
                offsets.Add(ToVector(offset));
            }
            return offsets;
        }

        /// <summary>
        /// Splits the first <paramref name="count"/> ships into the main body around the anchor and the stragglers.
        /// Returns the anchor ship.
        /// </summary>
        private IShipEntity SplitBody(IReadOnlyList<IShipEntity> ships, int count)
        {
            _body.Clear();
            _stragglers.Clear();
            _cohesionPoints.Clear();
            for (int i = 0; i < count; i++) _cohesionPoints.Add(ToPoint(ships[i].WorldPosition));
            int anchorIndex = FleetCohesion.FindAnchor(_cohesionPoints, FleetCohesion.RADIUS);
            FormationPoint anchorPoint = _cohesionPoints[anchorIndex];
            for (int i = 0; i < count; i++)
            {
                IShipEntity ship = ships[i];
                bool inBody = FleetCohesion.IsInBody(anchorPoint, _cohesionPoints[i], FleetCohesion.RADIUS,
                    _bodyMembers.Contains(ship));
                (inBody ? _body : _stragglers).Add(ship);
            }
            _bodyMembers.Clear();
            _bodyMembers.UnionWith(_body);
            return ships[anchorIndex];
        }

        /// <summary>Stragglers guard the anchor; a guard order is re-issued only when the anchor changes.</summary>
        private void RallyStragglers(EnemyStrategicContext context, IShipEntity anchor)
        {
            foreach (IShipEntity ship in _body) _rallyAnchors.Remove(ship);
            List<GameEntity> receivers = new List<GameEntity>();
            foreach (IShipEntity ship in _stragglers)
            {
                if (_rallyAnchors.TryGetValue(ship, out IShipEntity current) && current == anchor &&
                    ship.CurrentOrder == UnitOrderType.Guard) continue;
                _rallyAnchors[ship] = anchor;
                receivers.Add(context.Receivers[ship]);
            }
            if (receivers.Count > 0) _unitOrderService.IssueGuard(receivers, context.Receivers[anchor]);
        }

        private void ForgetLostRallies(EnemyStrategicContext context)
        {
            _lostShips.Clear();
            foreach (KeyValuePair<IShipEntity, IShipEntity> rally in _rallyAnchors)
                if (!context.Receivers.ContainsKey(rally.Key) || !context.Receivers.ContainsKey(rally.Value))
                    _lostShips.Add(rally.Key);
            foreach (IShipEntity ship in _lostShips) _rallyAnchors.Remove(ship);
        }

        private static List<GameEntity> Resolve(EnemyStrategicContext context,
            IReadOnlyList<IShipEntity> ships, int count)
        {
            List<GameEntity> receivers = new List<GameEntity>(count);
            for (int i = 0; i < count; i++)
                receivers.Add(context.Receivers[ships[i]]);
            return receivers;
        }

        private List<GameEntity> ResolveWithout(EnemyStrategicContext context,
            IReadOnlyList<IShipEntity> ships, int count, UnitOrderType runningOrder)
        {
            List<GameEntity> receivers = new List<GameEntity>(count);
            for (int i = 0; i < count; i++)
                if (ships[i].CurrentOrder != runningOrder &&
                    (!_assignedShips.Contains(ships[i]) || ships[i].CurrentOrder != UnitOrderType.None))
                    receivers.Add(context.Receivers[ships[i]]);
            return receivers;
        }

        private void StopRemaining(EnemyStrategicContext context,
            IReadOnlyList<IShipEntity> ships, int start)
        {
            List<GameEntity> idleCandidates = new List<GameEntity>();
            for (int i = start; i < ships.Count; i++)
                if (ships[i].CurrentOrder != UnitOrderType.None)
                    idleCandidates.Add(context.Receivers[ships[i]]);
            if (idleCandidates.Count > 0) _unitOrderService.IssueStop(idleCandidates);
        }
    }
}
