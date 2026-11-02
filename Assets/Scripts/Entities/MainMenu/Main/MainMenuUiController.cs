using System;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Play;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Versioning;

namespace EmpireAtWar.Entities.MainMenu.Main
{
    public class MainMenuUiController : UiController, IMainMenuPresenter, ILateDisposable
    {
        private readonly IPlayRoute _playRoute;
        private readonly ISettingsRoute _settingsRoute;
        private readonly ISettingsService _settingsService;
        private MainMenuUi _ui;

        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;

        public MainMenuUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IPlayRoute playRoute,
            ISettingsRoute settingsRoute,
            ISettingsService settingsService,
            ITooltipService tooltipService) : base(uiService, cancelRouter)
        {
            _playRoute = playRoute;
            _settingsRoute = settingsRoute;
            _settingsService = settingsService;
            _tooltips = new TooltipRequests(tooltipService);
        }

        public void LateDispose()
        {
            if (_tooltipHover != null) _tooltipHover.Dispose();
            if (_ui != null) _ui.Dispose();
            _ui = null;
            _tooltipHover = null;
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<MainMenuUi>();
                _ui.AssignPresenter(this);
                _ui.SetVersion(BuildInfo.LoadCurrent().DisplayText);
                _ui.Initialize();
                _tooltipHover = new TooltipHoverSubscription(_ui.TooltipHover,
                    HandleTooltipHover, _tooltips);
            }
            _ui.SetUsageConsentPromptVisible(!_settingsService.Saved.Privacy.HasAnsweredConsent);
            _ui.Show();
        }

        public void OpenPlay()
        {
            _tooltips.HideAll();
            _playRoute.Open();
        }

        public void OpenSettings()
        {
            _tooltips.HideAll();
            _settingsRoute.Open();
        }

        public void AnswerUsageDataConsent(bool isShared)
        {
            // A failed save is logged by the settings service; the prompt then returns on the next launch.
            _settingsService.AnswerUsageDataConsent(isShared);
            _ui.SetUsageConsentPromptVisible(false);
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _ui != null, () => new TooltipContent(title: (string)key,
                description: (string)key switch
                {
                    "Play" => "Choose Skirmish, Survival or Battle Simulation.",
                    "Settings" => "Edit display, camera and key-binding settings.",
                    "Quit" => "Exit the application.",
                    _ => throw new ArgumentOutOfRangeException(nameof(key))
                }));

        public void ExitApplication()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
