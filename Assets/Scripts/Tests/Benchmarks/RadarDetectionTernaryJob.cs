using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary><see cref="RadarDetectionIfJob"/> with every condition evaluated (<c>&amp;</c>) and the bit chosen by <c>?:</c>.</summary>
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    internal struct RadarDetectionTernaryJob : IJobParallelFor
    {
        private const int BITS_PER_WORD = 64;

        [ReadOnly] public NativeArray<RadarBenchmarkQuery> Queries;
        [ReadOnly] public NativeArray<RadarBenchmarkTarget> Targets;
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<byte> Hostility;

        // Each radar writes only its own WordsPerQuery slice.
        [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<ulong> DetectedBits;

        public int SlotCount;
        public int WordsPerQuery;

        public void Execute(int index)
        {
            RadarBenchmarkQuery query = Queries[index];
            int hostilityRow = query.OwnerSlot * SlotCount;
            int resultOffset = index * WordsPerQuery;
            for (int word = 0; word < WordsPerQuery; word++)
            {
                ulong bits = 0;
                int first = word * BITS_PER_WORD;
                int last = math.min(first + BITS_PER_WORD, Targets.Length);
                for (int i = first; i < last; i++)
                {
                    RadarBenchmarkTarget target = Targets[i];
                    bool hit = target.IsDetectable & Hostility[hostilityRow + target.OwnerSlot] != 0 &
                               math.lengthsq(Positions[i].xz - query.Center) <= query.RangeSquared;
                    bits |= hit ? 1ul << (i - first) : 0ul;
                }

                DetectedBits[resultOffset + word] = bits;
            }
        }
    }
}
