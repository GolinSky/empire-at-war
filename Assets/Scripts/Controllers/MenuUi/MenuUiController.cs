using EmpireAtWar.Commands;
using EmpireAtWar.Models.MenuUi;
using EmpireAtWar.Services.Popup;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar
{
    public class MenuUiController : Controller<MenuUiModel>, IMenuUiCommand
    {
        private readonly IPopupService popupService;

        public MenuUiController(MenuUiModel model, IPopupService popupService) : base(model)
        {
            this.popupService = popupService;
        }

        public void StartDemo()
        {
            popupService.OpenPopup(PopupType.SkirmishGameSetUp);
        }

        public void OpenOptions()
        {
        }

        public void ExitApplication()
        {
            Application.Quit();
        }
    }
}