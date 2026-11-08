using System.Collections.Generic;
using EmpireAtWar.Services.Networking.Messages;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>Messages one host frame produced: reliable spawns/despawns, unreliable keyframe chunks.</summary>
    public sealed class HostTickOutput
    {
        public List<EntitySpawnMessage> Spawns { get; } = new List<EntitySpawnMessage>();
        public List<EntityDespawnMessage> Despawns { get; } = new List<EntityDespawnMessage>();
        public List<EntityKeyframesMessage> Keyframes { get; } = new List<EntityKeyframesMessage>();

        public void Clear()
        {
            Spawns.Clear();
            Despawns.Clear();
            Keyframes.Clear();
        }
    }
}
