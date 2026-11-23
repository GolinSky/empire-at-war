using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Entities.PerformanceOverlay
{
    public sealed class PerformanceOverlayUi : BaseUi
    {
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI pingText;
        private PerformanceOverlayModel _model;

        private bool _isInitialized;

        public void Initialize(bool showPing)
        {
            if (_isInitialized)
            {
                return;
            }

            fpsText.SetText("-- FPS");
            pingText.SetText("-- ms");
            pingText.gameObject.SetActive(showPing);
            _model.OnFramesPerSecondChanged += UpdateFpsText;
            _model.OnPingChanged += UpdatePingText;
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            _model.OnFramesPerSecondChanged -= UpdateFpsText;
            _model.OnPingChanged -= UpdatePingText;
            _isInitialized = false;
        }

        public void SetModel(PerformanceOverlayModel model)
        {
            _model = model;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void UpdateFpsText(int framesPerSecond)
        {
            fpsText.SetText("{0} FPS", framesPerSecond);
        }

        private void UpdatePingText(int pingMs)
        {
            pingText.SetText("{0} ms", pingMs);
        }
    }
}
