using System;
using System.Collections.Generic;
using EmpireAtWar.Services.TimerPoolWrapperService;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorkShop.LightWeightFramework.Service;
using Zenject;

namespace EmpireAtWar.Services.SceneService
{
    public interface ISceneService : IService
    {
        event Action<SceneType> OnSceneActivation; 
        void LoadScene(SceneType sceneType);
        SceneType TargetScene { get; }
        bool IsSceneLoaded { get; }
        void ActivateScene();
    }

    public class SceneService : Service, ISceneService, IInitializable, ILateDisposable
    {
        private const float MinSceneProgress = 0.88f;
        private const int LoadingBuildIndex = 2;
        public event Action<SceneType> OnSceneActivation;

        private readonly ITimerPoolWrapperService timerPoolWrapperService;

        private AsyncOperation asyncOperation;

        private readonly Dictionary<SceneType, int> buildIndexDictionary = new Dictionary<SceneType, int>
        {
            { SceneType.MainMenu, 0 },
            { SceneType.Skirmish, 1 },
            { SceneType.Loading, LoadingBuildIndex }
        };

        
        private readonly Dictionary<int, SceneType> reverseBuildIndexDictionary = new Dictionary<int, SceneType>
        {
            {  0, SceneType.MainMenu },
            {  1, SceneType.Skirmish },
            { LoadingBuildIndex, SceneType.Loading }
        };

        public SceneType TargetScene { get; private set; }

        public bool IsSceneLoaded
        {
            get
            {
                if (asyncOperation == null)
                {
                    return false;
                }
                return asyncOperation.progress > MinSceneProgress;
            }
        }

        public SceneService(ITimerPoolWrapperService timerPoolWrapperService )
        {
            this.timerPoolWrapperService = timerPoolWrapperService;
        }

        public void ActivateScene()//fix this
        {
            if(asyncOperation == null) return;
            
          //  OnSceneActivation?.Invoke(LoadingScene);
            asyncOperation.allowSceneActivation = true;
        }


        public void LoadScene(SceneType sceneType)
        {
            TargetScene = sceneType;
            SceneManager.LoadScene(buildIndexDictionary[SceneType.Loading], LoadSceneMode.Single);
        }

        public void Initialize()
        {
            SceneManager.sceneLoaded += HandleLoadingScene;
            TargetScene = reverseBuildIndexDictionary[SceneManager.GetActiveScene().buildIndex];
        }

        public void LateDispose()
        {
            SceneManager.sceneLoaded -= HandleLoadingScene;
        }
        
        private void HandleLoadingScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            OnSceneActivation?.Invoke(reverseBuildIndexDictionary[scene.buildIndex]);
            if (scene.buildIndex == LoadingBuildIndex)
            {
                timerPoolWrapperService.Invoke(LoadTargetScene, 1f);
            }
        }

        private void LoadTargetScene()
        {
            asyncOperation = SceneManager.LoadSceneAsync(buildIndexDictionary[TargetScene]);
            asyncOperation.allowSceneActivation = false;
        }
    }
}