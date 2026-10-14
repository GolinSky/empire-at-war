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
        public void SetModel(SurvivalModel model) => _model = model;
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
            waveText.text = $"WAVE {_model.WaveIndex}/5";
            statusText.text = _model.Phase == SurvivalPhase.Preparation
                ? $"Prepare defenses · {Mathf.CeilToInt(_model.PreparationTimeLeft)}s" : _model.Phase.ToString();
            startWaveButton.gameObject.SetActive(_model.Phase == SurvivalPhase.Preparation);
        }
        public void Dispose()
        {
            if (!_initialized) return;
            _model.Changed -= Render;
            startWaveButton.onClick.RemoveListener(_presenter.StartWaveNow);
            _initialized = false;
        }
        private void OnDestroy() => Dispose();
    }
}
