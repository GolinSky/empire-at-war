using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Planet;
using EmpireAtWar.Services.TimerPoolWrapperService;
using UnityEngine;
using UnityEngine.SceneManagement;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.SceneService
{
    public interface ISceneService : IService
    {
        event Action<SceneType> OnSceneActivation; 
        void LoadScene(SceneType sceneType);
        void LoadSceneByPlanetType(PlanetType planetType);
        SceneType TargetScene { get; }
        bool IsSceneLoaded { get; }
        void ActivateScene();
    }

    public class SceneService : Service, ISceneService, IInitializable, ILateDisposable
    {
        private readonly SceneModel sceneModel;
        private const float MinSceneProgress = 0.88f;
        public event Action<SceneType> OnSceneActivation;

        private readonly ITimerPoolWrapperService timerPoolWrapperService;

        private AsyncOperation asyncOperation;

        
        private readonly Dictionary<PlanetType, SceneType> planetScenes = new Dictionary<PlanetType, SceneType>
        {
            {  PlanetType.Coruscant, SceneType.Coruscant },
            {  PlanetType.Kamino, SceneType.Kamino }
        };
        

        public SceneService(SceneModel sceneModel, ITimerPoolWrapperService timerPoolWrapperService )
        {
            this.sceneModel = sceneModel;
            this.timerPoolWrapperService = timerPoolWrapperService;
        }
        
        public void LoadSceneByPlanetType(PlanetType planetType)
        {
            if (!planetScenes.TryGetValue(planetType, out SceneType sceneType))
            {
                Debug.LogError($"No scene type for {planetType}");
            }
            
            LoadScene(sceneType);
        }

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

       

        public void ActivateScene()//fix this
        {
            if(asyncOperation == null) return;
            
          //  OnSceneActivation?.Invoke(LoadingScene);
            asyncOperation.allowSceneActivation = true;
            asyncOperation = null;
        }


        public void LoadScene(SceneType sceneType)
        {
            TargetScene = sceneType;
            
            SceneManager.LoadScene(sceneModel.GetLoadScene().SceneName, LoadSceneMode.Single);
        }

        public void Initialize()
        {
            SceneManager.sceneLoaded += HandleLoadingScene;

            TargetScene = sceneModel.GetCurrentScene();
        }

        public void LateDispose()
        {
            SceneManager.sceneLoaded -= HandleLoadingScene;
        }
        
        private void HandleLoadingScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            OnSceneActivation?.Invoke(sceneModel.GetSceneType(scene));
            if (sceneModel.IsLoadingScene(scene))
            {
                timerPoolWrapperService.Invoke(LoadTargetScene, 1f);//todo: move to const
            }
        }

        private void LoadTargetScene()
        {
            asyncOperation = SceneManager.LoadSceneAsync(sceneModel.GetScene(TargetScene).SceneName);
            asyncOperation.allowSceneActivation = false;
        }
    }
}