using EmpireAtWar.Services.Audio;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Audio")]
    public sealed class ShipAmbienceJobTests
    {
        [Test]
        public void Reduction_FindsClosestAudibleUnitInXZ()
        {
            Assert.That(Reduce(new[] { new float2(10f, 20f), new float2(13f, 24f) },
                new byte[] { 1, 1 }, new float2(12f, 24f)), Is.EqualTo(1f));
        }

        [Test]
        public void Reduction_ExcludesMaskedUnits()
        {
            Assert.That(Reduce(new[] { new float2(0f), new float2(3f, 4f) },
                new byte[] { 0, 1 }, new float2(0f)), Is.EqualTo(25f));
        }

        [Test]
        public void Reduction_EmptyOrFullyMaskedSetProducesSilence()
        {
            Assert.That(Reduce(new float2[0], new byte[0], new float2(0f)), Is.EqualTo(float.PositiveInfinity));
            Assert.That(Reduce(new[] { new float2(0f) }, new byte[] { 0 }, new float2(0f)), Is.EqualTo(float.PositiveInfinity));
        }

        private static float Reduce(float2[] values, byte[] mask, float2 focus)
        {
            using var positions = new NativeArray<float2>(values, Allocator.TempJob);
            using var audible = new NativeArray<byte>(mask, Allocator.TempJob);
            using var result = new NativeArray<float>(1, Allocator.TempJob);
            new ShipAmbienceDistanceJob
            {
                Positions = positions, Audible = audible, Count = values.Length,
                Focus = focus, MinimumDistanceSquared = result
            }.Schedule().Complete();
            return result[0];
        }
    }
}
