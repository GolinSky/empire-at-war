using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    // Owns snapshots only; routes and reservations remain owned by navigation.
    internal sealed class ShipSpacingCoordinator : IDisposable
    {
        private const float SNAPSHOT_INTERVAL = 0.2f;
        private const float CORRECTION_COOLDOWN = 1f;
        private const int MAX_CONSUMED_PER_FRAME = 32;
        // A holder still turning onto its target has not settled its heading; its turn check is stale.
        private const float HEADING_TOLERANCE_DEGREES = 5f;
        private static readonly ProfilerMarker _snapshotMarker = new ProfilerMarker("Battle.Spacing.Snapshot");
        private static readonly ProfilerMarker _consumeMarker = new ProfilerMarker("Battle.Spacing.Consume");
        private readonly ShipSpacingBatch _batch = new ShipSpacingBatch();
        private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
        private readonly List<Entry> _snapshot = new List<Entry>();
        private readonly ShipDestinationRegistry _registry;
        private float _nextSnapshot;
        private float _snapshotTime;
        private int _consumeIndex;
        private bool _hasResults;

        public int VisitedPairs { get; private set; }
        public int SaturatedQueries { get; private set; }
        public int Corrections { get; private set; }
        public int FailedCorrections { get; private set; }
        public int ReservationSaturations { get; private set; }
        public float QueueAge { get; private set; }

        public ShipSpacingCoordinator(ShipDestinationRegistry registry) => _registry = registry;

        public void Register(int id, IShipNavigationAgent agent) => _entries.Add(id, new Entry(id, agent));
        public void Unregister(int id) => _entries.Remove(id);

        public void Tick(float now)
        {
            if (_batch.TryComplete())
            {
                _hasResults = true;
                _consumeIndex = 0;
            }
            if (_batch.IsScheduled || _hasResults || now < _nextSnapshot || _entries.Count == 0) return;
            using (_snapshotMarker.Auto())
            {
                _nextSnapshot = now + SNAPSHOT_INTERVAL;
                _snapshotTime = now;
                _batch.EnsureCapacity(_entries.Count);
                _snapshot.Clear();
                var inputs = _batch.Inputs;
                float maxRadius = 0f;
                foreach (Entry entry in _entries.Values)
                {
                    IShipNavigationAgent agent = entry.Agent;
                    Vector3 position = agent.NavigationPosition;
                    bool eligible = entry.Spacing != null && entry.Spacing.CanSettle;
                    entry.TargetPosition = eligible ? entry.Spacing.SpacingTargetPosition : Vector3.zero;
                    inputs[_snapshot.Count] = new ShipSpacingInput
                    {
                        RegistrationId = entry.Id,
                        Revision = entry.Spacing == null ? 0 : entry.Spacing.SpacingRevision,
                        Position = new float2(position.x, position.z),
                        TargetPosition = new float2(entry.TargetPosition.x, entry.TargetPosition.z),
                        Forward = eligible ? PlanarForward(entry.Spacing) : float2.zero,
                        Radius = agent.NavigationRadius,
                        Bottom = agent.NavigationHullSpan.Bottom,
                        Top = agent.NavigationHullSpan.Top,
                        Eligible = eligible,
                        Moving = agent.NavigationCurrentSpeed > 0f || entry.Spacing != null && entry.Spacing.IsMoving,
                        Anchored = eligible && IsFailureUnchanged(entry,
                            entry.Spacing.SpacingRevision, position, entry.TargetPosition),
                        Cursor = entry.Cursor
                    };
                    maxRadius = Mathf.Max(maxRadius, agent.NavigationRadius);
                    _snapshot.Add(entry);
                }
                _batch.Schedule(_snapshot.Count, maxRadius);
            }
        }

        // Called only after normal orders, inside the existing navigation plan budget.
        public bool TryCorrect(float now)
        {
            if (!_hasResults) return false;
            using (_consumeMarker.Auto())
            {
                QueueAge = now - _snapshotTime;
                for (int consumed = 0; consumed < MAX_CONSUMED_PER_FRAME && _consumeIndex < _snapshot.Count; consumed++)
                {
                    int index = _consumeIndex++;
                    Entry entry = _snapshot[index];
                    ShipSpacingInput input = _batch.Inputs[index];
                    ShipSpacingResult result = _batch.Results[index];
                    VisitedPairs += result.Visited;
                    if (result.Saturated) SaturatedQueries++;
                    if (!_entries.TryGetValue(entry.Id, out Entry current) || !ReferenceEquals(current, entry)) continue;
                    entry.Cursor = result.NextCursor;
                    IShipSpacingAgent agent = entry.Spacing;
                    if (!result.HasCorrection || agent == null || !agent.CanSettle ||
                        agent.SpacingRevision != result.Revision || now < entry.NextCorrection ||
                        !_entries.TryGetValue(result.BlockerId, out Entry blocker)) continue;
                    if (!ReferenceEquals(blocker, _snapshot[result.BlockerIndex]) ||
                        blocker.Agent.NavigationCurrentSpeed > 0f ||
                        blocker.Spacing != null && blocker.Spacing.IsMoving) continue;
                    ShipSpacingInput blockerInput = _batch.Inputs[result.BlockerIndex];
                    Vector3 position = agent.NavigationPosition;
                    Vector3 blockerPosition = blocker.Agent.NavigationPosition;
                    float tolerance = Mathf.Max(0.1f, agent.NavigationRadius * 0.1f);
                    if (math.distancesq(new float2(position.x, position.z), input.Position) > tolerance * tolerance ||
                        math.distancesq(new float2(blockerPosition.x, blockerPosition.z), blockerInput.Position) > tolerance * tolerance ||
                        blocker.Spacing != null && blocker.Spacing.SpacingRevision != blockerInput.Revision ||
                        (agent.SpacingTargetPosition - entry.TargetPosition).sqrMagnitude > tolerance * tolerance ||
                        math.dot(PlanarForward(agent), input.Forward) <
                        Mathf.Cos(HEADING_TOLERANCE_DEGREES * Mathf.Deg2Rad)) continue;
                    // Recheck the actual pair; old snapshots must not displace a newly clear hull.
                    float radius = agent.NavigationRadius + blocker.Agent.NavigationRadius;
                    float settleDistance = radius * (1f - ShipSpacingJob.MIN_PENETRATION_FRACTION);
                    Vector3 delta = position - blockerPosition;
                    if (delta.x * delta.x + delta.z * delta.z > settleDistance * settleDistance ||
                        !agent.NavigationHullSpan.Overlaps(blocker.Agent.NavigationHullSpan)) continue;
                    // Whole-cell hashes remain stable as a saturated query rotates its visited slice.
                    if (entry.FailedOccupancyHash == result.OccupancyHash &&
                        IsFailureUnchanged(entry, result.Revision, position, agent.SpacingTargetPosition)) continue;

                    entry.NextCorrection = now + CORRECTION_COOLDOWN;
                    _registry.BeginSpatialQueries();
                    bool moving;
                    try
                    {
                        moving = agent.TrySettle(new Vector3(result.Goal.x, agent.NavigationHeight, result.Goal.y), result.Revision);
                    }
                    finally
                    {
                        ReservationSaturations += _registry.SpatialQuerySaturations;
                        _registry.EndSpatialQueries();
                    }
                    entry.Failed = !moving;
                    if (!moving) FailedCorrections++;
                    entry.FailedRevision = result.Revision;
                    entry.FailedBlocker = result.BlockerId;
                    entry.FailedBlockerEntry = blocker;
                    entry.FailedPosition = position;
                    entry.FailedBlockerPosition = blockerPosition;
                    entry.FailedTargetPosition = agent.SpacingTargetPosition;
                    entry.FailedOccupancyHash = result.OccupancyHash;
                    // Capture after Plan: its own failed Stop must not immediately wake this failure.
                    entry.FailedReservationVersion = _registry.ReservationVersion;
                    Corrections++;
                    if (_consumeIndex == _snapshot.Count) _hasResults = false;
                    return true;
                }
                if (_consumeIndex == _snapshot.Count) _hasResults = false;
                return false;
            }
        }

        public void Dispose() => _batch.Dispose();

        private static float2 PlanarForward(IShipSpacingAgent agent)
        {
            Vector3 forward = agent.SpacingForward;
            return math.normalize(new float2(forward.x, forward.z));
        }

        private bool IsFailureUnchanged(Entry entry, int revision, Vector3 position, Vector3 target)
        {
            float tolerance = Mathf.Max(0.1f, entry.Agent.NavigationRadius * 0.1f);
            return entry.Failed && entry.FailedRevision == revision &&
                entry.FailedReservationVersion == _registry.ReservationVersion &&
                (position - entry.FailedPosition).sqrMagnitude < tolerance * tolerance &&
                (target - entry.FailedTargetPosition).sqrMagnitude < tolerance * tolerance &&
                _entries.TryGetValue(entry.FailedBlocker, out Entry blocker) &&
                ReferenceEquals(blocker, entry.FailedBlockerEntry) &&
                (blocker.Agent.NavigationPosition - entry.FailedBlockerPosition).sqrMagnitude < tolerance * tolerance;
        }

        private sealed class Entry
        {
            public readonly int Id;
            public readonly IShipNavigationAgent Agent;
            public readonly IShipSpacingAgent Spacing;
            public int Cursor;
            public Vector3 TargetPosition;
            public float NextCorrection;
            public bool Failed;
            public int FailedRevision;
            public int FailedBlocker;
            public Entry FailedBlockerEntry;
            public Vector3 FailedPosition;
            public Vector3 FailedBlockerPosition;
            public Vector3 FailedTargetPosition;
            public uint FailedOccupancyHash;
            public int FailedReservationVersion;

            public Entry(int id, IShipNavigationAgent agent)
            {
                Id = id;
                Agent = agent;
                Spacing = agent as IShipSpacingAgent;
            }
        }
    }
}
