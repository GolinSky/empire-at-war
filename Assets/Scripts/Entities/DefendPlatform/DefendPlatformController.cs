using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatformController : Controller<DefendPlatformModel>
    {
        public DefendPlatformController(
            DefendPlatformModel model) : base(model)
        {
          
        }
        
    }
}