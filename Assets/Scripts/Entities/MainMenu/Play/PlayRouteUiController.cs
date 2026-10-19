using EmpireAtWar.Entities.Saves;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.Play
{
    public sealed class PlayRouteUiController : UiController, IPlayRoute, IPlayPresenter, ILateDisposable
    {
        private readonly ISkirmishRoute _skirmishRoute;
        private readonly ISurvivalRoute _survivalRoute;
        private readonly ISimulationRoute _simulationRoute;
        private readonly ILoadGameRoute _loadGameRoute;
        private PlayUi _ui;

        public PlayRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            ISkirmishRoute skirmishRoute, ISurvivalRoute survivalRoute, ISimulationRoute simulationRoute,
            ILoadGameRoute loadGameRoute) : base(uiService, cancelRouter)
        {
            _skirmishRoute = skirmishRoute;
            _survivalRoute = survivalRoute;
            _simulationRoute = simulationRoute;
            _loadGameRoute = loadGameRoute;
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

        public void OpenSkirmish() => _skirmishRoute.Open();
        public void OpenSurvival() => _survivalRoute.Open();
        public void OpenSimulation() => _simulationRoute.Open();
        public void OpenLoadGame() => _loadGameRoute.Open();

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
