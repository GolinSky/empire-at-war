using Zenject;

namespace EmpireAtWar.Entities.MenuUi
{
    public class MainMenuOrchestrator : IInitializable
    {
        private readonly MenuUiController _menuUiController;

        public MainMenuOrchestrator(MenuUiController menuUiController)
        {
            _menuUiController = menuUiController;
        }

        public void Initialize()
        {
            _menuUiController.SpawnMenuUi();
        }
    }
}
