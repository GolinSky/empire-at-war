using EmpireAtWar.Components.AttackComponent;
using System;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class AttackTargetState: IBaseState
    {
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IShipMovement _shipMoveComponent;
        private IHealthModelObserver _mainTarget;
        private IEntity _mainTargetEntity;
        private Transform _mainTargetTransform;
        private IHardPointModel _focusedHardPoint;
        private Vector3 _formationOffset;
        private Vector3 _pursuitDestination;
        private bool _hasPursuitDestination;
        private bool _wasMoving;
        private bool _isClosingRange;

        private Vector3 TargetPosition => _mainTargetTransform.position;
        private Vector3 MovementTargetPosition => TargetPosition +
            Vector3.ClampMagnitude(_formationOffset, _weaponComponent.AttackDistance * 0.8f);
        private float PursuitDestinationUpdateDistance => Mathf.Max(
            _shipMoveComponent.NavigationRadius,
            _weaponComponent.AttackDistance * 0.1f);

        public AttackTargetState(
            IAttackDataFactory attackDataFactory,
            IWeaponComponent weaponComponent,
            IShipMovement shipMoveComponent)
        {
            _attackDataFactory = attackDataFactory;
            _weaponComponent = weaponComponent;
            _shipMoveComponent = shipMoveComponent;
        }

        public bool IsComplete => _mainTarget == null || _mainTarget.IsDestroyed || !_mainTarget.HasUnits;

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

            UpdateMoveState();
        }

        public void Exit()
        {
            _weaponComponent.ResetTarget();
            _hasPursuitDestination = false;
            _isClosingRange = false;
        }

        private void UpdateFormationMove()
        {
            bool inRange = _weaponComponent.HasEnoughRange(
                _shipMoveComponent.GetRange(TargetPosition));
            if (_isClosingRange && inRange)
            {
                if (_shipMoveComponent.IsMoving || _shipMoveComponent.IsBlocked)
                {
                    _shipMoveComponent.Stop();
                }

                _shipMoveComponent.LookAtTarget(TargetPosition);
                return;
            }

            Vector3 destination = _isClosingRange ? TargetPosition : MovementTargetPosition;
            float updateDistance = PursuitDestinationUpdateDistance;
            if (_hasPursuitDestination &&
                (destination - _pursuitDestination).sqrMagnitude < updateDistance * updateDistance)
            {
                if (_shipMoveComponent.IsMoving || _shipMoveComponent.IsBlocked)
                {
                    return;
                }

                if (inRange)
                {
                    _shipMoveComponent.LookAtTarget(TargetPosition);
                    return;
                }

                // A congested or map-clamped slot can be outside weapon range.
                // Close the remaining distance through the same reservation allocator.
                _isClosingRange = true;
                destination = TargetPosition;
            }

            _pursuitDestination = destination;
            _hasPursuitDestination = true;
            _shipMoveComponent.MoveToPosition(destination, preserveCourse: true);
        }

        private void UpdateMoveState()
        {
            if (_formationOffset.sqrMagnitude > Mathf.Epsilon)
            {
                UpdateFormationMove();
                return;
            }

            if (_wasMoving && !_shipMoveComponent.IsMoving)
            {
                _hasPursuitDestination = false;
            }

            _wasMoving = _shipMoveComponent.IsMoving;
            if (_weaponComponent.HasEnoughRange(
                    _shipMoveComponent.GetRange(TargetPosition)))
            {
                if (_shipMoveComponent.IsMoving || _shipMoveComponent.IsBlocked)
                {
                    _shipMoveComponent.Stop();
                }

                _shipMoveComponent.LookAtTarget(TargetPosition);
                _hasPursuitDestination = false;
                return;
            }

            Vector3 movementTargetPosition = MovementTargetPosition;
            if (_hasPursuitDestination &&
                _weaponComponent.HasEnoughRange(Vector3.Distance(
                    _pursuitDestination,
                    TargetPosition)))
            {
                return;
            }

            if (_hasPursuitDestination &&
                (movementTargetPosition - _pursuitDestination).sqrMagnitude <
                PursuitDestinationUpdateDistance *
                PursuitDestinationUpdateDistance)
            {
                return;
            }

            _pursuitDestination = movementTargetPosition;
            _hasPursuitDestination = true;
            _shipMoveComponent.MoveToPosition(_pursuitDestination, preserveCourse: true);
            _wasMoving = _shipMoveComponent.IsMoving;
        }
    }
}
