using System;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Battle;
using UnityEngine;

namespace EmpireAtWar.Entities.MainMenu.Simulation
{
    public sealed class SimulationBattleLauncher : ISimulationLauncher
    {
        private readonly SimulationModel _model;
        private readonly SimulationDirector _director;
        private readonly SkirmishOrchestrator _flow;
        private readonly SkirmishStateModel _state;
        private bool _resume;
        public SimulationBattleLauncher(SimulationModel model, SimulationDirector director,
            SkirmishOrchestrator flow, SkirmishStateModel state)
        { _model = model; _director = director; _flow = flow; _state = state; }
        public SimulationSetup Current => _model.Setup;
        public void Open()
        {
            _resume = _state.State == BattleState.Running;
            if (_resume) _flow.TogglePause();
        }
        public void Cancel() { if (_resume) _flow.TogglePause(); _resume = false; }
        public void Launch(SimulationSetup setup) { _resume = false; _ = LaunchAsync(setup); }
        private async Awaitable LaunchAsync(SimulationSetup setup)
        {
            try { await _director.RestartAsync(setup); }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception); throw; }
        }
    }
}
