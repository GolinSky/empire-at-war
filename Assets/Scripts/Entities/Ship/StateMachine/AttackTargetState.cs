using EmpireAtWar.Components.AttackComponent;
using System;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Utils;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class AttackTargetState: IBaseState
    {
        private const float STALL_RETRY_INTERVAL = 0.5f;
        private const float HARD_POINT_SCAN_INTERVAL = 0.1f;
        private const float STALL_BEARING_STEP_DEGREES = 20f;
        private const float ARTILLERY_APPROACH_FACTOR = 0.9f;
        private const float ARTILLERY_REPLAN_RANGE_FACTOR = 0.05f;
        private const int ARTILLERY_STALL_RETRIES = 6;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker _hardPointApproachMarker = new ProfilerMarker("Battle.Ship.Attack.HardPointApproach");
        private static readonly ProfilerMarker _artilleryMarker = new ProfilerMarker("Battle.Ship.Attack.Artillery");
        private static readonly ProfilerMarker _holdLookAtMarker = new ProfilerMarker("Battle.Ship.Attack.HoldLookAt");
#endif
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IShipMovement _shipMoveComponent;
        private readonly IArtilleryCapability _artilleryCapability;
        private readonly IFocusFireClaim _focusFire;
        private IHealthModelObserver _mainTarget;
        private IEntity _mainTargetEntity;
        private IHardPointModel _focusedHardPoint;
        private AttackData _attackData;

        private Transform _mainTargetTransform;
        private int _focusedHardPointId = UnitOrderModel.NO_HARD_POINT;

        private Vector3 _formationOffset;
        private Vector3 _pursuitDestination;
        private Vector3 _lastAttemptPosition;
        private float _stallRetryTimer;
        private int _stallAttempt;
        private bool _hasHardPointApproach;
        private float _hardPointScanTimer;

        private bool _hasPursuitDestination;
        private bool _wasMoving;
        private bool _hasHeldPosition;
        // Holding right now (no approach leg since the last hold); enables the leave margin.
        private bool _isHolding;
        private int _artilleryRevision = -1;
        private ShipClass _artilleryTargetClass;
        private float _artilleryReach;
        private bool _usesArtillery;
        private Vector3 _artilleryPlanTargetPosition;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const string REASON_MOVING = "moving";
        private string _holdReason = "none";
        private string _moveReason = "none";
        private string _pendingEvent;
        private int _legCount;
#endif

        private Vector3 TargetPosition => _mainTargetTransform.position;
        // The distance depends on what is being attacked: heavy guns hold back from capitals, lasers close on corvettes.
        private float EngagementRange => _weaponComponent.GetEngagementRange(_mainTarget.ShipClass);
        private Vector3 StandoffPosition => ShipEngagement.GetStandoffPosition(
            _shipMoveComponent, _weaponComponent, _mainTargetEntity,
            _hasHeldPosition ? Vector3.zero : _formationOffset);
        private bool IsInRange => ShipEngagement.IsInRange(_shipMoveComponent, _weaponComponent,
            _mainTargetEntity, TargetPosition);
        // Both reach tests also need the forward main batteries in range bow-on (see ShipEngagement.CanHoldOn).
        private bool IsInReach => ShipEngagement.IsInReach(_shipMoveComponent, _weaponComponent,
            _mainTargetEntity, TargetPosition) && _weaponComponent.MainBatteryReaches(_mainTargetEntity,
            ShipEngagement.ContactDistance(_shipMoveComponent, _mainTargetEntity), 1f, false);
        // Reach with the leave margin: a ship holding on its target only re-approaches beyond this. The main battery
        // keeps holding while the mains in reach carry half of the bearing main weight; requiring every main made the
        // ship flap between hold and approach at the range edge.
        private bool IsInHeldReach => ShipEngagement.IsInReach(_shipMoveComponent, _weaponComponent,
            _mainTargetEntity, TargetPosition, ShipEngagement.HELD_REACH_FACTOR) &&
            _weaponComponent.MainBatteryReaches(_mainTargetEntity,
                ShipEngagement.ContactDistance(_shipMoveComponent, _mainTargetEntity), 1f, true);
        private float PursuitDestinationUpdateDistance => Mathf.Max(
            _shipMoveComponent.NavigationRadius,
            EngagementRange * 0.1f);

        /// <summary>The ship has held at firing range on the current target since it was set; a group pace no longer applies.</summary>
        public bool HasReachedRange { get; private set; }

        /// <summary>Class of the current or completed target.</summary>
        public ShipClass? LastTargetClass { get; private set; }

        /// <summary>Where the current or completed target was last seen alive.</summary>
        public Vector3 LastTargetPosition { get; private set; }

        private bool _holdPosition;

        public bool IsComplete => _mainTarget == null || _mainTarget.IsDestroyed || !_mainTarget.HasUnits ||
                                  _mainTargetEntity.IsCloaked() || _holdPosition && !IsInHeldReach;

        public AttackTargetState(
            IAttackDataFactory attackDataFactory,
            IWeaponComponent weaponComponent,
            IShipMovement shipMoveComponent,
            IFocusFireClaim focusFire,
            IArtilleryCapability artilleryCapability = null)
        {
            _focusFire = focusFire;
            _attackDataFactory = attackDataFactory;
            _weaponComponent = weaponComponent;
            _shipMoveComponent = shipMoveComponent;
            _artilleryCapability = artilleryCapability;
        }

        public void SetData(IEntity mainTarget, Vector3 formationOffset, int hardPointId, bool holdPosition = false)
        {
            _holdPosition = holdPosition;
            if (mainTarget == null)
            {
                throw new ArgumentNullException(nameof(mainTarget));
            }

            bool targetChanged = !IsTheSameTarget(mainTarget) ||
                                 _focusedHardPointId != hardPointId;
            _focusedHardPointId = hardPointId;
            _mainTargetEntity = mainTarget;
            _mainTarget = _mainTargetEntity.HealthModel;
            LastTargetClass = _mainTarget.ShipClass;
            _mainTargetTransform = _mainTargetEntity.GetFacade<IEntityTransformFacade>().Transform;
            LastTargetPosition = _mainTargetTransform.position;
            _focusedHardPoint = hardPointId == UnitOrderModel.NO_HARD_POINT
                ? null
                : _mainTargetEntity.GetFacade<IHardPointsFacade>().HardPoints[hardPointId];
            formationOffset.y = 0f;
            _formationOffset = formationOffset;
            if (targetChanged)
            {
                _shipMoveComponent.CancelCombatHold();
                ResetPursuit();
                HasReachedRange = false;
            }
        }

        public void SetData(IEntity mainTarget)
        {
            SetData(mainTarget, Vector3.zero, UnitOrderModel.NO_HARD_POINT);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal bool IsHoldingAtRange => _hasHeldPosition && !_hasPursuitDestination;

        /// <summary>Telemetry only: key=value pairs describing why this ship holds or moves on an Attack order.</summary>
        public string AttackDiagnostics
        {
            get
            {
                if (_mainTargetEntity == null) return "";
                Vector3 position = _shipMoveComponent.CurrentPosition;
                Vector3 target = TargetPosition;
                float slotBearing = _formationOffset.sqrMagnitude > 0f
                    ? Mathf.Atan2(_formationOffset.x, _formationOffset.z) * Mathf.Rad2Deg : float.NaN;
                float destDistance = _hasPursuitDestination ? PlanarGeometry.Distance(_pursuitDestination, target) : -1f;
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "hold={0};move={1};legs={2};dist={3:F0};inReach={4};inRange={5};pursuing={6};held={7};" +
                    "slotBearing={8:F0};slotMag={9:F0};destDist={10:F0};artillery={11};artReach={12:F0};engage={13:F0}",
                    _holdReason, _moveReason, _legCount, PlanarGeometry.Distance(position, target),
                    IsInReach ? 1 : 0, IsInRange ? 1 : 0, _hasPursuitDestination ? 1 : 0, _hasHeldPosition ? 1 : 0,
                    slotBearing, _formationOffset.magnitude, destDistance, _usesArtillery ? 1 : 0,
                    _artilleryReach, EngagementRange);
            }
        }

        /// <summary>Telemetry only: the hold/leg change since the last call, or null.</summary>
        public string TakeAttackEvent()
        {
            string pending = _pendingEvent;
            _pendingEvent = null;
            return pending;
        }
#endif

        public bool IsTheSameTarget(IEntity entity)
        {
            return _mainTarget != null && _mainTargetEntity.Id == entity.Id;
        }

        public bool IsTheSameTarget(
            IEntity entity,
            Vector3 formationOffset)
        {
            formationOffset.y = 0f;
            return IsTheSameTarget(entity) &&
                   (_formationOffset - formationOffset).sqrMagnitude <=
                   Mathf.Epsilon;
        }

        public void Enter()
        {
            if (_mainTargetEntity == null || _mainTarget == null)
            {
                throw new InvalidOperationException("AttackTargetState requires a target before Enter.");
            }

            _hasHardPointApproach = false;
            // Ordered attackers come past the cap split; a replacement pick over the cap attacks without a slot.
            _focusFire.TryClaim(_mainTargetEntity);
            if (_mainTargetEntity.TryGetFacade(out IHealthFacade _))
            {
                _attackData = _focusedHardPoint == null || _focusedHardPoint.IsDestroyed
                    ? _attackDataFactory.ConstructData(_mainTargetEntity)
                    : _attackDataFactory.ConstructHardPointData(_mainTargetEntity, _focusedHardPoint.Id);
                _weaponComponent.AddTarget(_attackData, AttackType.OrderedTarget);
                if (!IsComplete) UpdateMoveState();
            }

        }

        public void Tick(float deltaTime)
        {
            if (IsComplete)
            {
                return;
            }

            LastTargetPosition = TargetPosition;

            // Once the chosen hardpoint is gone the order keeps going against the whole ship.
            if (_focusedHardPoint != null && _focusedHardPoint.IsDestroyed)
            {
                _focusedHardPoint = null;
                _hasHardPointApproach = false;
                _attackData = _attackDataFactory.ConstructData(_mainTargetEntity);
                _weaponComponent.AddTarget(_attackData, AttackType.OrderedTarget);
            }

            UpdateMoveState(deltaTime);
        }

        public void Exit()
        {
            _shipMoveComponent.CancelCombatHold();
            _weaponComponent.ResetTarget();
            _focusFire.Release();
            _attackData = null;
            ResetPursuit();
            HasReachedRange = false;
        }

        private void ResetPursuit()
        {
            _hasPursuitDestination = false;
            _hasHeldPosition = false;
            _isHolding = false;
            _stallAttempt = 0;
            _wasMoving = false;
            _hasHardPointApproach = false;
        }

        private void UpdateMoveState(float deltaTime = 0f)
        {
            if (_holdPosition)
            {
                HoldAndLookAtTarget("replacementInReach");
                return;
            }

            // A large ship's circular footprint can extend far beyond its nearest hardpoints.
            if (_attackData != null &&
                (_mainTarget.ShipClass == ShipClass.BattleCruiser || _mainTarget.ShipClass == ShipClass.Dreadnought) &&
                _mainTargetEntity.TryGetFacade(out INavigationFootprintFacade footprint) &&
                footprint.NavigationRadius > _weaponComponent.AttackDistance &&
                TryUpdateHardPointApproach(deltaTime)) return;

            if (_artilleryCapability != null && TryUpdateArtilleryMove(deltaTime)) return;
            if (_isHolding ? IsInHeldReach : IsInReach)
            {
                HoldAndLookAtTarget("inReach");
                return;
            }
            if (RetryStalledMove(deltaTime)) return;

            if (_wasMoving && !_shipMoveComponent.IsMoving)
            {
                _hasPursuitDestination = false;
            }

            _wasMoving = _shipMoveComponent.IsMoving;
            // Keep the chosen endpoint even when our own detour changes the target bearing.
            if (_hasPursuitDestination && _shipMoveComponent.IsMoving) return;

            Vector3 standoffPosition = StandoffPosition;
            float updateDistance = PursuitDestinationUpdateDistance;
            if (_hasPursuitDestination &&
                (standoffPosition - _pursuitDestination).sqrMagnitude < updateDistance * updateDistance)
            {
                return;
            }

            MoveToPursuitDestination(standoffPosition, preserveCourse: true, "approach");
        }

        private bool TryUpdateHardPointApproach(float deltaTime)
        {
            _hardPointScanTimer -= deltaTime;
            // A committed leg ignores the offset until it ends, so a scan only has to catch mount-range entry.
            // Arrival and a new target rescan at once; a moving target is followed by the leg that starts on arrival.
            if (_hasHardPointApproach && _hasPursuitDestination && _shipMoveComponent.IsMoving &&
                _hardPointScanTimer > 0f) return true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var scope = _hardPointApproachMarker.Auto();
#endif
            _hardPointScanTimer = HARD_POINT_SCAN_INTERVAL;
            _hasHardPointApproach = _weaponComponent.TryGetHardPointApproach(_attackData, out Vector3 offset);
            if (!_hasHardPointApproach) return false;
            UpdateHardPointApproach(offset, deltaTime);
            return true;
        }

        private void HoldAndLookAtTarget(string reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var scope = _holdLookAtMarker.Auto();
#endif
            NoteHold(reason);
            _hasHeldPosition = HasReachedRange = true;
            _isHolding = true;
            _hasPursuitDestination = false;
            _wasMoving = false;
            _stallAttempt = 0;
            _shipMoveComponent.HoldPosition(_mainTargetEntity);
            _shipMoveComponent.LookAtTarget(_mainTargetEntity);
        }

        private void UpdateHardPointApproach(Vector3 offset, float deltaTime)
        {
            // A held ship whose nearest mount drifted just out of range keeps holding (leave margin).
            float heldSlack = _weaponComponent.AttackDistance * (ShipEngagement.HELD_REACH_FACTOR - 1f);
            if (offset == Vector3.zero || _isHolding && offset.sqrMagnitude <= heldSlack * heldSlack)
            {
                HoldAndLookAtTarget("hardPointInReach");
                return;
            }

            Vector3 position = _shipMoveComponent.CurrentPosition;
            if (_hasPursuitDestination)
            {
                if (_shipMoveComponent.IsMoving) return;
                _stallRetryTimer -= deltaTime;
                if (_stallRetryTimer > 0f) return;
                _stallAttempt = PlanarGeometry.Distance(position, _lastAttemptPosition) < 0.1f ? _stallAttempt + 1 : 0;
            }

            float angle = STALL_BEARING_STEP_DEGREES * ((_stallAttempt + 1) / 2) *
                          (_stallAttempt % 2 == 1 ? 1f : -1f);
            MoveToPursuitDestination(position + Quaternion.AngleAxis(angle, Vector3.up) * offset,
                preserveCourse: _stallAttempt == 0, _stallAttempt == 0 ? "hardPointApproach" : "hardPointStallRetry");
        }

        private bool TryUpdateArtilleryMove(float deltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var scope = _artilleryMarker.Auto();
#endif
            float previousReach = _artilleryReach;
            int revision = _artilleryCapability.ArtilleryRevision;
            ShipClass targetClass = _mainTarget.ShipClass;
            if (_artilleryRevision != revision || _artilleryTargetClass != targetClass)
            {
                _artilleryRevision = revision;
                _artilleryTargetClass = targetClass;
                _artilleryReach = _artilleryCapability.GetArtilleryRange(targetClass);
            }

            Vector3 targetPosition = TargetPosition;
            float contact = 0f;
            if (_artilleryReach > 0f)
            {
                // A station's map keep-out radius can be much larger than its live navigation hull.
                contact = _mainTargetEntity.TryGetFacade(out IStationHullFacade hull)
                    ? _shipMoveComponent.NavigationRadius + hull.GetHullRadius(targetPosition) + 1f
                    : ShipEngagement.ContactDistance(_shipMoveComponent, _mainTargetEntity);
            }
            bool useArtillery = _artilleryReach > contact;
            if (useArtillery != _usesArtillery || (useArtillery && previousReach != _artilleryReach))
            {
                bool held = _hasHeldPosition;
                ResetPursuit();
                _hasHeldPosition = held;
            }
            _usesArtillery = useArtillery;
            if (!useArtillery) return false;

            float distanceSquared = PlanarGeometry.DistanceSquared(_shipMoveComponent.CurrentPosition, targetPosition);
            float holdReach = _isHolding ? _artilleryReach * ShipEngagement.HELD_REACH_FACTOR : _artilleryReach;
            if ((_isHolding ? IsInHeldReach : IsInReach) || distanceSquared <= holdReach * holdReach)
            {
                HoldAndLookAtTarget("artilleryInReach");
                return true;
            }

            if (_hasPursuitDestination)
            {
                if (_shipMoveComponent.IsMoving) return true;
                _stallRetryTimer -= deltaTime;
                if (_stallRetryTimer > 0f) return true;
                float replanDistance = Mathf.Max(_shipMoveComponent.NavigationRadius,
                    _artilleryReach * ARTILLERY_REPLAN_RANGE_FACTOR);
                bool targetMoved = PlanarGeometry.DistanceSquared(_artilleryPlanTargetPosition, targetPosition) >=
                                   replanDistance * replanDistance;
                if (targetMoved)
                    _stallAttempt = 0;
                else if (_stallAttempt >= ARTILLERY_STALL_RETRIES)
                    return true;
                else
                    _stallAttempt++;
            }

            float approachDistance = Mathf.Max(_artilleryReach * ARTILLERY_APPROACH_FACTOR, contact);
            Vector3 fromTarget = _shipMoveComponent.CurrentPosition - targetPosition;
            Vector3 bearing = PlanarGeometry.NormalizeDirection(
                _hasHeldPosition ? fromTarget : _formationOffset, fromTarget);
            if (_stallAttempt > 0)
            {
                float angle = STALL_BEARING_STEP_DEGREES * ((_stallAttempt + 1) / 2) *
                              (_stallAttempt % 2 == 1 ? 1f : -1f);
                bearing = Quaternion.AngleAxis(angle, Vector3.up) * bearing;
            }

            if (_stallAttempt == 0) _artilleryPlanTargetPosition = targetPosition;
            MoveToPursuitDestination(targetPosition + bearing * approachDistance, preserveCourse: _stallAttempt == 0,
                _stallAttempt == 0 ? "artilleryApproach" : "artilleryStallRetry");
            return true;
        }

        private bool RetryStalledMove(float deltaTime)
        {
            if (!_hasPursuitDestination) return false;
            if (PlanarGeometry.Distance(_shipMoveComponent.CurrentPosition, _lastAttemptPosition) >= 0.1f)
            {
                _stallAttempt = 0;
                return false;
            }

            if (_shipMoveComponent.IsMoving) return false;

            _stallRetryTimer -= deltaTime;
            if (_stallRetryTimer > 0f) return true;

            float angle = STALL_BEARING_STEP_DEGREES * (_stallAttempt / 2 + 1) *
                          (_stallAttempt % 2 == 0 ? 1f : -1f);
            Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * (StandoffPosition - TargetPosition);
            _stallAttempt++;
            MoveToPursuitDestination(TargetPosition + offset, preserveCourse: false, "stallRetry");
            return true;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void NoteHold(string reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_holdReason == reason) return;
            _holdReason = reason;
            _pendingEvent = "hold:" + reason;
#endif
        }

        private void MoveToPursuitDestination(Vector3 destination, bool preserveCourse, string reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _moveReason = reason;
            _holdReason = REASON_MOVING;
            _legCount++;
            _pendingEvent = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "leg:{0} dest=({1:F0},{2:F0}) destDist={3:F0} dist={4:F0} preserveCourse={5}", reason,
                destination.x, destination.z, PlanarGeometry.Distance(destination, TargetPosition),
                PlanarGeometry.Distance(_shipMoveComponent.CurrentPosition, TargetPosition), preserveCourse ? 1 : 0);
#endif
            _pursuitDestination = destination;
            _hasPursuitDestination = true;
            _isHolding = false;
            _lastAttemptPosition = _shipMoveComponent.CurrentPosition;
            _stallRetryTimer = STALL_RETRY_INTERVAL;
            _shipMoveComponent.MoveToPosition(destination, preserveCourse);
            _wasMoving = _shipMoveComponent.IsMoving;
        }
    }
}
