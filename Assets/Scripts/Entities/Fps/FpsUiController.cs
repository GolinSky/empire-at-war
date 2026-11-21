using EmpireAtWar.Services.Settings;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Fps
{
    /// <summary>Shows the FPS counter while the player's "Show FPS counter" setting is on.</summary>
    public sealed class FpsUiController : UiController, IInitializable, ITickable, ILateDisposable
    {
        private FpsUi _ui;

        private readonly FpsModel _model;
        private readonly ISettingsService _settings;

        public FpsUiController(IUiService uiService, IUiCancelRouter cancelRouter, FpsModel model, ISettingsService settings)
            : base(uiService, cancelRouter)
        {
            _model = model;
            _settings = settings;
        }

        private bool IsOn => _settings.Saved.Gameplay.ShowFps;

        public void Initialize()
        {
            _ui = CreateUi<FpsUi>();
            _ui.SetModel(_model);
            _ui.Initialize();
            _settings.SavedChanged += ApplyVisibility;
            ApplyVisibility();
        }

        public void LateDispose()
        {
            _settings.SavedChanged -= ApplyVisibility;
            if (!ReferenceEquals(_ui, null)) _ui.Dispose();
        }

        public void Tick()
        {
            if (!IsOn) return;
            SampleFrame(Time.unscaledDeltaTime);
        }

        public void SampleFrame(float unscaledDeltaTime)
        {
            _model.SampleFrame(unscaledDeltaTime);
        }

        private void ApplyVisibility()
        {
            if (IsOn) _ui.Show();
            else _ui.Hide();
        }
    }
}
