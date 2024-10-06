using EmpireAtWar.Models.Loading;
using EmpireAtWar.Services.SceneService;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Loading
{
    public class LoadingController : Controller<LoadingModel>, ITickable
    {
        private readonly ISceneService sceneService;

        public LoadingController(LoadingModel model, ISceneService sceneService) : base(model)
        {
            this.sceneService = sceneService;
        }

        public void Tick()
        {

            if (sceneService.IsSceneLoaded)
            {
                sceneService.ActivateScene();
            }
        }
    }
}