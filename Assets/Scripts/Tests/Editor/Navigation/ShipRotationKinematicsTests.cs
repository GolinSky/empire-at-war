using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Services.ShipNavigation;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Movement
{
    [Category("Navigation")]
    public sealed class ShipRotationKinematicsTests
    {
        [TestCase(0.25f)]
        [TestCase(0.4f)]
        public void RouteSpeed_ReductionImmediatelyLimitsDistanceTravelled(float coefficient)
        {
            GameObject root = new GameObject("Moving ship");
            GameObject body = new GameObject("Body");
            body.transform.SetParent(root.transform);
            ShipMovementTweenPlayer motion = new ShipMovementTweenPlayer(root.transform,
                body.transform, root.AddComponent<LineRenderer>(), Ease.Linear);
            try
            {
                ShipBezierRoute route = ShipBezierPath.BuildDirectRoute(
                    Vector3.zero, Vector3.forward, Vector3.forward * 1000f);
                motion.PlayPath(new ShipNavigationPlan(route, Vector3.forward * 1000f,
                    null, 0f, 100f), 10f, 90f, 90f, 0f, null);
                for (int i = 0; i < 30; i++) motion.Tick(0.1f);
                Vector3 before = root.transform.position;

                motion.SetRouteSpeed(10f * coefficient);
                motion.Tick(0.1f);

                Assert.That(Vector3.Distance(before, root.transform.position),
                    Is.LessThanOrEqualTo(coefficient + 0.001f));
            }
            finally
            {
                motion.Release();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BrakeForReplan_ContinuesAlongRouteUntilMomentumIsGone()
        {
            GameObject root = new GameObject("Moving ship");
            GameObject body = new GameObject("Body");
            body.transform.SetParent(root.transform);
            ShipMovementTweenPlayer motion = new ShipMovementTweenPlayer(root.transform,
                body.transform, root.AddComponent<LineRenderer>(), Ease.Linear);
            bool completed = false;
            try
            {
                ShipBezierRoute route = ShipBezierPath.BuildDirectRoute(
                    Vector3.zero, Vector3.forward, Vector3.forward * 1000f);
                motion.PlayPath(new ShipNavigationPlan(route, Vector3.forward * 1000f,
                    null, 0f, 100f), 10f, 90f, 90f, 0f, () => completed = true);
                for (int i = 0; i < 30; i++) motion.Tick(0.1f);
                float before = root.transform.position.z;

                motion.BrakeForReplan();
                motion.Tick(0.1f);

                Assert.That(root.transform.position.z - before, Is.InRange(0.8f, 1f));
                Assert.That(motion.IsBraking, Is.True);
                for (int i = 0; i < 20; i++) motion.Tick(0.1f);
                Assert.That(motion.IsBraking, Is.False);
                Assert.That(motion.HasMomentum, Is.False);
                Assert.That(completed, Is.False, "Braking is not arrival at the old destination.");
                Vector3 stopped = root.transform.position;
                motion.Tick(0.1f);
                Assert.That(root.transform.position, Is.EqualTo(stopped));
            }
            finally
            {
                motion.Release();
                Object.DestroyImmediate(root);
            }
        }

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
