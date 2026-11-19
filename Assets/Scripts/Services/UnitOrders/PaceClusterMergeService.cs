using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.ShipNavigation;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>
    /// Tracks active spatial pace clusters and merges pairs from the same order once they converge cleanly
    /// without an obstacle between them for <see cref="PaceClusterTuning.MergeHoldChecks"/> consecutive checks.
    /// </summary>
    public sealed class PaceClusterMergeService : ITickable
    {
        private readonly UnitOrderSettings _settings;
        private readonly IMapObstacleContactProvider _obstacles;
        private readonly List<PaceCluster> _clusters = new List<PaceCluster>();
        private readonly List<RadarContact> _obstacleBuffer = new List<RadarContact>();
        private Dictionary<ClusterPair, int> _pairHoldCounts = new Dictionary<ClusterPair, int>();
        private Dictionary<ClusterPair, int> _nextHoldCounts = new Dictionary<ClusterPair, int>();
        private float _timer;

        public int ActiveClusterCount => _clusters.Count;

        public PaceClusterMergeService(UnitOrderSettings settings, IMapObstacleContactProvider obstacles)
        {
            _settings = settings;
            _obstacles = obstacles;
        }

        public void Register(PaceCluster cluster)
        {
            if (cluster == null || cluster.IsMerged) return;
            _clusters.Add(cluster);
        }

        public void RegisterRange(IReadOnlyList<PaceCluster> clusters)
        {
            for (int i = 0; i < clusters.Count; i++) Register(clusters[i]);
        }

        public void Tick() => Tick(Time.deltaTime);

        internal void Tick(float deltaTime)
        {
            if (_clusters.Count == 0)
            {
                _timer = 0f;
                return;
            }

            PaceClusterTuning tuning = _settings != null ? _settings.PaceTuning : PaceClusterTuning.Default;
            _timer += deltaTime;
            if (_timer < tuning.MergeInterval) return;
            _timer -= tuning.MergeInterval;

            PruneInactive();
            if (_clusters.Count < 2)
            {
                _pairHoldCounts.Clear();
                return;
            }

            _obstacleBuffer.Clear();
            _obstacles?.CopyContacts(_obstacleBuffer);
            bool hasObstacles = _obstacleBuffer.Count > 0;
            float minHeadingDot = Mathf.Cos(tuning.MergeAngleDegrees * Mathf.Deg2Rad);

            _nextHoldCounts.Clear();
            for (int i = 0; i < _clusters.Count; i++)
            {
                PaceCluster a = _clusters[i];
                if (a.IsMerged || a.MemberCount == 0) continue;

                for (int j = i + 1; j < _clusters.Count; j++)
                {
                    PaceCluster b = _clusters[j];
                    if (b.IsMerged || b.MemberCount == 0) continue;
                    if (a.OrderId != b.OrderId) continue;

                    if (!CanMerge(a, b, tuning, minHeadingDot, hasObstacles)) continue;

                    ClusterPair key = new ClusterPair(a, b);
                    int hold = (_pairHoldCounts.TryGetValue(key, out int previous) ? previous : 0) + 1;
                    if (hold >= tuning.MergeHoldChecks)
                    {
                        if (a.MemberCount <= b.MemberCount) a.MergeInto(b);
                        else b.MergeInto(a);
                        if (a.IsMerged) break;
                    }
                    else
                    {
                        _nextHoldCounts[key] = hold;
                    }
                }
            }

            (_pairHoldCounts, _nextHoldCounts) = (_nextHoldCounts, _pairHoldCounts);
            _nextHoldCounts.Clear();
            PruneInactive();
        }

        private bool CanMerge(PaceCluster a, PaceCluster b, PaceClusterTuning tuning,
            float minHeadingDot, bool hasObstacles)
        {
            Vector2 centroidA = a.Centroid;
            Vector2 centroidB = b.Centroid;
            float maxDistance = tuning.MergeFactor * (a.Radius + b.Radius) + tuning.MergePadding;
            if (Vector2.DistanceSquared(centroidA, centroidB) > maxDistance * maxDistance) return false;

            Vector2 headingA = a.Heading;
            Vector2 headingB = b.Heading;
            if (headingA.LengthSquared() > 1e-6f && headingB.LengthSquared() > 1e-6f &&
                Vector2.Dot(headingA, headingB) < minHeadingDot)
                return false;

            if (!PaceClusterBuilder.AreSpeedsCompatible(a.Pace, b.Pace, tuning.LinkSpeedRatio))
                return false;

            if (hasObstacles && !ShipAvoidancePlanner.IsSegmentClear(
                    new Vector3(centroidA.X, 0f, centroidA.Y),
                    new Vector3(centroidB.X, 0f, centroidB.Y),
                    _obstacleBuffer, 0f, 0f, 0f))
                return false;

            return true;
        }

        private void PruneInactive()
        {
            for (int i = _clusters.Count - 1; i >= 0; i--)
            {
                PaceCluster cluster = _clusters[i];
                if (cluster.IsMerged || cluster.MemberCount == 0)
                    _clusters.RemoveAt(i);
            }
        }

        private readonly struct ClusterPair : IEquatable<ClusterPair>
        {
            private readonly PaceCluster _first;
            private readonly PaceCluster _second;

            public ClusterPair(PaceCluster first, PaceCluster second)
            {
                _first = first;
                _second = second;
            }

            public bool Equals(ClusterPair other) =>
                (ReferenceEquals(_first, other._first) && ReferenceEquals(_second, other._second)) ||
                (ReferenceEquals(_first, other._second) && ReferenceEquals(_second, other._first));

            public override bool Equals(object obj) => obj is ClusterPair other && Equals(other);

            public override int GetHashCode()
            {
                int h1 = _first.GetHashCode();
                int h2 = _second.GetHashCode();
                return h1 <= h2 ? HashCode.Combine(h1, h2) : HashCode.Combine(h2, h1);
            }
        }
    }
}
