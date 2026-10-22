using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>Verbatim copy of the production <c>RadarDetectionJob</c> loop: <c>if</c> + short-circuit <c>&amp;&amp;</c>.</summary>
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    internal struct RadarDetectionIfJob : IJobParallelFor
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
