using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Saves
{
    public sealed class BattleAutosave : ITickable
    {
        private readonly BattleClock _clock;
        private readonly BattleSaveActions _actions;
        private readonly AutosaveSchedule _schedule = new AutosaveSchedule();

        public BattleAutosave(BattleClock clock, BattleSaveActions actions)
        {
            _clock = clock;
            _actions = actions;
        }

        public void RequestWaveSave() => _schedule.RequestWaveSave();

        public void Tick()
        {
            if (_schedule.TakeDueSave(_clock.Elapsed, _actions.CanSave))
                _actions.Save(BattleSaveSlot.Autosave);
        }
    }
}
