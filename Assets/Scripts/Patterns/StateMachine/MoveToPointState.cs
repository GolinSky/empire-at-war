using EmpireAtWar.Models.Movement;
using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class MoveToPointState:ShipIdleState
    {
        private Vector3 screenPosition;
        private IShipMoveModelObserver shipMoveModelObserver;
        public MoveToPointState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            shipMoveModelObserver = model.GetModelObserver<IShipMoveModelObserver>();
        }
        
        public override void Enter()
        {
            base.Enter();
            shipMoveComponent.MoveToPositionOnScreen(screenPosition);
        }

        public void SetCoordinates(Vector2 screenPosition)
        {
            this.screenPosition = screenPosition;
        }

        public override void Update()
        {
            base.Update();
            if (!shipMoveModelObserver.IsMoving)
            {
                StateMachine.ChangeToDefaultState();
            }
        }

        protected override void HandleHealth()
        {
            // do not do anything here
            // maybe engine boost 
        }
    }
}