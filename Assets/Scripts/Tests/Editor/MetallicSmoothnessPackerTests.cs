using System;
using EmpireAtWar.Editor.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Rendering
{
    public sealed class MetallicSmoothnessPackerTests
    {
        [TestCase(0f, 255)]
        [TestCase(0.25f, 128)]
        [TestCase(0.5f, 75)]
        [TestCase(1f, 0)]
        public void Pack_UsesAutodeskSquareRootRoughness(float roughness, int expectedSmoothness)
        {
            Color32 packed = MetallicSmoothnessPacker.Pack(1f, roughness);

            Assert.That(packed.r, Is.EqualTo(255));
            Assert.That(packed.g, Is.Zero);
            Assert.That(packed.b, Is.Zero);
            Assert.That(packed.a, Is.EqualTo(expectedSmoothness));
        }

        [TestCase(false, false, 51, 51)]
        [TestCase(true, false, 204, 51)]
        [TestCase(false, true, 51, 128)]
        [TestCase(true, true, 204, 128)]
        public void Pack_SelectsMapsOrFallbacks(bool useMetallicMap, bool useRoughnessMap,
            int expectedMetallic, int expectedSmoothness)
        {
            Color[] metallic = { new Color(0.8f, 0.1f, 0.2f, 0.3f) };
            Color[] roughness = { new Color(0.25f, 1f, 1f, 1f) };

            Color32[] packed = MetallicSmoothnessPacker.Pack(metallic, roughness,
                0.2f, 0.64f, useMetallicMap, useRoughnessMap);

            Assert.That(packed, Has.Length.EqualTo(1));
            Assert.That(packed[0].r, Is.EqualTo(expectedMetallic));
            Assert.That(packed[0].a, Is.EqualTo(expectedSmoothness));
        }

        [Test]
        public void Pack_DisabledMapsNeedNoPixels()
        {
            Color32[] packed = MetallicSmoothnessPacker.Pack(null, null, 0f, 0.5f, false, false);

            Assert.That(packed, Has.Length.EqualTo(1));
            Assert.That(packed[0].r, Is.Zero);
            Assert.That(packed[0].a, Is.EqualTo(75));
        }

        [Test]
        public void Pack_UsesEveryPixelOfEnabledMap()
        {
            Color[] metallic = { Color.black, Color.white };

            Color32[] packed = MetallicSmoothnessPacker.Pack(metallic, null, 0.5f, 1f, true, false);

            Assert.That(packed, Has.Length.EqualTo(2));
            Assert.That(packed[0].r, Is.Zero);
            Assert.That(packed[1].r, Is.EqualTo(255));
            Assert.That(packed[1].a, Is.Zero);
        }

        [Test]
        public void Pack_RejectsDifferentEnabledMapSizes()
        {
            Assert.Throws<ArgumentException>(() => MetallicSmoothnessPacker.Pack(
                new Color[2], new Color[3], 0f, 0f, true, true));
        }
    }
}
