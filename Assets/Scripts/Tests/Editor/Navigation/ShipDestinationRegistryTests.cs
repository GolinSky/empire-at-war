using System;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Movement
{
    [Category("Navigation")]
    public sealed class ShipDestinationRegistryTests
    {
        [Test]
        public void PendingFinalPosition_RetainsActiveFinalPosition()
        {
            ShipDestinationRegistry registry = new ShipDestinationRegistry();
            int firstShip = registry.Register(
                () => new FormationPoint(-50f, 0f),
                5f,
                ShipHullSpan.Unbounded,
                new FormationPoint(0f, 0f));
            registry.ReservePendingFinalPosition(
                firstShip,
                new FormationPoint(20f, 0f));

            Assert.That(
                registry.HasClearance(new FormationPoint(0f, 0f), 5f),
                Is.False);
            Assert.That(
                registry.HasClearance(new FormationPoint(20f, 0f), 5f),
                Is.False);
        }

        [Test]
        public void Stop_ReplacesFinalReservationWithCurrentPosition()
        {
            FormationPoint currentPosition = new FormationPoint(5f, 0f);
            ShipDestinationRegistry registry = new ShipDestinationRegistry();
            int ship = registry.Register(
                () => currentPosition,
                5f,
                ShipHullSpan.Unbounded,
                new FormationPoint(40f, 0f));

            registry.Stop(ship);

            Assert.That(
                registry.HasClearance(new FormationPoint(40f, 0f), 5f),
                Is.True);
            Assert.That(
                registry.HasClearance(currentPosition, 5f),
                Is.False);
        }

        [Test]
        public void CancelPendingFinalPosition_PreservesActiveFinalPosition()
        {
            ShipDestinationRegistry registry = new ShipDestinationRegistry();
            int ship = registry.Register(
                () => new FormationPoint(-50f, 0f),
                5f,
                ShipHullSpan.Unbounded,
                new FormationPoint(0f, 0f));
            registry.ReservePendingFinalPosition(
                ship,
                new FormationPoint(20f, 0f));

            registry.CancelPendingFinalPosition(ship);

            Assert.That(
                registry.HasClearance(new FormationPoint(0f, 0f), 5f),
                Is.False);
            Assert.That(
                registry.HasClearance(new FormationPoint(20f, 0f), 5f),
                Is.True);
        }

        [Test]
        public void Register_AllowsSpawnOverlappingNavigationRadius()
        {
            ShipDestinationRegistry registry = new ShipDestinationRegistry();
            registry.Register(
                () => new FormationPoint(-50f, 0f),
                5f,
                ShipHullSpan.Unbounded,
                new FormationPoint(0f, 0f));

            Assert.DoesNotThrow(() => registry.Register(
                () => new FormationPoint(50f, 0f),
                5f,
                ShipHullSpan.Unbounded,
                new FormationPoint(0f, 0f)));
        }

        [Test]
        public void Register_AllowsShipWhoseHullPassesOverOccupiedPosition()
        {
            ShipDestinationRegistry registry = new ShipDestinationRegistry();
            registry.Register(
                () => new FormationPoint(-50f, 0f),
                5f,
                new ShipHullSpan(-20f, -10f),
                new FormationPoint(0f, 0f));

            Assert.DoesNotThrow(() => registry.Register(
                () => new FormationPoint(50f, 0f),
                5f,
                new ShipHullSpan(-5f, 5f),
                new FormationPoint(0f, 0f)));
        }

        [Test]
        public void SpatialQueries_ReadCurrentPositionsOnceAndRestoreLiveQueriesAfterScope()
        {
            var registry = new ShipDestinationRegistry();
            var position = new FormationPoint(0f, 0f);
            int reads = 0;
            registry.Register(() => { reads++; return position; }, 2f,
                ShipHullSpan.Unbounded, new FormationPoint(-100f, 0f));
            reads = 0;

            registry.BeginSpatialQueries();
            try
            {
                Assert.That(registry.HasClearance(position, 2f), Is.False);
                Assert.That(registry.HasClearance(new FormationPoint(20f, 0f), 2f), Is.True);
                Assert.That(reads, Is.EqualTo(1), "Candidate checks must reuse one occupancy snapshot.");
            }
            finally
            {
                registry.EndSpatialQueries();
            }

            position = new FormationPoint(20f, 0f);
            Assert.That(registry.HasClearance(position, 2f), Is.False);
            Assert.That(registry.HasClearance(new FormationPoint(0f, 0f), 2f), Is.True);
        }

        [Test]
        public void SpatialQueries_TrackReservationAndRegistrationMutations()
        {
            var registry = new ShipDestinationRegistry();
            int ship = registry.Register(() => new FormationPoint(-100f, 0f), 2f,
                ShipHullSpan.Unbounded, new FormationPoint(0f, 0f));
            registry.ReservePendingFinalPosition(ship, new FormationPoint(20f, 0f));
            registry.BeginSpatialQueries();
            try
            {
                Assert.That(registry.HasClearance(new FormationPoint(0f, 0f), 2f), Is.False);
                Assert.That(registry.HasClearance(new FormationPoint(20f, 0f), 2f), Is.False);
                registry.CommitActiveFinalPosition(ship, new FormationPoint(40f, 0f));
                Assert.That(registry.HasClearance(new FormationPoint(0f, 0f), 2f), Is.True);
                Assert.That(registry.HasClearance(new FormationPoint(20f, 0f), 2f), Is.True);
                Assert.That(registry.HasClearance(new FormationPoint(40f, 0f), 2f), Is.False);
                registry.ReservePendingFinalPosition(ship, new FormationPoint(60f, 0f));
                Assert.That(registry.HasClearance(new FormationPoint(60f, 0f), 2f), Is.False);
                registry.CancelPendingFinalPosition(ship);
                Assert.That(registry.HasClearance(new FormationPoint(60f, 0f), 2f), Is.True);
                registry.Stop(ship);
                Assert.That(registry.HasClearance(new FormationPoint(40f, 0f), 2f), Is.True);
                Assert.That(registry.HasClearance(new FormationPoint(-100f, 0f), 2f), Is.False);
                registry.Unregister(ship);
                Assert.That(registry.HasClearance(new FormationPoint(-100f, 0f), 2f), Is.True);
                registry.Register(() => new FormationPoint(100f, 0f), 2f,
                    ShipHullSpan.Unbounded, new FormationPoint(100f, 0f));
                Assert.That(registry.HasClearance(new FormationPoint(100f, 0f), 2f), Is.False);
            }
            finally
            {
                registry.EndSpatialQueries();
            }
        }

        [Test]
        public void SpatialQueries_IncludeLargeRadiiAndRespectHullHeightAndSelfExclusion()
        {
            var registry = new ShipDestinationRegistry();
            int large = registry.Register(() => new FormationPoint(-199f, 0f), 100f,
                new ShipHullSpan(-5f, 5f), new FormationPoint(-199f, 0f));
            int high = registry.Register(() => new FormationPoint(500f, 0f), 1f,
                new ShipHullSpan(10f, 20f), new FormationPoint(500f, 0f));
            registry.BeginSpatialQueries();
            try
            {
                Assert.That(registry.HasClearance(new FormationPoint(-98f, 0f), 2f), Is.False);
                Assert.That(registry.HasClearance(new FormationPoint(0f, 0f), 110f), Is.False,
                    "A query radius larger than the largest indexed hull must expand cell coverage.");
                Assert.That(registry.HasClearance(high, new FormationPoint(-199f, 0f), 1f), Is.True);
                Assert.That(registry.HasClearance(large, new FormationPoint(-199f, 0f), 100f), Is.True);
            }
            finally
            {
                registry.EndSpatialQueries();
            }
        }

        [Test]
        public void SpatialQueries_DenseBucketSaturationIsConservativelyBlocked()
        {
            var registry = new ShipDestinationRegistry();
            for (int i = 0; i < 130; i++)
                registry.Register(() => new FormationPoint(0f, 0f), 1f,
                    new ShipHullSpan(-10f, -5f), new FormationPoint(0f, 0f));
            int high = registry.Register(() => new FormationPoint(100f, 0f), 1f,
                new ShipHullSpan(5f, 10f), new FormationPoint(100f, 0f));
            registry.BeginSpatialQueries();
            try
            {
                Assert.That(registry.HasClearance(high, new FormationPoint(0f, 0f), 1f), Is.False);
                Assert.That(registry.SpatialQuerySaturations, Is.EqualTo(1));
            }
            finally
            {
                registry.EndSpatialQueries();
            }
            Assert.That(registry.HasClearance(high, new FormationPoint(0f, 0f), 1f), Is.True,
                "Ordinary clearance queries retain their uncapped live semantics.");
        }

        [Test]
        public void ReservationVersion_ChangesOnlyWithReservationMutationsAndNeverReadsPositions()
        {
            var registry = new ShipDestinationRegistry();
            var position = new FormationPoint(0f, 0f);
            int reads = 0;
            int ship = registry.Register(() => { reads++; return position; }, 2f,
                ShipHullSpan.Unbounded, position);
            int registeredVersion = registry.ReservationVersion;
            reads = 0;
            position = new FormationPoint(10f, 0f);

            Assert.That(registry.ReservationVersion, Is.EqualTo(registeredVersion));
            Assert.That(reads, Is.Zero);
            registry.CommitActiveFinalPosition(ship, new FormationPoint(0f, 0f));
            registry.CancelPendingFinalPosition(ship);
            Assert.That(registry.ReservationVersion, Is.EqualTo(registeredVersion));

            registry.ReservePendingFinalPosition(ship, new FormationPoint(20f, 0f));
            Assert.That(registry.ReservationVersion, Is.EqualTo(registeredVersion + 1));
            registry.ReservePendingFinalPosition(ship, new FormationPoint(20f, 0f));
            Assert.That(registry.ReservationVersion, Is.EqualTo(registeredVersion + 1));
            registry.CancelPendingFinalPosition(ship);
            registry.CommitActiveFinalPosition(ship, new FormationPoint(30f, 0f));
            registry.Stop(ship);
            Assert.That(registry.ReservationVersion, Is.EqualTo(registeredVersion + 4));
            registry.Stop(ship);
            Assert.That(registry.ReservationVersion, Is.EqualTo(registeredVersion + 4));
            registry.Unregister(ship);
            Assert.That(registry.ReservationVersion, Is.EqualTo(registeredVersion + 5));
        }
    }
}
