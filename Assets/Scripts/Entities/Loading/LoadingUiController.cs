using EmpireAtWar.Ui.Base;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.SceneService;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Loading
{
    public sealed class LoadingUiController : UiController, IInitializable, ILateDisposable
    {
        private readonly LoadingUi _ui;
        private readonly OnlineRecoveryProgress _recovery;
        private readonly ISceneService _scenes;

        public LoadingUiController(LoadingUi ui, OnlineRecoveryProgress recovery, ISceneService scenes)
        {
            _ui = ui;
            _recovery = recovery;
            _scenes = scenes;
        }

        public void Initialize()
        {
            _recovery.Changed += Render;
            _scenes.OnSceneActivation += OnSceneActivation;
            Render();
            _ui.Show();
        }

        private void Render() => _ui.RenderStatus(_recovery.IsActive ? _recovery.Status : string.Empty);

        private void OnSceneActivation(SceneType scene)
        {
            if (scene != SceneType.Loading) _ui.HideSceneCamera();
        }

        public void LateDispose()
        {
            _recovery.Changed -= Render;
            _scenes.OnSceneActivation -= OnSceneActivation;
            if (_ui != null) _ui.Hide();
        }
    }
}
