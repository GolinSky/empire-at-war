using EmpireAtWar.Models.Loading;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Controllers.Loading
{
    //todo: rename it
    public class LoadingController : Controller<LoadingData>, ITickable
    {
        private readonly ISceneService _sceneService;

        public LoadingController(ISceneService sceneService, LoadingData model) : base(model)
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