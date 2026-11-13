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
        public void RouteSpeed_ReductionBrakesWithoutSnappingSpeed(float coefficient)
        {
            GameObject root = new GameObject("Moving ship");
            GameObject body = new GameObject("Body");
            body.transform.SetParent(root.transform);
            ShipMovementTweenPlayer motion = new ShipMovementTweenPlayer(root.transform,
                body.transform, root.AddComponent<LineRenderer>(), Ease.Linear);
            try
            {
                ShipRoute route = new ShipRoute(new[]
                {
                    ShipRoutePiece.Line(Vector3.zero, Vector3.forward * 1000f)
                });
                motion.PlayPath(new ShipNavigationPlan(route, Vector3.forward * 1000f,
                    null, 0f, 100f), 10f, 90f, 90f, 0f, null);
                for (int i = 0; i < 30; i++) motion.Tick(0.1f);
                Vector3 before = root.transform.position;
                float beforeSpeed = motion.CurrentSpeed;

                motion.SetRouteSpeed(10f * coefficient);
                Assert.That(motion.CurrentSpeed, Is.EqualTo(beforeSpeed));
                motion.Tick(0.1f);

                Assert.That(Vector3.Distance(before, root.transform.position),
                    Is.GreaterThan(coefficient));
                Assert.That(motion.CurrentSpeed, Is.LessThan(beforeSpeed));
                for (int i = 0; i < 100; i++) motion.Tick(0.1f);
                Assert.That(motion.CurrentSpeed, Is.LessThanOrEqualTo(10f * coefficient + 0.001f));
            }
            finally
            {
                motion.Release();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RouteSpeed_ZeroPausesAndPositiveSpeedResumesTheOrder()
        {
            GameObject root = new GameObject("Disabled ship");
            GameObject body = new GameObject("Body");
            body.transform.SetParent(root.transform);
            ShipMovementTweenPlayer motion = new ShipMovementTweenPlayer(root.transform,
                body.transform, root.AddComponent<LineRenderer>(), Ease.Linear);
            bool completed = false;
            try
            {
                ShipRoute route = new ShipRoute(new[]
                {
                    ShipRoutePiece.Line(Vector3.zero, Vector3.forward * 1000f)
                });
                motion.PlayPath(new ShipNavigationPlan(route, Vector3.forward * 1000f,
                    null, 0f, 100f), 10f, 90f, 90f, 0f, () => completed = true);
                for (int i = 0; i < 30; i++) motion.Tick(0.1f);
                float beforeSpeed = motion.CurrentSpeed;
                motion.SetRouteSpeed(0f);
                Assert.That(motion.CurrentSpeed, Is.EqualTo(beforeSpeed));
                for (int i = 0; i < 30; i++) motion.Tick(0.1f);
                Vector3 stopped = root.transform.position;
                motion.Tick(1f);
                Assert.That(root.transform.position, Is.EqualTo(stopped));
                Assert.That(motion.CurrentSpeed, Is.Zero.Within(0.0001f));
                Assert.That(completed, Is.False);
                motion.SetRouteSpeed(10f);
                motion.Tick(0.1f);
                Assert.That(root.transform.position.z, Is.GreaterThan(stopped.z));
                Assert.That(motion.CurrentSpeed, Is.GreaterThan(0f));
                motion.SetRouteSpeed(0f);
                motion.BrakeForReplan();
                for (int i = 0; i < 30; i++) motion.Tick(0.1f);
                Assert.That(motion.IsBraking, Is.False);
                Assert.That(motion.HasMomentum, Is.False);
                Assert.That(completed, Is.False);
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
                ShipRoute route = new ShipRoute(new[]
                {
                    ShipRoutePiece.Line(Vector3.zero, Vector3.forward * 1000f)
                });
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
        public void ArcRoute_FollowsTangentWithoutCornerStops()
        {
            GameObject root = new GameObject("Curving ship");
            GameObject body = new GameObject("Body");
            body.transform.SetParent(root.transform);
            ShipMovementTweenPlayer motion = new ShipMovementTweenPlayer(root.transform,
                body.transform, root.AddComponent<LineRenderer>(), Ease.Linear);
            ShipRoutePiece arc = ShipRoutePiece.Arc(Vector3.forward * 10f,
                Vector3.forward, Mathf.PI * 5f, 0.1f);
            ShipRoute route = new ShipRoute(new[]
            {
                ShipRoutePiece.Line(Vector3.zero, arc.Start),
                arc,
                ShipRoutePiece.Line(arc.End, arc.End + arc.EndTangent * 10f)
            });
            bool completed = false;
            try
            {
                motion.PlayPath(new ShipNavigationPlan(route, route.Pieces[2].End,
                    null, 0f, 10f), 10f, 90f, 1f, 10f, () => completed = true);
                float minimumMovingSpeed = float.PositiveInfinity;
                for (int i = 0; i < 1000 && !completed; i++)
                {
                    motion.Tick(0.02f);
                    if (!motion.CurrentPathTangent.HasValue) continue;
                    Assert.That(Vector3.Angle(root.transform.forward,
                        motion.CurrentPathTangent.Value), Is.LessThan(0.01f));
                    if (root.transform.position.z > 9f && root.transform.position.x < 11f)
                        minimumMovingSpeed = Mathf.Min(minimumMovingSpeed, motion.CurrentSpeed);
                }
                Assert.That(completed, Is.True);
                Assert.That(minimumMovingSpeed, Is.GreaterThan(0f));
                Assert.That(Vector3.Distance(root.transform.position, route.Pieces[2].End),
                    Is.LessThan(0.001f));
            }
            finally
            {
                motion.Release();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReplanOntoTightArc_BrakesFromCurrentSpeedWithoutSnappingHeading()
        {
            GameObject root = new GameObject("Replanning ship");
            GameObject body = new GameObject("Body");
            body.transform.SetParent(root.transform);
            ShipMovementTweenPlayer motion = new ShipMovementTweenPlayer(root.transform,
                body.transform, root.AddComponent<LineRenderer>(), Ease.Linear);
            try
            {
                ShipRoute straight = new ShipRoute(new[]
                {
                    ShipRoutePiece.Line(Vector3.zero, Vector3.forward * 1000f)
                });
                motion.PlayPath(new ShipNavigationPlan(straight, Vector3.forward * 1000f,
                    null, 0f, 100f), 10f, 30f, 30f, 0f, null);
                for (int i = 0; i < 100; i++) motion.Tick(0.02f);
                float beforeSpeed = motion.CurrentSpeed;
                Quaternion beforeRotation = root.transform.rotation;
                ShipRoutePiece arc = ShipRoutePiece.Arc(root.transform.position,
                    root.transform.forward, 20f, 0.2f);
                ShipRoute curved = new ShipRoute(new[] { arc });
                motion.PlayPath(new ShipNavigationPlan(curved, arc.End,
                    null, 0f, 10f), 10f, 30f, 30f, 0f, null);

                Assert.That(motion.CurrentSpeed, Is.EqualTo(beforeSpeed));
                motion.Tick(0.02f);

                Assert.That(motion.CurrentSpeed, Is.LessThan(beforeSpeed));
                Assert.That(motion.CurrentSpeed, Is.GreaterThan(30f * Mathf.Deg2Rad / 0.2f));
                Assert.That(Quaternion.Angle(beforeRotation, root.transform.rotation),
                    Is.LessThanOrEqualTo(30f * 0.02f + 0.01f));
                Assert.That(Vector3.Angle(root.transform.forward,
                    motion.CurrentPathTangent.Value), Is.GreaterThan(0.1f));
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
