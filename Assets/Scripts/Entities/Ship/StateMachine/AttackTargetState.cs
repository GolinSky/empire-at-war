using EmpireAtWar.Components.AttackComponent;
using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Patterns.StateMachine;
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
        private const float ARTILLERY_HOLD_FACTOR = 0.95f;
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
        private int _artilleryRevision = -1;
        private ShipClass _artilleryTargetClass;
        private float _artilleryReach;
        private bool _usesArtillery;
        private bool _isArtilleryRetreat;
        private Vector3 _artilleryPlanTargetPosition;

        private Vector3 TargetPosition => _mainTargetTransform.position;
        // The distance depends on what is being attacked: heavy guns hold back from capitals, lasers close on corvettes.
        private float EngagementRange => _weaponComponent.GetEngagementRange(_mainTarget.ShipClass);
        private Vector3 StandoffPosition => ShipEngagement.GetStandoffPosition(
            _shipMoveComponent, _weaponComponent, _mainTargetEntity,
            _hasHeldPosition ? Vector3.zero : _formationOffset);
        private bool IsInRange => ShipEngagement.IsInRange(_shipMoveComponent, _weaponComponent,
            _mainTargetEntity, TargetPosition);
        private bool IsInReach => ShipEngagement.IsInReach(_shipMoveComponent, _weaponComponent,
            _mainTargetEntity, TargetPosition);
        // Hysteresis: a ship that is not mid-pursuit holds anywhere its guns reach; a pursuing ship stops
        // at the nearer pursuit distance so a receding target does not cause stop/start at the edge.
        private bool ShouldHold => _hasPursuitDestination && _shipMoveComponent.IsMoving ? IsInRange : IsInReach;
        private float PursuitDestinationUpdateDistance => Mathf.Max(
            _shipMoveComponent.NavigationRadius,
            EngagementRange * 0.1f);

        /// <summary>The ship has held at firing range on the current target since it was set; a group pace no longer applies.</summary>
        public bool HasReachedRange { get; private set; }

        public bool IsComplete => _mainTarget == null || _mainTarget.IsDestroyed || !_mainTarget.HasUnits || _mainTargetEntity.IsCloaked();

        public AttackTargetState(
            IAttackDataFactory attackDataFactory,
            IWeaponComponent weaponComponent,
            IShipMovement shipMoveComponent,
            IArtilleryCapability artilleryCapability = null)
        {
            _attackDataFactory = attackDataFactory;
            _weaponComponent = weaponComponent;
            _shipMoveComponent = shipMoveComponent;
            _artilleryCapability = artilleryCapability;
        }

        public void SetData(IEntity mainTarget, Vector3 formationOffset, int hardPointId)
        {
            if (mainTarget == null)
            {
                throw new ArgumentNullException(nameof(mainTarget));
            }

            bool targetChanged = !IsTheSameTarget(mainTarget) ||
                                 _focusedHardPointId != hardPointId;
            _focusedHardPointId = hardPointId;
            _mainTargetEntity = mainTarget;
            _mainTarget = _mainTargetEntity.HealthModel;
            _mainTargetTransform = _mainTargetEntity.GetFacade<IEntityTransformFacade>().Transform;
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
            if (_mainTargetEntity.TryGetFacade(out IHealthFacade _))
            {
                _attackData = _focusedHardPoint == null || _focusedHardPoint.IsDestroyed
                    ? _attackDataFactory.ConstructData(_mainTargetEntity)
                    : _attackDataFactory.ConstructHardPointData(_mainTargetEntity, _focusedHardPoint.Id);
                _weaponComponent.AddTarget(_attackData, AttackType.MainTarget);
                UpdateMoveState();
            }

        }

        public void Tick(float deltaTime)
        {
            if (IsComplete)
            {
                return;
            }

            // Once the chosen hardpoint is gone the order keeps going against the whole ship.
            if (_focusedHardPoint != null && _focusedHardPoint.IsDestroyed)
            {
                _focusedHardPoint = null;
                _hasHardPointApproach = false;
                _attackData = _attackDataFactory.ConstructData(_mainTargetEntity);
                _weaponComponent.AddTarget(_attackData, AttackType.MainTarget);
            }

            UpdateMoveState(deltaTime);
        }

        public void Exit()
        {
            _shipMoveComponent.CancelCombatHold();
            _weaponComponent.ResetTarget();
            _attackData = null;
            ResetPursuit();
            HasReachedRange = false;
        }

        private void ResetPursuit()
        {
            _hasPursuitDestination = false;
            _hasHeldPosition = false;
            _stallAttempt = 0;
            _wasMoving = false;
            _hasHardPointApproach = false;
        }

        private void UpdateMoveState(float deltaTime = 0f)
        {
            // A large ship's circular footprint can extend far beyond its nearest hardpoints.
            if (_attackData != null &&
                (_mainTarget.ShipClass == ShipClass.BattleCruiser || _mainTarget.ShipClass == ShipClass.Dreadnought) &&
                _mainTargetEntity.TryGetFacade(out INavigationFootprintFacade footprint) &&
                footprint.NavigationRadius > _weaponComponent.AttackDistance &&
                TryUpdateHardPointApproach(deltaTime)) return;

            if (_artilleryCapability != null && TryUpdateArtilleryMove(deltaTime)) return;
            if (RetryStalledMove(deltaTime)) return;

            // A ship holds while its guns reach the target and resumes only once it leaves that reach.
            if (_wasMoving && !_shipMoveComponent.IsMoving)
            {
                _hasPursuitDestination = false;
            }

            _wasMoving = _shipMoveComponent.IsMoving;
            if (ShouldHold)
            {
                HoldAndLookAtTarget();
                _hasHeldPosition = true;
                _hasPursuitDestination = false;
                return;
            }

            // Keep the chosen endpoint even when our own detour changes the target bearing.
            if (_hasPursuitDestination && _shipMoveComponent.IsMoving) return;

            Vector3 standoffPosition = StandoffPosition;
            float updateDistance = PursuitDestinationUpdateDistance;
            if (_hasPursuitDestination &&
                (standoffPosition - _pursuitDestination).sqrMagnitude < updateDistance * updateDistance)
            {
                return;
            }

            MoveToPursuitDestination(standoffPosition, preserveCourse: true);
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

        private void HoldAndLookAtTarget()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var scope = _holdLookAtMarker.Auto();
#endif
            HasReachedRange = true;
            _shipMoveComponent.HoldPosition(_mainTargetEntity);
            _shipMoveComponent.LookAtTarget(_mainTargetEntity);
        }

        private void UpdateHardPointApproach(Vector3 offset, float deltaTime)
        {
            if (offset == Vector3.zero)
            {
                HoldAndLookAtTarget();
                ResetPursuit();
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
                preserveCourse: _stallAttempt == 0);
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
                ResetPursuit();
            _usesArtillery = useArtillery;
            if (!useArtillery) return false;

            float distanceSquared = PlanarGeometry.DistanceSquared(_shipMoveComponent.CurrentPosition, targetPosition);
            float holdDistance = Mathf.Max(_artilleryReach * ARTILLERY_HOLD_FACTOR, Mathf.Min(_artilleryReach, contact + 1f));
            // Backing off turns the stern to the target, so it only happens when the hulls touch.
            float retreatDistance = contact + 1f;
            bool retreat = distanceSquared < retreatDistance * retreatDistance;
            bool hold = !retreat && distanceSquared <= holdDistance * holdDistance;
            bool moving = _shipMoveComponent.IsMoving;
            _stallRetryTimer -= deltaTime;

            // Finish the committed leg unless the target crosses the opposite edge of the band.
            if (_hasPursuitDestination && moving && (hold || retreat == _isArtilleryRetreat)) return true;
            if (hold)
            {
                HoldAndLookAtTarget();
                ResetPursuit();
                _hasHeldPosition = true;
                return true;
            }

            if (_hasPursuitDestination)
            {
                if (!moving) _shipMoveComponent.LookAtTarget(_mainTargetEntity);
                if (_stallRetryTimer > 0f) return true;
                float replanDistance = Mathf.Max(_shipMoveComponent.NavigationRadius,
                    _artilleryReach * (ARTILLERY_HOLD_FACTOR - ARTILLERY_APPROACH_FACTOR));
                bool targetMoved = PlanarGeometry.DistanceSquared(_artilleryPlanTargetPosition, targetPosition) >=
                                   replanDistance * replanDistance;
                if (retreat != _isArtilleryRetreat || targetMoved)
                    _stallAttempt = 0;
                else if (_stallAttempt >= ARTILLERY_STALL_RETRIES)
                    return true;
                else
                    _stallAttempt++;
            }

            float approachDistance = Mathf.Max(_artilleryReach * ARTILLERY_APPROACH_FACTOR, contact);
            Vector3 fromTarget = _shipMoveComponent.CurrentPosition - targetPosition;
            Vector3 bearing = PlanarGeometry.NormalizeDirection(
                retreat || _hasHeldPosition ? fromTarget : _formationOffset, fromTarget);
            if (_stallAttempt > 0)
            {
                float angle = STALL_BEARING_STEP_DEGREES * ((_stallAttempt + 1) / 2) *
                              (_stallAttempt % 2 == 1 ? 1f : -1f);
                bearing = Quaternion.AngleAxis(angle, Vector3.up) * bearing;
            }

            if (_stallAttempt == 0) _artilleryPlanTargetPosition = targetPosition;
            _isArtilleryRetreat = retreat;
            MoveToPursuitDestination(targetPosition + bearing * approachDistance, preserveCourse: _stallAttempt == 0);
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

            if (_shipMoveComponent.IsMoving || IsInReach) return false;

            _stallRetryTimer -= deltaTime;
            if (_stallRetryTimer > 0f) return true;

            float angle = STALL_BEARING_STEP_DEGREES * (_stallAttempt / 2 + 1) *
                          (_stallAttempt % 2 == 0 ? 1f : -1f);
            Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * (StandoffPosition - TargetPosition);
            _stallAttempt++;
            MoveToPursuitDestination(TargetPosition + offset, preserveCourse: false);
            return true;
        }

        private void MoveToPursuitDestination(Vector3 destination, bool preserveCourse)
        {
            _pursuitDestination = destination;
            _hasPursuitDestination = true;
            _lastAttemptPosition = _shipMoveComponent.CurrentPosition;
            _stallRetryTimer = STALL_RETRY_INTERVAL;
            _shipMoveComponent.MoveToPosition(destination, preserveCourse);
            _wasMoving = _shipMoveComponent.IsMoving;
        }
    }
}
