using System.Reflection;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;

namespace EmpireAtWar.Tests.Movement
{
    [Category("Navigation")]
    public sealed class ShipSpacingCoordinatorTests
    {
        [TestCase("revision")]
        [TestCase("target")]
        [TestCase("stopped")]
        [TestCase("heading")]
        [TestCase("own-position")]
        [TestCase("blocker-position")]
        [TestCase("blocker-moving")]
        [TestCase("blocker-braking")]
        [TestCase("unregistered")]
        [TestCase("reused-registration")]
        [TestCase("reused-blocker")]
        public void PendingSnapshot_RejectsStaleOrIneligibleWork(string change)
        {
            using var coordinator = new ShipSpacingCoordinator(new ShipDestinationRegistry());
            var blocker = new Agent { CanSettle = false };
            var ship = new Agent { NavigationPosition = Vector3.right * 2f };
            var replacement = new Agent { NavigationPosition = ship.NavigationPosition };
            coordinator.Register(1, blocker);
            coordinator.Register(2, ship);
            coordinator.Tick(0f);
            switch (change)
            {
                case "revision": ship.SpacingRevision++; break;
                case "target": ship.SpacingTargetPosition = Vector3.forward * 10f; break;
                case "stopped": ship.CanSettle = false; break;
                case "heading": ship.SpacingForward = Vector3.right; break;
                case "own-position": ship.NavigationPosition += Vector3.forward * 2f; break;
                case "blocker-position": blocker.NavigationPosition += Vector3.forward * 2f; break;
                case "blocker-moving": blocker.IsMoving = true; break;
                case "blocker-braking": blocker.NavigationCurrentSpeed = 1f; break;
                case "unregistered": coordinator.Unregister(2); break;
                case "reused-registration": coordinator.Unregister(2); coordinator.Register(2, replacement); break;
                case "reused-blocker": coordinator.Unregister(1); coordinator.Register(1, replacement); break;
            }
            CompleteSnapshot(coordinator, 0f);

            Assert.That(coordinator.TryCorrect(0f), Is.False);
            Assert.That(ship.Attempts, Is.Zero);
            Assert.That(replacement.Attempts, Is.Zero);
        }

        [TestCase(true, 0f)]
        [TestCase(false, 1f)]
        public void PassingShip_DoesNotDisplaceHolderUntilStopped(bool moving, float speed)
        {
            using var coordinator = new ShipSpacingCoordinator(new ShipDestinationRegistry());
            var blocker = new Agent { CanSettle = false, IsMoving = moving, NavigationCurrentSpeed = speed };
            var ship = new Agent { NavigationPosition = Vector3.right * 2f };
            coordinator.Register(1, blocker);
            coordinator.Register(2, ship);

            Snapshot(coordinator, 0f);
            Assert.That(coordinator.TryCorrect(0f), Is.False);
            Assert.That(ship.Attempts, Is.Zero);

            blocker.IsMoving = false;
            blocker.NavigationCurrentSpeed = 0f;
            Snapshot(coordinator, 1f);
            Assert.That(coordinator.TryCorrect(1f), Is.True);
            Assert.That(ship.Attempts, Is.EqualTo(1));
        }

        [Test]
        public void Corrections_RespectCooldownWithoutDependingOnMachineSpeed()
        {
            using var coordinator = new ShipSpacingCoordinator(new ShipDestinationRegistry());
            var ship = new Agent { NavigationPosition = Vector3.right * 2f };
            coordinator.Register(1, new Agent { CanSettle = false });
            coordinator.Register(2, ship);

            Snapshot(coordinator, 0f);
            Assert.That(coordinator.TryCorrect(0f), Is.True);
            Snapshot(coordinator, 0.25f);
            Assert.That(coordinator.TryCorrect(0.25f), Is.False);
            Snapshot(coordinator, 1.1f);
            Assert.That(coordinator.TryCorrect(1.1f), Is.True);
            Assert.That(ship.Attempts, Is.EqualTo(2));
        }

        [Test]
        public void FailedRoute_WaitsForPairTargetOrNearbyOccupancyChange()
        {
            using var coordinator = new ShipSpacingCoordinator(new ShipDestinationRegistry());
            var ship = new Agent { NavigationPosition = Vector3.right * 2f, AcceptCorrection = false };
            var nearby = new Agent { NavigationPosition = Vector3.right * 8f, CanSettle = false };
            coordinator.Register(1, new Agent { CanSettle = false });
            coordinator.Register(2, ship);
            coordinator.Register(3, nearby);

            Snapshot(coordinator, 0f);
            Assert.That(coordinator.TryCorrect(0f), Is.True);
            Drain(coordinator, 0f);
            Snapshot(coordinator, 2f);
            Assert.That(coordinator.TryCorrect(2f), Is.False);
            Assert.That(ship.Attempts, Is.EqualTo(1));

            nearby.NavigationPosition += Vector3.forward;
            Snapshot(coordinator, 4f);
            Assert.That(coordinator.TryCorrect(4f), Is.True);
            Drain(coordinator, 4f);
            ship.SpacingTargetPosition += Vector3.forward * 10f;
            Snapshot(coordinator, 6f);
            Assert.That(coordinator.TryCorrect(6f), Is.True);
            Assert.That(ship.Attempts, Is.EqualTo(3));
        }

