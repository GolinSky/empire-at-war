using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Ship
{
    public interface IShipCommand:ICommand
    {
    }
    public class PlayerShipCommand: Command<ShipController>, IShipCommand
    {
        public PlayerShipCommand(ShipController controller,  SelectionFacade selectionFacade, IMovable movable) : base(controller)
        {
            AddCommand(selectionFacade.Create(controller.GetModel(), movable));
        }
        
    }
}