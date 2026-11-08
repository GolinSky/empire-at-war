using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>Periodic and post-wave saves. Online battles never autosave (saving online is not supported yet).</summary>
    public sealed class BattleAutosave : ITickable
    {
        private readonly BattleClock _clock;
        private readonly BattleSaveActions _actions;
        private readonly AutosaveSchedule _schedule = new AutosaveSchedule();
        private readonly IMatchSession _session;

        public BattleAutosave(BattleClock clock, BattleSaveActions actions, IMatchSession session)
        {
            _session = session;
            _clock = clock;
            _actions = actions;
        }

        public void RequestWaveSave() => _schedule.RequestWaveSave();

        public void Tick()
        {
            if (_session.IsOnline) return;
            if (_schedule.TakeDueSave(_clock.Elapsed, _actions.CanSave))
                _actions.Save(BattleSaveSlot.Autosave);
        }
    }
}
