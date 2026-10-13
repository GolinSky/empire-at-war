using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.SceneService
{
    public interface ISceneService : IService
    {
        event Action<SceneType> OnSceneActivation;
        SceneType TargetScene { get; }
        bool IsSceneLoaded { get; }
        void LoadScene(SceneType sceneType);
        void ActivateScene();
    }
}
