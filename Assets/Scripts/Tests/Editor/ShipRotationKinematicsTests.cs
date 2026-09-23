using EmpireAtWar.Components.Ship.Movement;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Movement
{
    public sealed class ShipRotationKinematicsTests
    {
        [Test]
        public void CalculateMinimumTurnRadius_UsesSpeedAndAngularRate()
        {
            float radius = ShipRotationKinematics.CalculateMinimumTurnRadius(
                10f,
                90f);

            Assert.That(radius, Is.EqualTo(10f / (Mathf.PI * 0.5f)).Within(0.001f));
        }

        [Test]
        public void StepYaw_AcceleratesWithoutExceedingMaxRate()
        {
            float angularVelocity = 0f;
            Quaternion result = ShipRotationKinematics.StepYaw(
                Quaternion.identity,
                Vector3.back,
                ref angularVelocity,
                15f,
                5f,
                1f);

            Assert.That(Mathf.Abs(angularVelocity), Is.EqualTo(5f).Within(0.01f));
            Assert.That(Quaternion.Angle(Quaternion.identity, result),
                Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void StepYaw_ReachesTargetWithoutOvershoot()
        {
            Quaternion rotation = Quaternion.identity;
            float angularVelocity = 0f;
            float previousError = 90f;
            for (int i = 0; i < 500; i++)
            {
                rotation = ShipRotationKinematics.StepYaw(
                    rotation,
                    Vector3.right,
                    ref angularVelocity,
                    30f,
                    30f,
                    0.02f);
                float error = Vector3.Angle(rotation * Vector3.forward,
                    Vector3.right);
                Assert.That(error, Is.LessThanOrEqualTo(previousError + 0.01f));
                Assert.That(Mathf.Abs(angularVelocity), Is.LessThanOrEqualTo(30f));
                previousError = error;
            }

            Assert.That(previousError, Is.LessThan(0.1f));
        }

        [Test]
        public void CalculateBankFromYawRate_TracksActualTurnRate()
        {
            float bank = ShipRotationKinematics.CalculateBankFromYawRate(
                15f,
                30f,
                20f);

            Assert.That(bank, Is.EqualTo(-10f).Within(0.001f));
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
