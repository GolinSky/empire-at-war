using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class PatrolState : IBaseState
    {
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly IMapModelObserver _mapModelObserver;

        private bool _isPatrolling;

        public PatrolState(IShipMoveComponent shipMoveComponent, IMapModelObserver mapModelObserver)
        {
            _mapModelObserver = mapModelObserver;
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
                var sizeRange = _mapModelObserver.SizeRange;
                float randomX = Random.Range(sizeRange.Min.x, sizeRange.Max.x);
                float randomZ = Random.Range(sizeRange.Min.y, sizeRange.Max.y);
                Vector3 targetPosition = new Vector3(randomX, 0f, randomZ);

                _shipMoveComponent.MoveToPosition(targetPosition);
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
