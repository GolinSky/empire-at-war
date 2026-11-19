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
using EmpireAtWar.Services.TacticalMarkers;
using EmpireAtWar.Components.Hangar;

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
        private const float CAPTURE_APPROACH_STALL_SECONDS = 5f;

        private readonly IUnitOrderService _unitOrderService;
        private GameEntity _battleTarget;
        private GameEntity _defenseStation;
        private const float INTERCEPT_DISTANCE = 1500f;
        private const float NO_FIRING_SECONDS = 8f;
        private const float CAPTURE_EDGE_FRACTION = 0.9f;
        private const float PAD_ENTRY_FRACTION = 0.7f;
        private const int READY_START_QUARTERS = 3;
        private const int READY_KEEP_QUARTERS = 2;
        private const float ANCHOR_DPS_SWITCH_FACTOR = 1.25f;
        private const float DEFENSE_POINT_CHANGE_SQUARED = 200f * 200f;
        private readonly Dictionary<IShipEntity, Vector3> _defensePoints = new Dictionary<IShipEntity, Vector3>();
        private readonly Dictionary<IShipEntity, GameEntity> _defenseTargets = new Dictionary<IShipEntity, GameEntity>();
        private float _noFiringSince = -1f;
        private float _defenseAdvance;
        private float _defenseThreatRange;
        private readonly EnemyAttackTaskForce _attackForce = new EnemyAttackTaskForce();
        private readonly List<IShipEntity> _supportShips = new List<IShipEntity>();
        private readonly List<IShipEntity> _strikeShips = new List<IShipEntity>();
        private readonly Dictionary<IShipEntity, GameEntity> _strikeTargets = new Dictionary<IShipEntity, GameEntity>();

        private readonly List<IShipEntity> _captureShips = new List<IShipEntity>();
        private readonly List<IShipEntity> _defenseShips = new List<IShipEntity>();
        private Comparison<IShipEntity> _byDefenseDistance;
        private Vector3 _defenseCenter;
        private readonly List<FormationPoint> _positions = new List<FormationPoint>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<FormationPoint> _destinations = new List<FormationPoint>();
        private readonly Dictionary<IShipEntity, FormationPoint> _battleOffsets =
            new Dictionary<IShipEntity, FormationPoint>();

        private Vector3 _captureTarget;

        private bool _hasCaptureTarget;
        private EnemyCaptureObjective _captureObjective;
        private readonly Dictionary<IShipEntity, (float Distance, float ImprovedAt)> _captureApproaches =
            new Dictionary<IShipEntity, (float, float)>();
        private readonly Dictionary<IShipEntity, Vector3> _captureEscortPoints = new Dictionary<IShipEntity, Vector3>();
        private Vector3? _retreatAnchor;
        private Vector3? _guardPosition;
        public string ActionReason { get; private set; } = "";
        private EnemyStrategicState _lastState;
        private readonly HashSet<IShipEntity> _assignedShips = new HashSet<IShipEntity>();
        private readonly List<IShipEntity> _body = new List<IShipEntity>();
        private readonly List<IShipEntity> _stragglers = new List<IShipEntity>();
        private readonly List<FormationPoint> _cohesionPoints = new List<FormationPoint>();
        private readonly HashSet<IShipEntity> _bodyMembers = new HashSet<IShipEntity>();
        private readonly Dictionary<IShipEntity, IShipEntity> _rallyAnchors = new Dictionary<IShipEntity, IShipEntity>();
        private readonly List<IShipEntity> _lostShips = new List<IShipEntity>();
        private long _markerId = -1;
        private Vector3 _markerPoint;
        private readonly HashSet<IShipEntity> _markerShips = new HashSet<IShipEntity>();

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

        public void Execute(EnemyStrategicDecision decision, EnemyStrategicContext context, float? now = null)
        {
            _markerId = -1;
            _markerShips.Clear();
            if (_lastState != decision.State)
            {
                _assignedShips.Clear();
                _rallyAnchors.Clear();
                _captureApproaches.Clear();
                _hasCaptureTarget = false;
                _defenseStation = null;
                _defensePoints.Clear();
                _defenseTargets.Clear();
                _noFiringSince = -1f;
                _defenseAdvance = 0f;
                _attackForce.Clear();
                _strikeTargets.Clear();
            }
            _lastState = decision.State;
            ActionReason = decision.Reason;
            _body.Clear();
            _stragglers.Clear();
            _assignedShips.RemoveWhere(ship => !context.Receivers.ContainsKey(ship));
            _bodyMembers.RemoveWhere(ship => !context.Receivers.ContainsKey(ship));
            ForgetLostRallies(context);
            _lostShips.Clear();
            foreach (IShipEntity ship in _captureApproaches.Keys)
                if (!context.Receivers.ContainsKey(ship)) _lostShips.Add(ship);
            foreach (IShipEntity ship in _lostShips) _captureApproaches.Remove(ship);
            _lostShips.Clear();
            foreach (IShipEntity ship in _strikeTargets.Keys)
                if (!context.Receivers.ContainsKey(ship)) _lostShips.Add(ship);
            foreach (IShipEntity ship in _lostShips) _strikeTargets.Remove(ship);
            if (context.Ships.Count == 0) return;
            switch (decision.State)
            {
                case EnemyStrategicState.CaptureZone:
                    ExecuteCapture(decision, context, now ?? Time.time);
                    return;
                case EnemyStrategicState.HuntFleet:
                    IssueAttackOrHunt(context, decision.CommittedShipCount, context.EnemyFleetTarget);
                    return;
                case EnemyStrategicState.AssaultBase:
                    IssueAttackOrHunt(context, decision.CommittedShipCount, context.EnemyBaseTarget, allowHunt: false);
                    return;
                case EnemyStrategicState.EngageLocal:
                    ExecuteLocalEngagement(decision, context);
                    return;
                case EnemyStrategicState.GuardSite:
                    ExecuteSiteGuard(context);
                    return;
                case EnemyStrategicState.RaidStructures:
                    IssueAttackOrHunt(context, decision.CommittedShipCount, context.RaidTarget, allowHunt: false);
                    return;
                case EnemyStrategicState.DefendBase:
                    ExecuteDefense(decision, context);
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

        private void ExecuteLocalEngagement(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            GameEntity target = context.LocalThreatTarget;
            if (target == null || context.Snapshot.LocalAdvantage >
                EnemyAiDifficultyProfile.Get(context.Snapshot.Difficulty).RetreatAdvantage)
            {
                IssueAttackOrHunt(context, decision.CommittedShipCount, target, allowHunt: false, assemble: false);
                return;
            }

            _body.AddRange(context.Ships);
            _strikeShips.Clear();
            _strikeShips.AddRange(context.Ships);
            IssueStrikeAttack(context, target);
        }

        private void ExecuteSiteGuard(EnemyStrategicContext context)
        {
            Vector3 point = context.GuardPosition.Value;
            bool samePoint = _guardPosition == point;
            _guardPosition = point;
            context.OrderDestination = point;
            _body.AddRange(context.Ships);
            var receivers = new List<GameEntity>();
            foreach (IShipEntity ship in context.Ships)
            {
                bool insideGuardArea = PlanarDistance(ship.WorldPosition, point) <= FleetCohesion.RADIUS;
                bool alreadyGuarding = _assignedShips.Contains(ship) && samePoint &&
                    (ship.CurrentOrder == UnitOrderType.AttackMove ||
                     insideGuardArea && ship.CurrentOrder is UnitOrderType.None or UnitOrderType.Attack);
                if (!alreadyGuarding) receivers.Add(context.Receivers[ship]);
                _assignedShips.Add(ship);
            }
            if (receivers.Count > 0) _unitOrderService.IssueAttackMove(receivers, point);
        }

        private void ExecuteDefense(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            ActionReason = context.StationPressureReason;
            if (context.OwnBase == null)
            {
                IssueAttackOrHunt(context, decision.CommittedShipCount, context.DefenseTarget, allowHunt: false, assemble: false);
                return;
            }
            if (_defenseStation != context.OwnBase)
            {
                _defenseStation = context.OwnBase;
                _defenseThreatRange = 0f;
                _defensePoints.Clear();
                _defenseTargets.Clear();
                _defenseAdvance = 0f;
                _noFiringSince = -1f;
            }
            Vector3 center = context.OwnBase.GetFacade<IEntityTransformFacade>().Transform.position;
            float stationRadius = context.OwnBase.GetFacade<INavigationFootprintFacade>().NavigationRadius;
            GameEntity target = context.DefenseTarget;
            if (target != null && target.TryGetFacade(out IUnitCombatObserverFacade threatCombat))
                _defenseThreatRange = threatCombat.WeaponRange;
            float leash = stationRadius + Mathf.Max(INTERCEPT_DISTANCE, _defenseThreatRange + 300f);
            EnemyStationPressureTracker pressure = context.StationPressure;
            Vector3 known = new Vector3(pressure.ThreatX, pressure.ThreatY, pressure.ThreatZ);
            Vector3 bearing = known - center;
            bearing.y = 0f;
            float gap = bearing.magnitude;
            bool hasBearing = pressure.HasThreatPosition && gap > 0f;
            if (hasBearing) bearing /= gap;
            // The ships nearest the station answer first; the rest keep their current fight.
            List<IShipEntity> ships = _defenseShips;
            ships.Clear();
            ships.AddRange(context.Ships);
            _defenseCenter = center;
            _byDefenseDistance ??= CompareDefenseDistance;
            ships.Sort(_byDefenseDistance);
            int count = Math.Min(decision.CommittedShipCount, ships.Count);
            bool firing = false;
            for (int i = 0; i < count; i++)
            {
                GameEntity entity = context.Receivers[ships[i]];
                if (!entity.TryGetFacade(out IUnitCombatObserverFacade combat) || combat.Target == null) continue;
                Vector3 targetPosition = combat.Target.GetFacade<IEntityTransformFacade>().Transform.position;
                firing |= PlanarDistance(ships[i].WorldPosition, targetPosition) <= combat.WeaponRange;
            }
            if (firing || Time.time - pressure.LastDamageTime > 5f || !hasBearing) _noFiringSince = -1f;
            else if (_noFiringSince < 0f) _noFiringSince = Time.time;
            else if (Time.time - _noFiringSince >= NO_FIRING_SECONDS)
            {
                _defenseAdvance = leash;
                _noFiringSince = Time.time;
            }
            bool intercept = target != null && PlanarDistance(center,
                target.GetFacade<IEntityTransformFacade>().Transform.position) <= leash;
            if (intercept) _attackForce.Assemble(context, target, count);
            else _attackForce.Clear();
            List<Vector3> interceptOffsets = intercept ? BattleOffsets(ships, count, target) : null;
            context.DefensePointReason = hasBearing
                ? _defenseAdvance > 0f ? "Advance toward known threat: station damaged without defenders in range." :
                    "Threat-facing defense line from visible or last known intel."
                : "No known threat bearing; defend four points around the station.";
            var defenseReceivers = new List<GameEntity>();
            var defensePoints = new List<Vector3>();
            bool changedDefense = false;
            for (int i = 0; i < count; i++)
            {
                IShipEntity ship = ships[i];
                if (intercept && _attackForce.Ships.Contains(ship) && _attackForce.TargetDps(context, ship) > 0f)
                {
                    if (!_defenseTargets.TryGetValue(ship, out GameEntity previous) || previous != target)
                        _unitOrderService.IssueAttack(new[] { context.Receivers[ship] }, target,
                            new[] { interceptOffsets[i] });
                    _defenseTargets[ship] = target;
                    _defensePoints.Remove(ship);
                    context.OrderDestination = target.GetFacade<IEntityTransformFacade>().Transform.position;
                    continue;
                }
                float range = context.Receivers[ship].GetFacade<IUnitCombatObserverFacade>().WeaponRange;
                float distance = Mathf.Min(leash, stationRadius + ship.NavigationRadius +
                    (hasBearing ? Mathf.Min(range, 0.6f * gap) : 200f));
                if (hasBearing && _defenseAdvance > 0f)
                    distance = Mathf.Min(leash, Mathf.Max(distance, gap - range + ship.NavigationRadius));
                float slotAngle = count <= 5 ? (i - (count - 1) * 0.5f) * 15f :
                    Mathf.Lerp(-30f, 30f, i / (float)(count - 1));
                Vector3 direction = hasBearing ? Quaternion.Euler(0f, slotAngle, 0f) * bearing :
                    new Vector3(Mathf.Cos(i % 4 * Mathf.PI * 0.5f), 0f, Mathf.Sin(i % 4 * Mathf.PI * 0.5f));
                Vector3 point = center + direction * distance;
                bool clear = false;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    float angle = (attempt % 2 == 0 ? 1f : -1f) * ((attempt + 1) / 2) * 15f;
                    point = center + Quaternion.Euler(0f, angle, 0f) * direction * distance;
                    if (!context.IsDefensePointClear(point, ship.NavigationRadius)) continue;
                    clear = true;
                    break;
                }
                if (!clear)
                {
                    if (_defenseTargets.Remove(ship))
                        _unitOrderService.IssueStop(new[] { context.Receivers[ship] });
                    context.DefensePointReason = "No clear defense point on the threat-facing arc; stop interception and retain safe positions.";
                    continue;
                }
                if (!_defensePoints.TryGetValue(ship, out Vector3 previousPoint) ||
                    (previousPoint - point).sqrMagnitude > DEFENSE_POINT_CHANGE_SQUARED)
                {
                    changedDefense = true;
                    _defensePoints[ship] = point;
                }
                _defenseTargets.Remove(ship);
                defenseReceivers.Add(context.Receivers[ship]);
                defensePoints.Add(_defensePoints[ship]);
                context.OrderDestination = _defensePoints[ship];
            }
            if (changedDefense)
                _unitOrderService.IssueAttackMove(defenseReceivers, defensePoints);
            ReleaseUncommittedDefenders(context, ships, count);
            ActionReason += "; " + context.DefensePointReason;
        }

        // Ships outside the defense detachment keep a fight they are in (attack, attack-move, hunt). Former defenders
        // and ships on stale non-combat orders stop, so nothing walks off on an order no state manages any more.
        private void ReleaseUncommittedDefenders(EnemyStrategicContext context, IReadOnlyList<IShipEntity> ships,
            int count)
        {
            // A new list per stop: order listeners may keep the receivers they were given.
            List<GameEntity> released = null;
            for (int i = count; i < ships.Count; i++)
            {
                IShipEntity ship = ships[i];
                bool wasDefender = _defensePoints.Remove(ship) | _defenseTargets.Remove(ship);
                UnitOrderType order = ship.CurrentOrder;
                bool inFight = order == UnitOrderType.Attack || order == UnitOrderType.AttackMove ||
                               order == UnitOrderType.Hunt;
                if (order != UnitOrderType.None && (wasDefender || !inFight))
                    (released ??= new List<GameEntity>()).Add(context.Receivers[ship]);
            }
            if (released != null) _unitOrderService.IssueStop(released);
        }

        private int CompareDefenseDistance(IShipEntity first, IShipEntity second) =>
            PlanarDistance(first.WorldPosition, _defenseCenter).CompareTo(PlanarDistance(second.WorldPosition, _defenseCenter));

        private void ExecuteCapture(EnemyStrategicDecision decision, EnemyStrategicContext context, float now)
        {
            _captureShips.Clear();
            _captureShips.AddRange(context.Ships);
            _captureShips.Sort((a, b) => a.NavigationRadius != b.NavigationRadius
                ? a.NavigationRadius.CompareTo(b.NavigationRadius)
                : b.NavigationSpeed.CompareTo(a.NavigationSpeed));
            int count = Math.Min(decision.CommittedShipCount, _captureShips.Count);
            EnemyCaptureObjective objective = context.CaptureObjective;
            bool sameObjective = objective == null ? _captureObjective == null :
                _captureObjective != null && objective.Id == _captureObjective.Id &&
                objective.Kind == _captureObjective.Kind && objective.Owner == _captureObjective.Owner &&
                objective.BlockingFacilityId == _captureObjective.BlockingFacilityId;
            bool sameTarget = _hasCaptureTarget && sameObjective &&
                (_captureTarget - context.CaptureTarget).sqrMagnitude <= CAPTURE_TARGET_EPSILON_SQUARED;
            if (!sameTarget)
            {
                _assignedShips.Clear();
                _captureApproaches.Clear();
                _captureEscortPoints.Clear();
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
            if (context.CaptureBlockingFacility is GameEntity facility && !facility.HealthModel.IsDestroyed)
            {
                var attackers = new List<GameEntity>();
                foreach (IShipEntity ship in _body)
                {
                    GameEntity receiver = context.Receivers[ship];
                    if (!sameTarget || !_assignedShips.Contains(ship) || ship.CurrentOrder != UnitOrderType.Attack ||
                        receiver.GetFacade<IUnitOrderObserverFacade>().AttackTarget != facility)
                        attackers.Add(receiver);
                }
                if (attackers.Count > 0) _unitOrderService.IssueAttack(attackers, facility);
                ActionReason = "Clear the hostile mine before capturing the site.";
                FinishCaptureAssignments(context, anchor, count);
                return;
            }
            var captureGroups = new Dictionary<Vector3, List<GameEntity>>();
            var changedPoints = new HashSet<Vector3>();
            float radius = context.CaptureObjective == null ? 1f : context.CaptureObjective.Radius;
            // A satellite pad is captured by presence in its capture range, not by reaching its centre: the force
            // stops just inside the circle on the side it arrives from, next to the pad's guns.
            Vector3 approachPoint = _captureTarget;
            if (objective != null && objective.Kind == EnemyCaptureObjectiveType.Facility && !objective.IsMine)
            {
                Vector3 fromPad = anchor.WorldPosition - _captureTarget;
                fromPad.y = 0f;
                if (fromPad.sqrMagnitude > Mathf.Epsilon)
                    approachPoint = _captureTarget + fromPad.normalized * radius * PAD_ENTRY_FRACTION;
            }
            int escortIndex = 0;
            foreach (IShipEntity ship in _body)
            {
                Vector3 capturePoint = approachPoint;
                bool fits = context.IsDefensePointClear(capturePoint, ship.NavigationRadius);
                for (int sample = 0; !fits && sample < 4; sample++)
                {
                    float angle = sample * Mathf.PI * 0.5f;
                    capturePoint = _captureTarget + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius * 0.5f;
                    fits = context.IsDefensePointClear(capturePoint, ship.NavigationRadius);
                }
                if (!fits)
                {
                    float angle = escortIndex++ * Mathf.PI * 0.5f;
                    Vector3 point = _captureTarget + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) *
                        (radius + ship.NavigationRadius);
                    bool retain = sameTarget && _captureEscortPoints.TryGetValue(ship, out Vector3 previous) &&
                        (previous - point).sqrMagnitude <= CAPTURE_TARGET_EPSILON_SQUARED &&
                        (ship.CurrentOrder == UnitOrderType.AttackMove ||
                         ship.CurrentOrder == UnitOrderType.None && (ship.WorldPosition - point).sqrMagnitude <=
                         ship.NavigationRadius * ship.NavigationRadius);
                    if (!retain) _unitOrderService.IssueAttackMove(new[] { context.Receivers[ship] }, point);
                    _captureEscortPoints[ship] = point;
                    continue;
                }
                bool wasEscort = _captureEscortPoints.Remove(ship);
                Vector3 offset = ship.WorldPosition - _captureTarget;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (!sameTarget || wasEscort || !_assignedShips.Contains(ship) || distance <= radius ||
                    !_captureApproaches.TryGetValue(ship, out var approach) || distance + 1f < approach.Distance)
                {
                    approach = (distance, now);
                    _captureApproaches[ship] = approach;
                }
                float leash = Math.Max(FleetCohesion.RADIUS, radius + ship.NavigationRadius * 2f);
                bool diverted = distance > approach.Distance + leash;
                bool stalled = distance > radius && now - approach.ImprovedAt >= CAPTURE_APPROACH_STALL_SECONDS;
                if (sameTarget && !wasEscort && ship.CurrentOrder == UnitOrderType.AttackMove && (diverted || stalled))
                {
                    // Stay in combat posture: an attack-move to the circle edge, never a Move that ignores hostiles.
                    Vector3 edgeDirection = offset.sqrMagnitude > Mathf.Epsilon ? offset.normalized : Vector3.right;
                    _unitOrderService.IssueAttackMove(new[] { context.Receivers[ship] },
                        _captureTarget + edgeDirection * radius * CAPTURE_EDGE_FRACTION);
                    ActionReason = stalled ? "Capture approach stalled outside the objective; move into the circle." :
                        "Capture diversion exceeded its leash; return to the objective.";
                    continue;
                }
                if (!captureGroups.TryGetValue(capturePoint, out List<GameEntity> receivers))
                {
                    receivers = new List<GameEntity>();
                    captureGroups.Add(capturePoint, receivers);
                }
                receivers.Add(context.Receivers[ship]);
                bool arrived = ship.CurrentOrder == UnitOrderType.None && distance <= radius;
                if (sameTarget && !wasEscort && _assignedShips.Contains(ship) &&
                    (arrived || ship.CurrentOrder == UnitOrderType.AttackMove))
                    continue;
                changedPoints.Add(capturePoint);
            }
            foreach (Vector3 point in changedPoints)
                _unitOrderService.IssueAttackMove(captureGroups[point], point);
            if (changedPoints.Count > 0)
                ActionReason = sameTarget ? "Recovering idle or newly available ships outside the capture area." :
                    "New capture objective; approach with a bounded combat diversion.";
            else if (ActionReason == decision.Reason)
                ActionReason = "Retain capture assignment and active engagement; arrived ships remain in the circle.";
            FinishCaptureAssignments(context, anchor, count);
        }

        private void FinishCaptureAssignments(EnemyStrategicContext context, IShipEntity anchor, int count)
        {
            _assignedShips.Clear();
            _assignedShips.UnionWith(_body);
            RallyStragglers(context, anchor);
            StopRemaining(context, _captureShips, count);
        }

        /// <param name="attackTarget">Hostile unit of an Attack marker; null attacks-moves to the point.</param>
        public void ExecuteMarker(TacticalMarker marker,
            GameEntity attackTarget, EnemyStrategicContext context)
        {
            // A unit target is chased by its own order, so its movement does not need fresh orders.
            if (_markerId != marker.Id || attackTarget == null && (_markerPoint - marker.Point).sqrMagnitude > 1f)
                _markerShips.Clear();
            _markerId = marker.Id;
            _markerPoint = marker.Point;
            _body.Clear();
            _stragglers.Clear();
            var receivers = new List<GameEntity>();
            foreach (IShipEntity ship in context.Ships)
            {
                if (marker.Type == TacticalMarkerType.Retreat &&
                    !AllyAiMarkerSource.Near(ship.WorldPosition, marker.Point)) continue;
                if (!_markerShips.Add(ship)) continue;
                receivers.Add(context.Receivers[ship]);
            }
            if (receivers.Count == 0) return;
            ActionReason = $"Accepted allied {marker.Type} request.";
            context.OrderDestination = marker.Point;
            switch (marker.Type)
            {
                case TacticalMarkerType.Retreat:
                    _unitOrderService.IssueRetreat(receivers, context.RetreatAnchor.Value);
                    break;
                case TacticalMarkerType.Attack when attackTarget != null:
                    _unitOrderService.IssueAttack(receivers, attackTarget);
                    break;
                case TacticalMarkerType.Attack:
                case TacticalMarkerType.Defend:
                    _unitOrderService.IssueAttackMove(receivers, marker.Point);
                    break;
                default:
                    _unitOrderService.IssueMove(receivers, marker.Point);
                    break;
            }
        }

        public void Rally(EnemyStrategicContext context)
        {
            _markerId = -1;
            _markerShips.Clear();
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
            // A force that was ready for this target keeps attacking while most of the body stays together.
            int requiredQuarters = _wasReady && _readyTarget == target ? READY_KEEP_QUARTERS : READY_START_QUARTERS;
            bool ready = !targeted || _body.Count * 4 >= count * requiredQuarters && _attackForce.IsReady(context, _body);
            _wasReady = ready && targeted;
            _readyTarget = target;
            if (targeted && ready)
            {
                float anchorDps = _attackForce.TargetDps(context, anchor);
                foreach (IShipEntity ship in _body)
                {
                    float dps = _attackForce.TargetDps(context, ship);
                    if (dps <= anchorDps * ANCHOR_DPS_SWITCH_FACTOR) continue;
                    anchor = ship;
                    anchorDps = dps;
                }
                _heldAnchor = anchor;
            }
            RallyStragglers(context, anchor, releaseBody: !targeted);
            if (ready)
            {
                bool sameBattleTarget = _battleTarget == target;
                _strikeShips.Clear();
                _supportShips.Clear();
                if (targeted) _attackForce.AssignScreens(context, _body, anchor);
                // A carrier never screens: an attack-move would fly it onto the target, while an attack order
                // keeps it at standoff and hands the target to its guarding bombers.
                foreach (IShipEntity ship in _body)
                    if (!targeted || !_attackForce.IsScreen(ship) || IsCarrier(context, ship)) _strikeShips.Add(ship);
                    else _supportShips.Add(ship);
                foreach (IShipEntity ship in _supportShips) _strikeTargets.Remove(ship);
                if (target != null) IssueStrikeAttack(context, target);
                else
                {
                    _strikeTargets.Clear();
                    _unitOrderService.IssueHunt(Resolve(context, _strikeShips, _strikeShips.Count));
                }
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

        /// <summary>
        /// Attack orders go only to strike ships whose target, slot or active order changed. Re-sending an identical
        /// attack resets the ship's approach and makes its weapons re-sort their candidates.
        /// </summary>
        private void IssueStrikeAttack(EnemyStrategicContext context, GameEntity target)
        {
            List<Vector3> offsets = BattleOffsets(_strikeShips, _strikeShips.Count, target, out bool rebuilt);
            ApplyCarrierStandoff(context, target, offsets);
            List<GameEntity> receivers = new List<GameEntity>();
            List<Vector3> receiverOffsets = new List<Vector3>();
            for (int i = 0; i < _strikeShips.Count; i++)
            {
                IShipEntity ship = _strikeShips[i];
                if (!rebuilt && ship.CurrentOrder == UnitOrderType.Attack &&
                    _strikeTargets.TryGetValue(ship, out GameEntity previous) && previous == target) continue;
                _strikeTargets[ship] = target;
                receivers.Add(context.Receivers[ship]);
                receiverOffsets.Add(offsets[i]);
            }
            if (receivers.Count > 0) _unitOrderService.IssueAttack(receivers, target, receiverOffsets);
        }

        /// <summary>
        /// Carriers attack from the gun line's side of the target at their own weapon range, behind the gun ships,
        /// instead of a close battle-formation slot. The attack order keeps the target for their guarding bombers.
        /// </summary>
        private void ApplyCarrierStandoff(EnemyStrategicContext context, GameEntity target, List<Vector3> offsets)
        {
            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            Vector3 gunLine = Vector3.zero;
            int gunShips = 0;
            foreach (IShipEntity ship in _strikeShips)
            {
                if (IsCarrier(context, ship)) continue;
                gunLine += ship.WorldPosition;
                gunShips++;
            }
            for (int i = 0; i < _strikeShips.Count; i++)
            {
                IShipEntity ship = _strikeShips[i];
                if (!IsCarrier(context, ship)) continue;
                float weaponRange = context.Receivers[ship].GetFacade<IUnitCombatObserverFacade>().WeaponRange;
                // Without guns the attack state cannot judge range; the carrier keeps its formation slot.
                if (weaponRange <= 0f) continue;
                Vector3 side = (gunShips > 0 ? gunLine / gunShips : ship.WorldPosition) - targetPosition;
                side.y = 0f;
                if (side.sqrMagnitude <= Mathf.Epsilon) continue;
                float targetRadius = target.TryGetFacade(out INavigationFootprintFacade footprint)
                    ? footprint.NavigationRadius : 0f;
                offsets[i] = side.normalized * (targetRadius + ship.NavigationRadius + weaponRange);
            }
        }

        /// <summary>A ship whose hangar can still launch or still has squadrons out.</summary>
        private static bool IsCarrier(EnemyStrategicContext context, IShipEntity ship) =>
            context.Receivers[ship].TryGetFacade(out IHangarAirWingFacade airWing) &&
            (airWing.CanLaunch || airWing.LaunchedCount > 0);

        private List<Vector3> BattleOffsets(IReadOnlyList<IShipEntity> ships,
            int count, GameEntity target) => BattleOffsets(ships, count, target, out _);

        private List<Vector3> BattleOffsets(IReadOnlyList<IShipEntity> ships,
            int count, GameEntity target, out bool rebuilt)
        {
            bool rebuild = _battleTarget != target;
            if (rebuild) _battleOffsets.Clear();
            // A new target re-slots everyone; ships joining the same target only get slots of their own, so
            // ships already engaged keep their orders.
            _positions.Clear();
            _radii.Clear();
            _newSlotShips.Clear();
            for (int i = 0; i < count; i++)
            {
                if (_battleOffsets.ContainsKey(ships[i])) continue;
                _newSlotShips.Add(ships[i]);
                _positions.Add(ToPoint(ships[i].WorldPosition));
                _radii.Add(ships[i].NavigationRadius);
            }
            if (_newSlotShips.Count > 0)
            {
                BattleFormationModel.CalculateDestinations(_positions, _radii,
                    default, _destinations,
                    target.TryGetFacade(out INavigationFootprintFacade footprint)
                        ? footprint.NavigationRadius : 0f);
                for (int i = 0; i < _newSlotShips.Count; i++)
                    _battleOffsets.Add(_newSlotShips[i], _destinations[i]);
            }
            rebuilt = rebuild;
            _battleTarget = target;
            List<Vector3> offsets = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                FormationPoint offset = _battleOffsets[ships[i]];
                offsets.Add(ToVector(offset));
            }
            return offsets;
        }

        // Kept across decisions so anchored guard/attack orders are not re-issued by a one-ship fluctuation.
        private IShipEntity _heldAnchor;
        private bool _wasReady;
        private GameEntity _readyTarget;
        private readonly List<IShipEntity> _newSlotShips = new List<IShipEntity>();

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
            int heldIndex = -1;
            for (int i = 0; i < count; i++)
                if (ships[i] == _heldAnchor) heldIndex = i;
            int anchorIndex = FleetCohesion.FindAnchor(_cohesionPoints, FleetCohesion.RADIUS, heldIndex);
            _heldAnchor = ships[anchorIndex];
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
