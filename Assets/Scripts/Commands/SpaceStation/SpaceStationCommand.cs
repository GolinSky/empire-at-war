using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Game;

namespace EmpireAtWar.Commands.SpaceStation
{
    public interface IShipCommand:ICommand
    {
        
    }
    
    public class SpaceStationCommand:Command<SpaceStationController>
    {
        public SpaceStationCommand(SpaceStationController controller,INavigationService navigationService, IGameObserver gameObserver) : base(controller, gameObserver)
        {
            AddCommand(
                    new SelectionCommand(controller, navigationService, controller)
                );
        }
    }
}