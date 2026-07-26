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
        private const float REPATH_INTERVAL = 0.5f;

        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly LazyInject<StateMachine1> _stateMachine;
        private readonly LazyInject<IdleState> _idleState;
        private IHealthModelObserver _mainTarget;
        private IEntity _mainTargetEntity;
        private float _repathTimer;

        private Vector3 TargetPosition => _mainTarget.Transform.position;// REFACTOR THIS


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
        
        public void SetData(IEntity mainTarget)
        {
            _mainTargetEntity = mainTarget;
            _mainTarget = _mainTargetEntity.HealthModel;
        }
        
        public bool IsTheSameTarget(IEntity entity)
        {
            return _mainTarget != null && _mainTargetEntity.Id == entity.Id;
        }
        
        public void Enter()
        {
            if (_mainTargetEntity == null || _mainTarget == null)
            {
                throw new InvalidOperationException("AttackTargetState requires a target before Enter.");
            }

            _repathTimer = 0f;
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

            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                _shipMoveComponent.MoveToPosition(TargetPosition);
                _repathTimer = REPATH_INTERVAL;
            }
        }

        public void Exit()
        {
        }
        
        private void UpdateMoveState()
        {
            if (!_weaponComponent.HasEnoughRange(_shipMoveComponent.GetRange(TargetPosition)))
            {
                _shipMoveComponent.MoveToPosition(TargetPosition);
            }
            else
            {
                _shipMoveComponent.LookAtTarget(TargetPosition);
            }
        }
    }
}
