using System.Collections.Generic;
using EmpireAtWar.Services.ShipSpawning;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Round 5, E1b: the fixed forward ring around own units.</summary>
    [Category("Ships")]
    public sealed class ForwardSpawnRingTests
    {
        private static readonly Vector3 UNIT = new Vector3(1000f, 0f, 500f);

        [Test]
        public void FirstPoint_IsStraightAwayFromTheEnemy()
        {
            Vector3 enemy = UNIT + new Vector3(0f, 0f, 2000f);

            Assert.That(ForwardSpawnRing.TryFind(new[] { UNIT }, enemy, _ => true, _ => true,
                out Vector3 position, out ForwardSpawnAttempt attempt), Is.True);

            Vector3 offset = position - UNIT;
            Assert.That(offset.magnitude, Is.EqualTo(ForwardSpawnRing.RADIUS).Within(0.01f));
            Assert.That(Vector3.Dot(offset.normalized, (UNIT - enemy).normalized), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(attempt.UnitIndex, Is.Zero);
            Assert.That(attempt.Points, Is.EqualTo(1));
        }

        [Test]
        public void UnseenAndBlockedPoints_AreSkippedAndCounted()
        {
            Vector3 enemy = UNIT + new Vector3(0f, 0f, 2000f);
            // The point straight away is not open, the next one has no room for the hull.
            int open = 0;
            int clear = 0;

            Assert.That(ForwardSpawnRing.TryFind(new[] { UNIT }, enemy, _ => open++ > 0, _ => clear++ > 0,
                out Vector3 position, out ForwardSpawnAttempt attempt), Is.True);

            Assert.That(attempt.NotOpen, Is.EqualTo(1));
            Assert.That(attempt.NotClear, Is.EqualTo(1));
            Assert.That(attempt.Points, Is.EqualTo(3));
            Assert.That(Vector3.Dot((position - UNIT).normalized, (UNIT - enemy).normalized), Is.LessThan(0.99f));
        }

        [Test]
        public void HullTest_RunsOnlyOnOpenPoints()
        {
            int hullTests = 0;

            ForwardSpawnRing.TryFind(new[] { UNIT }, Vector3.zero, _ => false, _ => { hullTests++; return true; },
                out _, out _);

            Assert.That(hullTests, Is.Zero);
        }

        [Test]
        public void SecondUnit_IsTriedWhenTheFirstRingIsClosed()
        {
            Vector3 second = new Vector3(-2000f, 0f, 0f);

            Assert.That(ForwardSpawnRing.TryFind(new[] { UNIT, second }, Vector3.zero,
                point => (point - second).magnitude < 300f, _ => true, out _, out ForwardSpawnAttempt attempt),
                Is.True);

            Assert.That(attempt.UnitIndex, Is.EqualTo(1));
            Assert.That(attempt.NotOpen, Is.EqualTo(ForwardSpawnRing.POINTS));
        }

        [Test]
        public void AllPointsFail_ReturnsFalseAfterAtMostThreeUnits()
        {
            var units = new List<Vector3> { UNIT, UNIT * 2f, UNIT * 3f, UNIT * 4f };

            Assert.That(ForwardSpawnRing.TryFind(units, Vector3.zero, _ => false, _ => true,
                out _, out ForwardSpawnAttempt attempt), Is.False);

            Assert.That(attempt.UnitIndex, Is.EqualTo(-1));
            Assert.That(attempt.Points, Is.EqualTo(ForwardSpawnRing.MAX_UNITS * ForwardSpawnRing.POINTS));
        }
    }
}
