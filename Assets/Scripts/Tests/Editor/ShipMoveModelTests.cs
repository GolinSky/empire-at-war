using EmpireAtWar.Components.Ship.Movement;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipMoveModelTests
    {
        [Test]
        public void ConfigureSpawnPose_HyperSpaceEntryFollowsStartRotation()
        {
            ShipMoveModel model = new ShipMoveModel();
            Vector3 destination = new Vector3(12f, 5f, -7f);
            Quaternion rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);

            model.ConfigureSpawnPose(destination, rotation, true);

            Vector3 jumpDirection = destination - model.JumpPosition;
            Assert.That(
                Vector3.Angle(jumpDirection, rotation * Vector3.forward),
                Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(model.StartRotation, rotation), Is.LessThan(0.001f));
        }
    }
}
