using System;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class NavigateState : IBaseState
    {
        private readonly IShipMovement _shipMoveComponent;

        private Vector3 _destination;

        private bool _hasPendingDestination;

        public bool IsComplete => !_shipMoveComponent.IsMoving;

        public NavigateState(IShipMovement shipMoveComponent)
        {
            _shipMoveComponent = shipMoveComponent;
        }

        public void SetWorldDestination(Vector3 destination)
        {
            _destination = destination;
            _hasPendingDestination = true;
        }

        public void Enter()
        {
            if (!_hasPendingDestination)
            {
                throw new InvalidOperationException("NavigateState requires a destination before Enter.");
            }

            _shipMoveComponent.MoveToPosition(_destination);
            _hasPendingDestination = false;
        }

        public void Tick(float deltaTime)
        {
        }

        public void Exit()
        {
        }
    }
}
