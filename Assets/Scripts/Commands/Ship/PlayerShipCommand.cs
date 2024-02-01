using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;
using Zenject;

namespace EmpireAtWar.Commands.Ship
{
    public interface IShipCommand:ICommand
    {
    }
    public class PlayerShipCommand: Command<ShipController>, IShipCommand, IInitializable
    {
        public PlayerShipCommand(ShipController controller,  SelectionFacade selectionCommand, IMovable movable) : base(controller)
        {
            AddCommand(selectionCommand.Create(controller.GetModel(), movable));
        }
        
        public void Initialize()
        {
        }
    }
}