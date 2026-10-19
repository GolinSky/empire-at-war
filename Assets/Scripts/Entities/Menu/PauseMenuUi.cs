using TMPro;
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
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private GameObject savePanel;
        [SerializeField] private TMP_InputField saveName;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button quickSaveButton;
        [SerializeField] private Button quickLoadButton;
        [SerializeField] private TMP_Text saveStatus;

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
            optionsButton.onClick.AddListener(_presenter.OpenSettings);
            exitButton.onClick.AddListener(_presenter.ExitSkirmish);
            saveButton.onClick.AddListener(SaveNamed);
            quickSaveButton.onClick.AddListener(_presenter.QuickSave);
            quickLoadButton.onClick.AddListener(_presenter.QuickLoad);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            resumeButton.onClick.RemoveListener(_presenter.Close);
            optionsButton.onClick.RemoveListener(_presenter.OpenSettings);
            exitButton.onClick.RemoveListener(_presenter.ExitSkirmish);
            saveButton.onClick.RemoveListener(SaveNamed);
            quickSaveButton.onClick.RemoveListener(_presenter.QuickSave);
            quickLoadButton.onClick.RemoveListener(_presenter.QuickLoad);
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

        private void SaveNamed() => _presenter.SaveNamed(saveName.text);
        public void SetSavingAvailable(bool available) => savePanel.SetActive(available);
        public void SetSaveStatus(string status) => saveStatus.text = status;

        public void SetMenuVisible(bool isVisible)
        {
            menuPanel.SetActive(isVisible);
            if (isVisible) Show();
            else Hide();
        }
    }
}
