using static EmpireAtWar.Utils.FormationConversion;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity;
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
        private readonly AiOrderDeduplicator _orderFilter;
        private readonly IFocusFireSlots _focusFire;
        private readonly HashSet<IShipEntity> _interceptors = new HashSet<IShipEntity>();
        private readonly List<IShipEntity> _interceptOrder = new List<IShipEntity>();
        /// <summary>Strike ships over the focus fire limit; they attack-move to the target instead (approach 5).</summary>
        private readonly List<IShipEntity> _overflowShips = new List<IShipEntity>();
        private readonly List<IShipEntity> _strikeOrder = new List<IShipEntity>();
        private readonly Dictionary<IShipEntity, GameEntity> _overflowTargets = new Dictionary<IShipEntity, GameEntity>();
        private readonly Dictionary<string, (List<GameEntity> Receivers, List<Vector3> Offsets)> _retargetReceivers =
            new Dictionary<string, (List<GameEntity>, List<Vector3>)>();
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
        /// <summary>
        /// A state change keeps the ships' assignments when the new order point lies this close to the last one (W49):
        /// ships keep their order and arrive instead of being sent to the same place again.
        /// </summary>
        public const float ORDER_POINT_KEEP_DISTANCE = 200f;
        private readonly Dictionary<IShipEntity, Vector3> _defensePoints = new Dictionary<IShipEntity, Vector3>();
        private readonly Dictionary<IShipEntity, GameEntity> _defenseTargets = new Dictionary<IShipEntity, GameEntity>();
        private float _noFiringSince = -1f;
        private float _defenseAdvance;
        private float _defenseThreatRange;
        private readonly EnemyAttackTaskForce _attackForce = new EnemyAttackTaskForce();
        private readonly List<IShipEntity> _committedShips = new List<IShipEntity>();
        private readonly List<IShipEntity> _pushShips = new List<IShipEntity>();
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
        /// <summary>Ships that have been inside the capture circle since the current capture target was set.</summary>
        private readonly HashSet<IShipEntity> _captureEntered = new HashSet<IShipEntity>();
        /// <summary>Gather point each uncommitted main-force ship was last sent to (W31).</summary>
        private readonly Dictionary<IShipEntity, Vector3> _holdPoints = new Dictionary<IShipEntity, Vector3>();
        private Vector3? _retreatAnchor;
        private readonly List<IShipEntity> _guardShips = new List<IShipEntity>();
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
        private GameEntity _markerTarget;
        private readonly HashSet<IShipEntity> _markerShips = new HashSet<IShipEntity>();
        private Vector3? _lastOrderPoint;

        // Orders pass a filter: the same order to the same target or point is not sent again (E9 churn), and a ship
        // fighting on its way keeps that fight (W66).
        public EnemyTaskForceExecutor(IUnitOrderService unitOrderService, IFocusFireSlots focusFire)
        {
            _focusFire = focusFire;
            _orderFilter = new AiOrderDeduplicator(unitOrderService);
            _unitOrderService = _orderFilter;
        }

        /// <summary>Orders this executor passed on since it was created, counted per ship (W57).</summary>
        public int IssuedOrderCount => _orderFilter.PassedOrders;

        /// <summary>
        /// The last Hold, RebuildFleet or rally sent the ships to the group's gather point: they leave the spawn zone on
        /// that order, so nothing else moves them out (W55).
        /// </summary>
        public bool HoldsAtGatherPoint { get; private set; }

        /// <summary>Ships committed with the main body by the last offensive order; 0 for other states.</summary>
        public int BodyCount => _body.Count;
        /// <summary>
        /// X9: the last capture pass held the force while its body assembled; the capture stall clock does not run
        /// meanwhile.
        /// </summary>
        public bool IsAssemblingCapture { get; private set; }

        /// <summary>Ships committed with the main body by the last order (W32 local rating origin).</summary>
        public IReadOnlyList<IShipEntity> Body => _body;

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
            // S6: joining an allied assault needs no capital strike group; the allied ships are the strike.
            IReadOnlyList<IShipEntity> capitals = decision.State == EnemyStrategicState.AssaultBase &&
                !context.Snapshot.JoinsAllyAssault ? context.AssaultCapitals : null;
            if (target == null) return true;
            return _attackForce.Assemble(context, target, decision.CommittedShipCount, capitals) &&
                (capitals == null || _attackForce.Ships.Count >= EnemyFactionModel.MINIMUM_ASSAULT_CAPITALS);
        }

        public void Execute(EnemyStrategicDecision decision, EnemyStrategicContext context, float? now = null)
        {
            _markerId = -1;
            _markerShips.Clear();
            HoldsAtGatherPoint = false;
            IsAssemblingCapture = false;
            ForgetEngagedEnRoute(context);
            // W49: a new state that sends the ships to the point they already hold or head for keeps their assignments.
            Vector3? orderPoint = GetOrderPoint(decision.State, context);
            bool samePoint = orderPoint.HasValue && _lastOrderPoint.HasValue &&
                PlanarDistance(orderPoint.Value, _lastOrderPoint.Value) <= ORDER_POINT_KEEP_DISTANCE;
            _lastOrderPoint = orderPoint;
            if (_lastState != decision.State && !samePoint)
            {
                _assignedShips.Clear();
                _rallyAnchors.Clear();
                _captureApproaches.Clear();
                _captureEntered.Clear();
                _holdPoints.Clear();
                _hasCaptureTarget = false;
                _defenseStation = null;
                _defensePoints.Clear();
                _defenseTargets.Clear();
                _noFiringSince = -1f;
                _defenseAdvance = 0f;
                _attackForce.Clear();
                _strikeTargets.Clear();
                // X3: a strike that ended keeps no readiness, anchor or battle slots for the next one.
                _wasReady = false;
                _readyTarget = null;
                _heldAnchor = null;
                _battleTarget = null;
                _battleOffsets.Clear();
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
            _captureEntered.RemoveWhere(ship => !context.Receivers.ContainsKey(ship));
            _lostShips.Clear();
            foreach (IShipEntity ship in _strikeTargets.Keys)
                if (!context.Receivers.ContainsKey(ship)) _lostShips.Add(ship);
            foreach (IShipEntity ship in _lostShips) _strikeTargets.Remove(ship);
            _lostShips.Clear();
            foreach (IShipEntity ship in _holdPoints.Keys)
                if (!context.Receivers.ContainsKey(ship)) _lostShips.Add(ship);
            foreach (IShipEntity ship in _lostShips) _holdPoints.Remove(ship);
            // X5: a ship that left the group keeps no order memory: back in the group, its orders are not dropped.
            _orderFilter.RetainOnly(context.Receivers.Values);
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
                    // S1a / S1b: clear the station's surroundings and fight on the way; S4 / S6: the end game and an
                    // allied assault send every ship, not only the capitals.
                    IssueAttackOrHunt(context, decision.CommittedShipCount, context.AssaultTarget ?? context.EnemyBaseTarget,
                        allowHunt: false, eligibleShips: IsWholeFleetAssault(context) ? null : context.AssaultCapitals,
                        keepTargets: true, pushTarget: context.EnemyBaseTarget);
                    return;
                case EnemyStrategicState.EngageLocal:
                    ExecuteLocalEngagement(decision, context);
                    return;
                case EnemyStrategicState.GuardSite:
                case EnemyStrategicState.Consolidate:
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
                    HoldsAtGatherPoint = context.GuardPosition.HasValue;
                    if (HoldsAtGatherPoint) context.OrderDestination = context.GuardPosition;
                    StopRemaining(context, context.Ships, 0);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(decision.State));
            }
        }

        private static bool IsWholeFleetAssault(EnemyStrategicContext context) =>
            context.Snapshot.IsEndGame || context.Snapshot.JoinsAllyAssault;

        /// <summary>
        /// Where a point-based state sends the ships: the capture target, the gather or guard point, or the retreat
        /// anchor. Target-based states (attack, hunt, defense, engage) have none.
        /// </summary>
        private static Vector3? GetOrderPoint(EnemyStrategicState state, EnemyStrategicContext context) => state switch
        {
            EnemyStrategicState.CaptureZone => context.CaptureTarget,
            EnemyStrategicState.GuardSite or EnemyStrategicState.Consolidate or EnemyStrategicState.Hold or
                EnemyStrategicState.RebuildFleet => context.GuardPosition,
            EnemyStrategicState.RetreatValue => context.RetreatAnchor,
            _ => null
        };

        /// <summary>
        /// A ship fighting a warship that hit it on its attack-move is not ordered (the order filter skips it, W66);
        /// its assignment is dropped so it is placed afresh once the fight ends.
        /// </summary>
        private void ForgetEngagedEnRoute(EnemyStrategicContext context)
        {
            foreach (IShipEntity ship in context.Ships)
                if (IsEngagingEnRoute(context, ship)) ForgetShip(ship);
        }

        /// <summary>
        /// X4: drops every per-ship assignment, so the next state places the ship afresh instead of trusting an order
        /// another path replaced. <paramref name="keepGathering"/> keeps the rally anchor and hold point that a rally
        /// itself manages.
        /// </summary>
        private void ForgetShip(IShipEntity ship, bool keepGathering = false)
        {
            _assignedShips.Remove(ship);
            if (!keepGathering)
            {
                _holdPoints.Remove(ship);
                _rallyAnchors.Remove(ship);
            }
            _strikeTargets.Remove(ship);
            _overflowTargets.Remove(ship);
            _captureApproaches.Remove(ship);
            _captureEntered.Remove(ship);
            _captureEscortPoints.Remove(ship);
            _defensePoints.Remove(ship);
            _defenseTargets.Remove(ship);
        }

        /// <summary>A group ship fighting on its way (W66, D3): the group keeps going and may ask for a reinforcement there.</summary>
        public bool TryFindEngagedEnRoute(EnemyStrategicContext context, out Vector3 position)
        {
            foreach (IShipEntity ship in context.Ships)
            {
                if (!IsEngagingEnRoute(context, ship)) continue;
                position = ship.WorldPosition;
                return true;
            }
            position = default;
            return false;
        }

        private static bool IsEngagingEnRoute(EnemyStrategicContext context, IShipEntity ship) =>
            context.Receivers[ship].GetFacade<IUnitOrderObserverFacade>().IsEngagingEnRoute;

        private void ExecuteLocalEngagement(EnemyStrategicDecision decision, EnemyStrategicContext context)
        {
            GameEntity target = context.LocalThreatTarget;
            if (target == null || context.Snapshot.LocalAdvantage >
                EnemyAiDifficultyProfile.Get(context.Snapshot.Difficulty).RetreatAdvantage)
            {
                // X2: the local fight commits every ship; none is sent away to the gather point.
                IssueAttackOrHunt(context, context.Ships.Count, target, allowHunt: false, assemble: false,
                    keepTargets: true);
                return;
            }

            _body.AddRange(context.Ships);
            _strikeShips.Clear();
            _strikeShips.AddRange(context.Ships);
            IssueStrikeAttack(context, target, keepTargets: true);
        }

        // Guarding and holding share one bookkeeping: a ship sent to the point by Hold keeps it under GuardSite (W49).
        private void ExecuteSiteGuard(EnemyStrategicContext context, List<IShipEntity> excluded = null)
        {
            _guardShips.Clear();
            foreach (IShipEntity ship in context.Ships)
            {
                if (excluded != null && excluded.Contains(ship)) continue;
                _body.Add(ship);
                _guardShips.Add(ship);
            }
            HoldAtPoint(context, _guardShips, 0, context.GuardPosition.Value);
            context.OrderDestination = context.GuardPosition;
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
            // Interceptors past the focus fire limit take a defense point instead.
            if (intercept) SelectInterceptors(context, target, ships, count);
            else _interceptors.Clear();
            List<Vector3> interceptOffsets = intercept ? BattleOffsets(ships, count, target) : null;
            context.DefensePointReason = hasBearing
                ? _defenseAdvance > 0f ? "Advance toward known threat: station damaged without defenders in range." :
                    "Threat-facing defense line from visible or last known intel."
                : "No known threat bearing; defend four points around the station.";
            var defenseReceivers = new List<GameEntity>();
            var defensePoints = new List<Vector3>();
            List<IShipEntity> unplaced = null;
            bool changedDefense = false;
            for (int i = 0; i < count; i++)
            {
                IShipEntity ship = ships[i];
                if (intercept && _interceptors.Contains(ship))
                {
                    // X6: an interceptor whose attack ended on the same target is sent again.
                    if (!_defenseTargets.TryGetValue(ship, out GameEntity previous) || previous != target ||
                        ship.CurrentOrder != UnitOrderType.Attack)
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
                    // X2: no clear point on the arc: hold at the gather point instead of stopping where it is.
                    if (_defenseTargets.Remove(ship) | _defensePoints.Remove(ship) || ship.CurrentOrder == UnitOrderType.None)
                        (unplaced ??= new List<IShipEntity>()).Add(ship);
                    context.DefensePointReason = "No clear defense point on the threat-facing arc; hold at the gather point.";
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
                using (ShipOrderTelemetry.Detail("path=defensePoint"))
                    _unitOrderService.IssueAttackMove(defenseReceivers, defensePoints);
            if (unplaced != null) StopRemaining(context, unplaced, 0);
            ReleaseUncommittedDefenders(context, ships, count);
            ActionReason += "; " + context.DefensePointReason;
        }

        /// <summary>
        /// The defenders that attack <paramref name="target"/>: armed members of the intercept force, up to
        /// <see cref="StrikeLimit"/>. Carriers and slot holders first, then last cycle's interceptors of this target,
        /// then the nearest to the station. Picking nearest-first let a non-holder take the attack while the holder
        /// still held the slot: the order service sent the newcomer onto an overflow attack-move, the holder fell back
        /// to a defense point, and the two swapped every decision (0.5–3 s in the 19:24 log).
        /// </summary>
        private void SelectInterceptors(EnemyStrategicContext context, GameEntity target, IReadOnlyList<IShipEntity> ships,
            int count)
        {
            _interceptors.Clear();
            _interceptOrder.Clear();
            for (int i = 0; i < count; i++)
                if (_attackForce.Ships.Contains(ships[i]) && _attackForce.TargetDps(context, ships[i]) > 0f)
                    _interceptOrder.Add(ships[i]);
            int limit = Math.Min(_interceptOrder.Count, StrikeLimit(context, target, ships, count));
            // Passes keep the station-distance order inside each rank.
            for (int pass = 0; pass < 4 && _interceptors.Count < limit; pass++)
                foreach (IShipEntity ship in _interceptOrder)
                {
                    if (_interceptors.Count >= limit) break;
                    if (_interceptors.Contains(ship) || InterceptPass(context, target, ship) != pass) continue;
                    _interceptors.Add(ship);
                }
        }

        // 0 carrier, 1 slot holder, 2 last cycle's interceptor of this target, 3 any other.
        private int InterceptPass(EnemyStrategicContext context, GameEntity target, IShipEntity ship)
        {
            int rank = StrikeRank(context, target, ship);
            if (rank < 2) return rank;
            return _defenseTargets.TryGetValue(ship, out GameEntity previous) && previous == target ? 2 : 3;
        }

        // Ships outside the defense detachment keep a fight they are in (attack, attack-move, hunt). Former defenders,
        // idle ships and ships on stale non-combat orders hold at the gather point (X2), so nothing walks off on an
        // order no state manages any more and nothing stands idle; without a gather point they stop.
        private void ReleaseUncommittedDefenders(EnemyStrategicContext context, IReadOnlyList<IShipEntity> ships,
            int count)
        {
            List<IShipEntity> released = null;
            for (int i = count; i < ships.Count; i++)
            {
                IShipEntity ship = ships[i];
                bool wasDefender = _defensePoints.Remove(ship) | _defenseTargets.Remove(ship);
                UnitOrderType order = ship.CurrentOrder;
                bool inFight = order == UnitOrderType.Attack || order == UnitOrderType.AttackMove ||
                               order == UnitOrderType.Hunt;
                if (wasDefender || !inFight) (released ??= new List<IShipEntity>()).Add(ship);
            }
            if (released != null) StopRemaining(context, released, 0);
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
            // A zone force was sized by the allocator; trimming it again by the commit ratio left ships idle (W31).
            int count = context.IsZoneForce ? _captureShips.Count :
                Math.Min(decision.CommittedShipCount, _captureShips.Count);
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
                _captureEntered.Clear();
                _captureEscortPoints.Clear();
            }
            _captureTarget = context.CaptureTarget;
            _captureObjective = objective;
            _hasCaptureTarget = true;
            IShipEntity anchor = SplitBody(_captureShips, count);
            if (count >= 6 && _body.Count * 4 < count * 3)
            {
                IsAssemblingCapture = true;
                RallyBody(context, anchor);
                HoldUncommitted(context, _captureShips, count, anchor);
                ActionReason = $"Assembling capture force: {_body.Count}/{count} ships in the body.";
                return;
            }
            if (context.CaptureBlockingFacility is GameEntity routed && !routed.HealthModel.IsDestroyed &&
                objective != null &&
                (context.CaptureTarget - objective.Position).sqrMagnitude > CAPTURE_TARGET_EPSILON_SQUARED)
            {
                // The straight line to the facility passes a hostile-held centre: go round it first.
                var movers = new List<GameEntity>();
                foreach (IShipEntity ship in _body)
                    movers.Add(context.Receivers[ship]);
                using (ShipOrderTelemetry.Detail("path=captureRoute"))
                    _unitOrderService.IssueAttackMove(movers, context.CaptureTarget);
                ActionReason = "Route around the hostile-held centre before attacking the blocking structure.";
                FinishCaptureAssignments(context, anchor, count);
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
                ActionReason = "Destroy the hostile mine or satellite before capturing the site.";
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
                    if (!retain)
                        using (ShipOrderTelemetry.Detail("path=captureEscort"))
                            _unitOrderService.IssueAttackMove(new[] { context.Receivers[ship] }, point);
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
                if (distance <= radius) _captureEntered.Add(ship);
                float leash = Math.Max(FleetCohesion.RADIUS, radius + ship.NavigationRadius * 2f);
                bool diverted = distance > approach.Distance + leash;
                // Only a ship that never reached the circle is stalled; one that arrived and went out to fight is
                // bounded by the leash alone.
                bool stalled = !_captureEntered.Contains(ship) && distance > radius &&
                               now - approach.ImprovedAt >= CAPTURE_APPROACH_STALL_SECONDS;
                if (sameTarget && !wasEscort && ship.CurrentOrder == UnitOrderType.AttackMove && (diverted || stalled))
                {
                    // Stay in combat posture: an attack-move to the circle edge, never a Move that ignores hostiles.
                    Vector3 edgeDirection = offset.sqrMagnitude > Mathf.Epsilon ? offset.normalized : Vector3.right;
                    using (ShipOrderTelemetry.Detail("path=captureEdge"))
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
                using (ShipOrderTelemetry.Detail("path=capture"))
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
            HoldUncommitted(context, _captureShips, count, anchor);
        }

        /// <summary>
        /// Main-force ships outside the committed share hold at the group's gather point (attack-move), or guard the
        /// body's anchor when the group has none: never order <c>None</c> away from a hold point (W31).
        /// </summary>
        private void HoldUncommitted(EnemyStrategicContext context, IReadOnlyList<IShipEntity> ships, int start,
            IShipEntity anchor)
        {
            _lostShips.Clear();
            foreach (IShipEntity ship in _holdPoints.Keys)
                if (!context.Receivers.ContainsKey(ship)) _lostShips.Add(ship);
            foreach (IShipEntity ship in _lostShips) _holdPoints.Remove(ship);
            for (int i = 0; i < start; i++) _holdPoints.Remove(ships[i]);
            if (start >= ships.Count) return;
            if (!context.GuardPosition.HasValue)
            {
                _supportShips.Clear();
                for (int i = start; i < ships.Count; i++)
                {
                    _holdPoints.Remove(ships[i]);
                    _supportShips.Add(ships[i]);
                }
                GuardSupport(context, _supportShips, anchor);
                return;
            }
            HoldAtPoint(context, ships, start, context.GuardPosition.Value);
        }

        /// <summary>
        /// Ships from <paramref name="start"/> on attack-move to <paramref name="point"/>; one that holds there already
        /// (on its way there, or arrived and idle or fighting) is left alone (W31, W55). A point moved less than
        /// <see cref="ORDER_POINT_KEEP_DISTANCE"/> counts as the same, so a ship keeps its order and arrives (W49).
        /// </summary>
        private void HoldAtPoint(EnemyStrategicContext context, IReadOnlyList<IShipEntity> ships, int start, Vector3 point,
            string path = "path=hold")
        {
            List<GameEntity> receivers = null;
            for (int i = start; i < ships.Count; i++)
            {
                IShipEntity ship = ships[i];
                _rallyAnchors.Remove(ship);
                bool holding = _holdPoints.TryGetValue(ship, out Vector3 previous) &&
                    PlanarDistance(previous, point) <= ORDER_POINT_KEEP_DISTANCE &&
                    (ship.CurrentOrder == UnitOrderType.AttackMove && _orderFilter.WasLastSent(context.Receivers[ship],
                         UnitOrderType.AttackMove, previous, AiOrderDeduplicator.SAME_POINT_DISTANCE) ||
                     ship.CurrentOrder is UnitOrderType.None or UnitOrderType.Attack &&
                     PlanarDistance(ship.WorldPosition, point) <= FleetCohesion.RADIUS + ship.NavigationRadius);
                if (holding) continue;
                // The point a ship was last sent to stays its hold point until it is sent again.
                _holdPoints[ship] = point;
                (receivers ??= new List<GameEntity>()).Add(context.Receivers[ship]);
            }
            if (receivers != null)
                using (ShipOrderTelemetry.Detail(path))
                    _unitOrderService.IssueAttackMove(receivers, point);
        }

        /// <param name="attackTarget">Hostile unit of an Attack marker; null attacks-moves to the point.</param>
        /// <param name="reason">Action reason; null for a marker an ally placed.</param>
        public void ExecuteMarker(TacticalMarker marker,
            GameEntity attackTarget, EnemyStrategicContext context, string reason = null)
        {
            // A unit target is chased by its own order, so its movement does not need fresh orders; a new target does.
            if (_markerId != marker.Id || !ReferenceEquals(_markerTarget, attackTarget) ||
                attackTarget == null && (_markerPoint - marker.Point).sqrMagnitude > 1f)
                _markerShips.Clear();
            _markerId = marker.Id;
            _markerPoint = marker.Point;
            _markerTarget = attackTarget;
            _body.Clear();
            _stragglers.Clear();
            var receivers = new List<GameEntity>();
            foreach (IShipEntity ship in context.Ships)
            {
                if (marker.Type == TacticalMarkerType.Retreat &&
                    !AllyAiMarkerSource.Near(ship.WorldPosition, marker.Point)) continue;
                // A ship fighting on its way takes the marker once its fight ends (W66).
                if (marker.Type != TacticalMarkerType.Retreat && IsEngagingEnRoute(context, ship)) continue;
                if (!_markerShips.Add(ship)) continue;
                ForgetShip(ship);
                receivers.Add(context.Receivers[ship]);
            }
            if (receivers.Count == 0) return;
            ActionReason = reason ?? $"Accepted allied {marker.Type} request.";
            context.OrderDestination = marker.Point;
            switch (marker.Type)
            {
                case TacticalMarkerType.Retreat:
                    _unitOrderService.IssueRetreat(receivers, context.RetreatAnchor.Value);
                    break;
                // S5: an allied station's defence attacks the known attacker instead of parking at the station.
                case TacticalMarkerType.Attack when attackTarget != null:
                case TacticalMarkerType.Defend when attackTarget != null:
                    IssueCappedMarkerAttack(context, receivers, attackTarget);
                    break;
                case TacticalMarkerType.Attack:
                case TacticalMarkerType.Defend:
                    using (ShipOrderTelemetry.Detail("path=marker"))
                        _unitOrderService.IssueAttackMove(receivers, marker.Point);
                    break;
                default:
                    _unitOrderService.IssueMove(receivers, marker.Point);
                    break;
            }
        }

        /// <summary>
        /// The main force gathers around its anchor ship. With a gather point the anchor attack-moves there and the rest
        /// guards it, so the force arrives together; it never stops away from the point (W55).
        /// </summary>
        public void Rally(EnemyStrategicContext context)
        {
            _markerId = -1;
            _markerShips.Clear();
            HoldsAtGatherPoint = context.GuardPosition.HasValue;
            if (context.Ships.Count == 0) return;
            ForgetEngagedEnRoute(context);
            foreach (IShipEntity ship in context.Ships) ForgetShip(ship, keepGathering: true);
            IShipEntity anchor = SplitBody(context.Ships, context.Ships.Count);
            RallyBody(context, anchor, context.GuardPosition);
            ActionReason = $"Assembling main force: {_body.Count}/{context.Ships.Count} ships in the body.";
        }

        private void RallyBody(EnemyStrategicContext context, IShipEntity anchor, Vector3? gatherPoint = null)
        {
            if (gatherPoint.HasValue)
            {
                _anchorHold.Clear();
                _anchorHold.Add(anchor);
                HoldAtPoint(context, _anchorHold, 0, gatherPoint.Value);
            }
            else if (anchor.CurrentOrder != UnitOrderType.None)
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
                     PlanarDistance(ship.WorldPosition, context.RetreatAnchor.Value) <=
                     FleetCohesion.RADIUS + ship.NavigationRadius);
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
            int committed, GameEntity target, bool allowHunt = true, bool assemble = true,
            IReadOnlyList<IShipEntity> eligibleShips = null, bool keepTargets = false, GameEntity pushTarget = null)
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
                _attackForce.Assemble(context, target, count, eligibleShips);
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
                if (target != null) IssueStrikeAttack(context, target, keepTargets);
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
                        using (ShipOrderTelemetry.Detail("path=screen"))
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
                _committedShips.Clear();
                _committedShips.AddRange(_attackForce.Ships);
                // N1: the eligible assault ships the strike does not need push on to the station, fighting on the way.
                // Guarding the centre left the strike trading with the station's fresh spawns (2026-11-26: 450 s).
                if (ready && pushTarget != null && pushTarget != target)
                {
                    _pushShips.Clear();
                    foreach (IShipEntity ship in eligibleShips ?? context.Ships)
                        if (!_attackForce.Ships.Contains(ship)) _pushShips.Add(ship);
                    HoldAtPoint(context, _pushShips, 0, pushTarget.GetFacade<IEntityTransformFacade>().Transform.position,
                        "path=assaultPush");
                    _committedShips.AddRange(_pushShips);
                }
                if (context.Snapshot.HasSecuredCentre && context.GuardPosition.HasValue &&
                    _lastState is (EnemyStrategicState.AssaultBase or EnemyStrategicState.RaidStructures))
                    ExecuteSiteGuard(context, _committedShips);
                else
                {
                    // X1, X8: every ship outside the strike force gets an order: it holds at the gather point, or
                    // guards the anchor when the group has none.
                    foreach (IShipEntity ship in context.Ships)
                        if (!_committedShips.Contains(ship)) _supportShips.Add(ship);
                    if (context.GuardPosition.HasValue)
                        HoldAtPoint(context, _supportShips, 0, context.GuardPosition.Value);
                    else GuardSupport(context, _supportShips, anchor);
                }
                ActionReason = ready
                    ? $"Target-specific force: {_strikeShips.Count} strike ships; {screeningCount} active screens; {_stragglers.Count} rallying; {_supportShips.Count} reserve."
                    : $"Assembling target counters: {_body.Count}/{count} ships in the body; {_supportShips.Count} reserve.";
            }
            else StopRemaining(context, context.Ships, count);
        }

        /// <summary>
        /// Attack orders go only to strike ships whose target, slot or active order changed. Re-sending an identical
        /// attack resets the ship's approach and makes its weapons re-sort their candidates. With
        /// <paramref name="keepTargets"/> a ship keeps attacking its own target until that one is destroyed, cloaked or
        /// out of reach (<see cref="EnemyAttackTargetRules.KeepsTarget"/>).
        /// </summary>
        private void IssueStrikeAttack(EnemyStrategicContext context, GameEntity target, bool keepTargets = false)
        {
            SplitStrikeOverflow(context, target);
            IssueOverflowAttackMove(context, target);
            List<Vector3> offsets = BattleOffsets(_strikeShips, _strikeShips.Count, target, out bool rebuilt);
            ApplyCarrierStandoff(context, target, offsets);
            _retargetReceivers.Clear();
            for (int i = 0; i < _strikeShips.Count; i++)
            {
                IShipEntity ship = _strikeShips[i];
                GameEntity previous = null;
                bool attacking = ship.CurrentOrder == UnitOrderType.Attack &&
                                 _strikeTargets.TryGetValue(ship, out previous) && previous != null;
                if (attacking && !rebuilt && previous == target) continue;
                if (attacking && keepTargets && previous != target && KeepsTarget(context, ship, previous)) continue;
                _strikeTargets[ship] = target;
                string reason = GetRetargetReason(attacking ? previous : null, target, keepTargets);
                if (!_retargetReceivers.TryGetValue(reason, out var group))
                    _retargetReceivers[reason] = group = (new List<GameEntity>(), new List<Vector3>());
                group.Receivers.Add(context.Receivers[ship]);
                group.Offsets.Add(offsets[i]);
            }
            // One order per reason, so each ai_orders row says why the ship changed target (W20).
            foreach (KeyValuePair<string, (List<GameEntity> Receivers, List<Vector3> Offsets)> group in _retargetReceivers)
                using (ShipOrderTelemetry.Detail("retarget=" + group.Key))
                    _unitOrderService.IssueAttack(group.Value.Receivers, target, group.Value.Offsets);
        }

        /// <summary>
        /// How many of <paramref name="ships"/> (the first <paramref name="count"/>) may hold <paramref name="target"/> as
        /// main target: the focus fire ship cap scaled by difficulty (rounded up, at least 1), never more than the
        /// team's free slots plus the slots these ships already hold. <see cref="int.MaxValue"/> for unlimited targets.
        /// </summary>
        private int StrikeLimit(EnemyStrategicContext context, GameEntity target, IReadOnlyList<IShipEntity> ships,
            int count)
        {
            int cap = _focusFire.GetCap(target, isSquadron: false);
            if (cap == FocusFireCaps.UNLIMITED || count == 0) return int.MaxValue;
            float scale = EnemyAiDifficultyProfile.Get(context.Snapshot.Difficulty).FocusFireScale;
            int scaled = Math.Max(1, Mathf.CeilToInt(cap * scale));
            int held = 0;
            for (int i = 0; i < count; i++)
                if (_focusFire.Holds(context.Receivers[ships[i]], target)) held++;
            return Math.Min(scaled, held + _focusFire.FreeSlots(ships[0].Owner, target, isSquadron: false));
        }

        /// <summary>
        /// Focus fire (approach 5): strike ships past <see cref="StrikeLimit"/> leave <see cref="_strikeShips"/> for
        /// <see cref="_overflowShips"/>. Carriers stay first (an attack-move would fly them onto the target), then ships
        /// already holding a slot (no churn), then the most damage on the target, or the nearest when the task force is
        /// not assembled for it.
        /// </summary>
        private void SplitStrikeOverflow(EnemyStrategicContext context, GameEntity target)
        {
            _overflowShips.Clear();
            int limit = StrikeLimit(context, target, _strikeShips, _strikeShips.Count);
            if (_strikeShips.Count > limit)
            {
                _strikeOrder.Clear();
                _strikeOrder.AddRange(_strikeShips);
                Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
                bool ratesTarget = _attackForce.Targets(target);
                _strikeOrder.Sort((first, second) =>
                {
                    int rank = StrikeRank(context, target, first).CompareTo(StrikeRank(context, target, second));
                    if (rank != 0) return rank;
                    if (ratesTarget)
                        return _attackForce.TargetDps(context, second).CompareTo(_attackForce.TargetDps(context, first));
                    return PlanarDistance(first.WorldPosition, targetPosition)
                        .CompareTo(PlanarDistance(second.WorldPosition, targetPosition));
                });
                for (int i = limit; i < _strikeOrder.Count; i++) _overflowShips.Add(_strikeOrder[i]);
                foreach (IShipEntity ship in _overflowShips)
                {
                    _strikeShips.Remove(ship);
                    _strikeTargets.Remove(ship);
                }
            }

            foreach (IShipEntity ship in _strikeShips) _overflowTargets.Remove(ship);
        }

        private int StrikeRank(EnemyStrategicContext context, GameEntity target, IShipEntity ship) =>
            IsCarrier(context, ship) ? 0 : _focusFire.Holds(context.Receivers[ship], target) ? 1 : 2;

        // Same E9 rule as the screens: an overflow ship already attack-moving on this target is not sent again.
        private void IssueOverflowAttackMove(EnemyStrategicContext context, GameEntity target)
        {
            if (_overflowShips.Count == 0) return;
            List<GameEntity> receivers = new List<GameEntity>();
            foreach (IShipEntity ship in _overflowShips)
            {
                if (ship.CurrentOrder == UnitOrderType.AttackMove &&
                    _overflowTargets.TryGetValue(ship, out GameEntity previous) && previous == target) continue;
                _overflowTargets[ship] = target;
                receivers.Add(context.Receivers[ship]);
            }

            if (receivers.Count > 0)
                using (ShipOrderTelemetry.Detail("path=strikeOverflow"))
                    _unitOrderService.IssueAttackMove(receivers, target.GetFacade<IEntityTransformFacade>().Transform.position);
        }

        // A marker attack fills the focus fire limit with slot holders first, then the nearest ships; the rest
        // attack-move to the target.
        private void IssueCappedMarkerAttack(EnemyStrategicContext context, List<GameEntity> receivers,
            GameEntity target)
        {
            _strikeOrder.Clear();
            foreach (IShipEntity ship in context.Ships)
                if (receivers.Contains(context.Receivers[ship])) _strikeOrder.Add(ship);
            int limit = StrikeLimit(context, target, _strikeOrder, _strikeOrder.Count);
            if (_strikeOrder.Count <= limit)
            {
                _unitOrderService.IssueAttack(receivers, target);
                return;
            }

            Vector3 targetPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
            _strikeOrder.Sort((first, second) =>
            {
                int rank = StrikeRank(context, target, first).CompareTo(StrikeRank(context, target, second));
                return rank != 0 ? rank : PlanarDistance(first.WorldPosition, targetPosition)
                    .CompareTo(PlanarDistance(second.WorldPosition, targetPosition));
            });
            List<GameEntity> attackers = new List<GameEntity>(limit);
            List<GameEntity> overflow = new List<GameEntity>();
            for (int i = 0; i < _strikeOrder.Count; i++)
                (i < limit ? attackers : overflow).Add(context.Receivers[_strikeOrder[i]]);
            if (attackers.Count > 0) _unitOrderService.IssueAttack(attackers, target);
            using (ShipOrderTelemetry.Detail("path=markerOverflow"))
                _unitOrderService.IssueAttackMove(overflow, targetPosition);
        }

        /// <summary>
        /// dead | cloaked | outOfReach (the kept target was dropped), slot (same target, new formation), new (no target
        /// before, or the state does not keep targets).
        /// </summary>
        private static string GetRetargetReason(GameEntity previous, GameEntity target, bool keepTargets)
        {
            if (previous == null) return "new";
            if (previous == target) return "slot";
            if (previous.HealthModel.IsDestroyed) return "dead";
            if (previous.IsCloaked()) return "cloaked";
            return keepTargets ? "outOfReach" : "new";
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

        private static bool KeepsTarget(EnemyStrategicContext context, IShipEntity ship, GameEntity target)
        {
            // A destroyed or cloaked target is dropped before its position is read.
            if (target.HealthModel.IsDestroyed || target.IsCloaked()) return false;
            float weaponRange = context.Receivers[ship].GetFacade<IUnitCombatObserverFacade>().WeaponRange;
            float targetRadius = target.TryGetFacade(out INavigationFootprintFacade footprint)
                ? footprint.NavigationRadius : 0f;
            return EnemyAttackTargetRules.KeepsTarget(false, false, ship.WorldPosition,
                target.GetFacade<IEntityTransformFacade>().Transform.position, weaponRange + targetRadius);
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
        private readonly List<IShipEntity> _anchorHold = new List<IShipEntity>(1);
        private readonly List<float> _cohesionRadii = new List<float>();

        /// <summary>
        /// Splits the first <paramref name="count"/> ships into the main body around the anchor and the stragglers.
        /// Returns the anchor ship.
        /// </summary>
        private IShipEntity SplitBody(IReadOnlyList<IShipEntity> ships, int count)
        {
            _body.Clear();
            _stragglers.Clear();
            _cohesionPoints.Clear();
            _cohesionRadii.Clear();
            for (int i = 0; i < count; i++)
            {
                _cohesionPoints.Add(ToPoint(ships[i].WorldPosition));
                _cohesionRadii.Add(ships[i].NavigationRadius);
            }
            int heldIndex = -1;
            for (int i = 0; i < count; i++)
                if (ships[i] == _heldAnchor) heldIndex = i;
            int anchorIndex = FleetCohesion.FindAnchor(_cohesionPoints, _cohesionRadii, FleetCohesion.RADIUS, heldIndex);
            _heldAnchor = ships[anchorIndex];
            FormationPoint anchorPoint = _cohesionPoints[anchorIndex];
            for (int i = 0; i < count; i++)
            {
                IShipEntity ship = ships[i];
                bool inBody = FleetCohesion.IsInBody(anchorPoint, _cohesionRadii[anchorIndex], _cohesionPoints[i],
                    _cohesionRadii[i], FleetCohesion.RADIUS, _bodyMembers.Contains(ship));
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


        /// <summary>
        /// Ships from <paramref name="start"/> on with no task attack-move to the group's gather point and hold there:
        /// never order <c>None</c> away from a hold point (W55). Only a group without a gather point stops them.
        /// </summary>
        private void StopRemaining(EnemyStrategicContext context,
            IReadOnlyList<IShipEntity> ships, int start)
        {
            if (context.GuardPosition.HasValue)
            {
                HoldAtPoint(context, ships, start, context.GuardPosition.Value);
                return;
            }
            List<GameEntity> idleCandidates = new List<GameEntity>();
            for (int i = start; i < ships.Count; i++)
                if (ships[i].CurrentOrder != UnitOrderType.None)
                    idleCandidates.Add(context.Receivers[ships[i]]);
            if (idleCandidates.Count > 0) _unitOrderService.IssueStop(idleCandidates);
        }
    }
}
