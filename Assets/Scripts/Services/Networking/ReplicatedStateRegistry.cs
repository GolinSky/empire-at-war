using System.Collections.Generic;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Battle scope: every <see cref="IReplicatedState"/>, including player-scope ones the battle scope cannot see.
    /// A client keeps state that arrives before its system registers and applies it on registration.
    /// </summary>
    public sealed class ReplicatedStateRegistry
    {
        private readonly Dictionary<string, IReplicatedState> _states = new Dictionary<string, IReplicatedState>();
        private readonly Dictionary<string, string> _pending = new Dictionary<string, string>();

        public IEnumerable<IReplicatedState> States => _states.Values;

        public void Register(IReplicatedState state)
        {
            _states.Add(state.Key, state);
            if (!_pending.Remove(state.Key, out string json)) return;
            state.Apply(json);
        }

        public void Unregister(IReplicatedState state) => _states.Remove(state.Key);

        public void Apply(string key, string json)
        {
            if (_states.TryGetValue(key, out IReplicatedState state)) state.Apply(json);
            else _pending[key] = json;
        }
    }
}
