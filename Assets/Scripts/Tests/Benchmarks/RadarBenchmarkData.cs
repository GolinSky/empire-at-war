using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>Seeded radar scan input shared by every <see cref="BranchVariant"/> of the detection loop.</summary>
    internal sealed class RadarBenchmarkData : IDisposable
    {
        private const int SLOT_COUNT = 4;
        private const int BITS_PER_WORD = 64;
        private const float MAP_HALF_SIZE = 1000f;
        private const float RANGE = 400f;
        // Every Nth target has a destroyed transform (NaN position), as RadarScanService pre-fills.
        private const int DESTROYED_TARGET_STRIDE = 64;

        private NativeArray<RadarBenchmarkQuery> _queries;
        private NativeArray<RadarBenchmarkTarget> _targets;
        private NativeArray<float3> _positions;
        private NativeArray<byte> _hostility;
        private NativeArray<ulong> _detectedBits;
        private readonly int _wordsPerQuery;

        /// <param name="detectableRatio">Share of targets that are detectable; drives how predictable the first branch is.</param>
        public RadarBenchmarkData(int queryCount, int targetCount, float detectableRatio, uint seed)
        {
            Random random = new Random(seed);
            _queries = new NativeArray<RadarBenchmarkQuery>(queryCount, Allocator.Persistent);
            for (int i = 0; i < queryCount; i++)
            {
                _queries[i] = new RadarBenchmarkQuery(RandomPoint(ref random), RANGE * RANGE, i % SLOT_COUNT);
            }

            _targets = new NativeArray<RadarBenchmarkTarget>(targetCount, Allocator.Persistent);
            _positions = new NativeArray<float3>(targetCount, Allocator.Persistent);
            for (int i = 0; i < targetCount; i++)
            {
                _targets[i] = new RadarBenchmarkTarget(random.NextInt(SLOT_COUNT), random.NextFloat() < detectableRatio);
                float2 point = RandomPoint(ref random);
                _positions[i] = i % DESTROYED_TARGET_STRIDE == DESTROYED_TARGET_STRIDE - 1
                    ? new float3(float.NaN)
                    : new float3(point.x, random.NextFloat(-50f, 50f), point.y);
            }

            _hostility = new NativeArray<byte>(SLOT_COUNT * SLOT_COUNT, Allocator.Persistent);
            for (int row = 0; row < SLOT_COUNT; row++)
            for (int column = 0; column < SLOT_COUNT; column++)
            {
                _hostility[row * SLOT_COUNT + column] = (byte)(row != column ? 1 : 0);
            }

            _wordsPerQuery = (targetCount + BITS_PER_WORD - 1) / BITS_PER_WORD;
            _detectedBits = new NativeArray<ulong>(queryCount * _wordsPerQuery, Allocator.Persistent);
        }

        public NativeArray<ulong> DetectedBits => _detectedBits;

        /// <summary>Runs one variant on the calling thread, so timings compare kernels without scheduling noise.</summary>
        public void Run(BranchVariant variant)
        {
            switch (variant)
            {
                case BranchVariant.If:
                    new RadarDetectionIfJob
                    {
                        Queries = _queries,
                        Targets = _targets,
                        Positions = _positions,
                        Hostility = _hostility,
                        DetectedBits = _detectedBits,
                        SlotCount = SLOT_COUNT,
                        WordsPerQuery = _wordsPerQuery
                    }.Run(_queries.Length);
                    break;
                case BranchVariant.Ternary:
                    new RadarDetectionTernaryJob
                    {
                        Queries = _queries,
                        Targets = _targets,
                        Positions = _positions,
                        Hostility = _hostility,
                        DetectedBits = _detectedBits,
                        SlotCount = SLOT_COUNT,
                        WordsPerQuery = _wordsPerQuery
                    }.Run(_queries.Length);
                    break;
                case BranchVariant.Select:
                    new RadarDetectionSelectJob
                    {
                        Queries = _queries,
                        Targets = _targets,
                        Positions = _positions,
                        Hostility = _hostility,
                        DetectedBits = _detectedBits,
                        SlotCount = SLOT_COUNT,
                        WordsPerQuery = _wordsPerQuery
                    }.Run(_queries.Length);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(variant), variant, null);
            }
        }

        /// <summary>Plain C# detection over the same input, independent of every job variant.</summary>
        public ulong[] ComputeReference()
        {
            ulong[] bits = new ulong[_detectedBits.Length];
            for (int query = 0; query < _queries.Length; query++)
            for (int target = 0; target < _targets.Length; target++)
            {
                RadarBenchmarkTarget state = _targets[target];
                float2 delta = _positions[target].xz - _queries[query].Center;
                if (state.IsDetectable &&
                    _hostility[_queries[query].OwnerSlot * SLOT_COUNT + state.OwnerSlot] != 0 &&
                    math.dot(delta, delta) <= _queries[query].RangeSquared)
                {
                    bits[query * _wordsPerQuery + target / BITS_PER_WORD] |= 1ul << (target % BITS_PER_WORD);
                }
            }

            return bits;
        }

        public void Dispose()
        {
            _queries.Dispose();
            _targets.Dispose();
            _positions.Dispose();
            _hostility.Dispose();
            _detectedBits.Dispose();
        }

        private static float2 RandomPoint(ref Random random) =>
            random.NextFloat2(new float2(-MAP_HALF_SIZE), new float2(MAP_HALF_SIZE));
    }
}
