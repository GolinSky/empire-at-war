using EmpireAtWar.Models.Movement;
using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class MoveToPointState:ShipIdleState
    {
        private Vector3 targetPosition;
        private IShipMoveModelObserver shipMoveModelObserver;
        private bool useScreenCoordinates;
        public MoveToPointState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            shipMoveModelObserver = model.GetModelObserver<IShipMoveModelObserver>();
        }
        
        public override void Enter()
        {
            base.Enter();
            if (useScreenCoordinates)
            {
                shipMoveComponent.MoveToPositionOnScreen(targetPosition);
            }
            else
            {
                shipMoveComponent.MoveToPosition(targetPosition);

            }
        }

        public void SetScreenCoordinates(Vector2 screenPosition)
        {
            useScreenCoordinates = true;
            targetPosition = screenPosition;
        }
        
        public void SetWorldCoordinates(Vector3 screenPosition)
        {
            useScreenCoordinates = false;
            targetPosition = screenPosition;
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