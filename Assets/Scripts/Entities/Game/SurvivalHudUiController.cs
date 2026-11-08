using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.CoreGameUi;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SurvivalHudUiController : UiController, IInitializable, ILateDisposable, ISurvivalHudPresenter
    {
        private readonly SurvivalModel _model;
        private readonly SkirmishStateModel _state;
        private SurvivalHudUi _ui;
        private readonly CoreGameUiController _core;
        private readonly IMatchSession _session;
        public SurvivalHudUiController(IUiService uiService, IUiCancelRouter cancelRouter,
            SurvivalModel model, SkirmishStateModel state, CoreGameUiController core, IMatchSession session)
            : base(uiService, cancelRouter)
        { _model = model; _state = state; _core = core; _session = session; }
        public void Initialize()
        {
            _core.SetMode("SURVIVAL", false);
            _ui = CreateUi<SurvivalHudUi>();
            _ui.SetModel(_model);
            _ui.AssignPresenter(this);
            // Waves run on the host; an online client follows the host's countdown.
            _ui.SetStartWaveAvailable(_session.IsAuthority);
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
