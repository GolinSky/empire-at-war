using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public class FleeState : IBaseState
    {
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly IMapModelObserver _mapModel;
        private readonly PlayerType _playerType;

        public FleeState(IShipMoveComponent shipMoveComponent, IMapModelObserver mapModel, PlayerType playerType)
        {
            _shipMoveComponent = shipMoveComponent;
            _mapModel = mapModel;
            _playerType = playerType;
        }

        public void Enter()
        {
            Vector3 safePosition = _mapModel.GetStationPosition(_playerType);
            _shipMoveComponent.MoveToPosition(safePosition);
        }

        public void Update()
        {
            // Just keep moving to the base if we are not moving
            if (!_shipMoveComponent.IsMoving)
            {
                Vector3 safePosition = _mapModel.GetStationPosition(_playerType);
                _shipMoveComponent.MoveToPosition(safePosition);
            }
        }

        public void Exit()
        {
            _shipMoveComponent.Stop();
        }
    }
}
