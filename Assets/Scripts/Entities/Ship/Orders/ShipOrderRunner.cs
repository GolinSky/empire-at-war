using EmpireAtWar.Components.Ship.Health;
using static EmpireAtWar.Utils.FormationConversion;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.TeamContacts;
using EmpireAtWar.Models.Players;
using UnityEngine;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.Ship.Orders
{
    /// <summary>
    /// Turns the ship's current order into a state and is the only place that changes ship state.
    /// </summary>
    public sealed class ShipOrderRunner
    {
        private readonly IShipMovement _movement;
        private readonly IWeaponComponent _weapon;
        private readonly ICameraService _cameraService;
        private readonly ShipAbilityService _abilities;
        private readonly IShipAbilityFacade _abilityCaster;
        private readonly IRadarComponent _radar;
        private readonly ITeamContacts _teamContacts;
        private readonly PlayerId _owner;
        private readonly List<IEntity> _teamCandidates = new List<IEntity>();
        private GroupPace _group;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly ShipOrderTelemetrySource _telemetrySource;
#endif

        private readonly UnitOrderModel _orders;
        private readonly ShipStateMachine _stateMachine;
        private readonly IdleState _idleState;
        private readonly NavigateState _navigateState;
        private readonly AttackTargetState _attackTargetState;
        private readonly AttackMoveState _attackMoveState;
        private readonly GuardState _guardState;
        private readonly HuntState _huntState;
        private readonly AbilityApproachState _abilityApproachState;
        private ShipAbilityId _pendingAbilityId;
        private IEntity _pendingAbilityTarget;

        private const float PURSUIT_DAMAGE_WINDOW = 3f;
        private const float PURSUIT_CHECK_INTERVAL = 0.5f;
        private const float PURSUIT_LEASH = 1500f;
        // Team-vision replacement search around the dead target; ships do not cross the map for one.
        private const float REPLACEMENT_RADIUS = 1500f;
        private const float SHARED_REPLACEMENT_RANGE = 3000f;

        // An idle ship hit by something it cannot yet reach chases it, within a leash of where it stood.
        private float _lastHealth = float.PositiveInfinity;
        private float _lastDamagedAt = float.NegativeInfinity;
        private float _nextPursuitCheck;
        private bool _isPursuing;
        private Vector3 _pursuitOrigin;

        private UnitOrderType? _replicaOrder;
        // Host-only and not saved: a loaded attack resumes at the ship's own speed.
        private GroupPace _pace;
        private int _paceMember;

        // Online clients show the host's order; ships there never run their own (see ApplyReplicaOrder).
        public UnitOrderType CurrentOrder => _replicaOrder ??
            (_pendingAbilityTarget != null ? UnitOrderType.UseAbility : _orders.Current);
        public IEntity AttackTarget =>
            !_replicaOrder.HasValue && CurrentOrder == UnitOrderType.Attack ? _orders.Target : null;
        public int OrderRevision => _orders.Revision;
        /// <summary>Online client: the host just gave this ship a new order.</summary>
        public event Action<UnitOrderType> ReplicaOrderIssued;

        public void ApplyReplicaOrder(UnitOrderType order, bool isNewOrder)
        {
            _replicaOrder = order;
            if (isNewOrder) ReplicaOrderIssued?.Invoke(order);
        }
        public bool IsAbilityFacing { get; private set; }

        public void BeginAbilityFacing()
        {
            _movement.CancelCombatHold();
            IsAbilityFacing = true;
            _stateMachine.SetState(_idleState);
            _movement.Stop();
            RecordOrder("ability-facing", "Ability temporarily controls facing.");
        }

        public void FaceAbility(Vector3 direction) => _movement.LookInDirection(direction);

        public void EndAbilityFacing()
        {
            if (!IsAbilityFacing) return;
            IsAbilityFacing = false;
            StartOrder();
            RecordOrder("ability-resume", "Underlying order resumed after ability facing.");
        }

        public ShipOrderRunner(IShipMovement movement, IWeaponComponent weapon, ICameraService cameraService,
            UnitOrderModel orders, ShipStateMachine stateMachine,
            IdleState idleState, NavigateState navigateState,
            AttackTargetState attackTargetState, AttackMoveState attackMoveState, GuardState guardState,
            HuntState huntState, AbilityApproachState abilityApproachState,
            ShipAbilityService abilities, IShipAbilityFacade abilityCaster, IRadarComponent radar
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , ShipOrderTelemetrySource telemetrySource
#endif
            , ITeamContacts teamContacts, PlayerId owner)
        {
            _teamContacts = teamContacts;
            _owner = owner;
            _orders = orders;
            _stateMachine = stateMachine;
            _movement = movement;
            _weapon = weapon;
            _cameraService = cameraService;
            _idleState = idleState;
            _navigateState = navigateState;
            _attackTargetState = attackTargetState;
            _attackMoveState = attackMoveState;
            _guardState = guardState;
            _huntState = huntState;
            _abilityApproachState = abilityApproachState;
            _abilities = abilities;
            _abilityCaster = abilityCaster;
            _radar = radar;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _telemetrySource = telemetrySource;
#endif
        }

        public void Start()
        {
            _stateMachine.SetState(_idleState);
            RecordOrder("start", "Ship initialized in Idle.");
        }

        public void Tick(float deltaTime)
        {
            if (IsAbilityFacing)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                ShipOrderTelemetry.Sample(_telemetrySource, _abilityCaster, _orders, _movement, _weapon, _radar, "AbilityFacing");
#endif
                return;
            }
            _stateMachine.Tick(deltaTime);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool attacking = _stateMachine.CurrentState == _attackTargetState;
            string attackEvent = attacking ? _attackTargetState.TakeAttackEvent() : null;
            if (attackEvent != null)
                ShipOrderTelemetry.Record(_telemetrySource, _abilityCaster, _orders, _movement, _weapon, _radar,
                    attackEvent.StartsWith("leg:") ? "attackLeg" : "attackHold", attackEvent,
                    nameof(AttackTargetState), _attackTargetState.AttackDiagnostics);
#endif
            if (_stateMachine.CurrentState.IsComplete) CompleteOrder();
            UpdatePursuit();
            UpdatePace();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ShipOrderTelemetry.Sample(_telemetrySource, _abilityCaster, _orders, _movement, _weapon, _radar,
                _stateMachine.CurrentState.GetType().Name,
                _stateMachine.CurrentState == _attackTargetState ? _attackTargetState.AttackDiagnostics : "");
#endif
        }

        /// <summary>Called on every health change; a drop marks the ship as recently attacked.</summary>
        public void ObserveHealth(float totalHealth)
        {
            if (totalHealth < _lastHealth) _lastDamagedAt = Time.time;
            _lastHealth = totalHealth;
        }

        private void UpdatePursuit()
        {
            if (_isPursuing)
            {
                if (_orders.Current != UnitOrderType.Attack)
                {
                    // The chase ended on its own (target lost or killed): back to the post. A new order from outside wins.
                    _isPursuing = false;
                    if (_orders.Current == UnitOrderType.None) MoveTo(_pursuitOrigin);
                }
                else if ((_movement.CurrentPosition - _pursuitOrigin).sqrMagnitude > PURSUIT_LEASH * PURSUIT_LEASH)
                {
                    _isPursuing = false;
                    MoveTo(_pursuitOrigin);
                }
                return;
            }

            float now = Time.time;
            if (_orders.Current != UnitOrderType.None || _pendingAbilityTarget != null ||
                _stateMachine.CurrentState != _idleState || now - _lastDamagedAt > PURSUIT_DAMAGE_WINDOW ||
                now < _nextPursuitCheck) return;
            _nextPursuitCheck = now + PURSUIT_CHECK_INTERVAL;
            IEntity attacker = ShipEngagement.FindNearestEnemy(_radar, _movement, _weapon, inRangeOnly: false);
            if (attacker == null) return;
            _pursuitOrigin = _movement.CurrentPosition;
            RecordOrder("pursuit", $"Attacked while idle; pursuing {attacker.Id}.");
            Issue(UnitOrderType.Attack, target: attacker);
            _isPursuing = true;
        }

        public void Release()
        {
            RecordOrder("release", "Ship released; active order cancelled.");
            IsAbilityFacing = false;
            _pendingAbilityTarget = null;
            LeavePace();
            _orders.Clear();
            // Leaves the shared attack-move engagement so the group stops counting this ship.
            if (_stateMachine.CurrentState == _attackMoveState) _stateMachine.SetState(_idleState);
        }

        public void MoveTo(Vector2 screenPosition) =>
            MoveTo(_cameraService.GetWorldPoint(screenPosition, _movement.CurrentPosition));

        public void MoveTo(Vector3 worldPosition)
        {
            if (RejectWhileAnchored(UnitOrderType.Move)) return;
            FormationPoint destination = ToPoint(worldPosition);
            if (_pendingAbilityTarget == null && _orders.Matches(UnitOrderType.Move, destination)) return;
            Issue(UnitOrderType.Move, destination);
        }

        public void Attack(IEntity target, Vector3 formationOffset, GroupPace pace = null)
        {
            _abilities.TryCancel(_abilityCaster, ShipAbilityId.Cloak);
            FormationPoint offset = ToPoint(formationOffset);
            if (!(_pendingAbilityTarget == null && _orders.Matches(UnitOrderType.Attack, target: target, offset: offset)) &&
                !TryRefreshAttackSlot(target, offset, UnitOrderModel.NO_HARD_POINT))
                Issue(UnitOrderType.Attack, target: target, offset: offset);
            JoinPace(pace);
        }

        public void AttackHardPoint(IEntity target, int hardPointId, Vector3 formationOffset, GroupPace pace = null)
        {
            _abilities.TryCancel(_abilityCaster, ShipAbilityId.Cloak);
            FormationPoint offset = ToPoint(formationOffset);
            if (!(_pendingAbilityTarget == null && _orders.Matches(UnitOrderType.Attack, target: target, offset: offset,
                    targetHardPointId: hardPointId)) &&
                !TryRefreshAttackSlot(target, offset, hardPointId))
                Issue(UnitOrderType.Attack, target: target, offset: offset, targetHardPointId: hardPointId);
            JoinPace(pace);
        }

        // A repeated order joins its new group; the ship leaves the old one first.
        private void JoinPace(GroupPace pace)
        {
            _group = pace;
            LeavePace();
            if (pace == null || _orders.Current != UnitOrderType.Attack) return;
            _pace = pace;
            _paceMember = pace.Join(_movement.PaceSpeed);
        }

        private void LeavePace()
        {
            if (_pace == null) return;
            _pace.Leave(_paceMember);
            _pace = null;
            _movement.SetSpeedCap(float.PositiveInfinity);
        }

        // The group approaches at its slowest member's speed; each ship fights at its own speed once in range.
        private void UpdatePace()
        {
            if (_pace == null) return;
            if (_stateMachine.CurrentState != _attackTargetState || _attackTargetState.HasReachedRange)
            {
                LeavePace();
                return;
            }

            _pace.Report(_paceMember, _movement.PaceSpeed);
            _movement.SetSpeedCap(_pace.Pace);
        }

        // The same target ordered again only picks a new side: the ship keeps its hold and its current leg.
        private bool TryRefreshAttackSlot(IEntity target, FormationPoint offset, int hardPointId)
        {
            if (_pendingAbilityTarget != null ||
                !_orders.Matches(UnitOrderType.Attack, target: target, offset: _orders.Offset,
                    targetHardPointId: hardPointId) ||
                target.HealthModel.IsDestroyed) return false;
            _orders.UpdateOffset(offset);
            _attackTargetState.SetData(target, ToVector(offset), hardPointId);
            RecordOrder("refresh", $"Attack on {target.Id} kept; slot updated.");
            return true;
        }

        public void AttackMoveTo(Vector3 destination, AttackMoveEngagement engagement)
        {
            if (RejectWhileAnchored(UnitOrderType.AttackMove)) return;
            _abilities.TryCancel(_abilityCaster, ShipAbilityId.Cloak);
            FormationPoint point = ToPoint(destination);
            if (_pendingAbilityTarget == null && _orders.Engagement == engagement &&
                _orders.Matches(UnitOrderType.AttackMove, point)) return;
            Issue(UnitOrderType.AttackMove, point, engagement: engagement);
        }

        public void Guard(IEntity friendly, Vector3 offset)
        {
            if (RejectWhileAnchored(UnitOrderType.Guard)) return;
            _abilities.TryCancel(_abilityCaster, ShipAbilityId.Cloak);
            FormationPoint formationOffset = ToPoint(offset);
            if (_pendingAbilityTarget == null && _orders.Matches(UnitOrderType.Guard, target: friendly, offset: formationOffset)) return;
            Issue(UnitOrderType.Guard, target: friendly, offset: formationOffset);
        }

        public void MoveAlong(IReadOnlyList<Vector3> waypoints)
        {
            if (waypoints.Count == 0 || RejectWhileAnchored(UnitOrderType.WaypointMove)) return;
            List<FormationPoint> points = new List<FormationPoint>(waypoints.Count);
            foreach (Vector3 waypoint in waypoints) points.Add(ToPoint(waypoint));
            if (_pendingAbilityTarget == null && _orders.MatchesWaypoints(points)) return;
            Issue(UnitOrderType.WaypointMove, points[0], waypoints: points);
        }

        public void Hunt()
        {
            if (RejectWhileAnchored(UnitOrderType.Hunt)) return;
            _abilities.TryCancel(_abilityCaster, ShipAbilityId.Cloak);
            if (_pendingAbilityTarget == null && _orders.Current == UnitOrderType.Hunt) return;
            Issue(UnitOrderType.Hunt);
        }

        public void Retreat(Vector3 destination)
        {
            // Retreat must never soft-lock behind a gravity well: it ends the well first.
            _abilities.TryCancel(_abilityCaster, ShipAbilityId.Interdict);
            FormationPoint point = ToPoint(destination);
            if (_pendingAbilityTarget == null && _orders.Matches(UnitOrderType.Retreat, point)) return;
            _weapon.ResetTarget();
            Issue(UnitOrderType.Retreat, point);
        }

        public void CastAbility(ShipAbilityId id, IEntity target, float range)
        {
            // An anchored ship cannot approach; in-range casts never reach this path.
            if (RejectWhileAnchored(UnitOrderType.UseAbility)) return;
            _movement.CancelCombatHold();
            _pendingAbilityId = id;
            _pendingAbilityTarget = target;
            IsAbilityFacing = false;
            _abilityApproachState.SetData(target, range);
            _stateMachine.SetState(_abilityApproachState);
            RecordOrder("ability-approach", $"Approaching ability {id} target {target.Id} within {range}.");
        }

        public void Stop()
        {
            _weapon.ResetTarget();
            Issue(UnitOrderType.None);
        }

        // Movement orders are refused, not queued: the current order stays and nothing replays when the well ends.
        private bool RejectWhileAnchored(UnitOrderType type)
        {
            if (!_abilityCaster.Modifiers.IsAnchored) return false;
            RecordOrder("rejected", $"{type} refused: anchored by gravity well.");
            return true;
        }

        private void Issue(UnitOrderType type, FormationPoint destination = default,
            IEntity target = null, FormationPoint offset = default,
            IReadOnlyList<FormationPoint> waypoints = null, int targetHardPointId = UnitOrderModel.NO_HARD_POINT,
            AttackMoveEngagement engagement = null)
        {
            _movement.CancelCombatHold();
            if (_orders.Current != UnitOrderType.None || _pendingAbilityTarget != null)
                RecordOrder("cancel", $"Replaced by {type}.");
            IsAbilityFacing = false;
            _pendingAbilityTarget = null;
            _group = null;
            LeavePace();
            _orders.Replace(type, destination, target, offset, waypoints, targetHardPointId, engagement);
            RecordOrder("issued", "Requested order before state entry.");
            StartOrder();
        }

        private void StartOrder()
        {
            switch (_orders.Current)
            {
                case UnitOrderType.Move:
                case UnitOrderType.WaypointMove:
                case UnitOrderType.Retreat:
                    _navigateState.SetWorldDestination(ToVector(_orders.Destination));
                    _stateMachine.SetState(_navigateState);
                    break;
                case UnitOrderType.Attack:
                    if (_orders.Target.HealthModel.IsDestroyed)
                    {
                        Stop();
                        break;
                    }

                    _attackTargetState.SetData(_orders.Target, ToVector(_orders.Offset),
                        _orders.TargetHardPointId);
                    _stateMachine.SetState(_attackTargetState);
                    break;
                case UnitOrderType.AttackMove:
                    _attackMoveState.SetData(ToVector(_orders.Destination), _orders.Engagement);
                    _stateMachine.SetState(_attackMoveState);
                    break;
                case UnitOrderType.Guard:
                    if (_orders.Target.HealthModel.IsDestroyed)
                    {
                        Stop();
                        break;
                    }

                    _guardState.SetData(_orders.Target, ToVector(_orders.Offset));
                    _stateMachine.SetState(_guardState);
                    break;
                case UnitOrderType.Hunt:
                    _stateMachine.SetState(_huntState);
                    break;
                default:
                    _movement.Stop();
                    _stateMachine.SetState(_idleState);
                    break;
            }
        }

        // Own radar first, then the group's shared contact, then what the team sees around where the target died.
        private IEntity FindReplacement()
        {
            ShipClass? deadClass = _attackTargetState.LastTargetClass;
            IEntity own = ShipEngagement.FindNearestEnemy(_radar, _movement, _weapon, inRangeOnly: false, deadClass);
            if (own != null && IsSameClass(own, deadClass)) return own;
            IEntity shared = GetSharedReplacement();
            if (shared != null && IsSameClass(shared, deadClass)) return shared;
            if (own != null && !deadClass.HasValue) return own;
            // The team scan is shared and cached, but it is still only worth asking when nothing of the class is near.
            _teamCandidates.Clear();
            _teamContacts.FillVisibleHostiles(_owner, _attackTargetState.LastTargetPosition, REPLACEMENT_RADIUS,
                _teamCandidates);
            IEntity team = ShipEngagement.FindNearestEnemy(_teamCandidates, _movement, _weapon, deadClass);
            if (team != null && IsSameClass(team, deadClass)) return team;
            return own ?? shared ?? team;
        }

        private static bool IsSameClass(IEntity enemy, ShipClass? deadClass) =>
            deadClass.HasValue && enemy.HealthModel.ShipClass == deadClass.Value;

        // A contact the group found far away is not worth a cross-map chase.
        private IEntity GetSharedReplacement()
        {
            if (_group == null || _group.SharedReplacement == null) return null;
            IEntity shared = _group.SharedReplacement;
            return !shared.HealthModel.IsDestroyed && shared.HealthModel.HasUnits &&
                   _movement.GetRange(shared.GetFacade<IEntityTransformFacade>().Transform.position) <=
                   SHARED_REPLACEMENT_RANGE ? shared : null;
        }

        private void CompleteOrder()
        {
            if (_pendingAbilityTarget != null)
            {
                RecordOrder("ability-complete", _abilityApproachState.IsInRange
                    ? "Ability approach reached cast range." : "Ability approach ended without cast range.");
                IEntity target = _pendingAbilityTarget;
                _pendingAbilityTarget = null;
                _movement.Stop();
                _stateMachine.SetState(_idleState);
                // Keep the underlying order, including its waypoint index and shared engagement.
                if (_abilityApproachState.IsInRange)
                    _abilities.TryActivate(_abilityCaster, _pendingAbilityId, target);
                if (!IsAbilityFacing) StartOrder();
                return;
            }

            if (_orders.Current == UnitOrderType.WaypointMove && _orders.AdvanceWaypoint(out FormationPoint next))
            {
                _navigateState.SetWorldDestination(ToVector(next));
                _stateMachine.SetState(_navigateState);
                RecordOrder("waypoint", "Advanced to next waypoint.");
                return;
            }

            // A ship that killed its target keeps fighting: it attacks the nearest radar contact its loadout prefers
            // (fighters for a laser corvette), even one its guns do not reach yet.
            if (_orders.Current == UnitOrderType.Attack && _orders.Target.HealthModel.IsDestroyed)
            {
                RecordOrder("complete", "Attack target destroyed; checking replacement contacts.");
                IEntity nextTarget = FindReplacement();
                if (nextTarget != null)
                {
                    GroupPace group = _group;
                    FormationPoint slot = _orders.Offset;
                    Issue(UnitOrderType.Attack, target: nextTarget, offset: slot);
                    _group = group;
                    if (group != null) group.SharedReplacement = nextTarget;
                    return;
                }

                if (_isPursuing)
                {
                    _isPursuing = false;
                    RecordOrder("complete", "Pursuit target destroyed; returning to the post.");
                    MoveTo(_pursuitOrigin);
                    return;
                }

                // Nothing on this ship's radar: sweep toward where the target died instead of idling in place.
                if (_stateMachine.CurrentState == _attackTargetState)
                {
                    RecordOrder("complete", "Target destroyed; attack-moving to its last position.");
                    Issue(UnitOrderType.AttackMove, ToPoint(_attackTargetState.LastTargetPosition),
                        engagement: new AttackMoveEngagement());
                    return;
                }
            }

            RecordOrder("complete", _orders.Target != null && _orders.Target.HealthModel.IsDestroyed
                ? "Target destroyed; no replacement order." : "State completed; returning to Idle.");
            _orders.Clear();
            _stateMachine.SetState(_idleState);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void RecordOrder(string kind, string reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string state = _stateMachine.CurrentState != null ? _stateMachine.CurrentState.GetType().Name : "uninitialized";
            ShipOrderTelemetry.Record(_telemetrySource, _abilityCaster, _orders, _movement, _weapon, _radar,
                kind, reason, state);
#endif
        }

    }
}
