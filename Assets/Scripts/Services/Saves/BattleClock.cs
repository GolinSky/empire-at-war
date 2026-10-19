using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>Battle seconds elapsed while the battle runs; scaled by game speed, frozen while paused or loading.</summary>
    public sealed class BattleClock : ITickable
    {
        private readonly SkirmishStateModel _state;

        public float Elapsed { get; private set; }

        public BattleClock(SkirmishStateModel state)
        {
            _state = state;
        }

        public void Restore(float elapsed) => Elapsed = elapsed;

        public void Tick()
        {
            if (_state.State == BattleState.Running) Elapsed += Time.deltaTime;
        }
    }
}
