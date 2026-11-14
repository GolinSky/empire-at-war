using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Host side of tick replication, transport-agnostic: spawns/despawns follow <see cref="IEntityLocator"/>;
    /// each 20 Hz tick samples entity motion, keeps only keyframes the clients' prediction needs and splits them
    /// into MTU-sized chunks. <see cref="NetworkTickService"/> sends the results through Mirror.
    /// </summary>
    public sealed class HostTickReplicator : IDisposable
    {
        private readonly IEntityLocator _entities;
        private readonly NetworkTickClock _clock = new NetworkTickClock();
        private readonly NetIndexMap _indices = new NetIndexMap();
        private readonly DeadReckoningSender _sender = new DeadReckoningSender();
        private readonly Dictionary<long, Pose> _previousPoses = new Dictionary<long, Pose>();
        private readonly Dictionary<long, double> _previousTimes = new Dictionary<long, double>();
        private readonly List<EntityKeyframe> _pending = new List<EntityKeyframe>();
        private readonly List<EntityKeyframe[]> _chunks = new List<EntityKeyframe[]>();
        private readonly List<IEntity> _pendingSpawns = new List<IEntity>();
        private readonly List<EntityDespawnMessage> _despawns = new List<EntityDespawnMessage>();
        private readonly Dictionary<long, IEntity> _handedOver = new Dictionary<long, IEntity>();
        private bool _isHandingOver;

        public HostTickReplicator(IEntityLocator entities)
        {
            _entities = entities;
            foreach (IEntity entity in _entities.Entities) HandleAdded(entity);
            _entities.EntityAdded += HandleAdded;
            _entities.EntityRemoved += HandleRemoved;
        }

        public uint Tick => _clock.Tick;

        public bool TryGetIndex(long entityId, out ushort index) => _indices.TryGetIndex(entityId, out index);

        /// <summary>Advances the clock and fills <paramref name="output"/> with every message due this frame.</summary>
        public void Advance(float deltaTime, double now, HostTickOutput output)
        {
            output.Clear();
            // Spawns are built here, not in EntityAdded, so the unit save captures a fully initialized entity.
            foreach (IEntity entity in _pendingSpawns)
            {
                if (_indices.TryGetIndex(entity.Id, out ushort index) && TrySampleMotion(entity, now, false, out EntityMotion motion))
                    output.Spawns.Add(CreateSpawn(entity, index, motion, now));
            }

            output.Despawns.AddRange(_despawns);
            _pendingSpawns.Clear();
            _despawns.Clear();
            int due = _clock.Advance(deltaTime);
            if (due == 0) return;

            CollectKeyframes(now);
            KeyframeChunker.Split(_pending, _chunks);
            for (int i = 0; i < _chunks.Count; i++)
            {
                output.Keyframes.Add(new EntityKeyframesMessage
                {
                    Tick = _clock.Tick,
                    Time = now,
                    ChunkIndex = (byte)i,
                    ChunkCount = (byte)_chunks.Count,
                    Keyframes = _chunks[i]
                });
            }
            _chunks.Clear();
        }

        /// <summary>A joining/reconnecting client gets spawns for every live entity and a full keyframe set.</summary>
        public IReadOnlyList<EntitySpawnMessage> CreateBaselineSpawns(double now)
        {
            List<EntitySpawnMessage> spawns = new List<EntitySpawnMessage>();
            foreach (IEntity entity in _entities.Entities)
            {
                if (_indices.TryGetIndex(entity.Id, out ushort index) && TrySampleMotion(entity, now, false, out EntityMotion motion))
                    spawns.Add(CreateSpawn(entity, index, motion, now));
            }

            _sender.ForceFullResend();
            return spawns;
        }

        public void Dispose()
        {
            _entities.EntityAdded -= HandleAdded;
            _entities.EntityRemoved -= HandleRemoved;
        }

        private void CollectKeyframes(double now)
        {
            _pending.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_indices.TryGetIndex(entity.Id, out ushort index) || !TrySampleMotion(entity, now, true, out EntityMotion motion))
                    continue;
                if (_sender.TryCreateKeyframe(index, motion, now, out EntityKeyframe keyframe))
                    _pending.Add(keyframe);
            }
        }

        // Rates come from the previous tick sample; only tick sampling advances that history.
        private bool TrySampleMotion(IEntity entity, double now, bool recordSample, out EntityMotion motion)
        {
            motion = default;
            if (!entity.TryGetFacade(out IEntityTransformFacade facade)) return false;
            Transform transform = facade.Transform;
            Pose current = new Pose(transform.position, transform.rotation);
            float heading = transform.eulerAngles.y;
            motion = _previousPoses.TryGetValue(entity.Id, out Pose previous)
                ? EntityMotion.FromSamples(previous.position, previous.rotation.eulerAngles.y, current.position,
                    heading, (float)(now - _previousTimes[entity.Id]))
                : new EntityMotion(current.position, heading, 0f, 0f, 0f);
            if (!recordSample) return true;
            _previousPoses[entity.Id] = current;
            _previousTimes[entity.Id] = now;
            return true;
        }

        /// <summary>
        /// An AI takeover rebuilds a player's units under the same ids. Between begin and end, a removed entity keeps
        /// its net index and clients keep their copy; one that does not come back is despawned at the end.
        /// </summary>
        public void BeginHandover() => _isHandingOver = true;

        public void EndHandover()
        {
            _isHandingOver = false;
            foreach (IEntity entity in _handedOver.Values) Remove(entity);
            _handedOver.Clear();
        }

        private void HandleAdded(IEntity entity)
        {
            if (_handedOver.Remove(entity.Id)) return;
            _indices.Assign(entity.Id);
            _pendingSpawns.Add(entity);
        }

        private void HandleRemoved(IEntity entity)
        {
            if (_isHandingOver && _indices.TryGetIndex(entity.Id, out _) && !_pendingSpawns.Contains(entity))
            {
                _handedOver[entity.Id] = entity;
                return;
            }

            Remove(entity);
        }

        private void Remove(IEntity entity)
        {
            if (!_indices.TryGetIndex(entity.Id, out ushort index)) return;
            // Never announced: clients have nothing to remove.
            if (!_pendingSpawns.Remove(entity))
                _despawns.Add(new EntityDespawnMessage
                {
                    Index = index,
                    Tick = _clock.Tick,
                    Destroyed = entity.HealthModel.IsDestroyed
                });
            _sender.Forget(index);
            _previousPoses.Remove(entity.Id);
            _previousTimes.Remove(entity.Id);
            _indices.Release(entity.Id);
        }

        private EntitySpawnMessage CreateSpawn(IEntity entity, ushort index, EntityMotion motion, double now)
        {
            EntitySpawnMessage spawn = new EntitySpawnMessage
            {
                Index = index,
                EntityId = entity.Id,
                OwnerIndex = entity.Owner.IsNone ? -1 : entity.Owner.Index,
                Tick = _clock.Tick,
                Time = now,
                Keyframe = EntityKeyframeCodec.Encode(index, motion),
                Kind = EntitySpawnKind.Untracked
            };
            if (entity.TryGetFacade(out IUnitSaveFacade unit))
            {
                spawn.Kind = EntitySpawnKind.Unit;
                spawn.Payload = JsonUtility.ToJson(unit.Capture());
                spawn.IsArriving = entity.TryGetFacade(out IShipArrivalFacade arrival) && arrival.IsArriving;
            }
            else if (entity.TryGetFacade(out IStructureSaveFacade structure))
            {
                spawn.Kind = EntitySpawnKind.Structure;
                StructureSave save = structure.Capture();
                // Without it clients restore a structure under construction as already built.
                if (entity.TryGetFacade(out IConstructionFacade construction))
                    save.WithConstruction(construction.Progress, construction.BuildTime);
                spawn.Payload = JsonUtility.ToJson(save);
            }

            return spawn;
        }
    }
}
