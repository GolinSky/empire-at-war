using EmpireAtWar.Models.SpaceStation;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Controllers.SpaceStation
{
    public class SpaceStationController : Controller<SpaceStationModel>
    {
        public SpaceStationController(SpaceStationModel model) : base(model)
        {
        }
    }
}