using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>Battle seconds elapsed while the battle runs; scaled by game speed, frozen while paused or loading.</summary>
    public sealed class BattleClock : ITickable
    {
        private readonly SkirmishStateModel _state;

        /// <summary>Latest battle seconds, readable from log lines that have no clock injected.</summary>
        public static float CurrentElapsed { get; private set; }

        public float Elapsed { get; private set; }
        public event System.Action Restored;

        public BattleClock(SkirmishStateModel state)
        {
            _state = state;
            CurrentElapsed = 0f;
        }

        public void Restore(float elapsed)
        {
            Elapsed = elapsed;
            CurrentElapsed = elapsed;
            Restored?.Invoke();
        }

        public void Tick()
        {
            if (_state.State != BattleState.Running) return;
            Elapsed += Time.deltaTime;
            CurrentElapsed = Elapsed;
        }
    }
}
