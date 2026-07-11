using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.Ship
{
    public interface IShipCommand:ICommand
    {
    }
    public class PlayerShipCommand: Command<ShipController>, IShipCommand
    {
        public PlayerShipCommand(ShipController controller) : base(controller)
        {
    
        }
        
    }
}