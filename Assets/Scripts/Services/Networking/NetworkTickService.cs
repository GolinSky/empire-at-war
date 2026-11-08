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
            foreach (EntitySpawnMessage spawn in _output.Spawns) NetworkServer.SendToAll(spawn);
            foreach (EntityDespawnMessage despawn in _output.Despawns) NetworkServer.SendToAll(despawn);
            foreach (EntityKeyframesMessage chunk in _output.Keyframes) NetworkServer.SendToAll(chunk, Channels.Unreliable);
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

        private void SendChangedHealth()
        {
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.HealthModel.IsDestroyed || !_replicator.TryGetIndex(entity.Id, out ushort index)) continue;
                Vector2 health = new Vector2(entity.HealthModel.HullPercentage, entity.HealthModel.ShieldPercentage);
                if (_sentHealth.TryGetValue(entity.Id, out Vector2 sent) && sent == health) continue;
                if (!TryCaptureHealth(entity, out UnitHealthSave save)) continue;
                _sentHealth[entity.Id] = health;
                NetworkServer.SendToAll(new EntityHealthMessage { Index = index, HealthJson = JsonUtility.ToJson(save) });
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
