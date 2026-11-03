using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;
using System;
using EmpireAtWar.Components.Ui.Tooltip;
using TMPro;

namespace EmpireAtWar.Entities.MainMenu.Main
{
    public class MainMenuUi : BaseUi
    {
        private IMainMenuPresenter _presenter;

        [SerializeField] private Button startDemoButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitApplicationButton;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private TMP_Text versionText;

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void AssignPresenter(IMainMenuPresenter presenter)
        {
            if (_presenter != null)
            {
                throw new InvalidOperationException("MainMenuUi presenter is already assigned.");
            }
            _presenter = presenter;
        }

        public void SetVersion(string text)
        {
            versionText.text = text;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            startDemoButton.onClick.AddListener(_presenter.OpenPlay);
            optionsButton.onClick.AddListener(_presenter.OpenSettings);
            quitApplicationButton.onClick.AddListener(_presenter.ExitApplication);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            startDemoButton.onClick.RemoveListener(_presenter.OpenPlay);
            optionsButton.onClick.RemoveListener(_presenter.OpenSettings);
            quitApplicationButton.onClick.RemoveListener(_presenter.ExitApplication);
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
