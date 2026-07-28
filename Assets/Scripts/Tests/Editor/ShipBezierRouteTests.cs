using EmpireAtWar.Components.Ship.Movement;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Movement
{
    public sealed class ShipBezierRouteTests
    {
        [Test]
        public void EvaluateNormalizedDistance_UsesCubicBezierCurve()
        {
            CubicBezierSegment segment = new CubicBezierSegment(
                Vector3.zero,
                new Vector3(0f, 0f, 10f),
                new Vector3(10f, 0f, 10f),
                new Vector3(10f, 0f, 0f));
            ShipBezierRoute route = new ShipBezierRoute(
                new[] { segment });

            Vector3 midpoint = route.EvaluateNormalizedDistance(0.5f, out _);

            Assert.That(midpoint.x, Is.EqualTo(5f).Within(0.01f));
            Assert.That(midpoint.z, Is.EqualTo(7.5f).Within(0.01f));
        }

        [Test]
        public void BuildAvoidanceRoute_PreservesTangentAcrossDetour()
        {
            ShipBezierRoute route = ShipBezierPath.BuildAvoidanceRoute(
                Vector3.zero,
                Vector3.right,
                new Vector3(20f, 0f, 10f),
                new Vector3(40f, 0f, 0f));

            route.EvaluateNormalizedDistance(0.499f, out Vector3 incoming);
            route.EvaluateNormalizedDistance(0.501f, out Vector3 outgoing);

            Assert.That(Vector3.Dot(incoming, outgoing), Is.GreaterThan(0.99f));
        }

        [Test]
        public void BuildDirectRoute_DestinationBehind_MovesForwardThroughLateralTurn()
        {
            ShipBezierRoute route = ShipBezierPath.BuildDirectRoute(
                Vector3.zero,
                Vector3.forward,
                Vector3.back * 40f);

            Vector3 earlyPosition =
                route.EvaluateNormalizedDistance(0.05f, out Vector3 earlyTangent);
            float maximumLateralOffset = 0f;
            for (int i = 0; i < route.Samples.Length; i++)
            {
                maximumLateralOffset = Mathf.Max(
                    maximumLateralOffset,
                    Mathf.Abs(route.Samples[i].x));
            }

            Assert.That(
                Vector3.Dot(earlyPosition, Vector3.forward),
                Is.GreaterThan(0f));
            Assert.That(
                Vector3.Dot(earlyTangent, Vector3.forward),
                Is.GreaterThan(0f));
            Assert.That(maximumLateralOffset, Is.GreaterThan(1f));
        }

        [Test]
        public void BuildDirectRoute_DestinationBehind_UsesFullHalfCircle()
        {
            const float TURN_RADIUS = 10f;
            ShipBezierRoute route = ShipBezierPath.BuildDirectRoute(
                Vector3.zero,
                Vector3.forward,
                Vector3.back * 30f,
                TURN_RADIUS);

            Vector3 expectedHalfCircleEnd =
                Vector3.right * TURN_RADIUS * 2f;
            float distanceToHalfCircleEnd = float.MaxValue;
            int halfCircleEndIndex = 0;
            for (int i = 0; i < route.Samples.Length; i++)
            {
                float distance = Vector3.Distance(
                    route.Samples[i],
                    expectedHalfCircleEnd);
                if (distance < distanceToHalfCircleEnd)
                {
                    distanceToHalfCircleEnd = distance;
                    halfCircleEndIndex = i;
                }
            }

            Assert.That(
                distanceToHalfCircleEnd,
                Is.LessThan(TURN_RADIUS * 0.1f));
            Assert.That(halfCircleEndIndex, Is.GreaterThan(0));
            Assert.That(
                halfCircleEndIndex,
                Is.LessThan(route.Samples.Length - 1));
            Vector3 tangent =
                (route.Samples[halfCircleEndIndex + 1] -
                 route.Samples[halfCircleEndIndex - 1]).normalized;
            Assert.That(
                Vector3.Dot(tangent, Vector3.back),
                Is.GreaterThan(0.95f));
            Assert.That(
                Vector3.Dot(
                    (route.Samples[12] - route.Samples[11]).normalized,
                    (route.Samples[13] - route.Samples[12]).normalized),
                Is.GreaterThan(0.98f));
            Assert.That(
                Vector3.Dot(
                    (route.Samples[24] - route.Samples[23]).normalized,
                    (route.Samples[25] - route.Samples[24]).normalized),
                Is.GreaterThan(0.98f));
        }
    }
}
