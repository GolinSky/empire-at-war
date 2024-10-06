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
        
        private readonly IShipMoveModelObserver moveModel;
        private readonly IWeaponModelObserver weaponModel;
        private readonly IHealthModelObserver targetHealth;
        private IHardPointsProvider mainTarget;
        private ITimer moveTimer;
        private Vector3 TargetPosition => mainTarget.Transform.position;
        
        public ShipLockMainTargetState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            moveModel = model.GetModelObserver<IShipMoveModelObserver>();
            weaponModel = model.GetModelObserver<IWeaponModelObserver>();
            moveTimer = TimerFactory.ConstructTimer(MOVE_TIMER_DELAY);
        }

        public void SetData(IHardPointsProvider mainTarget)
        {
            this.mainTarget = mainTarget;
            
        }

        public override void Enter()
        {
            base.Enter();
              
            UpdateMoveState();
            
            weaponComponent.AddTarget(new AttackData(mainTarget,
                componentHub.GetComponent(mainTarget.ModelObserver),
                ShipUnitType.Any), AttackType.MainTarget);
        }

        private void UpdateMoveState()
        {
            float distance = Vector3.Distance(moveModel.CurrentPosition, TargetPosition);

            if (!weaponComponent.HasEnoughRange(distance))
            {
                Vector3 positionToMove = Vector3.Lerp(moveModel.CurrentPosition, TargetPosition, 0.8f);//todo move to SO
                shipMoveComponent.MoveToPosition(positionToMove);
            }
            else
            {
                shipMoveComponent.LookAtTarget(TargetPosition);
            }
        }

        public override void Update()
        {
            base.Update();
            if (!mainTarget.HasUnits)
            {
                StateMachine.ChangeToDefaultState();
                return;
            }

            if (moveTimer.IsComplete)
            {
                if (moveModel.IsMoving)
                {
                    UpdateMoveState();
                    moveTimer?.StartTimer();
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