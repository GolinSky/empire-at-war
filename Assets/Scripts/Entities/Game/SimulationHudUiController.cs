using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.CoreGameUi;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SimulationHudUiController : UiController, IInitializable, ITickable, ILateDisposable, ISimulationHudPresenter
    {
        private readonly SimulationModel _model;
        private readonly SkirmishStateModel _state;
        private readonly SkirmishOrchestrator _flow;
        private readonly ISimulationRoute _setup;
        private readonly ISimulationLauncher _launcher;
        private SimulationHudUi _ui;
        private readonly CoreGameUiController _core;
        public SimulationHudUiController(IUiService ui, IUiCancelRouter cancel, SimulationModel model,
            SkirmishStateModel state, SkirmishOrchestrator flow, ISimulationRoute setup, ISimulationLauncher launcher,
            CoreGameUiController core) : base(ui, cancel)
        { _model = model; _state = state; _flow = flow; _setup = setup; _launcher = launcher; _core = core; }
        public void Initialize() { _core.SetMode("SIMULATION", true); _ui = CreateUi<SimulationHudUi>(); _ui.Initialize(this); _ui.Show(); }
        public void Tick() => _ui.Render(_model, _state.State, _state.Speed);
        public void TogglePause() => _flow.TogglePause();
        public void ToggleSpeed() => _flow.ToggleSpeedUp();
        public void Restart() => _launcher.Launch(_model.Setup);
        public void New() => _setup.Open();
        public void LateDispose() { if (_ui != null) _ui.Dispose(); }
    }
}
