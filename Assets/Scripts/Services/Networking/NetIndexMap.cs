using System.Collections.Generic;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host-assigned 16-bit index per replicated entity, so per-tick data does not carry 64-bit ids.
    /// The long id is sent once in the spawn message; freed indices are reused after despawn.
    /// </summary>
    public sealed class NetIndexMap
    {
        private readonly Dictionary<long, ushort> _indices = new Dictionary<long, ushort>();
        private readonly Dictionary<ushort, long> _ids = new Dictionary<ushort, long>();
        private readonly Stack<ushort> _free = new Stack<ushort>();
        private ushort _next;

        public int Count => _indices.Count;

        public ushort Assign(long entityId)
        {
            ushort index = _free.Count > 0 ? _free.Pop() : checked(_next++);
            _indices.Add(entityId, index);
            _ids.Add(index, entityId);
            return index;
        }

        /// <summary>Client side: mirrors the host's assignment from a spawn message.</summary>
        public void Bind(ushort index, long entityId)
        {
            _indices.Add(entityId, index);
            _ids.Add(index, entityId);
        }

        public bool TryGetIndex(long entityId, out ushort index) => _indices.TryGetValue(entityId, out index);

        public bool TryGetEntityId(ushort index, out long entityId) => _ids.TryGetValue(index, out entityId);

        public void Release(long entityId)
        {
            ushort index = _indices[entityId];
            _indices.Remove(entityId);
            _ids.Remove(index);
            _free.Push(index);
        }

        /// <summary>Client side: forgets a binding without recycling (the host owns recycling).</summary>
        public void Unbind(ushort index)
        {
            _indices.Remove(_ids[index]);
            _ids.Remove(index);
        }
    }
}
