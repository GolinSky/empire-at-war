using EmpireAtWar.Models.Map;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Controllers.Map
{
    public class MapController: Controller<MapModel>
    {
        public MapController(MapModel model) : base(model)
        {
        }
    }
}