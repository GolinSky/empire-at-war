using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>
    /// Clusters squadrons by on-screen distance. Every squadron belongs to exactly one cluster;
    /// a lone squadron is a 1×1 cluster. Ranks are compact and keep join order.
    /// </summary>
    [BurstCompile]
    internal struct SquadronGroupingJob : IJob
    {
        private const float JOIN_DWELL = 0.25f;
        private const float LEAVE_DWELL = 0.5f;
        private const float DWELL_EPSILON = 0.00001f;
        private const int MERGE_RANK_STRIDE = 8192;
        private const float MAX_EXTRA_PITCHES = 3f;

        private static readonly ProfilerMarker ReconcileMarker = new ProfilerMarker("Battle.Markers.Grouping.Reconcile");

        [ReadOnly] public NativeList<Member> Snapshot;
        public NativeList<Member> Members;
        public NativeList<Cluster> Clusters;
        public NativeList<ClusterOrder> Order;
        public NativeList<RankKey> Ranked;
        public NativeParallelHashMap<long, int> ClusterById;
        public NativeParallelMultiHashMap<Bucket, int> Buckets;
        public NativeReference<long> NextClusterId;
        public float Elapsed;
        public float JoinPx;
        public float LeavePx;
        public float Pitch;

        public void Execute()
        {
            using var scope = ReconcileMarker.Auto();
            Members.Clear();
            Members.AddRange(Snapshot.AsArray());
            Members.Sort(new MemberComparer());
            Clusters.Clear();
            Order.Clear();
            Ranked.Clear();
            ClusterById.Clear();
            Buckets.Clear();

            RestoreClusters();
            SplitClusters();
            BuildOrder();
            JoinClusters();
            AssignRanks();
        }

        private void RestoreClusters()
        {
            for (int i = 0; i < Members.Length; i++)
            {
                Member member = Members[i];
                if (member.ClusterId == 0) member.ClusterId = ++NextClusterId.Value;
                if (!ClusterById.TryGetValue(member.ClusterId, out int index))
                    index = CreateCluster(member.ClusterId, member.Owner);

                Cluster cluster = Clusters[index];
                member.Next = cluster.Head;
                cluster.Head = i;
                cluster.Count++;
                cluster.PositionSum += member.Position;
                Members[i] = member;
                Clusters[index] = cluster;
            }
        }

        private void SplitClusters()
        {
            int originalCount = Clusters.Length;
            for (int i = 0; i < originalCount; i++)
            {
                Cluster cluster = Clusters[i];
                int first = cluster.Head;
                MarkOutliers(first, cluster.Count, cluster.PositionSum);
                cluster.Head = -1;
                cluster.Count = 0;
                cluster.PositionSum = float2.zero;
                for (int index = first; index >= 0;)
                {
                    Member member = Members[index];
                    int next = member.Next;
                    member.LeaveAge = member.Apart ? member.LeaveAge + Elapsed : 0f;
                    member.Apart = false;
                    if (member.LeaveAge + DWELL_EPSILON >= LEAVE_DWELL)
                    {
                        int splitIndex = CreateCluster(++NextClusterId.Value, member.Owner);
                        Cluster split = Clusters[splitIndex];
                        split.Head = index;
                        split.Count = 1;
                        split.PositionSum = member.Position;
                        Clusters[splitIndex] = split;
                        member.ClusterId = split.Id;
                        member.Next = -1;
                        member.Rank = 0;
                        member.LeaveAge = 0f;
                        member.JoinAge = 0f;
                        member.JoinTarget = 0;
                    }
                    else
                    {
                        member.Next = cluster.Head;
                        cluster.Head = index;
                        cluster.Count++;
                        cluster.PositionSum += member.Position;
                    }

                    Members[index] = member;
                    index = next;
                }

                Clusters[i] = cluster;
            }
        }

        // Peels off the farthest member first, so one distant squadron cannot drag the
        // remaining members' reference point away and make them leave too.
        private void MarkOutliers(int first, int count, float2 sum)
        {
            float leaveSq = LeavePx * LeavePx;
            while (count > 1)
            {
                int farthest = -1;
                float farthestSq = leaveSq;
                for (int index = first; index >= 0; index = Members[index].Next)
                {
                    Member member = Members[index];
                    if (member.Apart) continue;
                    float2 others = (sum - member.Position) / (count - 1);
                    float distanceSq = math.distancesq(member.Position, others);
                    if (distanceSq <= farthestSq) continue;
                    farthest = index;
                    farthestSq = distanceSq;
                }

                if (farthest < 0) return;
                Member outlier = Members[farthest];
                outlier.Apart = true;
                Members[farthest] = outlier;
                sum -= outlier.Position;
                count--;
            }
        }

        private void BuildOrder()
        {
            for (int i = 0; i < Clusters.Length; i++)
                Order.AddNoResize(new ClusterOrder { Index = i, Id = Clusters[i].Id, Count = Clusters[i].Count });
            Order.Sort(new ClusterComparer());
        }

        private void JoinClusters()
        {
            float cellSize = JoinPx + MAX_EXTRA_PITCHES * Pitch;
            for (int i = 0; i < Order.Length; i++)
            {
                int clusterIndex = Order[i].Index;
                Cluster cluster = Clusters[clusterIndex];
                if (cluster.Count == 0) continue;
                float2 centroid = cluster.PositionSum / cluster.Count;
                int targetIndex = FindTarget(cluster, centroid, cellSize);
                if (targetIndex < 0)
                {
                    SetJoin(cluster, 0, 0f);
                    Buckets.Add(new Bucket(cluster.Owner, (int2)math.floor(centroid / cellSize)), clusterIndex);
                    continue;
                }

                Cluster target = Clusters[targetIndex];
                Member head = Members[cluster.Head];
                float age = head.JoinTarget == target.Id ? head.JoinAge + Elapsed : 0f;
                if (age + DWELL_EPSILON >= JOIN_DWELL)
                {
                    Merge(cluster, ref target);
                    Clusters[targetIndex] = target;
                    Cluster emptied = cluster;
                    emptied.Head = -1;
                    emptied.Count = 0;
                    Clusters[clusterIndex] = emptied;
                    continue;
                }

                SetJoin(cluster, target.Id, age);
                Buckets.Add(new Bucket(cluster.Owner, (int2)math.floor(centroid / cellSize)), clusterIndex);
            }
        }

        private int FindTarget(Cluster incoming, float2 centroid, float cellSize)
        {
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            int2 origin = (int2)math.floor(centroid / cellSize);
            for (int y = origin.y - 1; y <= origin.y + 1; y++)
            for (int x = origin.x - 1; x <= origin.x + 1; x++)
            {
                foreach (int index in Buckets.GetValuesForKey(new Bucket(incoming.Owner, new int2(x, y))))
                {
                    Cluster candidate = Clusters[index];
                    float2 other = candidate.PositionSum / candidate.Count;
                    float distance = math.distance(centroid, other);
                    float extra = math.max(0f, (SquadronClusterLayout.Columns(incoming.Count) +
                        SquadronClusterLayout.Columns(candidate.Count)) * 0.5f - 1f) * Pitch;
                    if (distance > JoinPx + extra || distance > bestDistance) continue;
                    if (best >= 0 && distance == bestDistance)
                    {
                        Cluster current = Clusters[best];
                        if (candidate.Count < current.Count ||
                            (candidate.Count == current.Count && candidate.Id >= current.Id))
                            continue;
                    }

                    best = index;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void SetJoin(Cluster cluster, long targetId, float age)
        {
            for (int index = cluster.Head; index >= 0; index = Members[index].Next)
            {
                Member member = Members[index];
                member.JoinTarget = targetId;
                member.JoinAge = age;
                Members[index] = member;
            }
        }

        private void Merge(Cluster incoming, ref Cluster target)
        {
            // Incoming members keep their relative order and queue behind the target's members.
            target.MergeBase += MERGE_RANK_STRIDE;
            for (int index = incoming.Head; index >= 0;)
            {
                Member member = Members[index];
                int next = member.Next;
                member.ClusterId = target.Id;
                member.Rank += target.MergeBase;
                member.JoinTarget = 0;
                member.JoinAge = 0f;
                member.LeaveAge = 0f;
                member.Next = target.Head;
                target.Head = index;
                Members[index] = member;
                index = next;
            }

            target.Count += incoming.Count;
            target.PositionSum += incoming.PositionSum;
        }

        private void AssignRanks()
        {
            for (int i = 0; i < Members.Length; i++)
                Ranked.AddNoResize(new RankKey { ClusterId = Members[i].ClusterId, Rank = Members[i].Rank, Index = i,
                    EntityId = Members[i].EntityId });
            Ranked.Sort(new RankComparer());
            int start = 0;
            for (int i = 0; i <= Ranked.Length; i++)
            {
                if (i < Ranked.Length && Ranked[i].ClusterId == Ranked[start].ClusterId) continue;
                for (int j = start; j < i; j++)
                {
                    Member member = Members[Ranked[j].Index];
                    member.Rank = j - start;
                    member.Count = i - start;
                    Members[Ranked[j].Index] = member;
                }

                start = i;
            }
        }

        private int CreateCluster(long id, int owner)
        {
            int index = Clusters.Length;
            Clusters.AddNoResize(new Cluster { Id = id, Owner = owner, Head = -1, MergeBase = 0 });
            ClusterById.TryAdd(id, index);
            return index;
        }

        internal struct Member
        {
            public long EntityId;
            public long ClusterId;
            public long JoinTarget;
            public int Owner;
            public int Rank;
            public int Count;
            public int Next;
            public float2 Position;
            public float JoinAge;
            public float LeaveAge;
            public bool Apart;
        }

        internal struct Cluster
        {
            public long Id;
            public int Owner;
            public int Head;
            public int Count;
            public int MergeBase;
            public float2 PositionSum;
        }

        internal struct ClusterOrder
        {
            public int Index;
            public long Id;
            public int Count;
        }

        internal struct RankKey
        {
            public long ClusterId;
            public int Rank;
            public int Index;
            public long EntityId;
        }

        internal readonly struct Bucket : IEquatable<Bucket>
        {
            public readonly int Owner;
            public readonly int2 Cell;

            public Bucket(int owner, int2 cell)
            {
                Owner = owner;
                Cell = cell;
            }

            public bool Equals(Bucket other) => Owner == other.Owner && math.all(Cell == other.Cell);
            public override int GetHashCode() => (int)math.hash(new int3(Cell, Owner));
        }

        private struct MemberComparer : IComparer<Member>
        {
            public int Compare(Member left, Member right) => left.EntityId.CompareTo(right.EntityId);
        }

        private struct ClusterComparer : IComparer<ClusterOrder>
        {
            public int Compare(ClusterOrder left, ClusterOrder right)
            {
                int count = right.Count.CompareTo(left.Count);
                return count != 0 ? count : left.Id.CompareTo(right.Id);
            }
        }

        private struct RankComparer : IComparer<RankKey>
        {
            public int Compare(RankKey left, RankKey right)
            {
                int cluster = left.ClusterId.CompareTo(right.ClusterId);
                if (cluster != 0) return cluster;
                int rank = left.Rank.CompareTo(right.Rank);
                return rank != 0 ? rank : left.EntityId.CompareTo(right.EntityId);
            }
        }
    }
}
