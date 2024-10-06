using EmpireAtWar.Models.Map;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Controllers.Map
{
    public interface IMapService
    {
        
    }
    //
    public class MapController: Controller<MapModel>, IMapService
    {
        public MapController(MapModel model) : base(model)
        {
        }
    }
}