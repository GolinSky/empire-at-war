using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Networking.Messages;
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
        private readonly List<EntitySpawnMessage> _spawns = new List<EntitySpawnMessage>();
        private readonly List<EntityDespawnMessage> _despawns = new List<EntityDespawnMessage>();
        private double _time;

        public HostTickReplicator(IEntityLocator entities)
        {
            _entities = entities;
            foreach (IEntity entity in _entities.Entities) HandleAdded(entity);
            _entities.EntityAdded += HandleAdded;
            _entities.EntityRemoved += HandleRemoved;
        }

        public uint Tick => _clock.Tick;

        /// <summary>Advances the clock and fills <paramref name="output"/> with every message due this frame.</summary>
        public void Advance(float deltaTime, double now, HostTickOutput output)
        {
            _time = now;
            output.Clear();
            output.Spawns.AddRange(_spawns);
            output.Despawns.AddRange(_despawns);
            _spawns.Clear();
            _despawns.Clear();
            int due = _clock.Advance(deltaTime);
            if (due == 0) return;

            CollectKeyframes(now);
            List<EntityKeyframe[]> chunks = KeyframeChunker.Split(_pending);
            for (int i = 0; i < chunks.Count; i++)
            {
                output.Keyframes.Add(new EntityKeyframesMessage
                {
                    Tick = _clock.Tick,
                    Time = now,
                    ChunkIndex = (byte)i,
                    ChunkCount = (byte)chunks.Count,
                    Keyframes = chunks[i]
                });
            }
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

        private void HandleAdded(IEntity entity)
        {
            ushort index = _indices.Assign(entity.Id);
            if (TrySampleMotion(entity, _time, false, out EntityMotion motion))
                _spawns.Add(CreateSpawn(entity, index, motion, _time));
        }

        private void HandleRemoved(IEntity entity)
        {
            if (!_indices.TryGetIndex(entity.Id, out ushort index)) return;
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

        private EntitySpawnMessage CreateSpawn(IEntity entity, ushort index, EntityMotion motion, double now) =>
            new EntitySpawnMessage
            {
                Index = index,
                EntityId = entity.Id,
                OwnerIndex = entity.Owner.IsNone ? -1 : entity.Owner.Index,
                Tick = _clock.Tick,
                Time = now,
                Keyframe = EntityKeyframeCodec.Encode(index, motion)
            };
    }
}
