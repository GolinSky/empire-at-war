using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.Saves;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.MainMenu.SinglePlayer
{
    public sealed class SinglePlayerRouteUiController : UiController, ISinglePlayerRoute, ISinglePlayerPresenter,
        ILateDisposable
    {
        private readonly ISkirmishRoute _skirmishRoute;
        private readonly ISurvivalRoute _survivalRoute;
        private readonly ILoadGameRoute _loadGameRoute;
        private SinglePlayerUi _ui;

        public SinglePlayerRouteUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            ISkirmishRoute skirmishRoute, ISurvivalRoute survivalRoute, ILoadGameRoute loadGameRoute)
            : base(uiService, cancelRouter)
        {
            _skirmishRoute = skirmishRoute;
            _survivalRoute = survivalRoute;
            _loadGameRoute = loadGameRoute;
        }

        public void Open()
        {
            if (_ui == null)
            {
                _ui = CreateUi<SinglePlayerUi>();
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
