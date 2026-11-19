using System;
using System.Collections.Generic;
using System.Numerics;

namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    /// <summary>
    /// Single-link union-find spatial clustering built once per order.
    /// Clusters of size 1 receive <c>null</c> (unit moves at its own speed).
    /// </summary>
    public static class PaceClusterBuilder
    {
        public static PaceCluster[] Build(
            IReadOnlyList<PaceMemberCandidate> candidates,
            int orderId,
            PaceClusterTuning tuning,
            Func<Vector2, Vector2, bool> isSegmentClear = null,
            List<PaceCluster> createdClusters = null)
        {
            int count = candidates.Count;
            PaceCluster[] result = new PaceCluster[count];
            if (count < 2) return result;

            int[] parent = new int[count];
            for (int i = 0; i < count; i++) parent[i] = i;

            for (int i = 0; i < count; i++)
            {
                PaceMemberCandidate a = candidates[i];
                for (int j = i + 1; j < count; j++)
                {
                    int rootA = Find(parent, i);
                    int rootB = Find(parent, j);
                    if (rootA == rootB) continue;

                    PaceMemberCandidate b = candidates[j];
                    float maxDist = MathF.Max(tuning.LinkDistance,
                        tuning.LinkRadiusFactor * (a.NavigationRadius + b.NavigationRadius));
                    if (Vector2.DistanceSquared(a.Position, b.Position) > maxDist * maxDist) continue;

                    if (!a.IsSquadron && !b.IsSquadron &&
                        !AreSpeedsCompatible(a.Speed, b.Speed, tuning.LinkSpeedRatio)) continue;

                    if (isSegmentClear != null && !isSegmentClear(a.Position, b.Position)) continue;

                    parent[rootB] = rootA;
                }
            }

            int[] sizes = new int[count];
            for (int i = 0; i < count; i++) sizes[Find(parent, i)]++;

            PaceCluster[] byRoot = new PaceCluster[count];
            for (int i = 0; i < count; i++)
            {
                int root = Find(parent, i);
                if (sizes[root] < 2) continue;
                PaceCluster cluster = byRoot[root];
                if (cluster == null)
                {
                    // Members set the straggler floor from their undamaged speeds when they join.
                    cluster = new PaceCluster(orderId, tuning: tuning);
                    byRoot[root] = cluster;
                    createdClusters?.Add(cluster);
                }
                result[i] = cluster;
            }

            return result;
        }

        public static bool AreSpeedsCompatible(float speedA, float speedB, float maxRatio)
        {
            if (float.IsInfinity(speedA) || float.IsNaN(speedA) ||
                float.IsInfinity(speedB) || float.IsNaN(speedB)) return true;
            if (speedA <= 0f || speedB <= 0f) return true;
            float min = MathF.Min(speedA, speedB);
            float max = MathF.Max(speedA, speedB);
            return max <= min * maxRatio;
        }

        private static int Find(int[] parent, int index)
        {
            int root = index;
            while (parent[root] != root) root = parent[root];
            while (parent[index] != root)
            {
                int next = parent[index];
                parent[index] = root;
                index = next;
            }
            return root;
        }
    }
}
