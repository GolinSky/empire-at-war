using EmpireAtWar.Services.ShipSpawning;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class ShipHullFootprintTests
    {
        // A long, narrow hull: 20 wide, 10 tall, 100 long.
        private static readonly Vector3 HALF_EXTENTS = new Vector3(10f, 5f, 50f);

        [Test]
        public void Overlaps_ParallelHullsSideBySide_AreClearOnceBeamsSeparate()
        {
            ShipHullFootprint ship = new ShipHullFootprint(Vector3.zero, HALF_EXTENTS, Quaternion.identity);

            Assert.That(ship.Overlaps(At(new Vector3(19f, 0f, 0f), Quaternion.identity)), Is.True);
            Assert.That(ship.Overlaps(At(new Vector3(21f, 0f, 0f), Quaternion.identity)), Is.False);
        }

        [Test]
        public void Overlaps_HullsAtDifferentHeights_PassOverEachOther()
        {
            ShipHullFootprint ship = new ShipHullFootprint(Vector3.zero, HALF_EXTENTS, Quaternion.identity);

            Assert.That(ship.Overlaps(At(new Vector3(0f, 11f, 0f), Quaternion.identity)), Is.False);
        }

        [Test]
        public void Overlaps_RotatedHull_UsesItsTurnedFootprint()
        {
            ShipHullFootprint ship = new ShipHullFootprint(Vector3.zero, HALF_EXTENTS, Quaternion.identity);
            Quaternion crossing = Quaternion.Euler(0f, 90f, 0f);

            // Turned 90 degrees, the other hull reaches 50 along X instead of 10.
            Assert.That(ship.Overlaps(At(new Vector3(55f, 0f, 0f), crossing)), Is.True);
            Assert.That(ship.Overlaps(At(new Vector3(61f, 0f, 0f), crossing)), Is.False);
        }

        [TestCase(0f)]
        [TestCase(45f)]
        [TestCase(90f)]
        public void Bounds_ContainsEveryRotatedCorner(float yaw)
        {
            var hull = new ShipHullFootprint(new Vector3(7f, 11f, -13f), HALF_EXTENTS,
                Quaternion.Euler(0f, yaw, 0f));
            Bounds bounds = hull.Bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                var sign = new Vector3((corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                Vector3 point = hull.Center + hull.Rotation * Vector3.Scale(HALF_EXTENTS, sign);
                Assert.That(bounds.SqrDistance(point), Is.LessThan(0.0001f));
            }
            Assert.That(bounds.center, Is.EqualTo(hull.Center));
        }

        private static ShipHullFootprint At(Vector3 center, Quaternion rotation)
        {
            return new ShipHullFootprint(center, HALF_EXTENTS, rotation);
        }
    }
}
