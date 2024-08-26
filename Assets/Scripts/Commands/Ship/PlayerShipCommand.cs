using EmpireAtWar.Ship;
using LightWeightFramework.Command;

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