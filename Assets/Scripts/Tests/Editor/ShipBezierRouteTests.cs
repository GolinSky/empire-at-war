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
    }
}
