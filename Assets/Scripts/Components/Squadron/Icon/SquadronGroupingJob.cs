using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    [BurstCompile]
    internal struct SquadronGroupingJob : IJob
    {
        private const float JOIN_RADIUS = 70f;
        private const float LEAVE_RADIUS = 1.35f * JOIN_RADIUS;
        private const float JOIN_SPEED = 22f;
        private const float LEAVE_SPEED = 1.5f * JOIN_SPEED;
        private const float JOIN_DWELL = 0.2f;
        private const float LEAVE_DWELL = 0.3f;
        private const float DWELL_EPSILON = 0.00001f;
        private const float COMPACTION_DWELL = 1.5f;

        private static readonly ProfilerMarker ReconcileMarker = new ProfilerMarker("Battle.Markers.Grouping.Reconcile");

        [ReadOnly] public NativeList<Member> Snapshot;
        public NativeList<Member> Members;
        public NativeList<Group> Groups;
        public NativeList<GroupOrder> Order;
        public NativeParallelHashMap<long, int> GroupById;
        public NativeParallelMultiHashMap<Bucket, int> Buckets;
        public NativeParallelHashMap<OccupiedCell, byte> Occupied;
        public NativeReference<BatchState> Batch;
        public float Elapsed;

        public void Execute()
        {
            using var scope = ReconcileMarker.Auto();
            BatchState state = Batch.Value;
            state.CandidateCount = 0;
            Members.Clear();
            Members.AddRange(Snapshot.AsArray());
            Members.Sort(new MemberComparer());
            Groups.Clear();
            Order.Clear();
            GroupById.Clear();
            Buckets.Clear();
            Occupied.Clear();

            RestoreGroups(ref state);
            SplitGroups(ref state);
            CompactSparseGroups();
            BuildRepresentativeOrder();
            JoinNearbyGroups(ref state);
            for (int i = 0; i < Members.Length; i++)
            {
                Member member = Members[i];
                member.Cell = GetCell(member.Slot);
                Members[i] = member;
            }
            Batch.Value = state;
        }

        private void RestoreGroups(ref BatchState state)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                Member member = Members[i];
                int groupIndex;
                if (member.GroupId == 0)
                {
                    groupIndex = CreateGroup(++state.NextGroupId, member.Owner, i);
                }
                else if (!GroupById.TryGetValue(member.GroupId, out groupIndex))
                {
                    groupIndex = CreateGroup(member.GroupId, member.Owner, i);
                }

                Group group = Groups[groupIndex];
                member.GroupId = group.Id;
                member.Next = group.Head;
                if (member.IsSeed)
                    group.Seed = i;
                group.Head = i;
                group.Count++;
                group.PositionSum += member.Position;
                group.VelocitySum += member.Velocity;
                Members[i] = member;
                Groups[groupIndex] = group;
            }
        }

        private void SplitGroups(ref BatchState state)
        {
            int originalCount = Groups.Length;
            for (int i = 0; i < originalCount; i++)
            {
                Group group = Groups[i];
                int seedIndex = SelectSeed(group, ref state.CandidateCount);
                Member seed = Members[seedIndex];
                int first = group.Head;
                group.Seed = seedIndex;
                group.Head = -1;
                group.Count = 0;
                group.MemberSignature = 0;
                for (int memberIndex = first; memberIndex >= 0;)
                {
                    Member member = Members[memberIndex];
                    int next = member.Next;
                    member.IsSeed = memberIndex == seedIndex;
                    member.LeaveAge = Compatible(member, seed, LEAVE_RADIUS, LEAVE_SPEED)
                        ? 0f
                        : member.LeaveAge + Elapsed;
                    if (member.LeaveAge + DWELL_EPSILON >= LEAVE_DWELL)
                    {
                        int splitIndex = CreateGroup(++state.NextGroupId, member.Owner, memberIndex, group.Id);
                        Group split = Groups[splitIndex];
                        split.Head = memberIndex;
                        split.Count = 1;
                        split.MemberSignature = MemberSignature(member.EntityId);
                        Groups[splitIndex] = split;
                        member.GroupId = split.Id;
                        member.IsSeed = true;
                        member.Next = -1;
                        member.LeaveAge = 0f;
                        member.JoinAge = 0f;
                        member.JoinTarget = 0;
                    }
                    else
                    {
                        member.Next = group.Head;
                        group.Head = memberIndex;
                        group.Count++;
                        group.MemberSignature ^= MemberSignature(member.EntityId);
                    }
                    Members[memberIndex] = member;
                    Occupied.TryAdd(new OccupiedCell(member.GroupId, member.Slot), 0);
                    memberIndex = next;
                }
                Groups[i] = group;
            }
        }

        private int SelectSeed(Group group, ref int candidateCount)
        {
            Member seed = Members[group.Seed];
            float2 center = group.PositionSum / group.Count;
            float2 velocity = group.VelocitySum / group.Count;
            float seedScore = DistanceScore(seed, center, velocity);
            if (seedScore <= 0.25f)
                return group.Seed;

            int best = group.Seed;
            for (int index = group.Head; index >= 0; index = Members[index].Next)
            {
                Member member = Members[index];
                float score = DistanceScore(member, center, velocity);
                if (score < seedScore || (score == seedScore && member.EntityId < Members[best].EntityId))
                {
                    best = index;
                    seedScore = score;
                }
            }

            if (best == group.Seed)
                return best;

            // Preserve the original identity on the side that keeps the most members.
            // At equal size, retaining the original seed avoids a gratuitous slot change.
            Member alternative = Members[best];
            int retained = 0;
            int alternativeCount = 0;
            for (int index = group.Head; index >= 0; index = Members[index].Next)
            {
                Member member = Members[index];
                candidateCount += 2;
                if (Compatible(member, seed, LEAVE_RADIUS, LEAVE_SPEED)) retained++;
                if (Compatible(member, alternative, LEAVE_RADIUS, LEAVE_SPEED)) alternativeCount++;
            }
            return alternativeCount > retained ? best : group.Seed;
        }

        private void CompactSparseGroups()
        {
            for (int i = 0; i < Groups.Length; i++)
            {
                Group group = Groups[i];
                Member seed = Members[group.Seed];
                float settledAge = seed.PriorCount == group.Count && seed.PriorSignature == group.MemberSignature
                    ? math.min(seed.SettledAge + Elapsed, COMPACTION_DWELL)
                    : 0f;
                seed.SettledAge = settledAge;
                seed.PriorCount = group.Count;
                seed.PriorSignature = group.MemberSignature;
                Members[group.Seed] = seed;
                if (settledAge < COMPACTION_DWELL)
                    continue;

                // Sort-free compaction only moves outliers that need over twice the occupied cells.
                // Nearby vacancies remain available for reinforcements without shifting survivors.
                int outerSlot = group.Count * 2;
                for (int index = group.Head; index >= 0; index = Members[index].Next)
                {
                    Member member = Members[index];
                    if (member.Slot < outerSlot)
                        continue;

                    Occupied.Remove(new OccupiedCell(group.Id, member.Slot));
                    member.Slot = ClaimFirstVacancy(ref group);
                    Members[index] = member;
                }
                Groups[i] = group;
            }
        }

        private static float DistanceScore(Member member, float2 center, float2 velocity)
        {
            return math.distancesq(member.Position, center) / (LEAVE_RADIUS * LEAVE_RADIUS) +
                   math.distancesq(member.Velocity, velocity) / (LEAVE_SPEED * LEAVE_SPEED);
        }

        private void BuildRepresentativeOrder()
        {
            for (int i = 0; i < Groups.Length; i++)
            {
                Group group = Groups[i];
                Order.AddNoResize(new GroupOrder { Index = i, Id = group.Id, Count = group.Count });
            }
            Order.Sort(new GroupComparer());
        }

        private void JoinNearbyGroups(ref BatchState state)
        {
            for (int i = 0; i < Order.Length; i++)
            {
                int groupIndex = Order[i].Index;
                Group group = Groups[groupIndex];
                Member seed = Members[group.Seed];
                var bucket = new Bucket(group.Owner, (int2)math.floor(seed.Position / JOIN_RADIUS),
                    (int)math.floor(math.length(seed.Velocity) / JOIN_SPEED));
                int targetIndex = FindTarget(group, seed, bucket, ref state.CandidateCount);
                if (targetIndex < 0)
                {
                    ClearJoin(group);
                    Buckets.Add(bucket, groupIndex);
                    continue;
                }

                Group target = Groups[targetIndex];
                float age = seed.JoinTarget == target.Id ? seed.JoinAge + Elapsed : 0f;
                bool reunitesSplit = group.SplitFrom != 0 && group.SplitFrom == target.SplitFrom;
                if (reunitesSplit || age + DWELL_EPSILON >= JOIN_DWELL)
                {
                    Merge(group, ref target, reunitesSplit);
                    Groups[targetIndex] = target;
                    continue;
                }

                for (int memberIndex = group.Head; memberIndex >= 0; memberIndex = Members[memberIndex].Next)
                {
                    Member member = Members[memberIndex];
                    member.JoinTarget = target.Id;
                    member.JoinAge = age;
                    Members[memberIndex] = member;
                }
            }
        }

        private int FindTarget(Group incoming, Member seed, Bucket origin, ref int candidateCount)
        {
            int bestIndex = -1;
            int bestCount = 0;
            long bestId = long.MaxValue;
            float bestDistance = float.PositiveInfinity;
            int minSpeed = math.max(0, origin.SpeedBand - 1);
            int maxSpeed = origin.SpeedBand + 1;
            for (int y = origin.Cell.y - 1; y <= origin.Cell.y + 1; y++)
            for (int x = origin.Cell.x - 1; x <= origin.Cell.x + 1; x++)
            for (int speed = minSpeed; speed <= maxSpeed; speed++)
            {
                // Speeds cannot differ by more than the velocity difference.
                var neighbor = new Bucket(incoming.Owner, new int2(x, y), speed);
                foreach (int index in Buckets.GetValuesForKey(neighbor))
                {
                    Group candidate = Groups[index];
                    Member targetSeed = Members[candidate.Seed];
                    candidateCount++;
                    float distance = math.distancesq(seed.Position, targetSeed.Position);
                    if (candidate.Count < bestCount ||
                        (candidate.Count == bestCount && (distance > bestDistance ||
                            (distance == bestDistance && candidate.Id >= bestId))) ||
                        !Compatible(seed, targetSeed, JOIN_RADIUS, JOIN_SPEED) ||
                        !FitsGroup(incoming, targetSeed, ref candidateCount))
                        continue;

                    bestIndex = index;
                    bestCount = candidate.Count;
                    bestDistance = distance;
                    bestId = candidate.Id;
                }
            }
            return bestIndex;
        }

        private bool FitsGroup(Group incoming, Member target, ref int candidateCount)
        {
            for (int index = incoming.Head; index >= 0; index = Members[index].Next)
            {
                if (index == incoming.Seed)
                    continue;
                candidateCount++;
                if (!Compatible(Members[index], target, JOIN_RADIUS, JOIN_SPEED))
                    return false;
            }
            return true;
        }

        private void ClearJoin(Group group)
        {
            for (int index = group.Head; index >= 0; index = Members[index].Next)
            {
                Member member = Members[index];
                member.JoinTarget = 0;
                member.JoinAge = 0f;
                Members[index] = member;
            }
        }

        private void Merge(Group incoming, ref Group target, bool preserveCells)
        {
            for (int index = incoming.Head; index >= 0;)
            {
                Member member = Members[index];
                int next = member.Next;
                Occupied.Remove(new OccupiedCell(member.GroupId, member.Slot));
                if (!preserveCells || !Occupied.TryAdd(new OccupiedCell(target.Id, member.Slot), 0))
                    member.Slot = ClaimFirstVacancy(ref target);
                member.GroupId = target.Id;
                member.IsSeed = false;
                member.JoinTarget = 0;
                member.JoinAge = 0f;
                member.LeaveAge = 0f;
                member.Next = target.Head;
                target.Head = index;
                Members[index] = member;
                index = next;
            }
            target.Count += incoming.Count;
        }

        private int ClaimFirstVacancy(ref Group group)
        {
            while (!Occupied.TryAdd(new OccupiedCell(group.Id, group.FirstVacancy), 0))
                group.FirstVacancy++;
            return group.FirstVacancy++;
        }

        private int CreateGroup(long id, int owner, int seed, long splitFrom = 0)
        {
            int index = Groups.Length;
            Groups.AddNoResize(new Group { Id = id, Owner = owner, Seed = seed, Head = -1, SplitFrom = splitFrom });
            if (splitFrom == 0)
                GroupById.TryAdd(id, index);
            return index;
        }

        private static bool Compatible(Member a, Member b, float radius, float speed)
        {
            return math.distancesq(a.Position, b.Position) <= radius * radius &&
                   math.distancesq(a.Velocity, b.Velocity) <= speed * speed;
        }

        private static uint MemberSignature(long id) => math.hash(new int2((int)id, (int)(id >> 32)));

        internal static int2 GetCell(int slot)
        {
            if (slot == 0)
                return int2.zero;

            // Complete concentric squares. The first nine slots fill a centered 3×3 grid.
            int ring = (int)math.ceil((math.sqrt(slot + 1f) - 1f) * 0.5f);
            int innerSide = 2 * ring - 1;
            int remaining = slot - innerSide * innerSide;
            int edge = 2 * ring;
            int side = remaining / edge;
            int offset = remaining % edge;
            return side switch
            {
                0 => new int2(-ring + offset, ring),
                1 => new int2(ring, ring - offset),
                2 => new int2(ring - offset, -ring),
                _ => new int2(-ring, -ring + offset)
            };
        }

        internal struct Member
        {
            public long EntityId;
            public long GroupId;
            public long JoinTarget;
            public int Owner;
            public int Slot;
            public int Next;
            public int PriorCount;
            public uint PriorSignature;
            public int2 Cell;
            public float2 Position;
            public float2 Velocity;
            public float JoinAge;
            public float LeaveAge;
            public float SettledAge;
            public bool IsSeed;
        }

        internal struct Group
        {
            public long Id;
            public long SplitFrom;
            public int Owner;
            public int Seed;
            public int Head;
            public int Count;
            public int FirstVacancy;
            public uint MemberSignature;
            public float2 PositionSum;
            public float2 VelocitySum;
        }

        internal struct GroupOrder
        {
            public int Index;
            public long Id;
            public int Count;
        }

        internal struct BatchState
        {
            public long NextGroupId;
            public int CandidateCount;
        }

        internal readonly struct Bucket : IEquatable<Bucket>
        {
            public readonly int Owner;
            public readonly int2 Cell;
            public readonly int SpeedBand;

            public Bucket(int owner, int2 cell, int speedBand)
            {
                Owner = owner;
                Cell = cell;
                SpeedBand = speedBand;
            }

            public bool Equals(Bucket other) => Owner == other.Owner && math.all(Cell == other.Cell) &&
                                               SpeedBand == other.SpeedBand;
            public override int GetHashCode() => (int)math.hash(new int4(Cell, SpeedBand, Owner));
        }

        internal readonly struct OccupiedCell : IEquatable<OccupiedCell>
        {
            private readonly long _group;
            private readonly int _slot;

            public OccupiedCell(long group, int slot)
            {
                _group = group;
                _slot = slot;
            }

            public bool Equals(OccupiedCell other) => _group == other._group && _slot == other._slot;
            public override int GetHashCode() => (int)math.hash(new int3((int)_group, (int)(_group >> 32), _slot));
        }

        private struct MemberComparer : IComparer<Member>
        {
            public int Compare(Member left, Member right) => left.EntityId.CompareTo(right.EntityId);
        }

        private struct GroupComparer : IComparer<GroupOrder>
        {
            public int Compare(GroupOrder left, GroupOrder right)
            {
                int count = right.Count.CompareTo(left.Count);
                return count != 0 ? count : left.Id.CompareTo(right.Id);
            }
        }
    }
}
