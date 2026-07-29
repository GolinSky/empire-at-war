using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Models.SkirmishCamera;
using NUnit.Framework;
using UnityEngine;
using System.Reflection;

namespace EmpireAtWar.Tests.Movement
{
    public sealed class ShipAvoidancePlannerTests
    {
        private Vector2Range _mapRange;

        [SetUp]
        public void SetUp()
        {
            _mapRange = new Vector2Range();
            SetRangeValue("<Min>k__BackingField", new Vector2(-100f, -100f));
            SetRangeValue("<Max>k__BackingField", new Vector2(100f, 100f));
        }

        private void SetRangeValue(string fieldName, Vector2 value)
        {
            FieldInfo field = _mapRange.GetType().BaseType?.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_mapRange, value);
        }

        [Test]
        public void ClampToMap_RespectsClearanceFromEveryEdge()
        {
            Vector3 result = ShipAvoidancePlanner.ClampToMap(
                new Vector3(150f, -40f, -150f),
                _mapRange,
                10f);

            Assert.That(result, Is.EqualTo(new Vector3(90f, -40f, -90f)));
        }

        [Test]
        public void TryCalculateDetour_AvoidsObstacleCrossingRoute()
        {
            List<RadarContact> contacts = new List<RadarContact>
            {
                new RadarContact(new Vector3(20f, 0f, 0f), 5f, false)
            };

            bool found = ShipAvoidancePlanner.TryCalculateDetour(
                Vector3.zero,
                new Vector3(50f, 0f, 0f),
                contacts,
                0f,
                0.5f,
                5f,
                _mapRange,
                out Vector3 detour);

            Assert.That(found, Is.True);
            Assert.That(Mathf.Abs(detour.z), Is.GreaterThanOrEqualTo(10f));
        }

        [Test]
        public void TryCalculateDetour_IgnoresShipAtDifferentHeight()
        {
            List<RadarContact> contacts = new List<RadarContact>
            {
                new RadarContact(new Vector3(20f, 10f, 0f), 5f, true)
            };

            bool found = ShipAvoidancePlanner.TryCalculateDetour(
                Vector3.zero,
                new Vector3(50f, 0f, 0f),
                contacts,
                0f,
                0.5f,
                5f,
                _mapRange,
                out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void TryCalculateDetour_UsesClearCandidateWhenObstacleIsNearMapEdge()
        {
            List<RadarContact> contacts = new List<RadarContact>
            {
                new RadarContact(new Vector3(20f, 0f, 90f), 5f, false)
            };

            bool found = ShipAvoidancePlanner.TryCalculateDetour(
                new Vector3(0f, 0f, 90f),
                new Vector3(50f, 0f, 90f),
                contacts,
                0f,
                0.5f,
                5f,
                _mapRange,
                out Vector3 detour);

            Assert.That(found, Is.True);
            Assert.That(detour.z, Is.LessThan(90f));
            Assert.That(
                Vector3.Distance(
                    detour,
                    new Vector3(20f, 0f, 90f)),
                Is.GreaterThanOrEqualTo(10f));
        }

        [Test]
        public void BuildAvoidance_CreatesSmoothCurveThroughDetour()
        {
            Vector3 origin = Vector3.zero;
            Vector3 detour = new Vector3(20f, 0f, 10f);
            Vector3 destination = new Vector3(50f, 0f, 0f);

            Vector3[] path = ShipBezierPath.BuildAvoidance(
                origin,
                Vector3.right,
                detour,
                destination);

            Assert.That(path.Length, Is.GreaterThan(3));
            Assert.That(path[0], Is.EqualTo(origin));
            Assert.That(path[path.Length / 2], Is.EqualTo(detour));
            Assert.That(path[path.Length - 1], Is.EqualTo(destination));

            Vector3 incoming = (detour - path[path.Length / 2 - 1]).normalized;
            Vector3 outgoing = (path[path.Length / 2 + 1] - detour).normalized;
            Assert.That(Vector3.Dot(incoming, outgoing), Is.GreaterThan(0.95f));
        }

        [Test]
        public void TryResolveDestination_MovesOccupiedTargetToClosestSafePoint()
        {
            List<RadarContact> contacts = new List<RadarContact>
            {
                new RadarContact(new Vector3(20f, 0f, 0f), 5f, false)
            };

            bool resolved = ShipAvoidancePlanner.TryResolveDestination(
                new Vector3(20f, 0f, 0f),
                Vector3.zero,
                contacts,
                0f,
                0.5f,
                5f,
                _mapRange,
                out Vector3 destination);

            Assert.That(resolved, Is.True);
            Assert.That(
                Vector2.Distance(
                    new Vector2(destination.x, destination.z),
                    new Vector2(20f, 0f)),
                Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void TryResolveDestination_RejectsCandidateOccupiedByAnotherShip()
        {
            List<RadarContact> contacts = new List<RadarContact>
            {
                new RadarContact(new Vector3(20f, 0f, 0f), 5f, false),
                new RadarContact(new Vector3(10f, 0f, 0f), 4f, true)
            };

            bool resolved = ShipAvoidancePlanner.TryResolveDestination(
                new Vector3(20f, 0f, 0f),
                Vector3.zero,
                contacts,
                0f,
                0.5f,
                5f,
                _mapRange,
                out Vector3 destination);

            Assert.That(resolved, Is.True);
            Assert.That(Vector3.Distance(destination, new Vector3(10f, 0f, 0f)), Is.GreaterThanOrEqualTo(9f));
        }
    }
}
