using EmpireAtWar.Services.Radar;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class RadarDetectionParityTests
    {
        private const int SLOT_COUNT = 4;
        private const int QUERY_COUNT = 8;
        private const int BITS_PER_WORD = 64;

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(63)]
        [TestCase(64)]
        [TestCase(65)]
        [TestCase(95)]
        [TestCase(96)]
        [TestCase(127)]
        [TestCase(128)]
        public void Detection_PreservesSingleEligibleBit(int targetIndex)
        {
            int count = targetIndex + 1;
            int words = (count + BITS_PER_WORD - 1) / BITS_PER_WORD;
            ulong[] eligibleBits = new ulong[words];
            eligibleBits[targetIndex / BITS_PER_WORD] = 1ul << (targetIndex % BITS_PER_WORD);
            using NativeArray<RadarQuery> queries = new NativeArray<RadarQuery>(
                new[] { new RadarQuery(float2.zero, 100f, 0) }, Allocator.TempJob);
            using NativeArray<float> x = new NativeArray<float>(count, Allocator.TempJob);
            using NativeArray<float> z = new NativeArray<float>(count, Allocator.TempJob);
            using NativeArray<ulong> eligible = new NativeArray<ulong>(eligibleBits, Allocator.TempJob);
            using NativeArray<ulong> actual = new NativeArray<ulong>(words, Allocator.TempJob);

            new RadarDetectionJob
            {
                Queries = queries, PositionX = x, PositionZ = z,
                EligibleBits = eligible, DetectedBits = actual, WordsPerQuery = words
            }.Schedule(1, 1).Complete();

            for (int word = 0; word < words; word++)
                Assert.That(actual[word], Is.EqualTo(eligible[word]), $"result word {word}");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(63)]
        [TestCase(64)]
        [TestCase(65)]
        [TestCase(129)]
        [TestCase(257)]
        public void EligibilityAndPositions_MatchLegacyDetection(int count)
        {
            int words = (count + BITS_PER_WORD - 1) / BITS_PER_WORD;
            RadarTarget[] states = new RadarTarget[count];
            float3[] positions = new float3[count];
            float[] x = new float[count];
            float[] z = new float[count];
            byte[] hostility = new byte[SLOT_COUNT * SLOT_COUNT];
            RadarQuery[] queries = new RadarQuery[QUERY_COUNT];
            System.Random random = new System.Random(642);
            for (int owner = 0; owner < SLOT_COUNT; owner++)
            {
                // Deliberately asymmetric: eligibility must use the querying owner's row.
                for (int other = 0; other < SLOT_COUNT; other++)
                    hostility[owner * SLOT_COUNT + other] = (byte)((owner + 1) % SLOT_COUNT == other ? 1 : 0);
            }
            for (int i = 0; i < count; i++)
            {
                states[i] = new RadarTarget(i % SLOT_COUNT, i % 5 != 0);
                x[i] = (float)(random.NextDouble() * 20 - 10);
                z[i] = (float)(random.NextDouble() * 20 - 10);
                if (i % 17 == 0) x[i] = float.NaN;
                if (i % 31 == 0) z[i] = float.PositiveInfinity;
                positions[i] = new float3(x[i], 1000f, z[i]);
            }
            if (count > 0)
            {
                states[0] = new RadarTarget(1, true);
                positions[0] = new float3(x[0] = 3f, 1000f, z[0] = 4f);
            }
            for (int i = 0; i < QUERY_COUNT; i++)
                queries[i] = new RadarQuery(i < SLOT_COUNT ? float2.zero : new float2(2f, -3f),
                    i < SLOT_COUNT ? 25f : 100f, i % SLOT_COUNT);

            using NativeArray<RadarTarget> targetData = new NativeArray<RadarTarget>(states, Allocator.TempJob);
            using NativeArray<float3> legacyPositions = new NativeArray<float3>(positions, Allocator.TempJob);
            using NativeArray<float> positionX = new NativeArray<float>(x, Allocator.TempJob);
            using NativeArray<float> positionZ = new NativeArray<float>(z, Allocator.TempJob);
            using NativeArray<byte> hostilityData = new NativeArray<byte>(hostility, Allocator.TempJob);
            using NativeArray<RadarQuery> queryData = new NativeArray<RadarQuery>(queries, Allocator.TempJob);
            using NativeArray<ulong> eligible = new NativeArray<ulong>(SLOT_COUNT * words, Allocator.TempJob);
            using NativeArray<ulong> actual = new NativeArray<ulong>(QUERY_COUNT * words, Allocator.TempJob);
            using NativeArray<ulong> expected = new NativeArray<ulong>(QUERY_COUNT * words, Allocator.TempJob);

            RadarScanService.BuildEligibility(targetData, hostilityData, SLOT_COUNT, words, eligible);
            for (int owner = 0; owner < SLOT_COUNT; owner++)
            {
                for (int word = 0; word < words; word++)
                {
                    ulong bits = 0;
                    for (int bit = 0; bit < BITS_PER_WORD; bit++)
                    {
                        int i = word * BITS_PER_WORD + bit;
                        if (i < count && states[i].IsDetectable && hostility[owner * SLOT_COUNT + states[i].OwnerSlot] != 0)
                            bits |= 1ul << bit;
                    }
                    Assert.That(eligible[owner * words + word], Is.EqualTo(bits), $"owner {owner}, word {word}");
                }
            }
            new LegacyRadarDetectionJob
            {
                Queries = queryData, Targets = targetData, Positions = legacyPositions,
                Hostility = hostilityData, DetectedBits = expected, WordsPerQuery = words
            }.Schedule(QUERY_COUNT, 4).Complete();
            new RadarDetectionJob
            {
                Queries = queryData, PositionX = positionX, PositionZ = positionZ,
                EligibleBits = eligible, DetectedBits = actual, WordsPerQuery = words
            }.Schedule(QUERY_COUNT, 4).Complete();
            for (int i = 0; i < actual.Length; i++)
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"result word {i}");

            // Reusing the same allocation must clear stale eligibility, including its final partial word.
            hostilityData.CopyFrom(new byte[hostility.Length]);
            RadarScanService.BuildEligibility(targetData, hostilityData, SLOT_COUNT, words, eligible);
            for (int i = 0; i < eligible.Length; i++) Assert.That(eligible[i], Is.Zero);
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct LegacyRadarDetectionJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<RadarQuery> Queries;
            [ReadOnly] public NativeArray<RadarTarget> Targets;
            [ReadOnly] public NativeArray<float3> Positions;
            [ReadOnly] public NativeArray<byte> Hostility;
            // The legacy kernel owns one disjoint result slice per query, as production does.
            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<ulong> DetectedBits;
            public int WordsPerQuery;

            public void Execute(int index)
            {
                RadarQuery query = Queries[index];
                int hostilityRow = query.OwnerSlot * SLOT_COUNT;
                int resultOffset = index * WordsPerQuery;
                for (int word = 0; word < WordsPerQuery; word++)
                {
                    ulong bits = 0;
                    int first = word * BITS_PER_WORD;
                    int last = math.min(first + BITS_PER_WORD, Targets.Length);
                    for (int i = first; i < last; i++)
                    {
                        RadarTarget target = Targets[i];
                        if (!target.IsDetectable || Hostility[hostilityRow + target.OwnerSlot] == 0) continue;
                        if (math.lengthsq(Positions[i].xz - query.Center) <= query.RangeSquared)
                            bits |= 1ul << (i - first);
                    }
                    DetectedBits[resultOffset + word] = bits;
                }
            }
        }
    }
}
