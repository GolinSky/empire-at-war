using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host only (online authority): runs <see cref="HostTickReplicator"/> each frame and sends its output —
    /// spawns/despawns reliable, keyframe chunks unreliable. Not registered in offline battles.
    /// </summary>
    public sealed class NetworkTickService : ITickable, ILateDisposable
    {
        private readonly HostTickReplicator _replicator;
        private readonly HostTickOutput _output = new HostTickOutput();

        public NetworkTickService(IEntityLocator entities)
        {
            _replicator = new HostTickReplicator(entities);
        }

        public uint CurrentTick => _replicator.Tick;

        public void Tick()
        {
            _replicator.Advance(Time.deltaTime, NetworkTime.time, _output);
            foreach (EntitySpawnMessage spawn in _output.Spawns) NetworkServer.SendToAll(spawn);
            foreach (EntityDespawnMessage despawn in _output.Despawns) NetworkServer.SendToAll(despawn);
            foreach (EntityKeyframesMessage chunk in _output.Keyframes) NetworkServer.SendToAll(chunk, Channels.Unreliable);
        }

        /// <summary>Join or reconnect: every live entity, then full keyframes on the next tick.</summary>
        public void SendBaseline(NetworkConnectionToClient connection)
        {
            foreach (EntitySpawnMessage spawn in _replicator.CreateBaselineSpawns(NetworkTime.time))
                connection.Send(spawn);
        }

        public void LateDispose() => _replicator.Dispose();
    }
}
