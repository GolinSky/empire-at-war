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
        private Vector3 targetPosition;
        
        public ShipLockMainTargetState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            moveModel = model.GetModelObserver<IShipMoveModelObserver>();
            weaponModel = model.GetModelObserver<IWeaponModelObserver>();
        }

        public void SetData(IHardPointsProvider mainTarget, Vector3 targetPosition)
        {
            this.targetPosition = targetPosition;
            this.mainTarget = mainTarget;
        }

        public override void Enter()
        {
            base.Enter();
              
            float distance = Vector3.Distance(moveModel.CurrentPosition, targetPosition);
            if (!weaponComponent.HasEnoughRange(distance))
            {
                Vector3 lookDirection = shipMoveComponent.CalculateLookDirection(targetPosition);
                float attackDistance = distance - (weaponModel.MaxAttackDistance / 2f);
                Vector3 attackPosition = moveModel.CurrentPosition +
                                         lookDirection.normalized * attackDistance;
                shipMoveComponent.MoveToPosition(attackPosition);
            }
            else
            {
                shipMoveComponent.LookAtTarget(targetPosition);
            }
            
            weaponComponent.AddTarget(new AttackData(mainTarget,
                componentHub.GetComponent(mainTarget.ModelObserver),
                ShipUnitType.Any), AttackType.MainTarget);
        }

        public override void Update()
        {
            base.Update();
            if (!mainTarget.HasUnits)
            {
                StateMachine.ChangeToDefaultState();
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