using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorkShop.LightWeightFramework.Service;

namespace EmpireAtWar.Services.SceneService
{
    public interface ISceneService : IService
    {
        void LoadScene(SceneType sceneType);
        SceneType LoadingScene { get; }
        bool IsSceneLoaded { get; }
        void ActivateScene();
    }

    public class SceneService : Service, ISceneService
    {
        private readonly Dictionary<SceneType, int> buildIndexDictionary = new Dictionary<SceneType, int>
        {
            { SceneType.MainMenu, 0 },
            { SceneType.Skirmish, 1 },
            { SceneType.Loading, 2 }
        };

        private AsyncOperation asyncOperation;

        public SceneType LoadingScene { get; private set; }

        public bool IsSceneLoaded
        {
            get
            {
                if (asyncOperation == null)
                {
                    return true;
                }

                return asyncOperation.isDone;
            }
        }

        public void ActivateScene()
        {
            if(asyncOperation == null) return;
            
            asyncOperation.allowSceneActivation = true;
        }

        public void LoadScene(SceneType sceneType)
        {
            LoadingScene = sceneType;
            SceneManager.LoadScene(buildIndexDictionary[SceneType.Loading], LoadSceneMode.Single);
            asyncOperation = SceneManager.LoadSceneAsync(buildIndexDictionary[LoadingScene], LoadSceneMode.Single);
            asyncOperation.allowSceneActivation = false;
        }
    }
}