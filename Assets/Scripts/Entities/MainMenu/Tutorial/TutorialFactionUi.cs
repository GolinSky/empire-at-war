using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Tutorial
{
    /// <summary>Faction pick for the guided tutorial battle; one button per faction.</summary>
    public sealed class TutorialFactionUi : BaseUi
    {
        [SerializeField] private Button republicButton;
        [SerializeField] private Button separatistButton;
        [SerializeField] private Button rebellionButton;
        [SerializeField] private Button empireButton;
        [SerializeField] private Button backButton;
        private ITutorialPresenter _presenter;
        private UnityAction _republic;
        private UnityAction _separatist;
        private UnityAction _rebellion;
        private UnityAction _empire;
        private bool _isInitialized;

        public void AssignPresenter(ITutorialPresenter presenter) => _presenter = presenter;

        public void Initialize()
        {
            if (_isInitialized) return;
            _republic = () => _presenter.StartTutorial(FactionType.Republic);
            _separatist = () => _presenter.StartTutorial(FactionType.Separatist);
            _rebellion = () => _presenter.StartTutorial(FactionType.Rebellion);
            _empire = () => _presenter.StartTutorial(FactionType.Empire);
            republicButton.onClick.AddListener(_republic);
            separatistButton.onClick.AddListener(_separatist);
            rebellionButton.onClick.AddListener(_rebellion);
            empireButton.onClick.AddListener(_empire);
            backButton.onClick.AddListener(_presenter.Close);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            republicButton.onClick.RemoveListener(_republic);
            separatistButton.onClick.RemoveListener(_separatist);
            rebellionButton.onClick.RemoveListener(_rebellion);
            empireButton.onClick.RemoveListener(_empire);
            backButton.onClick.RemoveListener(_presenter.Close);
            _isInitialized = false;
        }

        private void OnDestroy() => Dispose();
    }
}
