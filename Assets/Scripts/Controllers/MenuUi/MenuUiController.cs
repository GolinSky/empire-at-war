using EmpireAtWar.Commands;
using EmpireAtWar.Models.MenuUi;
using EmpireAtWar.Services.Popup;
using LightWeightFramework.Controller;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;

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

        public bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand
        {
            command = default;
            return false;
        }
    }
}