using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;

namespace EmpireAtWar.Entities.Fps
{
    public sealed class FpsUi : BaseUi, IFpsUi
    {
        [SerializeField] private TextMeshProUGUI fpsText;

        private FpsModel _model;
        private bool _isInitialized;

        public void SetModel(FpsModel model)
        {
            _model = model;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            fpsText.SetText("-- FPS");
            _model.OnFramesPerSecondChanged += UpdateFpsText;
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            _model.OnFramesPerSecondChanged -= UpdateFpsText;
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void UpdateFpsText(int framesPerSecond)
        {
            fpsText.SetText("{0} FPS", framesPerSecond);
        }
    }
}
