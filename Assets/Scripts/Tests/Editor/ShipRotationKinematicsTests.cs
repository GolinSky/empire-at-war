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
        public void CalculateBankAngle_UsesTurnRateInsteadOfResidualHeadingError()
        {
            float bank = ShipRotationKinematics.CalculateBankAngle(
                Quaternion.identity,
                Vector3.right,
                30f,
                0.1f,
                20f);

            Assert.That(Mathf.Abs(bank), Is.EqualTo(20f).Within(0.001f));
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

        [Test]
        public void CalculateLookBankAngle_ScalesConfiguredMaximumByTurnAngle()
        {
            float bank = ShipRotationKinematics.CalculateLookBankAngle(
                Quaternion.identity,
                Vector3.right,
                20f);

            Assert.That(bank, Is.EqualTo(-10f).Within(0.001f));
        }
    }
}
