using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Entities.MainMenu.SinglePlayer;
using EmpireAtWar.Entities.MainMenu.Tutorial;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Play
{
    /// <summary>Play: Single Player, Multiplayer, AI vs AI or Tutorial.</summary>
    public sealed class PlayRouteUiController : UiController, IPlayRoute, IPlayPresenter, ILateDisposable
    {
        private readonly ISinglePlayerRoute _singlePlayerRoute;
        private readonly IOnlineRoute _multiplayerRoute;
        private readonly ISimulationRoute _simulationRoute;
        private readonly ITutorialRoute _tutorialRoute;
        private PlayUi _ui;

        public PlayRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            ISinglePlayerRoute singlePlayerRoute, IOnlineRoute multiplayerRoute, ISimulationRoute simulationRoute,
            ITutorialRoute tutorialRoute)
            : base(uiService, cancelRouter)
        {
            _singlePlayerRoute = singlePlayerRoute;
            _multiplayerRoute = multiplayerRoute;
            _simulationRoute = simulationRoute;
            _tutorialRoute = tutorialRoute;
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<PlayUi>();
                _ui.AssignPresenter(this);
                _ui.Initialize();
            }
            _ui.Show();
            Focus();
        }

        public void Close()
        {
            _ui.Hide();
            Unfocus();
        }

        public void OpenSinglePlayer() => _singlePlayerRoute.Open();
        public void OpenMultiplayer() => _multiplayerRoute.Open();
        public void OpenSimulation() => _simulationRoute.Open();
        public void OpenTutorial() => _tutorialRoute.Open();

        protected override bool HandleCancel()
        {
            Close();
            return true;
        }

        public void LateDispose()
        {
            if (_ui != null) _ui.Dispose();
            _ui = null;
            Unfocus();
        }
    }
}
