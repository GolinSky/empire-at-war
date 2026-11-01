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
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Global strategy chooses the objective and task-force receivers; ship states and
    /// the tactical brain execute it. Capture routes retain their engagements, and
    /// station defense intercepts the visible attacker.
    /// Ships already retreating are not re-ordered, so their slots stay stable.
    /// Offensive orders go only to the task force's main body; stragglers (new builds, slow ships) guard its
    /// anchor ship until they have joined it, so the fleet arrives together instead of one ship at a time.
    /// </summary>
    public sealed class EnemyTaskForceExecutor
    {
        private const float CAPTURE_TARGET_EPSILON_SQUARED = 1f;

        private readonly IUnitOrderService _unitOrderService;
        private GameEntity _battleTarget;
        private GameEntity _defenseStation;
        private readonly EnemyAttackTaskForce _attackForce = new EnemyAttackTaskForce();
        private readonly List<IShipEntity> _supportShips = new List<IShipEntity>();
        private readonly List<IShipEntity> _strikeShips = new List<IShipEntity>();

        private readonly List<IShipEntity> _captureShips = new List<IShipEntity>();
        private readonly List<FormationPoint> _positions = new List<FormationPoint>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<FormationPoint> _destinations = new List<FormationPoint>();
        private readonly Dictionary<IShipEntity, FormationPoint> _battleOffsets =
            new Dictionary<IShipEntity, FormationPoint>();

        private Vector3 _captureTarget;

        private bool _hasCaptureTarget;
        private EnemyCaptureObjective _captureObjective;
        private readonly Dictionary<IShipEntity, float> _captureDistances = new Dictionary<IShipEntity, float>();
        private Vector3? _retreatAnchor;
        public string ActionReason { get; private set; } = "";
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

        public bool CanAssemble(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            GameEntity target;
            switch (decision.State)
            {
                case EnemyStrategicState.HuntFleet: target = context.EnemyFleetTarget; break;
                case EnemyStrategicState.AssaultBase: target = context.EnemyBaseTarget; break;
                case EnemyStrategicState.RaidStructures: target = context.RaidTarget; break;
                default: return true;
            }
            return target == null || _attackForce.Assemble(context, target, decision.CommittedShipCount);
        }

        public void Execute(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            if (_lastState != decision.State)
            {
                _assignedShips.Clear();
                _rallyAnchors.Clear();
                _captureDistances.Clear();
                _hasCaptureTarget = false;
                _defenseStation = null;
                _attackForce.Clear();
            }
            _lastState = decision.State;
            ActionReason = decision.Reason;
            _body.Clear();
            _stragglers.Clear();
            _assignedShips.RemoveWhere(ship => !context.Receivers.ContainsKey(ship));
            _bodyMembers.RemoveWhere(ship => !context.Receivers.ContainsKey(ship));
            ForgetLostRallies(context);
            _lostShips.Clear();
            foreach (IShipEntity ship in _captureDistances.Keys)
                if (!context.Receivers.ContainsKey(ship)) _lostShips.Add(ship);
            foreach (IShipEntity ship in _lostShips) _captureDistances.Remove(ship);
            if (context.Ships.Count == 0) return;
            switch (decision.State)
            {
                case EnemyStrategicState.CaptureZone:
                    ExecuteCapture(decision, context);
                    return;
                case EnemyStrategicState.HuntFleet:
                    IssueAttackOrHunt(context, decision.CommittedShipCount, context.EnemyFleetTarget);
                    return;
                case EnemyStrategicState.AssaultBase:
                    IssueAttackOrHunt(context, decision.CommittedShipCount, context.EnemyBaseTarget, allowHunt: false);
                    return;
                case EnemyStrategicState.EngageLocal:
                    IssueAttackOrHunt(context, decision.CommittedShipCount, context.LocalThreatTarget, allowHunt: false, assemble: false);
                    return;
                case EnemyStrategicState.RaidStructures:
                    IssueAttackOrHunt(context, decision.CommittedShipCount, context.RaidTarget, allowHunt: false);
                    return;
                case EnemyStrategicState.DefendBase:
                    ActionReason = context.StationPressureReason;
                    if (context.OwnBase != null)
                    {
                        List<GameEntity> defenders = new List<GameEntity>();
                        int defenseCount = Math.Min(decision.CommittedShipCount, context.Ships.Count);
                        for (int i = 0; i < defenseCount; i++)
                        {
                            IShipEntity ship = context.Ships[i];
                            if (_defenseStation != context.OwnBase || ship.CurrentOrder != UnitOrderType.AttackMove)
                                defenders.Add(context.Receivers[ship]);
                        }
                        _defenseStation = context.OwnBase;
                        if (defenders.Count > 0)
                            _unitOrderService.IssueAttackMove(defenders,
                                context.OwnBase.GetFacade<IEntityTransformFacade>().Transform.position);
                        StopRemaining(context, context.Ships, defenseCount);
                        return;
                    }
                    IssueAttackOrHunt(context, decision.CommittedShipCount, context.DefenseTarget, allowHunt: false, assemble: false);
                    return;
                case EnemyStrategicState.RetreatValue:
                    ExecuteRetreat(context);
                    return;
                case EnemyStrategicState.Hold:
                case EnemyStrategicState.RebuildFleet:
                    StopRemaining(context, context.Ships, 0);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(decision.State));
            }
        }

        private void ExecuteCapture(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            _captureShips.Clear();
            _captureShips.AddRange(context.Ships);
            _captureShips.Sort((a, b) => b.NavigationSpeed.CompareTo(a.NavigationSpeed));
            int count = Math.Min(decision.CommittedShipCount, _captureShips.Count);
            EnemyCaptureObjective objective = context.CaptureObjective;
            bool sameObjective = objective == null ? _captureObjective == null :
                _captureObjective != null && objective.Id == _captureObjective.Id &&
                objective.Kind == _captureObjective.Kind && objective.Owner == _captureObjective.Owner;
            bool sameTarget = _hasCaptureTarget && sameObjective &&
                (_captureTarget - context.CaptureTarget).sqrMagnitude <= CAPTURE_TARGET_EPSILON_SQUARED;
            if (!sameTarget)
            {
                _assignedShips.Clear();
                _captureDistances.Clear();
            }
            _captureTarget = context.CaptureTarget;
            _captureObjective = objective;
            _hasCaptureTarget = true;
            IShipEntity anchor = SplitBody(_captureShips, count);
            if (count >= 6 && _body.Count * 4 < count * 3)
            {
                RallyBody(context, anchor);
                ActionReason = $"Assembling capture force: {_body.Count}/{count} ships in the body.";
                return;
            }
            List<GameEntity> approaching = new List<GameEntity>();
            float radius = context.CaptureObjective == null ? 1f : context.CaptureObjective.Radius;
            foreach (IShipEntity ship in _body)
            {
                Vector3 offset = ship.WorldPosition - _captureTarget;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (!_captureDistances.TryGetValue(ship, out float closest)) closest = distance;
                _captureDistances[ship] = Math.Min(closest, distance);
                float leash = Math.Max(FleetCohesion.RADIUS, radius + ship.NavigationRadius * 2f);
                bool diverted = distance > closest + leash;
                if (sameTarget && ship.CurrentOrder == UnitOrderType.AttackMove && diverted)
                {
                    _unitOrderService.IssueMove(new[] { context.Receivers[ship] }, context.CaptureTarget);
                    ActionReason = "Capture diversion exceeded its leash; return to the objective.";
                    continue;
                }
                if (sameTarget && ship.CurrentOrder == UnitOrderType.Move && distance > radius)
                    continue;
                bool arrived = ship.CurrentOrder == UnitOrderType.None && distance <= radius;
                if (sameTarget && _assignedShips.Contains(ship) &&
                    (arrived || ship.CurrentOrder == UnitOrderType.AttackMove))
                    continue;
                approaching.Add(context.Receivers[ship]);
            }
            if (approaching.Count > 0) _unitOrderService.IssueAttackMove(approaching, context.CaptureTarget);
            if (approaching.Count > 0)
                ActionReason = sameTarget ? "Recovering idle or newly available ships outside the capture area." :
                    "New capture objective; approach with a bounded combat diversion.";
            else if (ActionReason == decision.Reason)
                ActionReason = "Retain capture assignment and active engagement; arrived ships remain in the circle.";
            _assignedShips.Clear();
            _assignedShips.UnionWith(_body);
            RallyStragglers(context, anchor);
            StopRemaining(context, _captureShips, count);
        }

        public void Rally(EnemyStrategicContext context)
        {
            if (context.Ships.Count == 0) return;
            IShipEntity anchor = SplitBody(context.Ships, context.Ships.Count);
            RallyBody(context, anchor);
            ActionReason = $"Assembling main force: {_body.Count}/{context.Ships.Count} ships in the body.";
        }

        private void RallyBody(EnemyStrategicContext context, IShipEntity anchor)
        {
            if (anchor.CurrentOrder != UnitOrderType.None)
                _unitOrderService.IssueStop(new[] { context.Receivers[anchor] });
            _supportShips.Clear();
            foreach (IShipEntity ship in _body)
                if (ship != anchor) _supportShips.Add(ship);
            GuardSupport(context, _supportShips, anchor);
            RallyStragglers(context, anchor, releaseBody: false);
        }

        private void ExecuteRetreat(EnemyStrategicContext context)
        {
            ActionReason = context.RecoveryReason;
            if (!context.Snapshot.HasSafeRetreat)
            {
                StopRemaining(context, context.Ships, 0);
                return;
            }
            bool anchorChanged = _retreatAnchor != context.RetreatAnchor;
            _retreatAnchor = context.RetreatAnchor;
            List<GameEntity> receivers = new List<GameEntity>();
            foreach (IShipEntity ship in context.Ships)
            {
                bool arrived = _assignedShips.Contains(ship) && ship.CurrentOrder == UnitOrderType.None &&
                    (!context.RetreatAnchor.HasValue ||
                     PlanarDistance(ship.WorldPosition, context.RetreatAnchor.Value) <= FleetCohesion.RADIUS);
                if (!anchorChanged && (ship.CurrentOrder == UnitOrderType.Retreat || arrived)) continue;
                receivers.Add(context.Receivers[ship]);
            }
            if (receivers.Count > 0)
            {
                if (context.RetreatAnchor.HasValue)
                    _unitOrderService.IssueRetreat(receivers, context.RetreatAnchor.Value);
                else _unitOrderService.IssueRetreat(receivers);
            }
            foreach (IShipEntity ship in context.Ships) _assignedShips.Add(ship);
        }

        private static float PlanarDistance(Vector3 from, Vector3 to)
        {
            Vector3 offset = from - to;
            offset.y = 0f;
            return offset.magnitude;
        }

        private void IssueAttackOrHunt(EnemyStrategicContext context,
            int committed, GameEntity target, bool allowHunt = true, bool assemble = true)
        {
            int count = Math.Min(committed, context.Ships.Count);
            if (target == null && !allowHunt)
            {
                _attackForce.Clear();
                StopRemaining(context, context.Ships, 0);
                return;
            }
            IReadOnlyList<IShipEntity> attackers = context.Ships;
            bool targeted = assemble && target != null;
            if (targeted)
            {
                _attackForce.Assemble(context, target, count);
                attackers = _attackForce.Ships;
                count = attackers.Count;
                if (count == 0)
                {
                    ActionReason = "No available ship can damage the target or its defenders.";
                    StopRemaining(context, context.Ships, 0);
                    return;
                }
            }
            else _attackForce.Clear();
            IShipEntity anchor = SplitBody(attackers, count);
            bool ready = !targeted || _body.Count * 4 >= count * 3 && _attackForce.IsReady(context, _body);
            if (targeted && ready)
                foreach (IShipEntity ship in _body)
                    if (_attackForce.TargetDps(context, ship) > _attackForce.TargetDps(context, anchor)) anchor = ship;
            RallyStragglers(context, anchor, releaseBody: !targeted);
            if (ready)
            {
                bool sameBattleTarget = _battleTarget == target;
                _strikeShips.Clear();
                _supportShips.Clear();
                if (targeted) _attackForce.AssignScreens(context, _body, anchor);
                foreach (IShipEntity ship in _body)
                    if (!targeted || !_attackForce.IsScreen(ship)) _strikeShips.Add(ship);
                    else _supportShips.Add(ship);
                List<GameEntity> receivers = Resolve(context, _strikeShips, _strikeShips.Count);
                if (target != null)
                    _unitOrderService.IssueAttack(receivers, target, BattleOffsets(_strikeShips, _strikeShips.Count, target));
                else _unitOrderService.IssueHunt(receivers);
                if (_supportShips.Count > 0)
                {
                    List<GameEntity> screens = new List<GameEntity>();
                    foreach (IShipEntity ship in _supportShips)
                        if (!sameBattleTarget || ship.CurrentOrder != UnitOrderType.AttackMove)
                            screens.Add(context.Receivers[ship]);
                    if (screens.Count > 0)
                        _unitOrderService.IssueAttackMove(screens,
                            target.GetFacade<IEntityTransformFacade>().Transform.position);
                }
            }
            else
            {
                // The anchor waits while required counters assemble instead of attacking piecemeal.
                if (anchor.CurrentOrder != UnitOrderType.None)
                    _unitOrderService.IssueStop(new[] { context.Receivers[anchor] });
                _supportShips.Clear();
                foreach (IShipEntity ship in _body)
                    if (ship != anchor) _supportShips.Add(ship);
                GuardSupport(context, _supportShips, anchor);
            }
            if (targeted)
            {
                int screeningCount = ready ? _supportShips.Count : 0;
                _supportShips.Clear();
                foreach (IShipEntity ship in context.Ships)
                    if (!_attackForce.Ships.Contains(ship)) _supportShips.Add(ship);
                GuardSupport(context, _supportShips, anchor);
                ActionReason = ready
                    ? $"Target-specific force: {_strikeShips.Count} strike ships; {screeningCount} active screens; {_stragglers.Count} rallying; {_supportShips.Count} reserve."
                    : $"Assembling target counters: {_body.Count}/{count} ships in the body; {_supportShips.Count} reserve.";
            }
            else StopRemaining(context, context.Ships, count);
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
        private void RallyStragglers(EnemyStrategicContext context, IShipEntity anchor, bool releaseBody = true)
        {
            if (releaseBody)
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

        private void GuardSupport(EnemyStrategicContext context, IReadOnlyList<IShipEntity> ships, IShipEntity anchor)
        {
            List<GameEntity> receivers = new List<GameEntity>();
            foreach (IShipEntity ship in ships)
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
