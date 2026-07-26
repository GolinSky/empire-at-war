using System;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public sealed class NavigateState : IBaseState
    {
        private readonly IShipMoveComponent _shipMoveComponent;
        private Vector3 _worldDestination;
        private Vector2 _screenDestination;
        private bool _useScreenDestination;
        private bool _hasPendingDestination;

        public NavigateState(IShipMoveComponent shipMoveComponent)
        {
            _shipMoveComponent = shipMoveComponent ??
                throw new ArgumentNullException(nameof(shipMoveComponent));
        }

        public void SetWorldDestination(Vector3 destination)
        {
            _worldDestination = destination;
            _useScreenDestination = false;
            _hasPendingDestination = true;
        }

        public void SetScreenDestination(Vector2 destination)
        {
            _screenDestination = destination;
            _useScreenDestination = true;
            _hasPendingDestination = true;
        }

        public void Enter()
        {
            if (!_hasPendingDestination)
            {
                throw new InvalidOperationException("NavigateState requires a destination before Enter.");
            }

            if (_useScreenDestination)
            {
                _shipMoveComponent.MoveToPositionOnScreen(_screenDestination);
            }
            else
            {
                _shipMoveComponent.MoveToPosition(_worldDestination);
            }

            _hasPendingDestination = false;
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }
    }
}
