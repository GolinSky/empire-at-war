using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.CoreGameUi;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SurvivalHudUiController : UiController, IInitializable, ILateDisposable, ISurvivalHudPresenter
    {
        private readonly SurvivalModel _model;
        private readonly SkirmishStateModel _state;
        private SurvivalHudUi _ui;
        private readonly CoreGameUiController _core;
        public SurvivalHudUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            SurvivalModel model, SkirmishStateModel state, CoreGameUiController core) : base(uiService, cancelRouter)
        { _model = model; _state = state; _core = core; }
        public void Initialize()
        {
            _core.SetMode("SURVIVAL", false);
            _ui = CreateUi<SurvivalHudUi>();
            _ui.SetModel(_model);
            _ui.AssignPresenter(this);
            _ui.Initialize();
            _ui.Show();
        }
        public void StartWaveNow()
        {
            if (_state.State == BattleState.Running) _model.StartWaveNow();
        }
        public void LateDispose() { if (_ui != null) _ui.Dispose(); }
    }
}
