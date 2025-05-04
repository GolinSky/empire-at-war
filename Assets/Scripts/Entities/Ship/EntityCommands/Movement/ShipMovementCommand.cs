using EmpireAtWar.Components.Ship.AiComponent;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.EntityCommands.Movement
{
    public class ShipMovementCommand:  IMoveCommand
    {
        private readonly PlayerStateComponent _playerStateComponent;

        public ShipMovementCommand(PlayerStateComponent playerStateComponent)
        {
            _playerStateComponent = playerStateComponent;
        }
        public void MoveTo(Vector2 screenPosition)
        {
            _playerStateComponent.MoveTo(screenPosition);
        }
    }
}