        [Test]
        public void FailedRoute_RetriesWhenFarShipsPendingReservationClearsWithoutMovement()
        {
            var registry = new ShipDestinationRegistry();
            using var coordinator = new ShipSpacingCoordinator(registry);
            var ship = new Agent { NavigationPosition = Vector3.right * 2f, AcceptCorrection = false };
            var far = new Agent { NavigationPosition = Vector3.right * 1000f, CanSettle = false };
            int farRegistration = registry.Register(
                () => new FormationPoint(far.NavigationPosition.x, far.NavigationPosition.z),
                far.NavigationRadius, far.NavigationHullSpan,
                new FormationPoint(1000f, 0f));
            registry.ReservePendingFinalPosition(farRegistration,
                new FormationPoint(12f, 0f));
            coordinator.Register(1, new Agent { CanSettle = false });
            coordinator.Register(2, ship);
            coordinator.Register(3, far);

            Snapshot(coordinator, 0f);
            Assert.That(coordinator.TryCorrect(0f), Is.True);
            Drain(coordinator, 0f);
            Snapshot(coordinator, 2f);
            Assert.That(coordinator.TryCorrect(2f), Is.False);
            Assert.That(ship.Attempts, Is.EqualTo(1));

            registry.CancelPendingFinalPosition(farRegistration);
            Snapshot(coordinator, 4f);

            Assert.That(coordinator.TryCorrect(4f), Is.True);
            Assert.That(ship.Attempts, Is.EqualTo(2));
            Assert.That(far.NavigationPosition, Is.EqualTo(Vector3.right * 1000f));
        }

        [Test]
        public void SaturatedFailedRoute_RetriesOnlyAfterNearbyOccupancyChanges()
        {
            using var coordinator = new ShipSpacingCoordinator(new ShipDestinationRegistry());
            var ship = new Agent { AcceptCorrection = false };
            coordinator.Register(1000, ship);
            for (int id = 1; id < 128; id++)
                coordinator.Register(id, new Agent { CanSettle = false });
            var lastBlocker = new Agent { CanSettle = false };
            coordinator.Register(128, lastBlocker);

            Snapshot(coordinator, 0f);
            Assert.That(coordinator.TryCorrect(0f), Is.True);
            Drain(coordinator, 0f);
            Snapshot(coordinator, 2f);
            Drain(coordinator, 2f);
            Snapshot(coordinator, 4f);
            Drain(coordinator, 4f);

            Assert.That(ship.Attempts, Is.EqualTo(1));
            Assert.That(coordinator.SaturatedQueries, Is.GreaterThan(0));

            lastBlocker.NavigationPosition += Vector3.forward;
            Snapshot(coordinator, 6f);
            Assert.That(coordinator.TryCorrect(6f), Is.True);
            Assert.That(ship.Attempts, Is.EqualTo(2));
        }

        [Test]
        public void FailedHigherRegistration_AnchorsAndLetsLowerRegistrationYield()
        {
            using var coordinator = new ShipSpacingCoordinator(new ShipDestinationRegistry());
            var low = new Agent();
            var high = new Agent { AcceptCorrection = false };
            coordinator.Register(1, low);
            coordinator.Register(2, high);

            Snapshot(coordinator, 0f);
            Assert.That(coordinator.TryCorrect(0f), Is.True);
            Assert.That(low.Attempts, Is.Zero);
            Assert.That(high.Attempts, Is.EqualTo(1));
            Snapshot(coordinator, 2f);
            Assert.That(coordinator.TryCorrect(2f), Is.True);
            Assert.That(low.Attempts, Is.EqualTo(1));
        }

        private static void Snapshot(ShipSpacingCoordinator coordinator, float now)
        {
            coordinator.Tick(now);
            CompleteSnapshot(coordinator, now);
        }

        private static void CompleteSnapshot(ShipSpacingCoordinator coordinator, float now)
        {
            // Synchronize the submitted job, never guess a completion deadline from machine speed.
            var batch = (ShipSpacingBatch)typeof(ShipSpacingCoordinator)
                .GetField("_batch", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(coordinator);
            var handle = (JobHandle)typeof(ShipSpacingBatch)
                .GetField("_handle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(batch);
            handle.Complete();
            coordinator.Tick(now);
        }

        private static void Drain(ShipSpacingCoordinator coordinator, float now)
        {
            // Each call consumes at most 32 of this fixture's at most 129 results.
            for (int i = 0; i < 5; i++) coordinator.TryCorrect(now);
        }

        private sealed class Agent : IShipSpacingAgent
        {
            public Vector3 NavigationPosition { get; set; }
            public float NavigationHeight => 0f;
            public ShipHullSpan NavigationHullSpan => new ShipHullSpan(-5f, 5f);
            public float NavigationRadius => 5f;
            public float NavigationSpeed => 10f;
            public float NavigationCurrentSpeed { get; set; }
            public float NavigationRotationSpeed => 30f;
            public bool IgnoresMapObstacles => false;
            public int SpacingRevision { get; set; }
            public bool CanSettle { get; set; } = true;
            public bool IsMoving { get; set; }
            // A distant target ahead: the sideways nudge stays within the settle turn limit.
            public Vector3 SpacingTargetPosition { get; set; } = Vector3.forward * 1000f;
            public Vector3 SpacingForward { get; set; } = Vector3.forward;
            public bool AcceptCorrection { get; set; } = true;
            public int Attempts { get; private set; }

            public bool TrySettle(Vector3 destination, int revision)
            {
                Attempts++;
                return AcceptCorrection;
            }
        }
    }
}
