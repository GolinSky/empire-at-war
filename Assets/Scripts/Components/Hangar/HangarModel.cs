using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Hangar
{
    /// <summary>
    /// Finite squadron reserve with a cap on simultaneously deployed squadrons per bay.
    /// Lost squadrons are replaced from the reserve after the launch interval, but a bay launches only while fewer than
    /// its MaxActive squadrons left it in the last <see cref="RELAUNCH_COOLDOWN"/> seconds; an empty reserve launches nothing.
    /// </summary>
    public sealed class HangarModel : Model
    {
        // Seconds a launch counts against its bay; abilities that launch at once ignore the limit.
        public const float RELAUNCH_COOLDOWN = 60f;

        private readonly IHangarData _data;

        private readonly int[] _reserve;
        private readonly int[] _active;
        private readonly bool[] _disabled;
        // Index = bay: seconds left on each of its recent launches.
        private readonly List<float>[] _recentLaunches;

        private float _cooldown;

        public bool IsOperational { get; private set; } = true;
        public int BayCount => _reserve.Length;

        public HangarModel(IHangarData data)
        {
            _data = data;
            _reserve = new int[data.HangarBays.Count];
            _active = new int[data.HangarBays.Count];
            _disabled = new bool[data.HangarBays.Count];
            _recentLaunches = new List<float>[data.HangarBays.Count];
            for (int i = 0; i < _reserve.Length; i++)
            {
                _reserve[i] = data.HangarBays[i].Reserve;
                _recentLaunches[i] = new List<float>(data.HangarBays[i].MaxActive);
            }

            _cooldown = data.HangarInitialDelay;
        }

        public int GetReserve(int bay) => _reserve[bay];

        public float Cooldown => _cooldown;

        /// <summary>Loads saved reserves and launch timer with no squadron out; <see cref="AdoptLaunched"/> counts each restored one.</summary>
        public void Restore(IReadOnlyList<int> reserve, float cooldown)
        {
            if (reserve.Count != _reserve.Length)
                throw new InvalidOperationException($"Saved {reserve.Count} hangar bays; the hangar has {_reserve.Length}.");
            for (int i = 0; i < _reserve.Length; i++)
            {
                _reserve[i] = reserve[i];
                _active[i] = 0;
                _recentLaunches[i].Clear();
            }

            _cooldown = cooldown;
        }

        // A restored squadron counts as just launched.
        public void AdoptLaunched(int bay)
        {
            _active[bay]++;
            _recentLaunches[bay].Add(RELAUNCH_COOLDOWN);
        }

        public int GetActive(int bay) => _active[bay];

        public bool CanLaunchReplacement => FindReplacementBay() >= 0;

        public bool TryLaunchReplacement(out int bay)
        {
            bay = FindReplacementBay();
            if (bay < 0) return false;
            Launch(bay);
            return true;
        }

        private int FindReplacementBay()
        {
            if (!IsOperational) return -1;
            for (int i = 0; i < _reserve.Length; i++)
                if (!_disabled[i] && _reserve[i] > 0 && _reserve[i] < _data.HangarBays[i].Reserve &&
                    _data.HangarBays[i].MaxActive > 0 && _active[i] == 0)
                    return i;
            return -1;
        }

        public bool TryLaunch(float deltaTime, out int bay)
        {
            bay = -1;
            if (!IsOperational)
            {
                return false;
            }

            AdvanceRelaunch(deltaTime);
            _cooldown -= deltaTime;
            if (_cooldown > 0f)
            {
                return false;
            }

            bay = FindLaunchBay(respectRelaunch: true);
            if (bay < 0) return false;
            Launch(bay);
            return true;
        }

        public bool CanLaunchImmediately => FindLaunchBay(respectRelaunch: false) >= 0;

        public bool TryLaunchImmediately(out int bay)
        {
            bay = FindLaunchBay(respectRelaunch: false);
            if (bay < 0) return false;
            Launch(bay);
            return true;
        }

        private void Launch(int bay)
        {
            _reserve[bay]--;
            _active[bay]++;
            _recentLaunches[bay].Add(RELAUNCH_COOLDOWN);
            _cooldown = _data.HangarLaunchInterval;
        }

        private int FindLaunchBay(bool respectRelaunch)
        {
            if (!IsOperational) return -1;
            for (int i = 0; i < _reserve.Length; i++)
                if (!_disabled[i] && _reserve[i] > 0 && _active[i] < _data.HangarBays[i].MaxActive &&
                    (!respectRelaunch || _recentLaunches[i].Count < _data.HangarBays[i].MaxActive))
                    return i;
            return -1;
        }

        /// <summary>Runs the launch timers without launching, for a frame whose launch slot is already taken.</summary>
        public void AdvanceCooldown(float deltaTime)
        {
            if (IsOperational)
            {
                AdvanceRelaunch(deltaTime);
                _cooldown -= deltaTime;
            }
        }

        private void AdvanceRelaunch(float deltaTime)
        {
            foreach (List<float> launches in _recentLaunches)
            {
                for (int i = launches.Count - 1; i >= 0; i--)
                {
                    launches[i] -= deltaTime;
                    if (launches[i] <= 0f) launches.RemoveAt(i);
                }
            }
        }

        public void SquadronLost(int bay)
        {
            _active[bay] = Math.Max(0, _active[bay] - 1);
            _cooldown = Math.Max(_cooldown, _data.HangarLaunchInterval);
        }

        public void DisableBay(int bay)
        {
            _disabled[bay] = true;
            foreach (bool disabled in _disabled)
            {
                if (!disabled)
                {
                    return;
                }
            }

            IsOperational = false;
        }

        public void EnableBay(int bay)
        {
            _disabled[bay] = false;
            Resume();
        }

        public void Resume() => IsOperational = true;

        public void Shutdown() => IsOperational = false;
    }
}
