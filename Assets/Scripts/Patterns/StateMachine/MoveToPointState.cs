using EmpireAtWar.Models.Movement;
using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class MoveToPointState:ShipIdleState
    {
        private Vector3 _targetPosition;
        private IShipMoveModelObserver _shipMoveModelObserver;
        private bool _useScreenCoordinates;
        public MoveToPointState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            _shipMoveModelObserver = _model.GetModelObserver<IShipMoveModelObserver>();
        }
        
        public override void Enter()
        {
            base.Enter();
            if (_useScreenCoordinates)
            {
                _shipMoveComponent.MoveToPositionOnScreen(_targetPosition);
            }
            else
            {
                _shipMoveComponent.MoveToPosition(_targetPosition);

            }
        }

        public void SetScreenCoordinates(Vector2 screenPosition)
        {
            _useScreenCoordinates = true;
            _targetPosition = screenPosition;
        }
        
        public void SetWorldCoordinates(Vector3 screenPosition)
        {
            _useScreenCoordinates = false;
            _targetPosition = screenPosition;
        }

        public override void Update()
        {
            base.Update();
            if (!_shipMoveModelObserver.IsMoving)
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