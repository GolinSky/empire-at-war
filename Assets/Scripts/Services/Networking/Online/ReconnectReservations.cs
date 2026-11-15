using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Networking.Online
{
    /// <summary>Host-clock recovery deadlines. Retries cannot renew a reservation.</summary>
    public sealed class ReconnectReservations
    {
        public const float GRACE_SECONDS = 300f;
        public const float LOAD_ALLOWANCE_SECONDS = 180f;

        private readonly Dictionary<int, float> _deadlines = new Dictionary<int, float>();
        private readonly HashSet<int> _accepted = new HashSet<int>();

        public int Count => _deadlines.Count;

        public void MarkDisconnected(int slotIndex, float now)
        {
            if (!_deadlines.ContainsKey(slotIndex)) _deadlines.Add(slotIndex, now + GRACE_SECONDS);
        }

        public bool TryAccept(int slotIndex, float now)
        {
            // A valid early rejoin supersedes a connection whose loss the transport has not detected yet.
            MarkDisconnected(slotIndex, now);
            if (now >= _deadlines[slotIndex]) return false;
            if (_accepted.Add(slotIndex))
                _deadlines[slotIndex] = Math.Max(_deadlines[slotIndex], now + LOAD_ALLOWANCE_SECONDS);
            return true;
        }

        public float Remaining(int slotIndex, float now) => Math.Max(0f, _deadlines[slotIndex] - now);

        public List<int> Expired(float now)
        {
            var expired = new List<int>();
            foreach (KeyValuePair<int, float> entry in _deadlines)
                if (now >= entry.Value) expired.Add(entry.Key);
            return expired;
        }

        public void Complete(int slotIndex) => Remove(slotIndex);

        public void Remove(int slotIndex)
        {
            _deadlines.Remove(slotIndex);
            _accepted.Remove(slotIndex);
        }

        public void Clear()
        {
            _deadlines.Clear();
            _accepted.Clear();
        }
    }
}
