using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    public sealed class SquadronLaunchSlots : ISquadronLaunchSlots
    {
        private int _takenFrame = -1;

        public bool IsAvailable => _takenFrame != Time.frameCount;

        public void Take() => _takenFrame = Time.frameCount;
    }
}
