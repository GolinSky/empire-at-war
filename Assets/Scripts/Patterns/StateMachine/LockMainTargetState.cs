using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class LockMainTargetState: UnitIdleState
    {
        private readonly IAttackDataFactory _attackDataFactory;
        private IEntity _mainTarget;
        private IHealthModelObserver _healthModel;

        public LockMainTargetState(UnitStateMachine stateMachine, IAttackDataFactory attackDataFactory) : base(stateMachine)
        {
            _attackDataFactory = attackDataFactory;
        }
        
        public void SetData(IEntity mainTarget)
        {
            _mainTarget = mainTarget;
            _healthModel = _mainTarget.Model.GetModelObserver<IHealthModelObserver>();
        }

        public override void Enter()
        {
            base.Enter();

            if (_healthModel.HasUnits && !_healthModel.IsDestroyed)
            {
                if (_mainTarget.TryGetCommand(out IHealthCommand healthCommand))
                {
                    AttackData attackData = _attackDataFactory.ConstructData(_mainTarget);
                    new AttackData(_healthModel,
                        healthCommand, HardPointType.Any);
                    _weaponComponent.AddTarget(attackData, AttackType.MainTarget);
                }
                else
                {
                    Debug.LogError("Entity has not the IHealthCommand");
                    ExitInternal();
                }
            }
            else
            {
                Debug.LogError("Entity has no units or destroyed");
                ExitInternal();
            }
        }

        public override void Update()
        {
            base.Update();
            if (!_healthModel.HasUnits)
            {
                ExitInternal();
            }
            //check if target is alive
        }

        private void ExitInternal()
        {
            StateMachine.ChangeToDefaultState();
        }
    }
}