using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Menu
{
    public class PauseMenuUi : BaseUi, IPauseMenuUiView
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject menuPanel;

        private IPauseMenuPresenter _presenter;
        private bool _isInitialized;

        public void SetPresenter(IPauseMenuPresenter presenter)
        {
            _presenter = presenter;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_presenter == null)
            {
                throw new System.InvalidOperationException(
                    "PauseMenuUi presenter must be set before initialization.");
            }

            exitButton.onClick.AddListener(_presenter.ExitSkirmish);
            resumeButton.onClick.AddListener(_presenter.ResumeGame);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            exitButton.onClick.RemoveListener(_presenter.ExitSkirmish);
            resumeButton.onClick.RemoveListener(_presenter.ResumeGame);
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void SetMenuVisible(bool isVisible)
        {
            menuPanel.SetActive(isVisible);
        }
    }
}
