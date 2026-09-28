using EmpireAtWar.Entities.MainMenu.Main;
using Zenject;

namespace EmpireAtWar.Entities.MainMenu
{
    public class MainMenuOrchestrator : IInitializable
    {
        private readonly MainRouteController _mainRoute;

        public MainMenuOrchestrator(MainRouteController mainRoute)
        {
            _mainRoute = mainRoute;
        }

        public void Initialize()
        {
            _mainRoute.Open();
        }
    }
}
