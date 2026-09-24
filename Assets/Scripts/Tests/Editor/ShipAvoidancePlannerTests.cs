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
        public void TryResolveDestination_KeepsShipOccupiedTarget()
        {
            List<RadarContact> contacts = new List<RadarContact>
            {
                new RadarContact(new Vector3(20f, 0f, 0f), 5f, true)
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

            Assert.That(resolved, Is.False);
            Assert.That(destination, Is.EqualTo(new Vector3(20f, 0f, 0f)));
        }

        [Test]
        public void IsRouteClear_IgnoresShipContact()
        {
            ShipBezierRoute route = ShipBezierPath.BuildDirectRoute(
                Vector3.zero,
                Vector3.right,
                new Vector3(30f, 0f, 0f));
            RadarContact contact = new RadarContact(
                new Vector3(15f, 0f, 0f),
                3f,
                true);

            bool isClear = ShipAvoidancePlanner.IsRouteClear(
                route,
                new[] { contact },
                0f,
                0.5f,
                2f);

            Assert.That(isClear, Is.True);
        }

        [Test]
        public void IsRouteClear_RejectsInitialStaticObstacleOverlap()
        {
            ShipBezierRoute route = ShipBezierPath.BuildDirectRoute(
                Vector3.zero,
                Vector3.right,
                new Vector3(30f, 0f, 0f));

            bool isClear = ShipAvoidancePlanner.IsRouteClear(
                route,
                new[] { new RadarContact(Vector3.zero, 3f, false) },
                0f,
                0.5f,
                2f);

            Assert.That(isClear, Is.False);
        }

    }
}
