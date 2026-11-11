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
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Versioning;

namespace EmpireAtWar.Entities.MainMenu.Main
{
    public class MainMenuUiController : UiController, IMainMenuPresenter, ILateDisposable
    {
        private readonly IPlayRoute _playRoute;
        private readonly ISettingsRoute _settingsRoute;
        private readonly ISettingsService _settingsService;
        private readonly IConfirmPopupService _confirmPopups;
        private MainMenuUi _ui;

        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;

        public MainMenuUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IPlayRoute playRoute,
            ISettingsRoute settingsRoute,
            ISettingsService settingsService,
            IConfirmPopupService confirmPopups,
            ITooltipService tooltipService) : base(uiService, cancelRouter)
        {
            _playRoute = playRoute;
            _settingsRoute = settingsRoute;
            _settingsService = settingsService;
            _confirmPopups = confirmPopups;
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
                if (!_settingsService.Saved.Privacy.HasAnsweredConsent) RequestUsageDataConsent();
            }
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

        private void RequestUsageDataConsent()
        {
            // A failed save is logged by the settings service; the popup then returns on the next launch.
            _confirmPopups.ShowAccept(new AcceptPopupContent(
                    title: "Help improve Empire at War?",
                    message: "Share gameplay statistics? Change this in Settings > Gameplay.\n\n" +
                        "Crash diagnostics are collected separately and are not controlled by this choice.",
                    acceptLabel: "Allow",
                    declineLabel: "Decline"),
                result =>
                {
                    if (result != ConfirmPopupResult.Dismissed)
                        _settingsService.AnswerUsageDataConsent(result == ConfirmPopupResult.Accepted);
                });
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
