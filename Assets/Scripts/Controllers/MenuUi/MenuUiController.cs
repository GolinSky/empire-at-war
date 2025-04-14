using EmpireAtWar.Commands;
using EmpireAtWar.Models.MenuUi;
using EmpireAtWar.Services.Popup;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar
{
    public class MenuUiController : Controller<MenuUiModel>, IMenuUiCommand
    {
        private readonly IPopupService _popupService;

        public MenuUiController(MenuUiModel model, IPopupService popupService) : base(model)
        {
            _popupService = popupService;
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
            Application.Quit();
        }
    }
}