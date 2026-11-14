using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    internal sealed class SquadronIconGrouping : IDisposable
    {
        private const int INITIAL_CAPACITY = 32;
        private const float SNAPSHOT_INTERVAL = 0.1f;

        private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("Battle.Markers.Grouping.Schedule");
        private static readonly ProfilerMarker ConsumeMarker = new ProfilerMarker("Battle.Markers.Grouping.Consume");

        private readonly Dictionary<long, SquadronGroupingJob.Member> _published = new(INITIAL_CAPACITY);
        private readonly Dictionary<long, int> _groupCounts = new(INITIAL_CAPACITY);
        private readonly HashSet<long> _forgotten = new(INITIAL_CAPACITY);

        private NativeList<SquadronGroupingJob.Member> _snapshot = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeList<SquadronGroupingJob.Member> _members = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeList<SquadronGroupingJob.Group> _groups = new(INITIAL_CAPACITY * 2, Allocator.Persistent);
        private NativeList<SquadronGroupingJob.GroupOrder> _order = new(INITIAL_CAPACITY * 2, Allocator.Persistent);
        private NativeParallelHashMap<long, int> _groupById = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeParallelMultiHashMap<SquadronGroupingJob.Bucket, int> _buckets = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeParallelHashMap<SquadronGroupingJob.OccupiedCell, byte> _occupied = new(INITIAL_CAPACITY, Allocator.Persistent);
        private NativeReference<SquadronGroupingJob.BatchState> _batchState = new(Allocator.Persistent);
        private JobHandle _pending;

        public bool HasPendingSnapshot { get; private set; }
        public int LastCandidateCount { get; private set; }

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
            _groups.Capacity = nextCapacity * 2;
            _order.Capacity = nextCapacity * 2;
            _groupById.Capacity = nextCapacity;
            _buckets.Capacity = nextCapacity;
            _occupied.Capacity = nextCapacity;
            _published.EnsureCapacity(nextCapacity);
            _groupCounts.EnsureCapacity(nextCapacity);
        }

        public void Add(long id, int owner, Vector2 position, Vector2 velocity)
        {
            _published.TryGetValue(id, out SquadronGroupingJob.Member member);
            if (member.Owner != owner)
                member = default;
            member.EntityId = id;
            member.Owner = owner;
            member.Position = new float2(position.x, position.y);
            member.Velocity = new float2(velocity.x, velocity.y);
            _snapshot.AddNoResize(member);
        }

        public void Schedule(float elapsed = SNAPSHOT_INTERVAL)
        {
            using var scope = ScheduleMarker.Auto();
            _pending = new SquadronGroupingJob
            {
                Snapshot = _snapshot,
                Members = _members,
                Groups = _groups,
                Order = _order,
                GroupById = _groupById,
                Buckets = _buckets,
                Occupied = _occupied,
                Batch = _batchState,
                Elapsed = Mathf.Clamp(elapsed, 0f, SNAPSHOT_INTERVAL)
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
            LastCandidateCount = _batchState.Value.CandidateCount;
            _published.Clear();
            _groupCounts.Clear();
            for (int i = 0; i < _members.Length; i++)
            {
                SquadronGroupingJob.Member member = _members[i];
                if (_forgotten.Contains(member.EntityId))
                    continue;
                _published.Add(member.EntityId, member);
                _groupCounts.TryGetValue(member.GroupId, out int count);
                _groupCounts[member.GroupId] = count + 1;
            }
            _forgotten.Clear();
            return true;
        }

        public bool TryGetSlot(long id, out long groupId, out Vector2Int cell)
        {
            if (_published.TryGetValue(id, out SquadronGroupingJob.Member member) &&
                _groupCounts[member.GroupId] > 1)
            {
                groupId = member.GroupId;
                cell = new Vector2Int(member.Cell.x, member.Cell.y);
                return true;
            }
            groupId = 0;
            cell = default;
            return false;
        }

        public void Forget(long id)
        {
            if (_published.Remove(id, out SquadronGroupingJob.Member member))
            {
                int remaining = _groupCounts[member.GroupId] - 1;
                if (remaining == 0)
                    _groupCounts.Remove(member.GroupId);
                else
                    _groupCounts[member.GroupId] = remaining;
            }

            if (HasPendingSnapshot)
                _forgotten.Add(id);
        }

        public void Dispose()
        {
            _pending.Complete();
            _snapshot.Dispose();
            _members.Dispose();
            _groups.Dispose();
            _order.Dispose();
            _groupById.Dispose();
            _buckets.Dispose();
            _occupied.Dispose();
            _batchState.Dispose();
        }
    }
}
