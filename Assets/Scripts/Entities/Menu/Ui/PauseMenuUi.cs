using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Menu
{
    public class PauseMenuUi : BaseUi, IPauseMenuUiView, ITooltipHoverView
    {
        private IPauseMenuRouteNavigation _navigation;

        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private TooltipHoverView tooltipHover;

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_navigation == null)
            {
                throw new System.InvalidOperationException(
                    "PauseMenuUi navigation must be set before initialization.");
            }

            resumeButton.onClick.AddListener(_navigation.Close);
            optionsButton.onClick.AddListener(_navigation.OpenSettings);
            exitButton.onClick.AddListener(_navigation.ExitSkirmish);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            resumeButton.onClick.RemoveListener(_navigation.Close);
            optionsButton.onClick.RemoveListener(_navigation.OpenSettings);
            exitButton.onClick.RemoveListener(_navigation.ExitSkirmish);
            _isInitialized = false;
        }

        public void SetNavigation(IPauseMenuRouteNavigation navigation)
        {
            _navigation = navigation;
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
