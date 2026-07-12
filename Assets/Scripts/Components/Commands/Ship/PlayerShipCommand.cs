using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.Ship
{
    public interface IShipCommand:ICommand
    {
    }
    public class PlayerShipCommand: Command<EmpireAtWar.Ship.Ship>, IShipCommand
    {
        public PlayerShipCommand(EmpireAtWar.Ship.Ship ship) : base(ship)
        {
    
        }
        
    }
}
