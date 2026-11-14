using EmpireAtWar.Components.AttackComponent;
using System;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class AttackTargetState: IBaseState
    {
        // A moving ship only halts early when the target closes inside this fraction.
        private const float MINIMUM_RANGE_FACTOR = 0.5f;
        private const float STALL_RETRY_INTERVAL = 0.5f;
        private const float STALL_BEARING_STEP_DEGREES = 20f;

        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IShipMovement _shipMoveComponent;
        private IHealthModelObserver _mainTarget;
        private IEntity _mainTargetEntity;
        private IHardPointModel _focusedHardPoint;

        private Transform _mainTargetTransform;

        private Vector3 _formationOffset;
        private Vector3 _pursuitDestination;
        private Vector3 _lastAttemptPosition;
        private float _stallRetryTimer;
        private int _stallAttempt;

        private bool _hasPursuitDestination;
        private bool _wasMoving;
        private bool _isClosingRange;

        private Vector3 TargetPosition => _mainTargetTransform.position;
        // The distance depends on what is being attacked: heavy guns hold back from capitals, lasers close on corvettes.
        private float EngagementRange => _weaponComponent.GetEngagementRange(_mainTarget.ShipClass);
        private Vector3 MovementTargetPosition => TargetPosition +
            Vector3.ClampMagnitude(_formationOffset, EngagementRange * 0.8f);
        private Vector3 StandoffPosition => ShipEngagement.GetStandoffPosition(
            _shipMoveComponent, _weaponComponent, _mainTargetEntity);
        private float ContactDistance => ShipEngagement.ContactDistance(_shipMoveComponent, _mainTargetEntity);
        private float PursuitDestinationUpdateDistance => Mathf.Max(
            _shipMoveComponent.NavigationRadius,
            EngagementRange * 0.1f);

        public bool IsComplete => _mainTarget == null || _mainTarget.IsDestroyed || !_mainTarget.HasUnits || _mainTargetEntity.IsCloaked();

        public AttackTargetState(
            IAttackDataFactory attackDataFactory,
            IWeaponComponent weaponComponent,
            IShipMovement shipMoveComponent)
        {
            _attackDataFactory = attackDataFactory;
            _weaponComponent = weaponComponent;
            _shipMoveComponent = shipMoveComponent;
        }

        public void SetData(IEntity mainTarget, Vector3 formationOffset, int hardPointId)
        {
            if (mainTarget == null)
            {
                throw new ArgumentNullException(nameof(mainTarget));
            }

            bool targetChanged = !IsTheSameTarget(mainTarget, formationOffset);
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
                _hasPursuitDestination = false;
                _isClosingRange = false;
                _stallAttempt = 0;
                _wasMoving = false;
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

            if (_mainTargetEntity.TryGetFacade(out IHealthFacade healthFacade))
            {
                AttackData attackData = _focusedHardPoint == null || _focusedHardPoint.IsDestroyed
                    ? _attackDataFactory.ConstructData(_mainTargetEntity)
                    : _attackDataFactory.ConstructHardPointData(_mainTargetEntity, _focusedHardPoint.Id);
                _weaponComponent.AddTarget(attackData, AttackType.MainTarget);
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
                _weaponComponent.AddTarget(_attackDataFactory.ConstructData(_mainTargetEntity), AttackType.MainTarget);
            }

            UpdateMoveState(deltaTime);
        }

        public void Exit()
        {
            _weaponComponent.ResetTarget();
            _hasPursuitDestination = false;
            _isClosingRange = false;
            _stallAttempt = 0;
            _wasMoving = false;
        }

        private void UpdateFormationMove()
        {
            bool inRange = _weaponComponent.HasEnoughRange(
                _shipMoveComponent.GetRange(TargetPosition), ContactDistance, _mainTarget.ShipClass);
            if (inRange && (!_isClosingRange || !_shipMoveComponent.IsMoving))
            {
                if (_shipMoveComponent.IsMoving) _shipMoveComponent.Stop();
                _shipMoveComponent.LookAtTarget(TargetPosition);
                _isClosingRange = true;
                _hasPursuitDestination = false;
                return;
            }

            if (_hasPursuitDestination && _shipMoveComponent.IsMoving) return;

            // After a formation leg, close any remaining gap from its actual endpoint.
            if (_hasPursuitDestination) _isClosingRange = true;
            Vector3 destination = _isClosingRange ? StandoffPosition : MovementTargetPosition;
            MoveToPursuitDestination(destination, preserveCourse: true);
        }

        private void UpdateMoveState(float deltaTime = 0f)
        {
            if (RetryStalledMove(deltaTime)) return;

            if (_formationOffset.sqrMagnitude > Mathf.Epsilon)
            {
                UpdateFormationMove();
                return;
            }

            // Hysteresis: a stopped ship resumes only once the target leaves engage range,
            // and a pursuing ship brakes into a standoff point inside it.
            if (_wasMoving && !_shipMoveComponent.IsMoving)
            {
                _hasPursuitDestination = false;
            }

            _wasMoving = _shipMoveComponent.IsMoving;
            float range = _shipMoveComponent.GetRange(TargetPosition);
            if (!_shipMoveComponent.IsMoving && _weaponComponent.HasEnoughRange(range, ContactDistance, _mainTarget.ShipClass))
            {
                _shipMoveComponent.LookAtTarget(TargetPosition);
                _hasPursuitDestination = false;
                return;
            }

            if (_shipMoveComponent.IsMoving &&
                range <= EngagementRange * MINIMUM_RANGE_FACTOR)
            {
                _shipMoveComponent.Stop();
                _shipMoveComponent.LookAtTarget(TargetPosition);
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

        private bool RetryStalledMove(float deltaTime)
        {
            if (!_hasPursuitDestination) return false;
            if (PlanarGeometry.Distance(_shipMoveComponent.CurrentPosition, _lastAttemptPosition) >= 0.1f)
            {
                _stallAttempt = 0;
                return false;
            }

            if (_shipMoveComponent.IsMoving || _weaponComponent.HasEnoughRange(
                    _shipMoveComponent.GetRange(TargetPosition), ContactDistance, _mainTarget.ShipClass)) return false;

            _stallRetryTimer -= deltaTime;
            if (_stallRetryTimer > 0f) return true;

            float angle = STALL_BEARING_STEP_DEGREES * (_stallAttempt / 2 + 1) *
                          (_stallAttempt % 2 == 0 ? 1f : -1f);
            Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * (StandoffPosition - TargetPosition);
            _stallAttempt++;
            _isClosingRange = true;
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
