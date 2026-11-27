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
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.TeamContacts;
using EmpireAtWar.Services.UnitOrders;
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
        private readonly IFocusFireClaim _focusFire;
        private readonly List<IEntity> _ownCandidates = new List<IEntity>();
        private readonly List<IEntity> _teamCandidates = new List<IEntity>();
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

        // Team-vision replacement search around the dead target; ships do not cross the map for one.
        private const float REPLACEMENT_RADIUS = 1500f;
        // Seconds between radar checks of an idle ship watching for the next fight after its attack-move.
        private const float FIGHT_WATCH_INTERVAL = 0.5f;

        private UnitOrderType? _replicaOrder;
        // Host-only and not saved: a loaded order resumes at the ship's own speed.
        private PaceCluster _pace;
        private int _paceMember;
        private float _stragglerTimer;
        private float _stuckTimer;
        // A ship turning or accelerating at the start of its order is slow but not stuck.
        private bool _hasPaceMoved;
        // Set when an attack-move ends; any new order clears it.
        private bool _watchesForFight;
        private float _fightWatchTimer;

        // Online clients show the host's order; ships there never run their own (see ApplyReplicaOrder).
        public UnitOrderType CurrentOrder => _replicaOrder ??
            (_pendingAbilityTarget != null ? UnitOrderType.UseAbility : _orders.Current);
        public IEntity AttackTarget =>
            !_replicaOrder.HasValue && CurrentOrder == UnitOrderType.Attack ? _orders.Target : null;
        public int OrderRevision => _orders.Revision;
        /// <summary>Attack-move ship stopped on the way to fight a warship that hit it (W66: the AI group reads this).</summary>
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
            , ITeamContacts teamContacts, PlayerId owner, IFocusFireClaim focusFire)
        {
            _focusFire = focusFire;
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
            string settleEvent = _movement is IShipMovementDiagnostics diagnostics ? diagnostics.TakeSettleEvent() : null;
            if (settleEvent != null)
                ShipOrderTelemetry.Record(_telemetrySource, _abilityCaster, _orders, _movement, _weapon, _radar,
                    "settle", settleEvent, _stateMachine.CurrentState.GetType().Name,
                    attacking ? _attackTargetState.AttackDiagnostics : "");
#endif
            if (_stateMachine.CurrentState.IsComplete) CompleteOrder();
            WatchForFight(deltaTime);
            UpdatePace(deltaTime);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShipOrderTelemetry.IsSampleDue(_telemetrySource, _orders))
            {
                attacking = _stateMachine.CurrentState == _attackTargetState;
                ShipOrderTelemetry.Sample(_telemetrySource, _abilityCaster, _orders, _movement, _weapon, _radar,
                    _stateMachine.CurrentState.GetType().Name,
                    attacking ? _attackTargetState.AttackDiagnostics : "",
                    _stateMachine.CurrentState == _attackMoveState ? _attackMoveState.EngagementTarget : null,
                    attacking ? _attackTargetState.IsHoldingAtRange : null);
            }
