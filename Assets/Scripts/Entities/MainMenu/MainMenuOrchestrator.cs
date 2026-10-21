using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.MainMenu.Play;
using EmpireAtWar.Entities.Saves;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.MainMenu
{
    public class MainMenuOrchestrator : IInitializable
    {
        private readonly MainMenuUiController _mainRoute;
        private readonly IPlayRoute _playRoute;
        private readonly ILoadGameRoute _loadGameRoute;
        private readonly GameData _game;

        public MainMenuOrchestrator(MainMenuUiController mainRoute, IPlayRoute playRoute = null,
            ILoadGameRoute loadGameRoute = null, GameData game = null)
        {
            _mainRoute = mainRoute;
            _playRoute = playRoute;
            _loadGameRoute = loadGameRoute;
            _game = game;
        }

        public void Initialize()
        {
            _mainRoute.Open();
            if (_game != null && !string.IsNullOrEmpty(_game.PendingLoadError))
            {
                _playRoute.Open();
                _loadGameRoute.Open();
            }
        }
    }
}
