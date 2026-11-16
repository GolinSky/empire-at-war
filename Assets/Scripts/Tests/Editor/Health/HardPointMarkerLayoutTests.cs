using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class HardPointMarkerLayoutTests
    {
        [TestCase(1f)]
        [TestCase(0.6f)]
        [TestCase(2f)]
        public void CoincidentMarkers_EachHasAnIndependentClickTarget(float scale)
        {
            List<Vector2> positions = new List<Vector2>();
            for (int index = 0; index < 16; index++) positions.Add(new Vector2(500f, 400f));
            float spacing = 60f * scale;
            HardPointMarkerLayout.Separate(positions, spacing);

            for (int index = 0; index < positions.Count; index++)
            {
                Assert.That(HardPointMarkerLayout.FindClosest(positions, positions[index], spacing * 0.5f),
                    Is.EqualTo(index), "Every displayed icon must select its own hardpoint.");
                for (int other = 0; other < index; other++)
                {
                    Vector2 delta = positions[index] - positions[other];
                    Assert.That(Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)),
                        Is.GreaterThanOrEqualTo(55f * scale), "Target rings must not overlap.");
                }
            }
        }

        [Test]
        public void SeparatedMarkers_KeepTheirWorldProjectedPositions()
        {
            List<Vector2> positions = new List<Vector2> { Vector2.zero, new Vector2(100f, 0f) };
            Vector2[] original = positions.ToArray();
            HardPointMarkerLayout.Separate(positions, 60f);
            Assert.That(positions, Is.EqualTo(original));
            Assert.That(HardPointMarkerLayout.FindClosest(positions, new Vector2(500f, 500f), 30f), Is.EqualTo(-1));
        }

        [Test]
        public void Layout_IsStableAcrossFrames_AndDoesNotReorderHardpoints()
        {
            List<Vector2> first = new List<Vector2>
                { Vector2.zero, new Vector2(5f, 5f), Vector2.zero, new Vector2(80f, -5f) };
            List<Vector2> nextFrame = new List<Vector2>(first);
            HardPointMarkerLayout.Separate(first, 60f);
            HardPointMarkerLayout.Separate(nextFrame, 60f);
            Assert.That(nextFrame, Is.EqualTo(first));
            for (int index = 0; index < first.Count; index++)
                Assert.That(HardPointMarkerLayout.FindClosest(first, first[index], 30f), Is.EqualTo(index));
        }

        [Test]
        public void Picking_UsesNearestCenterInSharedHitArea()
        {
            List<Vector2> positions = new List<Vector2> { Vector2.zero, new Vector2(20f, 0f) };
            Assert.That(HardPointMarkerLayout.FindClosest(positions, new Vector2(1f, 0f), 30f), Is.Zero);
            Assert.That(HardPointMarkerLayout.FindClosest(positions, new Vector2(19f, 0f), 30f), Is.EqualTo(1));
        }
    }
}
