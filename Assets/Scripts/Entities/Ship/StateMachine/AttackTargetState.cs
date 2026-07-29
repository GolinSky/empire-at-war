using EmpireAtWar.Components.AttackComponent;
using System;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class AttackTargetState: IBaseState
    {
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly LazyInject<StateMachine1> _stateMachine;
        private readonly LazyInject<IdleState> _idleState;
        private IHealthModelObserver _mainTarget;
        private IEntity _mainTargetEntity;
        private Vector3 _formationOffset;

        private Vector3 TargetPosition => _mainTarget.Transform.position;// REFACTOR THIS
        private Vector3 MovementTargetPosition => TargetPosition + _formationOffset;


        public AttackTargetState(
            IAttackDataFactory attackDataFactory,
            IWeaponComponent weaponComponent,
            IShipMoveComponent shipMoveComponent,
            LazyInject<StateMachine1> stateMachine,
            LazyInject<IdleState> idleState)
        {
            _attackDataFactory = attackDataFactory;
            _weaponComponent = weaponComponent;
            _shipMoveComponent = shipMoveComponent;
            _stateMachine = stateMachine;
            _idleState = idleState;
        }
        
        public void SetData(IEntity mainTarget, Vector3 formationOffset)
        {
            _mainTargetEntity = mainTarget ??
                throw new ArgumentNullException(nameof(mainTarget));
            _mainTarget = _mainTargetEntity.HealthModel;
            formationOffset.y = 0f;
            _formationOffset = formationOffset;
        }

        public void SetData(IEntity mainTarget)
        {
            SetData(mainTarget, Vector3.zero);
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

            if (_mainTargetEntity.TryGetCommand(out IHealthCommand healthCommand))
            {
                AttackData attackData = _attackDataFactory.ConstructData(_mainTargetEntity);
                _weaponComponent.AddTarget(attackData, AttackType.MainTarget);
                
                UpdateMoveState();
            }
            
        }

        public void Update()
        {
            if (_mainTarget == null || _mainTarget.IsDestroyed || !_mainTarget.HasUnits)
            {
                _weaponComponent.ResetTarget();
                _mainTarget = null;
                _mainTargetEntity = null;
                _stateMachine.Value.SetState(_idleState.Value);
                return;
            }

            float range = _shipMoveComponent.GetRange(TargetPosition);
            if (_weaponComponent.HasEnoughRange(range))
            {
                if (_shipMoveComponent.IsMoving)
                {
                    _shipMoveComponent.Stop();
                }

                _shipMoveComponent.LookAtTarget(TargetPosition);
                return;
            }

            if (!_shipMoveComponent.IsMoving)
            {
                _shipMoveComponent.MoveToPosition(MovementTargetPosition);
            }
        }

        public void Exit()
        {
        }
        
        private void UpdateMoveState()
        {
            if (!_weaponComponent.HasEnoughRange(_shipMoveComponent.GetRange(TargetPosition)))
            {
                _shipMoveComponent.MoveToPosition(MovementTargetPosition);
            }
            else
            {
                _shipMoveComponent.LookAtTarget(TargetPosition);
            }
        }
    }
}
