using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.Planet
{
    public class PlanetController : Controller<PlanetData>
    {
        public PlanetController(PlanetData model) : base(model)
        {
        }
    }
}
