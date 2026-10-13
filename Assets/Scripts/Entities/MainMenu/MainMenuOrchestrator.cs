using EmpireAtWar.Entities.MainMenu.Main;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.MainMenu
{
    public class MainMenuOrchestrator : IInitializable
    {
        private readonly MainMenuUiController _mainRoute;

        public MainMenuOrchestrator(MainMenuUiController mainRoute)
        {
            _mainRoute = mainRoute;
        }

        public void Initialize()
        {
            _mainRoute.Open();
        }
    }
}
