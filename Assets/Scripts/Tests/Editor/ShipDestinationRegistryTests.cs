using System;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Movement
{
    public sealed class ShipDestinationRegistryTests
    {
        [Test]
        public void PendingFinalPosition_RetainsActiveFinalPosition()
        {
            ShipDestinationRegistry registry = new ShipDestinationRegistry();
            int firstShip = registry.Register(
                () => new FormationPoint(-50f, 0f),
                5f,
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
        public void Register_RejectsIncomingShipAtOccupiedArrivalPosition()
        {
            ShipDestinationRegistry registry = new ShipDestinationRegistry();
            registry.Register(
                () => new FormationPoint(-50f, 0f),
                5f,
                new FormationPoint(0f, 0f));

            Assert.That(
                () => registry.Register(
                    () => new FormationPoint(50f, 0f),
                    5f,
                    new FormationPoint(0f, 0f)),
                Throws.InvalidOperationException);
        }
    }
}
