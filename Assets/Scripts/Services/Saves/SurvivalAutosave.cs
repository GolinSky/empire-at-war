using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Saves
{
    public sealed class SurvivalAutosave : IInitializable, ILateDisposable
    {
        private readonly SurvivalModel _model;
        private readonly BattleAutosave _autosave;
        private SurvivalPhase _phase;

        public SurvivalAutosave(SurvivalModel model, BattleAutosave autosave)
        {
            _model = model;
            _autosave = autosave;
        }

        public void Initialize()
        {
            _phase = _model.Phase;
            _model.Changed += OnChanged;
        }

        public void LateDispose() => _model.Changed -= OnChanged;

        private void OnChanged()
        {
            if (_phase == SurvivalPhase.WaveActive && _model.Phase == SurvivalPhase.Preparation)
                _autosave.RequestWaveSave();
            _phase = _model.Phase;
        }
    }
}
