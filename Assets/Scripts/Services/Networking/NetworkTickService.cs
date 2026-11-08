using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Saves;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host only (online authority): runs <see cref="HostTickReplicator"/> each frame and sends its output —
    /// spawns/despawns reliable, keyframe chunks unreliable — plus changed unit health at
    /// <see cref="HEALTH_INTERVAL_SECONDS"/>. Nothing is sent before the battle begins: each client starts from
    /// its baseline (<see cref="SendBaseline"/>). Not registered in offline battles.
    /// </summary>
    public sealed class NetworkTickService : ITickable, ILateDisposable
    {
        private const float HEALTH_INTERVAL_SECONDS = 0.25f;

        private readonly IEntityLocator _entities;
        private readonly OnlineMatchService _online;
        private readonly HostTickReplicator _replicator;
        private readonly HostTickOutput _output = new HostTickOutput();
        private readonly Dictionary<long, Vector2> _sentHealth = new Dictionary<long, Vector2>();
        private readonly HashSet<long> _announcedDeaths = new HashSet<long>();
        private float _healthTimer;

        public NetworkTickService(IEntityLocator entities, OnlineMatchService online)
        {
            _entities = entities;
            _online = online;
            _replicator = new HostTickReplicator(entities);
        }

        public uint CurrentTick => _replicator.Tick;

        public void Tick()
        {
            _replicator.Advance(Time.deltaTime, NetworkTime.time, _output);
            if (!_online.HasBegun) return;
            // Despawns first: a pooled squadron can leave and come back under the same id in one tick.
            foreach (EntityDespawnMessage despawn in _output.Despawns) HostBroadcast.ToClients(_online, despawn);
            foreach (EntitySpawnMessage spawn in _output.Spawns) HostBroadcast.ToClients(_online, spawn);
            foreach (EntityKeyframesMessage chunk in _output.Keyframes) HostBroadcast.ToClients(_online, chunk, Channels.Unreliable);
            _healthTimer -= Time.unscaledDeltaTime;
            if (_healthTimer > 0f) return;
            _healthTimer = HEALTH_INTERVAL_SECONDS;
            SendChangedHealth();
        }

        /// <summary>Join or reconnect: every live entity, then full keyframes on the next tick.</summary>
        public void SendBaseline(NetworkConnectionToClient connection)
        {
            foreach (EntitySpawnMessage spawn in _replicator.CreateBaselineSpawns(NetworkTime.time))
                connection.Send(spawn);
        }

        public void LateDispose() => _replicator.Dispose();

        /// <summary>See <see cref="HostTickReplicator.BeginHandover"/>.</summary>
        public void BeginHandover() => _replicator.BeginHandover();

        public void EndHandover() => _replicator.EndHandover();

        private void SendChangedHealth()
        {
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_replicator.TryGetIndex(entity.Id, out ushort index)) continue;
                if (entity.HealthModel.IsDestroyed)
                {
                    // Dead units stay in the locator through their death presentation; announce the death once.
                    if (_announcedDeaths.Add(entity.Id)) HostBroadcast.ToClients(_online, new EntityDiedMessage { Index = index });
                    continue;
                }

                _announcedDeaths.Remove(entity.Id);
                Vector2 health = new Vector2(entity.HealthModel.HullPercentage, entity.HealthModel.ShieldPercentage);
                if (_sentHealth.TryGetValue(entity.Id, out Vector2 sent) && sent == health) continue;
                if (!TryCaptureHealth(entity, out UnitHealthSave save)) continue;
                _sentHealth[entity.Id] = health;
                HostBroadcast.ToClients(_online, new EntityHealthMessage { Index = index, HealthJson = JsonUtility.ToJson(save) });
            }
        }

        private static bool TryCaptureHealth(IEntity entity, out UnitHealthSave save)
        {
            save = null;
            if (entity.TryGetFacade(out IUnitSaveFacade unit)) save = unit.Capture().Health;
            else if (entity.TryGetFacade(out IStructureSaveFacade structure)) save = structure.Capture().Health;
            return save != null;
        }
    }
}
