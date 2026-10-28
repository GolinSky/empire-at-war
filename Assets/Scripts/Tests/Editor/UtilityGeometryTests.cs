using System;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Core")]
    public sealed class UtilityGeometryTests
    {
        [TestCase(3f, 4f, true)]
        [TestCase(0f, 0f, true)]
        [TestCase(3f, 4.01f, false)]
        public void CircleContainment_IncludesBoundaryAndIgnoresHeight(float x, float z, bool expected)
        {
            Assert.That(PlanarGeometry.ContainsCircle(
                new Vector3(x + 10f, 200f, z - 20f), new Vector3(10f, -200f, -20f), 5f),
                Is.EqualTo(expected));
        }

        [Test]
        public void OwnedCircle_ClearanceAndMovingCenterUsePlanarContainment()
        {
            var center = new GameObject("UtilityGeometryTests.Center");
            try
            {
                var circle = new OwnedCircle(default, center.transform, 4f);
                Assert.That(circle.Contains(new Vector3(3f, 100f, 4f), 1f), Is.True);
                Assert.That(circle.Contains(new Vector3(3f, 100f, 4f), 0f), Is.False);
                center.transform.position = new Vector3(10f, -100f, 0f);
                Assert.That(circle.Contains(new Vector3(13f, 100f, 4f), 1f), Is.True);
                Assert.That(circle.Contains(new Vector3(3f, 100f, 4f), 1f), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(center);
            }
        }

        [Test]
        public void PolylineDistance_ProjectsOntoClosestSegmentAndIgnoresHeight()
        {
            Vector3[] points = { new(0f, 100f, 0f), new(10f, -100f, 0f), new(10f, 300f, 10f) };
            Assert.That(PlanarGeometry.DistanceToPolyline(new Vector3(13f, 500f, 5f), points),
                Is.EqualTo(3f).Within(0.0001f));
            Assert.That(PlanarGeometry.DistanceToPolyline(new Vector3(-3f, 500f, -4f), points),
                Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void PolylineDistance_DegenerateSegmentUsesEndpoint()
        {
            Vector3[] points = { Vector3.zero, Vector3.up };
            Assert.That(PlanarGeometry.DistanceToPolyline(new Vector3(3f, 500f, 4f), points),
                Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void PolylineDistance_WithoutSegmentsPreservesUnboundedResult()
        {
            Assert.That(PlanarGeometry.DistanceToPolyline(Vector3.zero, Array.Empty<Vector3>()),
                Is.EqualTo(float.MaxValue));
            Assert.That(PlanarGeometry.DistanceToPolyline(Vector3.zero, new[] { Vector3.zero }),
                Is.EqualTo(float.MaxValue));
        }

        [Test]
        public void PlanarDirection_NormalizesRequestedDirectionBeforeFallback()
        {
            Vector3 direction = PlanarGeometry.NormalizeDirection(new Vector3(3f, 100f, 4f), Vector3.left);
            Assert.That(direction.x, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(direction.y, Is.Zero);
            Assert.That(direction.z, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void PlanarDirection_UsesPlanarFallbackThenForwardWhenBothAreVertical()
        {
            Assert.That(PlanarGeometry.NormalizeDirection(Vector3.up, new Vector3(-3f, 100f, 0f)),
                Is.EqualTo(Vector3.left));
            Assert.That(PlanarGeometry.NormalizeDirection(Vector3.up, Vector3.down),
                Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void Flatten_DropsHeightOnly()
        {
            Assert.That(PlanarGeometry.Flatten(new Vector3(3f, 100f, -4f)), Is.EqualTo(new Vector3(3f, 0f, -4f)));
        }

        [Test]
        public void PointTowards_MovesPlanarDistanceAtZeroHeight()
        {
            Assert.That(PlanarGeometry.GetPointTowards(new Vector3(1f, 50f, 1f), new Vector3(4f, -9f, 5f), 10f,
                Vector3.right), Is.EqualTo(new Vector3(7f, 0f, 9f)));
        }

        [Test]
        public void PointTowards_UsesFallbackWhenTargetOverlapsOrigin()
        {
            Assert.That(PlanarGeometry.GetPointTowards(new Vector3(1f, 50f, 1f), new Vector3(1f, -9f, 1f), 2f,
                Vector3.right), Is.EqualTo(new Vector3(3f, 0f, 1f)));
        }

        [Test]
        public void CircleBounds_SpanTheDiameterWithNoHeight()
        {
            Bounds bounds = PlanarGeometry.GetCircleBounds(new Vector3(2f, 7f, -3f), 4f);
            Assert.That(bounds.center, Is.EqualTo(new Vector3(2f, 7f, -3f)));
            Assert.That(bounds.size, Is.EqualTo(new Vector3(8f, 0f, 8f)));
        }

        [Test]
        public void PlanarDistance_DoesNotIncludeHeight()
        {
            Assert.That(PlanarGeometry.Distance(new Vector3(3f, 100f, 4f), Vector3.zero), Is.EqualTo(5f));
        }
    }
}
