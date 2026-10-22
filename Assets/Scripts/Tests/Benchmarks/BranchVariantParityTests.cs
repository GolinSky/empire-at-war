using NUnit.Framework;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>Every variant must return the plain C# reference result before its timing means anything.</summary>
    public abstract class BranchVariantParityTests
    {
        private const uint SEED = 0x51A7u;

        [Test]
        public void RadarDetection_MatchesReference([Values] BranchVariant variant,
            [Values(0.05f, 0.5f, 0.95f)] float detectableRatio)
        {
            // 300 targets → five words per query, the last one partial.
            using RadarBenchmarkData data = new RadarBenchmarkData(37, 300, detectableRatio, SEED);
            data.Run(variant);

            Assert.That(data.DetectedBits.ToArray(), Is.EqualTo(data.ComputeReference()));
        }

        [Test]
        public void TargetFilter_MatchesReference([Values] BranchVariant variant,
            [Values(0.05f, 0.5f, 0.95f)] float eligibleRatio)
        {
            using TargetFilterBenchmarkData data = new TargetFilterBenchmarkData(41, 57, 128, eligibleRatio, SEED);
            data.Run(variant);

            int2[] expected = data.ComputeReference();
            Assert.That(data.Results.ToArray(), Is.EqualTo(expected));
        }
    }
}
