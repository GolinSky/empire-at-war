using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Controllers.SpaceStation;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.SpaceStation
{
    public interface ISpaceStationCommand:ICommand
    {
        
    }
    
    public class SpaceStationCommand:Command<SpaceStationController>, ISpaceStationCommand
    {
        public SpaceStationCommand(SpaceStationController controller, SelectionFacade selectionFacade) : base(controller)
        {
        
        }
    }
}