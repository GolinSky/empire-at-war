using EmpireAtWar.Services.ShipSpawning;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
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

        private static ShipHullFootprint At(Vector3 center, Quaternion rotation)
        {
            return new ShipHullFootprint(center, HALF_EXTENTS, rotation);
        }
    }
}
