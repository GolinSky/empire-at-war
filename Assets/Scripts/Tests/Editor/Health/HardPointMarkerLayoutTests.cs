using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Health")]
    public sealed class HardPointMarkerLayoutTests
    {
        [Test]
        public void Picking_DoesNotMoveProjectedAnchors()
        {
            List<Vector2> positions = new List<Vector2> { Vector2.zero, Vector2.zero, new Vector2(2f, 0f) };
            Vector2[] original = positions.ToArray();
            HardPointMarkerLayout.FindClosest(positions, Vector2.one, 28f);
            Assert.That(positions, Is.EqualTo(original));
            Assert.That(HardPointMarkerLayout.FindClosest(positions, new Vector2(500f, 500f), 28f), Is.EqualTo(-1));
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
