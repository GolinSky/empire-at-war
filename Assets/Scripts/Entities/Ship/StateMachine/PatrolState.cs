using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Patterns.StateMachine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class PatrolState : IBaseState
    {
        private readonly IShipMoveComponent _shipMoveComponent;
        private bool _isPatrolling;

        public PatrolState(IShipMoveComponent shipMoveComponent)
        {
            _shipMoveComponent = shipMoveComponent;
        }

        public void Enter()
        {
            _isPatrolling = false;
        }

        public void Update()
        {
            if (!_shipMoveComponent.IsMoving && !_isPatrolling)
            {
                _shipMoveComponent.MoveAround();
                _isPatrolling = true;
            }
            if (!_shipMoveComponent.IsMoving)
            {
                _isPatrolling = false;
            }
        }

        public void Exit()
        {
            _shipMoveComponent.Stop();
        }
    }
}
