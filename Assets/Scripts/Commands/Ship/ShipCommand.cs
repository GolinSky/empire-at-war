using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Game;
using Zenject;

namespace EmpireAtWar.Commands.Ship
{
    public interface IShipCommand:ICommand
    {
    }
    public class ShipCommand: Command<ShipController>, IShipCommand, IInitializable
    {

        public ShipCommand(ShipController controller, IGameObserver gameObserver, INavigationService navigationService) : base(controller, gameObserver)
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