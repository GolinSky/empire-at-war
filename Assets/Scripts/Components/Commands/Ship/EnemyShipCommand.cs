using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.Ship
{
    public class EnemyShipCommand : Command<EmpireAtWar.Ship.Ship> , IShipCommand
    {
        public EnemyShipCommand(
            EmpireAtWar.Ship.Ship ship) : base(ship)
        {

        }
    }
}
