using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Hangar
{
    /// <summary>
    /// Finite squadron reserve with a cap on simultaneously deployed squadrons per bay.
    /// Lost squadrons are replaced from the reserve after the launch interval; an empty reserve launches nothing.
    /// </summary>
    public sealed class HangarModel : PureModel
    {
        private readonly IHangarData _data;
        private readonly int[] _reserve;
        private readonly int[] _active;
        private float _cooldown;

        public bool IsOperational { get; private set; } = true;
        public int BayCount => _reserve.Length;

        public HangarModel(IHangarData data)
        {
            _data = data;
            _reserve = new int[data.HangarBays.Count];
            _active = new int[data.HangarBays.Count];
            for (int i = 0; i < _reserve.Length; i++)
            {
                _reserve[i] = data.HangarBays[i].Reserve;
            }

            _cooldown = data.HangarInitialDelay;
        }

        public int GetReserve(int bay) => _reserve[bay];
        public int GetActive(int bay) => _active[bay];

        public bool TryLaunch(float deltaTime, out int bay)
        {
            bay = -1;
            if (!IsOperational)
            {
                return false;
            }

            _cooldown -= deltaTime;
            if (_cooldown > 0f)
            {
                return false;
            }

            for (int i = 0; i < _reserve.Length; i++)
            {
                if (_reserve[i] <= 0 || _active[i] >= _data.HangarBays[i].MaxActive)
                {
                    continue;
                }

                _reserve[i]--;
                _active[i]++;
                _cooldown = _data.HangarLaunchInterval;
                bay = i;
                return true;
            }

            return false;
        }

        public void SquadronLost(int bay)
        {
            _active[bay] = Math.Max(0, _active[bay] - 1);
            _cooldown = Math.Max(_cooldown, _data.HangarLaunchInterval);
        }

        public void Shutdown() => IsOperational = false;
    }
}
