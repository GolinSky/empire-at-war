using System.Collections.Generic;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>A new baseline supersedes queued events that happened before that client's snapshot.</summary>
    public sealed class NetworkEventSequence
    {
        private readonly Dictionary<int, long> _baselines = new Dictionary<int, long>();
        private long _sequence;

        public long Next() => ++_sequence;

        public void MarkBaseline(int connectionId) => _baselines[connectionId] = _sequence;

        public bool IsAfterBaseline(int connectionId, long sequence) =>
            !_baselines.TryGetValue(connectionId, out long baseline) || sequence > baseline;
    }
}
