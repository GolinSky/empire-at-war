using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    public sealed class SquadronLaunchSlots : ISquadronLaunchSlots
    {
        // At most ten reserve launches per second across the whole battle.
        private const float LAUNCH_INTERVAL = 0.1f;

        private float _nextLaunchTime = float.NegativeInfinity;

        public bool IsAvailable => Time.time >= _nextLaunchTime;

        public void Take() => _nextLaunchTime = Time.time + LAUNCH_INTERVAL;
    }
}
