using NUnit.Framework;
using Unity.Burst;
using Unity.PerformanceTesting;

namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>
    /// Times <see cref="BranchVariant"/> kernels on one thread (Burst_Jobs_Optimization_Plan, Phase 1).
    /// Sizes follow the 2026-10-11 battle captures: ~200 target requests × ~170 candidates; radars × units.
    /// Editor timings are indicative; decisions use the Player run of the same fixture.
    /// </summary>
    public abstract class BranchVariantBenchmarks
    {
        private const uint SEED = 0xBE7Cu;
        private const int WARMUP_COUNT = 10;
        private const int MEASUREMENT_COUNT = 30;
        private const int ITERATIONS_PER_MEASUREMENT = 5;

        private const int RADAR_QUERIES = 128;
        private const int RADAR_TARGETS = 1024;

        private const int FILTER_REQUESTS = 256;
        private const int FILTER_CANDIDATES_PER_REQUEST = 192;
        private const int FILTER_SLOTS = 1024;

        [OneTimeSetUp]
        public void RequireBurst()
        {
            // Without Burst the jobs run as managed C#, which says nothing about Burst code generation.
            Assume.That(BurstCompiler.IsEnabled, Is.True, "Burst compilation is disabled.");
        }

        [Test, Performance]
        public void RadarDetection([Values] BranchVariant variant, [Values(0.05f, 0.5f, 0.95f)] float detectableRatio)
        {
            using RadarBenchmarkData data = new RadarBenchmarkData(RADAR_QUERIES, RADAR_TARGETS, detectableRatio, SEED);
            MeasureKernel(() => data.Run(variant));
        }

        [Test, Performance]
        public void TargetFilter([Values] BranchVariant variant, [Values(0.05f, 0.5f, 0.95f)] float eligibleRatio)
        {
            using TargetFilterBenchmarkData data = new TargetFilterBenchmarkData(
                FILTER_REQUESTS, FILTER_CANDIDATES_PER_REQUEST, FILTER_SLOTS, eligibleRatio, SEED);
            MeasureKernel(() => data.Run(variant));
        }

        private static void MeasureKernel(System.Action kernel)
        {
            Measure.Method(kernel)
                .WarmupCount(WARMUP_COUNT)
                .MeasurementCount(MEASUREMENT_COUNT)
                .IterationsPerMeasurement(ITERATIONS_PER_MEASUREMENT)
                .SampleGroup(new SampleGroup("Time", SampleUnit.Microsecond, false))
                .Run();
        }
    }
}
