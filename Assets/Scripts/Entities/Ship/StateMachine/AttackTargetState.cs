using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class AttackTargetState: IBaseState
    {
        private readonly IAttackDataFactory _attackDataFactory;
        private readonly IWeaponComponent _weaponComponent;
        private readonly IShipMoveComponent _shipMoveComponent;
        private IHealthModelObserver _mainTarget;
        private IEntity _mainTargetEntity;

        private Vector3 TargetPosition => _mainTarget.Transform.position;// REFACTOR THIS


        public AttackTargetState(
            IAttackDataFactory attackDataFactory,
            IWeaponComponent weaponComponent,
            IShipMoveComponent shipMoveComponent)
        {
            _attackDataFactory = attackDataFactory;
            _weaponComponent = weaponComponent;
            _shipMoveComponent = shipMoveComponent;

        }
        
        public void SetData(IEntity mainTarget)
        {
            _mainTargetEntity = mainTarget;
            _mainTarget = _mainTargetEntity.Model.GetModelObserver<IHealthModelObserver>();
        }
        
        public bool IsTheSameTarget(IEntity entity)
        {
            return _mainTarget != null && _mainTargetEntity.Id == entity.Id;
        }
        
        public void Enter()
        {
            if (_mainTargetEntity.TryGetCommand(out IHealthCommand healthCommand))
            {
                AttackData attackData = _attackDataFactory.ConstructData(_mainTargetEntity);
                _weaponComponent.AddTarget(attackData, AttackType.MainTarget);
                
                UpdateMoveState();
            }
            
        }

        public void Update()
        {
            if (!_mainTarget.HasUnits)
            {
                _weaponComponent.ResetTarget();
                //shipMoveComponent.Reset();
                // StateMachine.ChangeToDefaultState();
                //EXIT
                return;
            }

            if (_shipMoveComponent.GetRange(TargetPosition) < _weaponComponent.AttackDistance)
            {
                if (_shipMoveComponent.IsMoving)
                {
                    _shipMoveComponent.Stop();
                }
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