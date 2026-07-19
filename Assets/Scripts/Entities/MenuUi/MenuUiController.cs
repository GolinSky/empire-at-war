using System;
using EmpireAtWar.Services.Popup;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MenuUi
{
    public class MenuUiController : IMenuUiPresenter, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly IPopupService _popupService;
        private readonly MenuUiModel _model;
        
        private IMenuUiView _ui;

        public MenuUiController(
            IUiService uiService, 
            IPopupService popupService, 
            MenuUiModel model)
        {
            _uiService = uiService;
            _popupService = popupService;
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
            _popupService.OpenPopup(PopupType.SkirmishGameSetUp);
        }

        public void OpenOptions()
        {
            _popupService.OpenPopup(PopupType.Settings);
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