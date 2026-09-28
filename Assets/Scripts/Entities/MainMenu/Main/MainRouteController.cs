using System;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MainMenu.Main
{
    public class MainRouteController : IMainRouteNavigation, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly ISkirmishRoute _skirmishRoute;
        private readonly ISettingsRoute _settingsRoute;
        private readonly MainMenuModel _model;

        private IMainMenuUi _ui;

        public MainRouteController(
            IUiService uiService,
            ISkirmishRoute skirmishRoute,
            ISettingsRoute settingsRoute,
            MainMenuModel model)
        {
            _uiService = uiService;
            _skirmishRoute = skirmishRoute;
            _settingsRoute = settingsRoute;
            _model = model;
        }

        public void Open()
        {
            BaseUi ui = _uiService.CreateUi(UiType.MainMenu);
            _ui = ui as IMainMenuUi
                ?? throw new InvalidOperationException("The main menu prefab does not implement IMainMenuUi.");

            _ui.SetModel(_model);
            _ui.SetNavigation(this);
            _ui.Initialize();
        }

        public void LateDispose()
        {
            _ui?.Dispose();
        }

        public void OpenSkirmish()
        {
            _skirmishRoute.Open();
        }

        public void OpenSettings()
        {
            _settingsRoute.Open();
        }

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
