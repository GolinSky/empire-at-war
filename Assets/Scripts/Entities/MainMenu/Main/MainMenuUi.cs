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
        [Header("Usage Data Consent")]
        [SerializeField] private GameObject usageConsentPrompt;
        [SerializeField] private Button allowUsageDataButton;
        [SerializeField] private Button declineUsageDataButton;

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

        public void SetUsageConsentPromptVisible(bool isVisible)
        {
            usageConsentPrompt.SetActive(isVisible);
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
            allowUsageDataButton.onClick.AddListener(AllowUsageData);
            declineUsageDataButton.onClick.AddListener(DeclineUsageData);
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
            allowUsageDataButton.onClick.RemoveListener(AllowUsageData);
            declineUsageDataButton.onClick.RemoveListener(DeclineUsageData);
            _isInitialized = false;
        }

        private void AllowUsageData() => _presenter.AnswerUsageDataConsent(true);

        private void DeclineUsageData() => _presenter.AnswerUsageDataConsent(false);

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
