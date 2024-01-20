using EmpireAtWar.Commands;
using EmpireAtWar.Models.MenuUi;
using EmpireAtWar.Services.SceneService;
using LightWeightFramework.Controller;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar
{
    public class MenuUiController : Controller<MenuUiModel>, IMenuUiCommand
    {
        private readonly ISceneService sceneService;

        public MenuUiController(MenuUiModel model, ISceneService sceneService) : base(model)
        {
            this.sceneService = sceneService;
        }

        public void StartDemo()
        {
            sceneService.LoadScene(SceneType.Skirmish);
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