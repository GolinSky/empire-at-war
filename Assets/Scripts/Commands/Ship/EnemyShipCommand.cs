using EmpireAtWar.Commands.Move;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.Ship;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Ship
{
    public class EnemyShipCommand : Command<ShipController> , IShipCommand
    {
        public EnemyShipCommand(
            ShipController controller,
            EnemySelectionFacade enemySelectionFacade,
            IHealthComponent healthComponent, 
            IMoveCommand moveCommand) : base(controller)
        {
            AddCommand(enemySelectionFacade.Create(controller.GetModel(), healthComponent));
            AddCommand(moveCommand);
        }
    }
}