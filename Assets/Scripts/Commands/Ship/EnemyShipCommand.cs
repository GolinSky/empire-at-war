using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.Ship
{
    public class EnemyShipCommand : Command<ShipController> , IShipCommand
    {
        public EnemyShipCommand(
            ShipController controller) : base(controller)
        {

        }
    }
}