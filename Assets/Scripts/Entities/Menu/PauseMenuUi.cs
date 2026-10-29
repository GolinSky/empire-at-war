using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Menu
{
    public class PauseMenuUi : BaseUi
    {
        private IPauseMenuPresenter _presenter;

        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private Button saveButton;

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_presenter == null)
            {
                throw new System.InvalidOperationException(
                    "PauseMenuUi navigation must be set before initialization.");
            }

            resumeButton.onClick.AddListener(_presenter.Close);
            restartButton.onClick.AddListener(_presenter.RestartBattle);
            optionsButton.onClick.AddListener(_presenter.OpenSettings);
            exitButton.onClick.AddListener(_presenter.ExitSkirmish);
            saveButton.onClick.AddListener(_presenter.OpenSaveGame);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            resumeButton.onClick.RemoveListener(_presenter.Close);
            restartButton.onClick.RemoveListener(_presenter.RestartBattle);
            optionsButton.onClick.RemoveListener(_presenter.OpenSettings);
            exitButton.onClick.RemoveListener(_presenter.ExitSkirmish);
            saveButton.onClick.RemoveListener(_presenter.OpenSaveGame);
            _isInitialized = false;
        }

        public void AssignPresenter(IPauseMenuPresenter presenter)
        {
            if (_presenter != null)
            {
                throw new System.InvalidOperationException("PauseMenuUi presenter is already assigned.");
            }
            _presenter = presenter;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void SetSavingAvailable(bool available) => saveButton.gameObject.SetActive(available);

        public void SetMenuVisible(bool isVisible)
        {
            menuPanel.SetActive(isVisible);
            if (isVisible) Show();
            else Hide();
        }
    }
}
