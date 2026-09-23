using System;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Combat;
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
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub(), new CombatModifiers());
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
        public void Request_DeduplicatesWithinPositionTolerance()
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub(), new CombatModifiers());
            model.Request(new NumericsVector3(10f, 0f, 0f));

            Assert.That(model.IsSameRequest(new NumericsVector3(9.96f, 0f, 0f)), Is.True);
            Assert.That(model.IsSameRequest(new NumericsVector3(9.94f, 0f, 0f)), Is.False);
        }

        [Test]
        public void Arrival_ExecutesQueuedDestination()
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub(), new CombatModifiers());
            NumericsVector3 destination = new NumericsVector3(10f, 5f, 0f);

            model.Request(destination);
            Assert.That(model.Phase, Is.EqualTo(MovementPhase.Arriving));
            Assert.That(model.FinishArrival(), Is.EqualTo(destination));
            model.Accept(destination);

            Assert.That(model.Phase, Is.EqualTo(MovementPhase.Moving));
            Assert.That(model.IsMoving, Is.True);
        }

        [Test]
        public void BlockedDestination_CanBeRetriedAfterRadarUpdate()
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub(), new CombatModifiers());
            NumericsVector3 destination = new NumericsVector3(10f, 5f, 0f);
            model.FinishArrival();
            model.Block(destination);

            Assert.That(model.IsBlocked, Is.True);
            Assert.That(model.TakePending(), Is.EqualTo(destination));
            model.Accept(destination);
            Assert.That(model.Phase, Is.EqualTo(MovementPhase.Moving));
        }

        [TestCase(MovementPhase.Arriving)]
        [TestCase(MovementPhase.Idle)]
        [TestCase(MovementPhase.Moving)]
        [TestCase(MovementPhase.Blocked)]
        public void StopAt_ClearsPendingOrders(MovementPhase phase)
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub(), new CombatModifiers());
            NumericsVector3 destination = new NumericsVector3(10f, 5f, 0f);
            if (phase != MovementPhase.Arriving)
            {
                model.FinishArrival();
            }

            if (phase == MovementPhase.Moving)
            {
                model.Accept(destination);
            }
            else if (phase == MovementPhase.Blocked)
            {
                model.Block(destination);
            }

            model.Request(destination);
            model.StopAt(NumericsVector3.Zero);

            Assert.That(model.Phase, Is.EqualTo(phase == MovementPhase.Arriving
                ? MovementPhase.Arriving : MovementPhase.Idle));
            Assert.That(model.PendingDestination, Is.Null);
            Assert.That(model.LastRequest, Is.Null);
        }

        [Test]
        public void PursueDuringMovement_DefersUntilPathCompletes()
        {
            ShipMoveModel model = new ShipMoveModel(new ShipMoveDataStub(), new CombatModifiers());
            model.FinishArrival();
            model.Accept(new NumericsVector3(10f, 5f, 0f));
            NumericsVector3 pursuit = new NumericsVector3(20f, 5f, 0f);

            model.Defer(pursuit);

            Assert.That(model.IsMoving, Is.True);
            Assert.That(model.TakePending(), Is.EqualTo(pursuit));
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
