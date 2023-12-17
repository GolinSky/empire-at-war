using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Game;

namespace EmpireAtWar.Commands.SpaceStation
{
    public interface ISpaceStationCommand:ICommand
    {
        
    }
    
    public class SpaceStationCommand:Command<SpaceStationController>, ISpaceStationCommand
    {
        public SpaceStationCommand(SpaceStationController controller,INavigationService navigationService, IGameObserver gameObserver) : base(controller, gameObserver)
        {
            AddCommand(
                    new SelectionCommand(controller, navigationService, controller)
                );
        }
    }
}