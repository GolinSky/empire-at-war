using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Play
{
    public sealed class PlayUi : BaseUi
    {
        [SerializeField] private Button singlePlayerButton;
        [SerializeField] private Button multiplayerButton;
        [SerializeField] private Button simulationButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button backButton;
        private IPlayPresenter _presenter;
        private bool _isInitialized;

        public void AssignPresenter(IPlayPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_isInitialized) return;
            singlePlayerButton.onClick.AddListener(_presenter.OpenSinglePlayer);
            multiplayerButton.onClick.AddListener(_presenter.OpenMultiplayer);
            simulationButton.onClick.AddListener(_presenter.OpenSimulation);
            tutorialButton.onClick.AddListener(_presenter.OpenTutorial);
            backButton.onClick.AddListener(_presenter.Close);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            singlePlayerButton.onClick.RemoveListener(_presenter.OpenSinglePlayer);
            multiplayerButton.onClick.RemoveListener(_presenter.OpenMultiplayer);
            simulationButton.onClick.RemoveListener(_presenter.OpenSimulation);
            tutorialButton.onClick.RemoveListener(_presenter.OpenTutorial);
            backButton.onClick.RemoveListener(_presenter.Close);
            _isInitialized = false;
        }

        private void OnDestroy() => Dispose();
    }
}
