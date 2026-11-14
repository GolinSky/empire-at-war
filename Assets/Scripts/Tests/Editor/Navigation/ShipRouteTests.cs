using EmpireAtWar.Components.Ship.Movement;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace EmpireAtWar.Tests.Movement
{
    [Category("Navigation")]
    public sealed class ShipRouteTests
    {
        [Test]
        public void NativePieces_RouteOwnsCopyAfterSourceDisposal()
        {
            ShipRoute route;
            using (var pieces = new NativeList<ShipRoutePiece>(2, Allocator.Temp))
            {
                pieces.Add(ShipRoutePiece.Line(Vector3.zero, Vector3.forward * 4f));
                pieces.Add(ShipRoutePiece.Line(Vector3.forward * 4f, Vector3.forward * 9f));
                route = new ShipRoute(pieces.AsArray());
                pieces.Clear();
            }

            Assert.That(route.Pieces.Count, Is.EqualTo(2));
            Assert.That(route.GetPieceStartDistance(1), Is.EqualTo(4f));
            Assert.That(route.Length, Is.EqualTo(9f));
            Assert.That(route.EvaluateNormalizedDistance(1f, out _), Is.EqualTo(Vector3.forward * 9f));
        }

        [Test]
        public void ManagedPieces_RouteOwnsDefensiveCopy()
        {
            var pieces = new[] { ShipRoutePiece.Line(Vector3.zero, Vector3.forward * 4f) };
            var route = new ShipRoute(pieces);

            pieces[0] = ShipRoutePiece.Line(Vector3.zero, Vector3.right);

            Assert.That(route.Length, Is.EqualTo(4f));
            Assert.That(route.EvaluateNormalizedDistance(1f, out _), Is.EqualTo(Vector3.forward * 4f));
        }

        [Test]
        public void Arc_UsesExactLengthAndSignedTangent()
        {
            const float RADIUS = 10f;
            ShipRoutePiece arc = ShipRoutePiece.Arc(Vector3.zero, Vector3.forward,
                Mathf.PI * RADIUS * 0.5f, 1f / RADIUS);
            Vector3 end = arc.Evaluate(arc.Length, out Vector3 tangent);
            Assert.That(Vector3.Distance(end, new Vector3(RADIUS, 0f, RADIUS)), Is.LessThan(0.0001f));
            Assert.That(Vector3.Angle(tangent, Vector3.right), Is.LessThan(0.01f));
            Assert.That(arc.Length, Is.EqualTo(Mathf.PI * RADIUS * 0.5f));
        }

        [Test]
        public void EvaluateDistance_AdvancesCursorAcrossTangentPieces()
        {
            ShipRoute route = CreateBend();
            int cursor = 0;
            float join = route.GetPieceStartDistance(1);
            route.EvaluateDistance(join - 0.001f, ref cursor, out Vector3 before, out _);
            route.EvaluateDistance(join, ref cursor, out Vector3 after, out float curvature);
            Assert.That(cursor, Is.EqualTo(1));
            Assert.That(Vector3.Angle(before, after), Is.LessThan(0.01f));
            Assert.That(curvature, Is.GreaterThan(0f));
            Assert.That(route.RequiresTurnAtPiece(1), Is.False);
        }

        [Test]
        public void SpeedProfile_BrakesBeforeArcAndReachesGoalAtRest()
        {
            ShipRoute route = CreateBend();
            ShipRouteSpeedProfile profile = new ShipRouteSpeedProfile(route, 20f, 30f, 0f);
            int phase = 0;
            int piece = 0;
            float previousDistance = 0f;
            for (int i = 1; i < 200; i++)
            {
                profile.EvaluateTime(profile.Duration * i / 200f, ref phase, out float distance, out float speed);
                route.EvaluateDistance(distance, ref piece, out _, out float curvature);
                Assert.That(distance, Is.GreaterThan(previousDistance));
                Assert.That(speed, Is.GreaterThan(0f));
                Assert.That(speed * Mathf.Abs(curvature), Is.LessThanOrEqualTo(30f * Mathf.Deg2Rad + 0.0001f));
                previousDistance = distance;
            }
            profile.EvaluateTime(profile.Duration, ref phase, out float endDistance, out float endSpeed);
            Assert.That(endDistance, Is.EqualTo(route.Length).Within(0.0001f));
            Assert.That(endSpeed, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(profile.Duration, Is.GreaterThan(route.Length / 20f));
        }

        [Test]
        public void SpeedProfile_InitialOverspeedBrakesWithoutSnapping()
        {
            ShipRoute route = new ShipRoute(new[]
            {
                ShipRoutePiece.Arc(Vector3.zero, Vector3.forward, 80f, 0.1f)
            });
            ShipRouteSpeedProfile profile = new ShipRouteSpeedProfile(route, 20f, 30f, 20f);
            int phase = 0;
            profile.EvaluateTime(0f, ref phase, out float start, out float initialSpeed);
            profile.EvaluateTime(0.1f, ref phase, out float moved, out float speed);
            Assert.That(start, Is.Zero);
            Assert.That(initialSpeed, Is.EqualTo(20f));
            Assert.That(speed, Is.EqualTo(20f - 20f / ShipRouteSpeedProfile.SPEED_RAMP_TIME * 0.1f).Within(0.0001f));
            Assert.That(speed, Is.GreaterThan(profile.GetSpeedLimit(0)));
            Assert.That(moved, Is.GreaterThan(0f));
        }

        [Test]
        public void SpeedProfile_SuffixPreservesPositionAndSpeed()
        {
            ShipRoute route = CreateBend();
            ShipRouteSpeedProfile profile = new ShipRouteSpeedProfile(route, 10f, 30f, 4f, startDistance: 7f);
            int phase = 0;
            profile.EvaluateTime(0f, ref phase, out float distance, out float speed);
            Assert.That(distance, Is.EqualTo(7f));
            Assert.That(speed, Is.EqualTo(4f));
            profile.EvaluateTime(profile.Duration, ref phase, out distance, out speed);
            Assert.That(distance, Is.EqualTo(route.Length).Within(0.0001f));
            Assert.That(speed, Is.Zero.Within(0.0001f));
        }

        [Test]
        public void SpeedProfile_TangentBreakStopsAtJoin()
        {
            ShipRoute route = new ShipRoute(new[]
            {
                ShipRoutePiece.Line(Vector3.zero, Vector3.forward * 10f),
                ShipRoutePiece.Line(Vector3.forward * 10f, new Vector3(10f, 0f, 10f))
            });
            ShipRouteSpeedProfile profile = new ShipRouteSpeedProfile(route, 10f, 30f, 0f);
            int phase = 0;
            profile.EvaluateTime(profile.GetPieceStartTime(1), ref phase, out float distance, out float speed);
            Assert.That(route.RequiresTurnAtPiece(1), Is.True);
            Assert.That(distance, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(speed, Is.Zero.Within(0.0001f));
        }

        [Test]
        public void SpeedProfile_ZeroSpeedBrakesThenHoldsWithoutCompleting()
        {
            ShipRoute route = CreateBend();
            ShipRouteSpeedProfile profile = new ShipRouteSpeedProfile(route, 0f, 30f, 10f, startDistance: 2f);
            int phase = 0;
            profile.EvaluateTime(0f, ref phase, out float start, out float initialSpeed);
            Assert.That(start, Is.EqualTo(2f));
            Assert.That(initialSpeed, Is.EqualTo(10f));
            profile.EvaluateTime(100f, ref phase, out float stopped, out float speed);
            Assert.That(stopped, Is.GreaterThan(start).And.LessThan(route.Length));
            Assert.That(speed, Is.Zero.Within(0.0001f));
            Assert.That(profile.Duration, Is.EqualTo(float.PositiveInfinity));
            Assert.That(profile.GetPieceStartTime(1), Is.EqualTo(float.PositiveInfinity));
            profile.EvaluateTime(200f, ref phase, out float held, out speed);
            Assert.That(held, Is.EqualTo(stopped));
            Assert.That(speed, Is.Zero.Within(0.0001f));
        }

        [Test]
        public void SpeedProfile_ZeroSpeedFromRestHoldsFinitePosition()
        {
            ShipRouteSpeedProfile profile = new ShipRouteSpeedProfile(CreateBend(), 0f, 30f, 0f, startDistance: 3f);
            int phase = 0;
            profile.EvaluateTime(100f, ref phase, out float distance, out float speed);
            Assert.That(distance, Is.EqualTo(3f));
            Assert.That(speed, Is.Zero);
            Assert.That(profile.Duration, Is.EqualTo(float.PositiveInfinity));
        }

        private static ShipRoute CreateBend()
        {
            ShipRoutePiece line = ShipRoutePiece.Line(Vector3.zero, Vector3.forward * 30f);
            ShipRoutePiece arc = ShipRoutePiece.Arc(line.End, line.EndTangent, Mathf.PI * 5f, 0.1f);
            return new ShipRoute(new[] { line, arc, ShipRoutePiece.Line(arc.End, arc.End + Vector3.right * 30f) });
        }
    }
}
