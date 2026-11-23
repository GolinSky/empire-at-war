using EmpireAtWar.Services.Networking.Diagnostics;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.PerformanceOverlay
{
    /// <summary>
    /// Shows the FPS counter, plus ping in online battles, while the player's "Show FPS counter" setting is on.
    /// </summary>
    public sealed class PerformanceOverlayUiController : UiController, IInitializable, ITickable, ILateDisposable
    {
        private PerformanceOverlayUi _ui;

        private readonly PerformanceOverlayModel _model;
        private readonly ISettingsService _settings;
        private readonly IPingSource _ping;

        public PerformanceOverlayUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            PerformanceOverlayModel model, ISettingsService settings, IPingSource ping)
            : base(uiService, cancelRouter)
        {
            _model = model;
            _settings = settings;
            _ping = ping;
        }

        private bool IsOn => _settings.Saved.Gameplay.ShowFps;

        public void Initialize()
        {
            _ui = CreateUi<PerformanceOverlayUi>();
            _ui.SetModel(_model);
            _ui.Initialize(_ping.IsOnline);
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
            if (!_model.SampleFrame(unscaledDeltaTime)) return;
            if (_ping.TryGetPingMs(out int pingMs)) _model.SetPing(pingMs);
        }

        private void ApplyVisibility()
        {
            if (IsOn) _ui.Show();
            else _ui.Hide();
        }
    }
}
