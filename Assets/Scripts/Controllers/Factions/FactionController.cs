using EmpireAtWar.Models.Factions;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Controllers.Factions
{
    public class FactionController : Controller<FactionModel>
    {
        public FactionController(FactionModel model) : base(model)
        {
        }
    }
}
