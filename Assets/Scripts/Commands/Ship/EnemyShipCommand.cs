using EmpireAtWar.Controllers.Ship;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Ship
{
    
    public class EnemyShipCommand : Command<ShipController> , IShipCommand
    {
        public EnemyShipCommand(ShipController controller) : base(controller)
        {
        }
    }
}