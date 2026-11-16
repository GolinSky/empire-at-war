using System;
using System.Threading;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Services.Networking.Online;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using LifetimeScope = global::VContainer.Unity.LifetimeScope;

namespace EmpireAtWar.Services.SceneService
{
    public class SceneService : Service, ISceneService, IInitializable, ILateDisposable
    {
        private const float MIN_SCENE_PROGRESS = 0.88f;
        private static readonly ProfilerMarker LOADING_SCENE_MARKER = new ProfilerMarker("SceneService.LoadLoadingScene");

        private readonly SceneData _sceneModel;
        private readonly ProjectScope _projectScope;
        private readonly OnlineRecoveryProgress _recovery;
        private AsyncOperation _asyncOperation;
        private IDisposable _sceneParent;
        private Scene _loadingScene;
        private CancellationTokenSource _loadCancellation = new CancellationTokenSource();

        public event Action<SceneType> OnSceneActivation;
        public SceneType TargetScene { get; private set; }

        public bool IsSceneLoaded => _asyncOperation != null && _asyncOperation.progress > MIN_SCENE_PROGRESS;

        public SceneService(SceneData sceneModel, ProjectScope projectScope, OnlineRecoveryProgress recovery)
        {
            _sceneModel = sceneModel;
            _projectScope = projectScope;
            _recovery = recovery;
        }

        public void Initialize()
        {
            Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.BelowNormal;
            SceneManager.sceneLoaded += HandleLoadingScene;
            TargetScene = _sceneModel.GetCurrentScene();
        }

        public void LateDispose()
        {
            _loadCancellation.Cancel();
            _loadCancellation.Dispose();
            _projectScope.BattlePreloader.Dispose();
            SceneManager.sceneLoaded -= HandleLoadingScene;
            if (_sceneParent != null)
            {
                _sceneParent.Dispose();
                _sceneParent = null;
            }
        }

        public void ActivateScene()
        {
            if (!IsSceneLoaded) return;
            _asyncOperation.allowSceneActivation = true;
        }

        public async void LoadScene(SceneType sceneType)
        {
            _loadCancellation.Cancel();
            _loadCancellation.Dispose();
            _loadCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = _loadCancellation.Token;
            try
            {
                if (_asyncOperation != null)
                {
                    AsyncOperation previousLoad = _asyncOperation;
                    previousLoad.allowSceneActivation = true;
                    while (!previousLoad.isDone) await Awaitable.NextFrameAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    _asyncOperation = null;
                }
                _projectScope.BattlePreloader.Dispose();
                // Transitions integrate async loads at up to 50 ms per frame; gameplay keeps background loads cheap.
                Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.High;
                TargetScene = sceneType;
                if (_sceneParent == null)
                    _sceneParent = LifetimeScope.EnqueueParent(_projectScope);
                using (LOADING_SCENE_MARKER.Auto())
                    SceneManager.LoadScene(_sceneModel.GetLoadScene().SceneName, LoadSceneMode.Single);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
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
                _loadingScene = scene;
                SceneType target = TargetScene;
                CancellationToken cancellationToken = _loadCancellation.Token;
                LoadTargetScene(target, cancellationToken);
            }
            else
            {
                Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.BelowNormal;
                if (loadSceneMode == LoadSceneMode.Additive) ReplaceLoadingScene(scene);
            }
        }

        private async void LoadTargetScene(SceneType target, CancellationToken cancellationToken)
        {
            try
            {
                // The loading screen renders its first frame before any load starts.
                await Awaitable.NextFrameAsync(cancellationToken);
                if (target == SceneType.Battle)
                    await _projectScope.BattlePreloader.PrepareAsync(_projectScope.GameData.Battle,
                        _projectScope.GameData.PendingBattleSave != null, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (_sceneParent == null)
                    _sceneParent = LifetimeScope.EnqueueParent(_projectScope);
                // Additive: a Single load would run Unity's blocking unused-asset sweep and GC only to free the
                // loading scene.
                _asyncOperation = SceneManager.LoadSceneAsync(_sceneModel.GetScene(target).SceneName, LoadSceneMode.Additive);
                _asyncOperation.allowSceneActivation = false;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        // Runs before the target scene's Start: later instantiations belong to the target. Recovery retains
        // only the existing loading presentation until synchronization; it has no competing EventSystem.
        private async void ReplaceLoadingScene(Scene target)
        {
            SceneManager.SetActiveScene(target);
            Scene loadingScene = _loadingScene;
            CancellationToken cancellationToken = _loadCancellation.Token;
            try
            {
                // Recovery retains the existing loading canvas while the battle applies its baseline.
                // LoadingUiController has already disabled its camera and listener on target activation.
                while (_recovery.IsActive) await Awaitable.NextFrameAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                foreach (GameObject root in loadingScene.GetRootGameObjects()) root.SetActive(false);
                // Fire-and-forget: roots are already inactive and nothing depends on the unload finishing.
                _ = SceneManager.UnloadSceneAsync(loadingScene);
                _loadingScene = default;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }
    }
}
