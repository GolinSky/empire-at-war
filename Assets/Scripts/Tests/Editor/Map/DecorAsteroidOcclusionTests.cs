using System;
using EmpireAtWar.Components.Environment;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class DecorAsteroidOcclusionTests
    {
        [Test]
        public void EmptyAndIsolatedRocksRemainUnoccluded()
        {
            Assert.That(DecorAsteroidOcclusion.Calculate(Array.Empty<Bounds>()), Is.Empty);
            Bounds[] rocks = { Rock(Vector3.zero), Rock(new Vector3(100f, 0f, 0f)) };
            Assert.That(DecorAsteroidOcclusion.Calculate(rocks), Is.EqualTo(new[] { 1f, 1f }));
        }

        [Test]
        public void NearbyRocksDarkenEachOtherAcrossCellBoundary()
        {
            Bounds[] rocks = { Rock(new Vector3(-0.1f, 0f, 0f)), Rock(new Vector3(2f, 0f, 0f)) };
            float[] visibility = DecorAsteroidOcclusion.Calculate(rocks);
            Assert.That(visibility[0], Is.InRange(0.45f, 0.99f));
            Assert.That(visibility[1], Is.EqualTo(visibility[0]).Within(0.00001f));
            Bounds[] translated = { Rock(new Vector3(1.9f, 4f, 2f)), Rock(new Vector3(4f, 4f, 2f)) };
            Assert.That(DecorAsteroidOcclusion.Calculate(translated)[0],
                Is.EqualTo(visibility[0]).Within(0.00001f));
        }

        [Test]
        public void DenseOverlappingRocksHaveBoundedFiniteVisibility()
        {
            Bounds[] rocks = new Bounds[64];
            for (int i = 0; i < rocks.Length; i++) rocks[i] = Rock(Vector3.zero);
            foreach (float visibility in DecorAsteroidOcclusion.Calculate(rocks))
                Assert.That(visibility, Is.EqualTo(0.45f));
        }

        [Test]
        public void ReorderingRocksPreservesVisibility()
        {
            Bounds[] rocks =
            {
                Rock(new Vector3(-3f, 0f, 0f)), Rock(Vector3.zero),
                new Bounds(new Vector3(4f, 2f, 0f), Vector3.one * 4f),
                Rock(new Vector3(20f, 0f, 1f))
            };
            float[] expected = DecorAsteroidOcclusion.Calculate(rocks);
            Array.Reverse(rocks);
            float[] actual = DecorAsteroidOcclusion.Calculate(rocks);
            Array.Reverse(actual);
            Assert.That(actual, Is.EqualTo(expected).Within(0.00001f));
        }

        [Test]
        public void InfluenceFadesOutWithDistance()
        {
            float close = DecorAsteroidOcclusion.Calculate(new[] { Rock(Vector3.zero), Rock(Vector3.right * 4f) })[0];
            float far = DecorAsteroidOcclusion.Calculate(new[] { Rock(Vector3.zero), Rock(Vector3.right * 8f) })[0];
            float outside = DecorAsteroidOcclusion.Calculate(new[] { Rock(Vector3.zero), Rock(Vector3.right * 12f) })[0];
            Assert.That(close, Is.LessThan(far));
            Assert.That(far, Is.LessThan(1f));
            Assert.That(outside, Is.EqualTo(1f));
        }

        private static Bounds Rock(Vector3 center)
        {
            return new Bounds(center, Vector3.one * 2f);
        }
    }
}
