using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Planet;
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
        private readonly SceneModel _sceneModel;
        private const float MIN_SCENE_PROGRESS = 0.88f;
        public event Action<SceneType> OnSceneActivation;

        private readonly ITimerPoolWrapperService _timerPoolWrapperService;

        private AsyncOperation _asyncOperation;

        
        private readonly Dictionary<PlanetType, SceneType> _planetScenes = new Dictionary<PlanetType, SceneType>
        {
            {  PlanetType.Coruscant, SceneType.Coruscant },
            {  PlanetType.Kamino, SceneType.Kamino }
        };
        

        public SceneService(SceneModel sceneModel, ITimerPoolWrapperService timerPoolWrapperService )
        {
            _sceneModel = sceneModel;
            _timerPoolWrapperService = timerPoolWrapperService;
        }
        
        public void LoadSceneByPlanetType(PlanetType planetType)
        {
            if (!_planetScenes.TryGetValue(planetType, out SceneType sceneType))
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
                if (_asyncOperation == null)
                {
                    return false;
                }
                return _asyncOperation.progress > MIN_SCENE_PROGRESS;
            }
        }

       

        public void ActivateScene()//fix this
        {
            if(_asyncOperation == null) return;
            
          //  OnSceneActivation?.Invoke(LoadingScene);
            _asyncOperation.allowSceneActivation = true;
            _asyncOperation = null;
        }


        public void LoadScene(SceneType sceneType)
        {
            TargetScene = sceneType;
            
            SceneManager.LoadScene(_sceneModel.GetLoadScene().SceneName, LoadSceneMode.Single);
        }

        public void Initialize()
        {
            SceneManager.sceneLoaded += HandleLoadingScene;

            TargetScene = _sceneModel.GetCurrentScene();
        }

        public void LateDispose()
        {
            SceneManager.sceneLoaded -= HandleLoadingScene;
        }
        
        private void HandleLoadingScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            OnSceneActivation?.Invoke(_sceneModel.GetSceneType(scene));
            if (_sceneModel.IsLoadingScene(scene))
            {
                _timerPoolWrapperService.Invoke(LoadTargetScene, 1f);//todo: move to const
            }
        }

        private void LoadTargetScene()
        {
            _asyncOperation = SceneManager.LoadSceneAsync(_sceneModel.GetScene(TargetScene).SceneName);
            _asyncOperation.allowSceneActivation = false;
        }
    }
}