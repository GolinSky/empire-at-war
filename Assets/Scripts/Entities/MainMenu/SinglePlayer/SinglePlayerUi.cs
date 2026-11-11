using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.SinglePlayer
{
    /// <summary>Offline modes: Skirmish, Survival and saved battles.</summary>
    public sealed class SinglePlayerUi : BaseUi
    {
        [SerializeField] private Button skirmishButton;
        [SerializeField] private Button survivalButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button backButton;
        private ISinglePlayerPresenter _presenter;
        private bool _isInitialized;

        public void AssignPresenter(ISinglePlayerPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_isInitialized) return;
            skirmishButton.onClick.AddListener(_presenter.OpenSkirmish);
            survivalButton.onClick.AddListener(_presenter.OpenSurvival);
            loadButton.onClick.AddListener(_presenter.OpenLoadGame);
            backButton.onClick.AddListener(_presenter.Close);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            skirmishButton.onClick.RemoveListener(_presenter.OpenSkirmish);
            survivalButton.onClick.RemoveListener(_presenter.OpenSurvival);
            loadButton.onClick.RemoveListener(_presenter.OpenLoadGame);
            backButton.onClick.RemoveListener(_presenter.Close);
            _isInitialized = false;
        }

        private void OnDestroy() => Dispose();
    }
}
