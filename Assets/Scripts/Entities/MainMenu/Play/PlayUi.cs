using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Play
{
    public sealed class PlayUi : BaseUi
    {
        [SerializeField] private Button skirmishButton;
        [SerializeField] private Button survivalButton;
        [SerializeField] private Button simulationButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button loadButton;
        private IPlayPresenter _presenter;
        private bool _isInitialized;

        public void AssignPresenter(IPlayPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_isInitialized) return;
            skirmishButton.onClick.AddListener(_presenter.OpenSkirmish);
            survivalButton.onClick.AddListener(_presenter.OpenSurvival);
            backButton.onClick.AddListener(_presenter.Close);
            simulationButton.interactable = true;
            simulationButton.onClick.AddListener(_presenter.OpenSimulation);
            loadButton.onClick.AddListener(_presenter.OpenLoadGame);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            simulationButton.onClick.RemoveListener(_presenter.OpenSimulation);
            loadButton.onClick.RemoveListener(_presenter.OpenLoadGame);
            skirmishButton.onClick.RemoveListener(_presenter.OpenSkirmish);
            survivalButton.onClick.RemoveListener(_presenter.OpenSurvival);
            backButton.onClick.RemoveListener(_presenter.Close);
            _isInitialized = false;
        }

        private void OnDestroy() => Dispose();
    }
}
