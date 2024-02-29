using EmpireAtWar.Models.Settings;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Controllers.Settings
{
    public class SettingsController : Controller<SettingsModel>
    {
        public SettingsController(SettingsModel model) : base(model)
        {
        }
    }
}