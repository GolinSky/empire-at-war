using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;
using Zenject;

namespace EmpireAtWar.Commands.Ship
{
    public interface IShipCommand:ICommand
    {
    }
    public class ShipCommand: Command<ShipController>, IShipCommand, IInitializable
    {

        public ShipCommand(ShipController controller, INavigationService navigationService) : base(controller)
        {
            AddCommand(
                new SelectionCommand(controller, navigationService, controller)
            );
        }
        
        public void Initialize()
        {
        }
    }
}