using EmpireAtWar.Models.Terrain;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Controllers.Terrain
{
    public class TerrainController:Controller<TerrainModel>
    {
        public TerrainController(TerrainModel model) : base(model)
        {
        }

    }
}