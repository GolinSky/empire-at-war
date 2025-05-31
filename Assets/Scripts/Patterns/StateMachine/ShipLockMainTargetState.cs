using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipLockMainTargetState:ShipIdleState
    {
        private readonly IAttackDataFactory _attackDataFactory;
        private const float MOVE_TIMER_DELAY = 15f;
        
        private readonly IShipMoveModelObserver _moveModel;
        private readonly IAttackModelObserver _attackModel;
        private readonly IHealthModelObserver _targetHealth;
        private readonly ITimer _moveTimer;

        private IHealthModelObserver _mainTarget;
        private IEntity _mainTargetEntity;
        private Vector3 TargetPosition => _mainTarget.Transform.position;
        
        public ShipLockMainTargetState(ShipStateMachine stateMachine, IAttackDataFactory attackDataFactory) : base(stateMachine)
        {
            _attackDataFactory = attackDataFactory;
            _moveModel = _model.GetModelObserver<IShipMoveModelObserver>();
            _attackModel = _model.GetModelObserver<IAttackModelObserver>();
            _moveTimer = TimerFactory.ConstructTimer(MOVE_TIMER_DELAY);
        }

        public void SetData(IEntity mainTarget)
        {
            _mainTargetEntity = mainTarget;
            _mainTarget = _mainTargetEntity.Model.GetModelObserver<IHealthModelObserver>();
        }

        public override void Enter()
        {
            base.Enter();
            
            if (_mainTargetEntity.TryGetCommand(out IHealthCommand healthCommand))
            {
                AttackData attackData = _attackDataFactory.ConstructData(_mainTargetEntity);
                _attackComponent.AddTarget(attackData, AttackType.MainTarget);
                
                UpdateMoveState();
            }
        }

        private void UpdateMoveState()
        {
            float distance = Vector3.Distance(_moveModel.CurrentPosition, TargetPosition);

            if (!_attackComponent.HasEnoughRange(distance))
            {
                Vector3 positionToMove = Vector3.Lerp(_moveModel.CurrentPosition, TargetPosition, 0.5f);//todo move to SO
                _shipMoveComponent.MoveToPosition(positionToMove);
            }
            else
            {
                _shipMoveComponent.LookAtTarget(TargetPosition);
            }
        }

        public override void Update()
        {
            base.Update();
            if (!_mainTarget.HasUnits)
            {
                _attackComponent.ResetTarget();
                //shipMoveComponent.Reset();
                StateMachine.ChangeToDefaultState();
                return;
            }

            if (_moveTimer.IsComplete)
            {
                if (_moveModel.IsMoving)
                {
                    UpdateMoveState();
                    _moveTimer?.StartTimer();
                }
            }
        
            
            //check if target is alive
        }

        public override void Exit()
        {
            base.Exit();
            //remove main target
        }

        protected override void HandleHealth()
        {
            //regroup but near to main target
        }
    }
}