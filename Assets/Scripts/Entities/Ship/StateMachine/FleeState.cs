using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class FleeState : IBaseState
    {
        private readonly IShipMovement _shipMoveComponent;
        private readonly IMapModelObserver _mapModel;

        private readonly PlayerId _owner;

        public bool IsComplete => false;

        public FleeState(
            IShipMovement shipMoveComponent,
            IMapModelObserver mapModel,
            PlayerId owner)
        {
            _shipMoveComponent = shipMoveComponent;
            _mapModel = mapModel;
            _owner = owner;
        }

        public void Enter()
        {
            Vector3 safePosition = _mapModel.GetStationPosition(_owner);
            _shipMoveComponent.MoveToPosition(safePosition);
        }

        public void Tick(float deltaTime)
        {
        }

        public void Exit()
        {
            _shipMoveComponent.Stop();
        }
    }
}
