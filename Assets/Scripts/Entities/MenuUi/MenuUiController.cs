using System;
using EmpireAtWar.Entities.MenuUi.Popups;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MenuUi
{
    public class MenuUiController : IMenuUiPresenter, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly IMainMenuPopupPresenter _popupPresenter;
        private readonly MenuUiModel _model;
        
        private IMenuUiView _ui;

        public MenuUiController(
            IUiService uiService, 
            IMainMenuPopupPresenter popupPresenter,
            MenuUiModel model)
        {
            _uiService = uiService;
            _popupPresenter = popupPresenter;
            _model = model;
        }

        public void SpawnMenuUi()
        {
            BaseUi ui = _uiService.CreateUi(UiType.MainMenu);
            _ui = ui as IMenuUiView
                ?? throw new InvalidOperationException("The main menu prefab does not implement IMenuUiView.");

            _ui.SetModel(_model);
            _ui.SetPresenter(this);
            _ui.Initialize();
        }

        public void LateDispose()
        {
            _ui?.Dispose();
        }

        public void StartDemo()
        {
            _popupPresenter.OpenSkirmish();
        }

        public void OpenOptions()
        {
            _popupPresenter.OpenSettings();
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
