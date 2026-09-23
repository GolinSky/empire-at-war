using System;
using EmpireAtWar.Components.Ship.Movement;
using NUnit.Framework;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipMoveModelTests
    {
        [Test]
        public void ConfigureSpawnPose_HyperSpaceEntryFollowsStartRotation()
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub());
            NumericsVector3 destination = new NumericsVector3(12f, 5f, -7f);
            NumericsQuaternion rotation = NumericsQuaternion.CreateFromAxisAngle(
                NumericsVector3.UnitY,
                (float)(Math.PI / 2d));

            model.ConfigureSpawnPose(destination, rotation, true);

            NumericsVector3 jumpDirection = NumericsVector3.Normalize(destination - model.JumpPosition);
            NumericsVector3 forward = NumericsVector3.Normalize(
                NumericsVector3.Transform(NumericsVector3.UnitZ, rotation));
            Assert.That(NumericsVector3.Distance(jumpDirection, forward), Is.LessThan(0.001f));
            Assert.That(Math.Abs(NumericsQuaternion.Dot(model.StartRotation, rotation)),
                Is.GreaterThan(0.999f));
        }

        [Test]
        public void IsMoving_UsesPositionTolerance()
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub());
            model.SetTargetPosition(new NumericsVector3(10f, 0f, 0f));

            Assert.That(model.IsMoving(new NumericsVector3(9.96f, 0f, 0f)), Is.False);
            Assert.That(model.IsMoving(new NumericsVector3(9.94f, 0f, 0f)), Is.True);
        }

        [Test]
        public void Arrival_ExecutesQueuedDestination()
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub());
            NumericsVector3 destination = new NumericsVector3(10f, 5f, 0f);

            model.RequestDestination(destination);
            Assert.That(model.Phase, Is.EqualTo(MovementPhase.Arriving));
            Assert.That(model.FinishArrival(), Is.EqualTo(destination));
            model.AcceptDestination(destination, true);

            Assert.That(model.Phase, Is.EqualTo(MovementPhase.Turning));
            Assert.That(model.IsNavigating, Is.True);
        }

        [Test]
        public void BlockedDestination_CanBeRetriedAfterRadarUpdate()
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub());
            NumericsVector3 destination = new NumericsVector3(10f, 5f, 0f);
            model.FinishArrival();
            model.BlockDestination(destination);

            Assert.That(model.IsBlocked, Is.True);
            Assert.That(model.TakeBlockedDestination(), Is.EqualTo(destination));
            model.AcceptDestination(destination, false);
            Assert.That(model.Phase, Is.EqualTo(MovementPhase.Moving));
        }

        [TestCase(MovementPhase.Arriving)]
        [TestCase(MovementPhase.Idle)]
        [TestCase(MovementPhase.Turning)]
        [TestCase(MovementPhase.Moving)]
        [TestCase(MovementPhase.Blocked)]
        public void StopAt_ClearsPendingOrders(MovementPhase phase)
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub());
            NumericsVector3 destination = new NumericsVector3(10f, 5f, 0f);
            if (phase != MovementPhase.Arriving)
            {
                model.FinishArrival();
            }

            if (phase == MovementPhase.Turning || phase == MovementPhase.Moving)
            {
                model.AcceptDestination(destination, phase == MovementPhase.Turning);
            }
            else if (phase == MovementPhase.Blocked)
            {
                model.BlockDestination(destination);
            }

            model.RequestDestination(destination);
            model.StopAt(NumericsVector3.Zero);

            Assert.That(model.Phase, Is.EqualTo(MovementPhase.Idle));
            Assert.That(model.QueuedDestination, Is.Null);
            Assert.That(model.DeferredDestination, Is.Null);
            Assert.That(model.BlockedDestination, Is.Null);
        }

        private sealed class ShipMoveDataStub : IShipMoveData
        {
            public float Speed => 10f;
            public float Height => 5f;
            public float RotationSpeed => 2f;
            public float TurnAcceleration => 2f;
            public float HyperSpaceDuration => 1f;
            public float BodyRotationMaxAngle => 10f;
            public float NavigationRadius => 5f;
        }
    }
}
