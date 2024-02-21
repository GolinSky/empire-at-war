using EmpireAtWar.Commands.Move;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
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
        public PlayerShipCommand(ShipController controller,  SelectionFacade selectionFacade, IMovable movable, IMoveCommand moveCommand, IWeaponCommand weaponCommand) : base(controller)
        {
            AddCommand(selectionFacade.Create(controller.GetModel(), movable));
            AddCommand(moveCommand);
            AddCommand(weaponCommand);
        }
        
    }
}