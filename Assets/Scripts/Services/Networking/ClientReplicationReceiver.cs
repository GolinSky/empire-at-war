using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Saves;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Online client: spawns the host's entities, moves them along the interpolated host timeline every frame,
    /// applies host health and deaths (explosions and wrecks stay local) and the host's time scale.
    /// </summary>
    public sealed class ClientReplicationReceiver : IInitializable, ILateTickable, ILateDisposable
    {
        private readonly ClientReplicaBuffer _replica;
        private readonly ReplicaEntitySpawner _spawner;
        private readonly IEntityLocator _entities;

        public ClientReplicationReceiver(ClientReplicaBuffer replica, ReplicaEntitySpawner spawner,
            IEntityLocator entities)
        {
            _replica = replica;
            _spawner = spawner;
            _entities = entities;
        }

        public void Initialize()
        {
            NetworkClient.RegisterHandler<EntitySpawnMessage>(OnSpawn);
            NetworkClient.RegisterHandler<EntityDespawnMessage>(OnDespawn);
            NetworkClient.RegisterHandler<EntityKeyframesMessage>(OnKeyframes);
            NetworkClient.RegisterHandler<EntityHealthMessage>(OnHealth);
            NetworkClient.RegisterHandler<MatchStateMessage>(OnMatchState);
        }

        public void LateDispose()
        {
            NetworkClient.UnregisterHandler<EntitySpawnMessage>();
            NetworkClient.UnregisterHandler<EntityDespawnMessage>();
            NetworkClient.UnregisterHandler<EntityKeyframesMessage>();
            NetworkClient.UnregisterHandler<EntityHealthMessage>();
            NetworkClient.UnregisterHandler<MatchStateMessage>();
        }

        public void LateTick()
        {
            _replica.Update(Time.unscaledDeltaTime);
            foreach (long entityId in _replica.EntityIds)
            {
                if (!_replica.TrySample(entityId, out EntityMotion motion) ||
                    !_entities.TryGetEntity(entityId, out IEntity entity) ||
                    !entity.TryGetFacade(out IEntityTransformFacade facade)) continue;
                facade.Transform.SetPositionAndRotation(motion.Position, Quaternion.Euler(0f, motion.Heading, 0f));
            }
        }

        private void OnSpawn(EntitySpawnMessage message)
        {
            if (_replica.OnSpawn(message, NetworkTime.localTime)) _spawner.Spawn(message);
        }

        private void OnDespawn(EntityDespawnMessage message)
        {
            if (!_replica.TryGetEntityId(message.Index, out long entityId)) return;
            _replica.OnDespawn(message);
            if (message.Destroyed && _entities.TryGetEntity(entityId, out IEntity entity))
                entity.GetFacade<IEntityDestroyFacade>().Destroy();
        }

        private void OnKeyframes(EntityKeyframesMessage message) => _replica.OnKeyframes(message, NetworkTime.localTime);

        private void OnHealth(EntityHealthMessage message)
        {
            if (_replica.TryGetEntityId(message.Index, out long entityId))
                _spawner.ApplyHealth(entityId, JsonUtility.FromJson<UnitHealthSave>(message.HealthJson));
        }

        private static void OnMatchState(MatchStateMessage message) => Time.timeScale = message.TimeScale;
    }
}
