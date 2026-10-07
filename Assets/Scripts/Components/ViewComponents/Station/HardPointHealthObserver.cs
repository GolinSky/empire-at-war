using System;

namespace EmpireAtWar.ViewComponents.Station
{
    /// <summary>Forwards one hardpoint's health percentage together with its id.</summary>
    public sealed class HardPointHealthObserver : IObserver<float>
    {
        private readonly int _hardPointId;
        private readonly Action<int, float> _changed;

        public HardPointHealthObserver(int hardPointId, Action<int, float> changed)
        {
            _hardPointId = hardPointId;
            _changed = changed;
        }

        public void UpdateState(float value) => _changed(_hardPointId, value);
    }
}