#endif
        }

        public void Release()
        {
            RecordOrder("release", "Ship released; active order cancelled.");
            IsAbilityFacing = false;
            _pendingAbilityTarget = null;
            _watchesForFight = false;
            LeavePace();
            _orders.Clear();
            // Leaves the shared attack-move engagement so the group stops counting this ship.
            if (_stateMachine.CurrentState == _attackMoveState) _stateMachine.SetState(_idleState);
        }

        public void MoveTo(Vector2 screenPosition) =>
            MoveTo(_cameraService.GetWorldPoint(screenPosition, _movement.CurrentPosition));

        public void MoveTo(Vector3 worldPosition, PaceCluster pace = null)
        {
            if (RejectWhileAnchored(UnitOrderType.Move)) return;
            FormationPoint destination = ToPoint(worldPosition);
            if (_pendingAbilityTarget == null && _orders.Matches(UnitOrderType.Move, destination))
            {
                JoinPace(pace);
                return;
            }
            Issue(UnitOrderType.Move, destination);
            JoinPace(pace);
        }

        public void Attack(IEntity target, Vector3 formationOffset, PaceCluster pace = null)
        {
            _abilities.TryCancel(_abilityCaster, ShipAbilityId.Cloak);
            FormationPoint offset = ToPoint(formationOffset);
            bool matchedOrRefreshed = (!_orders.HoldAttackPosition && _pendingAbilityTarget == null &&
                _orders.Matches(UnitOrderType.Attack, target: target, offset: offset)) ||
                TryRefreshAttackSlot(target, offset, UnitOrderModel.NO_HARD_POINT);
            if (!matchedOrRefreshed)
                Issue(UnitOrderType.Attack, target: target, offset: offset);
            bool alreadyInRange = matchedOrRefreshed &&
                _stateMachine.CurrentState == _attackTargetState && _attackTargetState.HasReachedRange;
            JoinPace(alreadyInRange ? null : pace);
        }

        public void AttackHardPoint(IEntity target, int hardPointId, Vector3 formationOffset, PaceCluster pace = null)
        {
            _abilities.TryCancel(_abilityCaster, ShipAbilityId.Cloak);
            FormationPoint offset = ToPoint(formationOffset);
            bool matchedOrRefreshed = (!_orders.HoldAttackPosition && _pendingAbilityTarget == null &&
                _orders.Matches(UnitOrderType.Attack, target: target, offset: offset,
                    targetHardPointId: hardPointId)) ||
                TryRefreshAttackSlot(target, offset, hardPointId);
            if (!matchedOrRefreshed)
                Issue(UnitOrderType.Attack, target: target, offset: offset, targetHardPointId: hardPointId);
            bool alreadyInRange = matchedOrRefreshed &&
                _stateMachine.CurrentState == _attackTargetState && _attackTargetState.HasReachedRange;
            JoinPace(alreadyInRange ? null : pace);
        }

        // A repeated order joins its new cluster; the ship leaves the old one first.
        private void JoinPace(PaceCluster pace)
        {
            LeavePace();
            if (pace == null || !IsPacedOrder(_orders.Current)) return;
            SamplePaceGeometry(out System.Numerics.Vector2 position, out float remaining,
                out System.Numerics.Vector2 heading);
            _pace = pace;
            _paceMember = pace.Join(_movement.PaceSpeed, remaining, position, _movement.NavigationRadius, heading,
                _movement.UndamagedPaceSpeed);
        }

        private void LeavePace()
        {
            _stragglerTimer = 0f;
            _stuckTimer = 0f;
            _hasPaceMoved = false;
            if (_pace == null) return;
            _pace.Leave(_paceMember);
            _pace = null;
            _movement.SetSpeedCap(float.PositiveInfinity);
        }

        // The cluster approaches at its slowest member's speed with light lag correction; each ship detaches once
        // arriving, engaging, damaged below the speed floor, straggling, or stuck.
        private void UpdatePace(float deltaTime)
        {
            if (_pace == null) return;
            if (!IsPacingActive() || _movement.IsArriving)
            {
                LeavePace();
                return;
            }

            float ownSpeed = _movement.PaceSpeed;
            if (_pace.IsBelowStragglerSpeed(ownSpeed))
            {
                LeavePace();
                return;
            }

            SamplePaceGeometry(out System.Numerics.Vector2 position, out float remaining,
                out System.Numerics.Vector2 heading);

            PaceClusterTuning tuning = _pace.Tuning;
            bool isSlow = _movement.IsMoving && _movement.CurrentSpeed < tuning.StuckSpeed;
            if (!isSlow) _hasPaceMoved = true;
            bool isStuck = isSlow && _hasPaceMoved;
            if (isStuck)
            {
                _stuckTimer += deltaTime;
                if (_stuckTimer > tuning.StuckDetachTime)
                {
                    LeavePace();
                    return;
                }
            }
            else
            {
                _stuckTimer = 0f;
            }

            _pace.Report(_paceMember, isStuck ? float.PositiveInfinity : ownSpeed, remaining, position, heading);
            if (!_pace.TryGetSpeedCap(remaining, deltaTime, ref _stragglerTimer, out float cap))
            {
                LeavePace();
                return;
            }

            _movement.SetSpeedCap(isStuck ? float.PositiveInfinity : cap);
        }

        private static bool IsPacedOrder(UnitOrderType order) =>
            order == UnitOrderType.Move || order == UnitOrderType.Attack || order == UnitOrderType.AttackMove;

        private bool IsPacingActive() => _orders.Current switch
        {
            UnitOrderType.Move => _stateMachine.CurrentState == _navigateState,
            UnitOrderType.Attack => _stateMachine.CurrentState == _attackTargetState &&
                                    !_attackTargetState.HasReachedRange,
            UnitOrderType.AttackMove => _stateMachine.CurrentState == _attackMoveState &&
                                        !_attackMoveState.IsEngaging,
            _ => false
        };

        private void SamplePaceGeometry(out System.Numerics.Vector2 position, out float remaining,
            out System.Numerics.Vector2 heading)
        {
            Vector3 current = _movement.CurrentPosition;
            position = new System.Numerics.Vector2(current.x, current.z);
            Vector3 goal;
            if (_orders.Current == UnitOrderType.Attack)
            {
                IEntity target = _orders.Target;
                if (target == null || target.HealthModel.IsDestroyed)
                {
                    remaining = float.NaN;
                    heading = default;
                    return;
                }

                goal = target.GetFacade<IEntityTransformFacade>().Transform.position + ToVector(_orders.Offset);
            }
            else
            {
                goal = ToVector(_orders.Destination);
            }

            System.Numerics.Vector2 delta = new System.Numerics.Vector2(goal.x - current.x, goal.z - current.z);
            float distSq = delta.LengthSquared();
            if (distSq > 1e-6f)
            {
                remaining = MathF.Sqrt(distSq);
                heading = delta / remaining;
            }
            else
            {
                remaining = 0f;
                heading = default;
            }
        }

        // The same target ordered again only picks a new side: the ship keeps its hold and its current leg.
        private bool TryRefreshAttackSlot(IEntity target, FormationPoint offset, int hardPointId)
        {
            if (_orders.HoldAttackPosition || _pendingAbilityTarget != null ||
                !_orders.Matches(UnitOrderType.Attack, target: target, offset: _orders.Offset,
                    targetHardPointId: hardPointId) ||
                target.HealthModel.IsDestroyed) return false;
            _orders.UpdateOffset(offset);
            _attackTargetState.SetData(target, ToVector(offset), hardPointId);
            RecordOrder("refresh", $"Attack on {target.Id} kept; slot updated.");
            return true;
        }

        public void AttackMoveTo(Vector3 destination, AttackMoveEngagement engagement, PaceCluster pace = null)
        {
            if (RejectWhileAnchored(UnitOrderType.AttackMove)) return;
            _abilities.TryCancel(_abilityCaster, ShipAbilityId.Cloak);
            FormationPoint point = ToPoint(destination);
            if (_pendingAbilityTarget == null && _orders.Engagement == engagement &&
                _orders.Matches(UnitOrderType.AttackMove, point))
            {
                bool engaging = _stateMachine.CurrentState == _attackMoveState && _attackMoveState.IsEngaging;
                JoinPace(engaging ? null : pace);
                return;
            }
            Issue(UnitOrderType.AttackMove, point, engagement: engagement);
            JoinPace(pace);
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
            // A running ability that steers the hull (Ion Pulse turn) cannot be cancelled by another cast.
            if (IsAbilityFacing)
            {
                RecordOrder("rejected", $"Cast {id} refused: ability controls facing.");
                return;
            }
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
            AttackMoveEngagement engagement = null, bool holdAttackPosition = false)
        {
            _movement.CancelCombatHold();
            _watchesForFight = false;
            if (_orders.Current != UnitOrderType.None || _pendingAbilityTarget != null)
                RecordOrder("cancel", $"Replaced by {type}.");
            _pendingAbilityTarget = null;
            LeavePace();
            _orders.Replace(type, destination, target, offset, waypoints, targetHardPointId, engagement, holdAttackPosition);
            RecordOrder("issued", "Requested order before state entry.");
            // A running ability that steers the hull (Ion Pulse turn) cannot be cancelled by orders:
            // the new order waits and starts from EndAbilityFacing.
            if (IsAbilityFacing)
            {
                RecordOrder("ability-queued", $"{type} starts after ability facing.");
                return;
            }
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
                        _orders.TargetHardPointId, _orders.HoldAttackPosition);
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

        // Own radar before team vision, the dead target's class first. Enemies with a free focus fire slot come before
        // full ones; a full one is taken only when nothing else is in reach.
        private IEntity FindReplacement()
        {
            ShipClass? deadClass = _attackTargetState.LastTargetClass;
            _ownCandidates.Clear();
            for (int i = 0; i < _radar.Enemies.Count; i++)
                if (ShipEngagement.CanEngage(_radar.Enemies[i], _movement, _weapon))
                    _ownCandidates.Add(_radar.Enemies[i]);
            _teamCandidates.Clear();
            _teamContacts.FillVisibleHostiles(_owner, _movement.CurrentPosition, REPLACEMENT_RADIUS,
                _teamCandidates);
            for (int i = _teamCandidates.Count - 1; i >= 0; i--)
                if (!ShipEngagement.CanEngage(_teamCandidates[i], _movement, _weapon))
                    _teamCandidates.RemoveAt(i);
            if (HasOpenCandidate(_ownCandidates) || HasOpenCandidate(_teamCandidates))
            {
                RemoveFullCandidates(_ownCandidates);
                RemoveFullCandidates(_teamCandidates);
            }

            IEntity own = ShipEngagement.FindNearestEnemy(_ownCandidates, _movement, _weapon, deadClass);
            if (own != null && own.HealthModel.ShipClass == deadClass) return own;
            IEntity team = ShipEngagement.FindNearestEnemy(_teamCandidates, _movement, _weapon, deadClass);
            return team != null && team.HealthModel.ShipClass == deadClass ? team : own ?? team;
        }

        public float GetEngagementRange(ShipClass targetClass) => _weapon.GetEngagementRange(targetClass);

        private Vector3 GetFightApproach() =>
            GetFightApproach(_attackTargetState.LastTargetPosition, _attackTargetState.LastTargetClass.Value);

        // The point on the line to the fight where the ship would sit at engagement range of that class; the ship's
        // own position when it is already that close.
        private Vector3 GetFightApproach(Vector3 fight, ShipClass targetClass)
        {
            Vector3 position = _movement.CurrentPosition;
            Vector3 offset = position - fight;
            offset.y = 0f;
            float range = _weapon.GetEngagementRange(targetClass);
            float distance = offset.magnitude;
            if (distance <= range) return position;
            Vector3 approach = fight + offset * (range / distance);
            approach.y = position.y;
            return approach;
        }

        private bool HasOpenCandidate(List<IEntity> candidates)
        {
            foreach (IEntity candidate in candidates)
                if (_focusFire.HasSlot(candidate)) return true;
            return false;
        }

        private void RemoveFullCandidates(List<IEntity> candidates)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
                if (!_focusFire.HasSlot(candidates[i])) candidates.RemoveAt(i);
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

            if (_orders.Current == UnitOrderType.Attack && _orders.Target.HealthModel.IsDestroyed)
            {
                RecordOrder("complete", "Attack target destroyed; checking contacts in weapons reach.");
                IEntity nextTarget = FindReplacement();
                if (nextTarget != null)
                {
                    Issue(UnitOrderType.Attack, target: nextTarget, holdAttackPosition: true);
                    return;
                }

                // Nothing in reach (a back-line ship): close up to firing range of the fight instead of idling.
                RecordOrder("complete", "Attack target destroyed; nothing in reach, attack-moving to the fight.");
                Issue(UnitOrderType.AttackMove, ToPoint(GetFightApproach()), engagement: new AttackMoveEngagement());
                return;
            }

            // A replacement target held from the ship's spot drifted out of reach: follow the fight, not idle.
            if (_orders.Current == UnitOrderType.Attack && _orders.HoldAttackPosition)
            {
                RecordOrder("complete", "Attack target left weapons reach; attack-moving to the fight.");
                IEntity lost = _orders.Target;
                Vector3 fight = lost.GetFacade<IEntityTransformFacade>().Transform.position;
                Issue(UnitOrderType.AttackMove, ToPoint(GetFightApproach(fight, lost.HealthModel.ShipClass)),
                    engagement: new AttackMoveEngagement());
                return;
            }

            bool wasAttackMove = _orders.Current == UnitOrderType.AttackMove;
            RecordOrder("complete", _orders.Target != null && _orders.Target.HealthModel.IsDestroyed
                ? "Target destroyed; no replacement order." : "State completed; returning to Idle.");
            _orders.Clear();
            _stateMachine.SetState(_idleState);
            // Set after the clear: Issue resets it, so any later order ends the watch. The first check waits one
            // interval so an AI or survival director can give its own order first.
            _watchesForFight = wasAttackMove;
            _fightWatchTimer = FIGHT_WATCH_INTERVAL;
        }

        // After an attack-move ends, an idle ship closes to engagement range of the next enemy ship its radar finds
        // beyond that range, instead of waiting for it. Squadrons are left to the guns: they would lead it off.
        private void WatchForFight(float deltaTime)
        {
            if (!_watchesForFight || _stateMachine.CurrentState != _idleState) return;
            _fightWatchTimer -= deltaTime;
            if (_fightWatchTimer > 0f) return;
            _fightWatchTimer = FIGHT_WATCH_INTERVAL;

            _ownCandidates.Clear();
            for (int i = 0; i < _radar.Enemies.Count; i++)
                if (!_radar.Enemies[i].HealthModel.ShipClass.IsStrikecraft())
                    _ownCandidates.Add(_radar.Enemies[i]);
            IEntity enemy = ShipEngagement.FindNearestEnemy(_ownCandidates, _movement, _weapon);
            if (enemy == null) return;
            Vector3 fight = enemy.GetFacade<IEntityTransformFacade>().Transform.position;
            Vector3 approach = GetFightApproach(fight, enemy.HealthModel.ShipClass);
            if (approach == _movement.CurrentPosition) return;
            RecordOrder("fightWatch", $"Enemy {enemy.Id} on radar beyond reach; attack-moving to it.");
            Issue(UnitOrderType.AttackMove, ToPoint(approach), engagement: new AttackMoveEngagement());
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
