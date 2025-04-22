using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipLockMainTargetState:ShipIdleState
    {
        private const float MOVE_TIMER_DELAY = 15f;
        
        private readonly IShipMoveModelObserver _moveModel;
        private readonly IWeaponModelObserver _weaponModel;
        private readonly IHealthModelObserver _targetHealth;
        private IHealthModelObserver _mainTarget;
        private ITimer _moveTimer;
        private Vector3 TargetPosition => _mainTarget.Transform.position;
        
        public ShipLockMainTargetState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            _moveModel = _model.GetModelObserver<IShipMoveModelObserver>();
            _weaponModel = _model.GetModelObserver<IWeaponModelObserver>();
            _moveTimer = TimerFactory.ConstructTimer(MOVE_TIMER_DELAY);
        }

        public void SetData(IHealthModelObserver mainTarget)
        {
            _mainTarget = mainTarget;
            
        }

        public override void Enter()
        {
            base.Enter();
              
            UpdateMoveState();
            
            _weaponComponent.AddTarget(new AttackData(_mainTarget,
                _componentHub.GetComponent(_mainTarget),
                HardPointType.Any), AttackType.MainTarget);
        }

        private void UpdateMoveState()
        {
            float distance = Vector3.Distance(_moveModel.CurrentPosition, TargetPosition);

            if (!_weaponComponent.HasEnoughRange(distance))
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
                _weaponComponent.ResetTarget();
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