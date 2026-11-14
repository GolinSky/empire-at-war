using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SurvivalHudUi : BaseUi
    {
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button startWaveButton;
        private SurvivalModel _model;
        private ISurvivalHudPresenter _presenter;
        private bool _initialized;
        private bool _canStartWave = true;
        private int? _displayedWave;
        private (SurvivalPhase Phase, int Second)? _displayedStatus;
        public void SetModel(SurvivalModel model) => _model = model;
        public void SetStartWaveAvailable(bool available) => _canStartWave = available;
        public void AssignPresenter(ISurvivalHudPresenter presenter) => _presenter = presenter;
        public void Initialize()
        {
            if (_initialized) return;
            _model.Changed += Render;
            startWaveButton.onClick.AddListener(_presenter.StartWaveNow);
            _initialized = true;
            Render();
        }
        private void Render()
        {
            int wave = _model.WaveIndex;
            if (_displayedWave != wave)
            {
                _displayedWave = wave;
                waveText.text = $"WAVE {wave}/5";
            }
            SurvivalPhase phase = _model.Phase;
            bool preparing = phase == SurvivalPhase.Preparation;
            int second = preparing ? Mathf.CeilToInt(_model.PreparationTimeLeft) : 0;
            if (_displayedStatus != (phase, second))
            {
                _displayedStatus = (phase, second);
                statusText.text = preparing ? $"Prepare defenses · {second}s" : phase.ToString();
            }
            startWaveButton.gameObject.SetActive(_canStartWave && preparing);
        }
        public void Dispose()
        {
            if (!_initialized) return;
            _model.Changed -= Render;
            startWaveButton.onClick.RemoveListener(_presenter.StartWaveNow);
            _initialized = false;
            _displayedWave = null;
            _displayedStatus = null;
        }
        private void OnDestroy() => Dispose();
    }
}
