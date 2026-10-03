using Utilities.ScriptUtils.Time;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using EmpireAtWar.Mvc;
using Zenject;

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

    public class SceneService : Service, ISceneService, IInitializable, ILateDisposable
    {
        private const float MIN_SCENE_PROGRESS = 0.88f;

        private readonly SceneData _sceneModel;
        private readonly TimerPoolService _timerPoolService;
        private AsyncOperation _asyncOperation;

        public event Action<SceneType> OnSceneActivation;

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

        public SceneService(SceneData sceneModel, TimerPoolService timerPoolService )
        {
            _sceneModel = sceneModel;
            _timerPoolService = timerPoolService;
        }

        public void Initialize()
        {
            Application.backgroundLoadingPriority = ThreadPriority.High;
            SceneManager.sceneLoaded += HandleLoadingScene;

            TargetScene = _sceneModel.GetCurrentScene();
        }

        public void LateDispose()
        {
            SceneManager.sceneLoaded -= HandleLoadingScene;
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

        private void HandleLoadingScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            OnSceneActivation?.Invoke(_sceneModel.GetSceneType(scene));
            if (_sceneModel.IsLoadingScene(scene))
            {
                _timerPoolService.Invoke(LoadTargetScene, 1f);//todo: move to const
            }
        }

        private void LoadTargetScene()
        {
            _asyncOperation = SceneManager.LoadSceneAsync(_sceneModel.GetScene(TargetScene).SceneName);
            _asyncOperation.allowSceneActivation = false;
        }
    }
}