using LightWeightFramework.Command;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface ISpaceStationCommand:ICommand
    {
        
    }
    public class SpaceStationController : Controller<SpaceStationModel>
    {
        public SpaceStationController(SpaceStationModel model) : base(model)
        {

        }
    }
}