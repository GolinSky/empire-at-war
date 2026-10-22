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
        [ReadOnly] public NativeArray<float> PositionX;
        [ReadOnly] public NativeArray<float> PositionZ;
        // OwnerSlot × WordsPerQuery; eligibility is shared by all radars of an owner.
        [ReadOnly] public NativeArray<ulong> EligibleBits;

        // Each radar writes only its own WordsPerQuery slice.
        [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<ulong> DetectedBits;

        public int WordsPerQuery;

        public void Execute(int index)
        {
            RadarQuery query = Queries[index];
            int eligibilityOffset = query.OwnerSlot * WordsPerQuery;
            int resultOffset = index * WordsPerQuery;
            for (int word = 0; word < WordsPerQuery; word++)
            {
                ulong eligible = EligibleBits[eligibilityOffset + word];
                ulong bits = 0;
                int first = word * BITS_PER_WORD;
                int last = math.min(first + BITS_PER_WORD, PositionX.Length);
                for (int i = first; i < last; i++)
                {
                    ulong bit = 1ul << (i - first);
                    if ((eligible & bit) == 0) continue;

                    float2 position = new float2(PositionX[i], PositionZ[i]);
                    if (math.lengthsq(position - query.Center) <= query.RangeSquared)
                    {
                        bits |= bit;
                    }
                }

                DetectedBits[resultOffset + word] = bits;
            }
        }
    }
}
