using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;
using System;
using EmpireAtWar.Components.Ui.Tooltip;

namespace EmpireAtWar.Entities.MainMenu.Main
{
    public class MainMenuUi : BaseUi, IMainMenuUi, ITooltipHoverView
    {
        private IMainMenuModel _model;
        private IMainRouteNavigation _navigation;

        [SerializeField] private Button startDemoButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitApplicationButton;
        [SerializeField] private TooltipHoverView tooltipHover;

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            if (_navigation == null)
            {
                throw new InvalidOperationException("MainMenuUi dependencies must be set before initialization.");
            }

            startDemoButton.onClick.AddListener(_navigation.OpenSkirmish);
            optionsButton.onClick.AddListener(_navigation.OpenSettings);
            quitApplicationButton.onClick.AddListener(_navigation.ExitApplication);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            startDemoButton.onClick.RemoveListener(_navigation.OpenSkirmish);
            optionsButton.onClick.RemoveListener(_navigation.OpenSettings);
            quitApplicationButton.onClick.RemoveListener(_navigation.ExitApplication);
            _isInitialized = false;
        }

        public void SetModel(IMainMenuModel model)
        {
            _model = model;
        }

        public void SetNavigation(IMainRouteNavigation navigation)
        {
            _navigation = navigation;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
