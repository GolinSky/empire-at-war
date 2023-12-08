using EmpireAtWar.Models.SkirmishCamera;
using LightWeightFramework.Controller;

namespace EmpireAtWar.Controllers.SkirmishCamera
{
    public class SkirmishCameraController:Controller<SkirmishCameraModel>
    {
        public SkirmishCameraController(SkirmishCameraModel model) : base(model)
        {
        }
    }
}