using System;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities.ScriptUtils.Time;
using LifetimeScope = global::VContainer.Unity.LifetimeScope;

namespace EmpireAtWar.Services.SceneService
{
    public class SceneService : Service, ISceneService, IInitializable, ILateDisposable
    {
        private const float MIN_SCENE_PROGRESS = 0.88f;

        private readonly SceneData _sceneModel;
        private readonly TimerPoolService _timerPoolService;
        private readonly ProjectScope _projectScope;
        private AsyncOperation _asyncOperation;
        private IDisposable _sceneParent;

        public event Action<SceneType> OnSceneActivation;
        public SceneType TargetScene { get; private set; }

        public bool IsSceneLoaded => _asyncOperation != null && _asyncOperation.progress > MIN_SCENE_PROGRESS;

        public SceneService(SceneData sceneModel, TimerPoolService timerPoolService, ProjectScope projectScope)
        {
            _sceneModel = sceneModel;
            _timerPoolService = timerPoolService;
            _projectScope = projectScope;
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
            if (_sceneParent != null)
            {
                _sceneParent.Dispose();
                _sceneParent = null;
            }
        }

        public void ActivateScene()
        {
            if (_asyncOperation == null) return;
            _asyncOperation.allowSceneActivation = true;
            _asyncOperation = null;
        }

        public void LoadScene(SceneType sceneType)
        {
            TargetScene = sceneType;
            if (_sceneParent == null)
            {
                _sceneParent = LifetimeScope.EnqueueParent(_projectScope);
            }
            SceneManager.LoadScene(_sceneModel.GetLoadScene().SceneName, LoadSceneMode.Single);
        }

        private void HandleLoadingScene(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (_sceneParent != null)
            {
                _sceneParent.Dispose();
                _sceneParent = null;
            }

            OnSceneActivation?.Invoke(_sceneModel.GetSceneType(scene));
            if (_sceneModel.IsLoadingScene(scene))
            {
                _timerPoolService.Invoke(LoadTargetScene, 1f);
            }
        }

        private void LoadTargetScene()
        {
            if (_sceneParent == null)
            {
                _sceneParent = LifetimeScope.EnqueueParent(_projectScope);
            }
            _asyncOperation = SceneManager.LoadSceneAsync(_sceneModel.GetScene(TargetScene).SceneName);
            _asyncOperation.allowSceneActivation = false;
        }
    }
}
