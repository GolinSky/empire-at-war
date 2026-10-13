using EmpireAtWar.Services.SceneService;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Loading
{
    public sealed class SceneActivationFlow : ITickable
    {
        private readonly ISceneService _sceneService;

        public SceneActivationFlow(ISceneService sceneService) { _sceneService = sceneService; }

        public void Tick()
        {
            if (_sceneService.IsSceneLoaded) _sceneService.ActivateScene();
        }
    }
}
