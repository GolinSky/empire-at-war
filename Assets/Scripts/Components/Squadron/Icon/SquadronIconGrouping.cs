using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>Publishes screen-space squadron clusters computed by a 10 Hz Burst job.</summary>
    internal sealed class SquadronIconGrouping : IDisposable
    {
        private const int INITIAL_CAPACITY = 32;
        private const float SNAPSHOT_INTERVAL = 0.1f;

        private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("Battle.Markers.Grouping.Schedule");
        private static readonly ProfilerMarker ConsumeMarker = new ProfilerMarker("Battle.Markers.Grouping.Consume");

        private readonly Dictionary<long, SquadronGroupingJob.Member> _published = new(INITIAL_CAPACITY);
        private readonly HashSet<long> _forgotten = new(INITIAL_CAPACITY);

        private NativeList<SquadronGroupingJob.Member> _snapshot = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeList<SquadronGroupingJob.Member> _members = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeList<SquadronGroupingJob.Cluster> _clusters = new(INITIAL_CAPACITY * 2, Allocator.Persistent);
        private NativeList<SquadronGroupingJob.ClusterOrder> _order = new(INITIAL_CAPACITY * 2, Allocator.Persistent);
        private NativeList<SquadronGroupingJob.RankKey> _ranked = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeParallelHashMap<long, int> _clusterById = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeParallelMultiHashMap<SquadronGroupingJob.Bucket, int> _buckets = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeReference<long> _nextClusterId = new(Allocator.Persistent);
        private JobHandle _pending;

        public bool HasPendingSnapshot { get; private set; }

        public void BeginSnapshot(int capacity)
        {
            if (HasPendingSnapshot)
                throw new InvalidOperationException("Consume the pending squadron grouping snapshot first.");

            _snapshot.Clear();
            if (capacity <= _snapshot.Capacity)
                return;

            int nextCapacity = Mathf.NextPowerOfTwo(capacity);
            _snapshot.Capacity = nextCapacity;
            _members.Capacity = nextCapacity;
            _ranked.Capacity = nextCapacity;
            _clusters.Capacity = nextCapacity * 2;
            _order.Capacity = nextCapacity * 2;
            _clusterById.Capacity = nextCapacity * 2;
            _buckets.Capacity = nextCapacity * 2;
            _published.EnsureCapacity(nextCapacity);
        }

        public void Add(long id, int owner, Vector2 screen)
        {
            _published.TryGetValue(id, out SquadronGroupingJob.Member member);
            if (member.Owner != owner)
                member = default;
            member.EntityId = id;
            member.Owner = owner;
            member.Position = new float2(screen.x, screen.y);
            _snapshot.AddNoResize(member);
        }

        public void Schedule(float elapsed, float joinPixels, float leavePixels, float pitch)
        {
            using var scope = ScheduleMarker.Auto();
            _pending = new SquadronGroupingJob
            {
                Snapshot = _snapshot,
                Members = _members,
                Clusters = _clusters,
                Order = _order,
                Ranked = _ranked,
                ClusterById = _clusterById,
                Buckets = _buckets,
                NextClusterId = _nextClusterId,
                Elapsed = Mathf.Clamp(elapsed, 0f, SNAPSHOT_INTERVAL),
                JoinPx = joinPixels,
                LeavePx = leavePixels,
                Pitch = pitch
            }.Schedule();
            HasPendingSnapshot = true;
            JobHandle.ScheduleBatchedJobs();
        }

        public bool TryComplete()
        {
            if (!HasPendingSnapshot || !_pending.IsCompleted)
                return false;

            using var scope = ConsumeMarker.Auto();
            _pending.Complete();
            HasPendingSnapshot = false;
            _published.Clear();
            for (int i = 0; i < _members.Length; i++)
            {
                SquadronGroupingJob.Member member = _members[i];
                if (_forgotten.Contains(member.EntityId))
                    continue;
                _published.Add(member.EntityId, member);
            }

            _forgotten.Clear();
            return true;
        }

        public bool TryGetCluster(long id, out long clusterId, out int rank, out int count)
        {
            if (_published.TryGetValue(id, out SquadronGroupingJob.Member member))
            {
                clusterId = member.ClusterId;
                rank = member.Rank;
                count = member.Count;
                return true;
            }

            clusterId = 0;
            rank = 0;
            count = 0;
            return false;
        }

        public void Forget(long id)
        {
            _published.Remove(id);

            if (HasPendingSnapshot)
                _forgotten.Add(id);
        }

        public void Dispose()
        {
            _pending.Complete();
            _snapshot.Dispose();
            _members.Dispose();
            _clusters.Dispose();
            _order.Dispose();
            _ranked.Dispose();
            _clusterById.Dispose();
            _buckets.Dispose();
            _nextClusterId.Dispose();
        }
    }
}
