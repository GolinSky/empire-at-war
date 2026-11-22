using System.Reflection;
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
        [TestCase(10f, 90f, 0f, 0f, false, true)]
        [TestCase(0f, 90f, 0f, 0f, false, false)]
        [TestCase(10f, 45f, 0f, 0f, false, false)]
        [TestCase(10f, 90f, 2f, 0f, false, false)]
        [TestCase(10f, 90f, 0f, 2f, false, false)]
        [TestCase(10f, 90f, 0f, 0f, true, false)]
        public void PlayPath_ReusesProfileOnlyWhenInputsMatch(float plannedSpeed, float plannedRotation,
            float plannedInitialSpeed, float plannedStartDistance, bool differentRoute, bool expectReuse)
        {
            var root = new GameObject("Moving ship");
            var motion = new ShipMovementTweenPlayer(root.transform, root.transform,
                root.AddComponent<LineRenderer>(), Ease.Linear);
            try
            {
                Vector3 destination = Vector3.forward * 50f;
                var route = new ShipRoute(new[] { ShipRoutePiece.Line(Vector3.zero, destination) });
                ShipRoute plannedRoute = differentRoute
                    ? new ShipRoute(new[] { ShipRoutePiece.Line(Vector3.zero, destination) }) : route;
                var planned = new ShipRouteSpeedProfile(plannedRoute, plannedSpeed, plannedRotation,
                    plannedInitialSpeed, startDistance: plannedStartDistance);

                motion.PlayPath(new ShipNavigationPlan(route, destination, null, 0f, planned),
                    10f, 90f, 90f, 0f, null);

                var active = (ShipRouteSpeedProfile)typeof(ShipMovementTweenPlayer)
                    .GetField("_speedProfile", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(motion);
                Assert.That(ReferenceEquals(active, planned), Is.EqualTo(expectReuse));
                Assert.That(active.Matches(route, 10f, 90f, 0f, 0f), Is.True);
            }
            finally
            {
                motion.Release();
                Object.DestroyImmediate(root);
            }
        }

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

        [TestCase(0.25f)]
        [TestCase(0.6f)]
        public void RouteSpeed_ReductionOnArcStaysOnCurveWithoutStopping(float coefficient)
        {
            GameObject root = new GameObject("Turning ship");
            GameObject body = new GameObject("Body");
            body.transform.SetParent(root.transform);
            ShipMovementTweenPlayer motion = new ShipMovementTweenPlayer(root.transform,
                body.transform, root.AddComponent<LineRenderer>(), Ease.Linear);
            try
            {
                const float speed = 10f;
                const float rotation = 90f;
                // Planned arcs are sized from top speed, so the ship turns at its full rate.
                float curvature = rotation * Mathf.Deg2Rad / speed;
                ShipRoutePiece arc = ShipRoutePiece.Arc(Vector3.zero, Vector3.forward,
                    Mathf.PI / curvature, curvature);
                ShipRoute route = new ShipRoute(new[]
                {
                    arc, ShipRoutePiece.Line(arc.End, arc.End + arc.EndTangent * 300f)
                });
                motion.PlayPath(new ShipNavigationPlan(route, route.Pieces[1].End,
                    null, 0f, 100f), speed, rotation, rotation, 0f, null);
                for (int i = 0; i < 40; i++) motion.Tick(0.05f);
                float beforeSpeed = motion.CurrentSpeed;
                Assert.That(Vector3.Dot(root.transform.position - arc.End, arc.EndTangent),
                    Is.LessThan(0f), "The ship must still be on the arc when the cap drops.");

                motion.SetRouteSpeed(speed * coefficient);

                bool reachedLine = false;
                for (int i = 0; i < 400 && !reachedLine; i++)
                {
                    motion.Tick(0.05f);
                    Assert.That(motion.CurrentSpeed, Is.GreaterThan(speed * coefficient * 0.99f),
                        $"Frame {i}: the ship must not brake below the new cap.");
                    Assert.That(Vector3.Angle(root.transform.forward,
                        motion.CurrentPathTangent.Value), Is.LessThan(0.01f),
                        $"Frame {i}: the hull must stay locked on the curve.");
                    reachedLine = Vector3.Dot(root.transform.position - arc.End, arc.EndTangent) > 1f;
                }
                Assert.That(reachedLine, Is.True);
                Assert.That(motion.CurrentSpeed, Is.LessThan(beforeSpeed));
                // A low cap may need part of the outgoing line to finish braking.
                for (int i = 0; i < 400; i++) motion.Tick(0.05f);
                Assert.That(motion.CurrentSpeed, Is.LessThanOrEqualTo(speed * coefficient + 0.001f));
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
                ShipRoute curved = new ShipRoute(new[]
                {
                    arc, ShipRoutePiece.Line(arc.End, arc.End + arc.EndTangent * 30f)
                });
                bool completed = false;
                motion.PlayPath(new ShipNavigationPlan(curved, curved.Pieces[1].End,
                    null, 0f, 10f), 10f, 30f, 30f, 0f, () => completed = true);

                Assert.That(motion.CurrentSpeed, Is.EqualTo(beforeSpeed));
                motion.Tick(0.02f);

                Assert.That(motion.CurrentSpeed, Is.LessThan(beforeSpeed));
                Assert.That(motion.CurrentSpeed, Is.GreaterThan(30f * Mathf.Deg2Rad / 0.2f));
                Assert.That(Quaternion.Angle(beforeRotation, root.transform.rotation),
                    Is.LessThanOrEqualTo(30f * 0.02f + 0.01f));
                Assert.That(Vector3.Angle(root.transform.forward,
                    motion.CurrentPathTangent.Value), Is.GreaterThan(0.1f));
                bool reachedLine = false;
                for (int i = 0; i < 3000 && !completed; i++)
                {
                    beforeRotation = root.transform.rotation;
                    motion.Tick(0.02f);
                    Assert.That(Quaternion.Angle(beforeRotation, root.transform.rotation),
                        Is.LessThanOrEqualTo(30f * 0.02f + 0.01f), $"Frame {i}");
                    reachedLine |= Vector3.Dot(root.transform.position - arc.End, arc.EndTangent) > 1f;
                }
                Assert.That(reachedLine, Is.True);
                Assert.That(completed, Is.True, "Heading recovery must resume and complete the route.");
                Assert.That(Vector3.Angle(root.transform.forward, arc.EndTangent), Is.LessThan(0.1f));
            }
            finally
            {
                motion.Release();
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(0.2f)]
        [TestCase(20f)]
        public void BrakeForReplan_DoesNotCrossSharpCorner(float distanceToCorner)
        {
            var root = new GameObject("Braking ship");
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform);
            var motion = new ShipMovementTweenPlayer(root.transform, body.transform,
                root.AddComponent<LineRenderer>(), Ease.Linear);
            try
            {
                var straight = new ShipRoute(new[]
                {
                    ShipRoutePiece.Line(Vector3.zero, Vector3.forward * 1000f)
                });
                motion.PlayPath(new ShipNavigationPlan(straight, Vector3.forward * 1000f,
                    null, 0f, 100f), 10f, 30f, 30f, 0f, null);
                for (int i = 0; i < 100; i++) motion.Tick(0.02f);
                Vector3 corner = root.transform.position + Vector3.forward * distanceToCorner;
                var route = new ShipRoute(new[]
                {
                    ShipRoutePiece.Line(root.transform.position, corner),
                    ShipRoutePiece.Line(corner, corner + Vector3.right * 50f)
                });
                bool completed = false;
                motion.PlayPath(new ShipNavigationPlan(route, route.Pieces[1].End,
                    null, 0f, 10f), 10f, 30f, 30f, 0f, () => completed = true);
                motion.BrakeForReplan();
                for (int i = 0; i < 200 && motion.IsBraking; i++)
                {
                    Quaternion before = root.transform.rotation;
                    motion.Tick(0.02f);
                    Assert.That(root.transform.position.x, Is.Zero.Within(0.0001f));
                    Assert.That(root.transform.position.z, Is.LessThanOrEqualTo(corner.z));
                    Assert.That(Quaternion.Angle(before, root.transform.rotation),
                        Is.LessThanOrEqualTo(0.61f));
                }
                Assert.That(motion.IsBraking, Is.False);
                Assert.That(motion.HasMomentum, Is.False);
                Assert.That(completed, Is.False);
                Assert.That(Vector3.Angle(root.transform.forward, Vector3.forward), Is.LessThan(0.01f));
                if (distanceToCorner < 1f)
                    Assert.That(Vector3.Distance(root.transform.position, corner), Is.LessThan(0.001f));
            }
            finally
            {
                motion.Release();
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ZeroTurnDuration_AlignsActualHullBeforeMoving(bool brakeDuringTurn)
        {
            var root = new GameObject("Misaligned ship");
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform);
            var motion = new ShipMovementTweenPlayer(root.transform, body.transform,
                root.AddComponent<LineRenderer>(), Ease.Linear);
            try
            {
                var route = new ShipRoute(new[] { ShipRoutePiece.Line(Vector3.zero, Vector3.right * 20f) });
                bool completed = false;
                motion.PlayPath(new ShipNavigationPlan(route, Vector3.right * 20f,
                    null, 0f, 10f), 10f, 30f, 30f, 0f, () => completed = true);
                motion.Tick(0.02f);
                Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
                Assert.That(Vector3.Angle(root.transform.forward, Vector3.right), Is.GreaterThan(80f));
                if (brakeDuringTurn) motion.BrakeForReplan();
                for (int i = 0; i < 1000 && !completed; i++)
                {
                    Quaternion before = root.transform.rotation;
                    motion.Tick(0.02f);
                    Assert.That(Quaternion.Angle(before, root.transform.rotation), Is.LessThanOrEqualTo(0.61f));
                    if (brakeDuringTurn)
                    {
                        Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
                        Assert.That(motion.IsBraking, Is.False);
                        Assert.That(motion.HasMomentum, Is.False);
                        break;
                    }
                }
                Assert.That(completed, Is.EqualTo(!brakeDuringTurn));
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
