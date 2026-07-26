using EmpireAtWar.Components.Ship.Movement;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Movement
{
    public sealed class ShipRotationKinematicsTests
    {
        [Test]
        public void Step_NeverExceedsConfiguredAngularSpeed()
        {
            Quaternion result = ShipRotationKinematics.Step(
                Quaternion.identity,
                Vector3.back,
                15f,
                1f);

            Assert.That(
                Quaternion.Angle(Quaternion.identity, result),
                Is.EqualTo(15f).Within(0.01f));
            Assert.That(
                Quaternion.Angle(result, Quaternion.LookRotation(Vector3.back)),
                Is.EqualTo(165f).Within(0.01f));
        }

        [Test]
        public void CalculateTurnDuration_UsesDegreesPerSecond()
        {
            float duration = ShipRotationKinematics.CalculateTurnDuration(
                Quaternion.identity,
                Vector3.back,
                15f);

            Assert.That(duration, Is.EqualTo(12f).Within(0.01f));
        }
    }
}
