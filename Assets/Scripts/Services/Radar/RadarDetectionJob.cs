using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.Radar
{
    /// <summary>
    /// One radar per index. Writes a bitset over targets: bit i set → target i is a hostile,
    /// detectable entity whose planar centre lies within the radar range.
    /// </summary>
    [BurstCompile]
    internal struct RadarDetectionJob : IJobParallelFor
    {
        private const int BITS_PER_WORD = 64;

        [ReadOnly] public NativeArray<RadarQuery> Queries;
        [ReadOnly] public NativeArray<RadarTarget> Targets;
        [ReadOnly] public NativeArray<float3> Positions;
        // SlotCount × SlotCount; 1 → row owner is hostile to column owner.
        [ReadOnly] public NativeArray<byte> Hostility;

        // Each radar writes only its own WordsPerQuery slice.
        [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<ulong> DetectedBits;

        public int SlotCount;
        public int WordsPerQuery;

        public void Execute(int index)
        {
            RadarQuery query = Queries[index];
            int hostilityRow = query.OwnerSlot * SlotCount;
            int resultOffset = index * WordsPerQuery;
            for (int word = 0; word < WordsPerQuery; word++)
            {
                ulong bits = 0;
                int first = word * BITS_PER_WORD;
                int last = math.min(first + BITS_PER_WORD, Targets.Length);
                for (int i = first; i < last; i++)
                {
                    RadarTarget target = Targets[i];
                    if (!target.IsDetectable || Hostility[hostilityRow + target.OwnerSlot] == 0)
                    {
                        continue;
                    }

                    if (math.lengthsq(Positions[i].xz - query.Center) <= query.RangeSquared)
                    {
                        bits |= 1ul << (i - first);
                    }
                }

                DetectedBits[resultOffset + word] = bits;
            }
        }
    }
}
