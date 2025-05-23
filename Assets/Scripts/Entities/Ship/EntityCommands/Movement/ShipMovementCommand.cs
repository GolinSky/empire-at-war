using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.EntityCommands.Movement
{
    public class ShipMovementCommand:  IMoveCommand
    {
        private readonly PlayerShipStateMachine _playerShipStateMachine;

        public ShipMovementCommand(PlayerShipStateMachine playerShipStateMachine)
        {
            _playerShipStateMachine = playerShipStateMachine;
        }
        public void MoveTo(Vector2 screenPosition)
        {
            _playerShipStateMachine.MoveTo(screenPosition);
        }
    }
}