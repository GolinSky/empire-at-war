using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipLockMainTargetState:ShipIdleState
    {
        private readonly IShipMoveModelObserver moveModel;
        private readonly IWeaponModelObserver weaponModel;
        private readonly IHealthModelObserver targetHealth;
        private IHardPointsProvider mainTarget;
        private Vector3 TargetPosition => mainTarget.Transform.position;
        
        public ShipLockMainTargetState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            moveModel = model.GetModelObserver<IShipMoveModelObserver>();
            weaponModel = model.GetModelObserver<IWeaponModelObserver>();
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
                Vector3 positionToMove = Vector3.Lerp(moveModel.CurrentPosition, TargetPosition, 0.8f);//todo move to so
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

            if (!moveModel.IsMoving)
            {
                UpdateMoveState();
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