using EmpireAtWar.Controllers.SpaceStation;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.SpaceStation
{
    public interface ISpaceStationCommand:ICommand
    {
        
    }
    
    public class SpaceStationCommand:Command<SpaceStationController>, ISpaceStationCommand
    {
        public SpaceStationCommand(SpaceStationController controller,INavigationService navigationService) : base(controller)
        {
            AddCommand(
                    new SelectionCommand(controller, navigationService, controller)
                );
        }
    }
}