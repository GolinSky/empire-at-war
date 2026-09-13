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

        private sealed class ShipMoveDataStub : IShipMoveData
        {
            public float Speed => 10f;
            public float Height => 5f;
            public float RotationSpeed => 2f;
            public float HyperSpaceDuration => 1f;
            public float BodyRotationMaxAngle => 10f;
            public float NavigationRadius => 5f;
        }
    }
}
