using System;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.MainMenu.Main
{
    public class MainRouteController : UiController, IMainRouteNavigation, ILateDisposable
    {
        private readonly ISkirmishRoute _skirmishRoute;
        private readonly ISettingsRoute _settingsRoute;
        private IMainMenuUi _ui;

        private readonly MainMenuModel _model;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;

        public MainRouteController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            ISkirmishRoute skirmishRoute,
            ISettingsRoute settingsRoute,
            ITooltipService tooltips, MainMenuModel model) : base(uiService, cancelRouter)
        {
            _skirmishRoute = skirmishRoute;
            _settingsRoute = settingsRoute;
            _model = model;
            _tooltips = new TooltipRequests(tooltips);
        }

        public void LateDispose()
        {
            if (_ui != null) _tooltipHover.Dispose();
            _ui?.Dispose();
        }

        public void Open()
        {
            BaseUi ui = UiService.CreateUi(UiType.MainMenu);
            _ui = ui as IMainMenuUi
                ?? throw new InvalidOperationException("The main menu prefab does not implement IMainMenuUi.");

            _ui.SetModel(_model);
            _ui.SetNavigation(this);
            _ui.Initialize();
            _tooltipHover = new TooltipHoverSubscription(((ITooltipHoverView)_ui).TooltipHover,
                HandleTooltipHover, _tooltips);
        }

        public void OpenSkirmish()
        {
            _tooltips.HideAll();
            _skirmishRoute.Open();
        }

        public void OpenSettings()
        {
            _tooltips.HideAll();
            _settingsRoute.Open();
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _ui != null, () => new TooltipContent(title: (string)key,
                description: (string)key switch
                {
                    "Skirmish" => "Set up a battle with factions, teams and AI opponents.",
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
