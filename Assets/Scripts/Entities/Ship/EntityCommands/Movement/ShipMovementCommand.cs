using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.EntityCommands.Movement
{
    public class ShipMovementCommand:  IMoveCommand
    {
        private readonly PlayerShipStateMachineComponent _playerShipStateMachineComponent;

        public ShipMovementCommand(PlayerShipStateMachineComponent playerShipStateMachineComponent)
        {
            _playerShipStateMachineComponent = playerShipStateMachineComponent;
        }
        public void MoveTo(Vector2 screenPosition)
        {
            _playerShipStateMachineComponent.MoveTo(screenPosition);
        }
    }
}