using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Commands.Ship
{
    public interface IShipCommand:ICommand
    {
    }
    public class PlayerShipCommand: Command<EmpireAtWar.Ship.Ship>, IShipCommand, IMoveCommand
    {
        public Vector3 WorldPosition => Controller.WorldPosition;

        public PlayerShipCommand(EmpireAtWar.Ship.Ship ship) : base(ship)
        {
        }

        public void MoveTo(Vector2 screenPosition)
        {
            Controller.MoveTo(screenPosition);
        }

        public void MoveTo(Vector3 worldPosition)
        {
            Controller.MoveTo(worldPosition);
        }
    }
}
