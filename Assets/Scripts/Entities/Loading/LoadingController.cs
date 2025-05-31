using EmpireAtWar.Models.Loading;
using EmpireAtWar.Services.SceneService;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Loading
{
    //todo: rename it
    public class LoadingController : Controller<LoadingModel>, ITickable
    {
        private readonly ISceneService _sceneService;

        public LoadingController(LoadingModel model, ISceneService sceneService) : base(model)
        {
            _sceneService = sceneService;
        }

        public void Tick()
        {

            if (_sceneService.IsSceneLoaded)
            {
                _sceneService.ActivateScene();
            }
        }
    }
}