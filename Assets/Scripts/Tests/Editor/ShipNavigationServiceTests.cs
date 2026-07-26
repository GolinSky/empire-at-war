using System.Reflection;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Movement
{
    public sealed class ShipNavigationServiceTests
    {
        private Vector2Range _mapRange;
        private ShipNavigationService _service;

        [SetUp]
        public void SetUp()
        {
            _mapRange = new Vector2Range();
            SetRangeValue("<Min>k__BackingField", new Vector2(-100f, -100f));
            SetRangeValue("<Max>k__BackingField", new Vector2(100f, 100f));
            _service = new ShipNavigationService();
        }

        [Test]
        public void Plan_ReservesDifferentFinalPointForSecondShip()
        {
            FakeAgent first = new FakeAgent(Vector3.zero, 0f, 8f);
            FakeAgent second = new FakeAgent(new Vector3(0f, 0f, 20f), 0f, 8f);
            _service.Register(first);
            _service.Register(second);

            ShipNavigationPlan firstPlan = Plan(first, new Vector3(50f, 0f, 0f));
            ShipNavigationPlan secondPlan = Plan(second, new Vector3(50f, 0f, 0f));

            Assert.That(secondPlan.Destination, Is.Not.EqualTo(firstPlan.Destination));
            Assert.That(
                Vector3.Distance(secondPlan.Destination, firstPlan.Destination),
                Is.GreaterThanOrEqualTo(16f));
        }

        [Test]
        public void Plan_AvoidsAnotherShipsReservedTrajectory()
        {
            FakeAgent horizontal = new FakeAgent(new Vector3(-50f, 0f, 0f), 0f, 8f);
            FakeAgent vertical = new FakeAgent(new Vector3(0f, 0f, -50f), 0f, 8f);
            _service.Register(horizontal);
            _service.Register(vertical);

            Plan(horizontal, new Vector3(50f, 0f, 0f));
            ShipNavigationPlan verticalPlan = Plan(vertical, new Vector3(0f, 0f, 50f));

            Assert.That(verticalPlan.Detour.HasValue, Is.True);
        }

        private ShipNavigationPlan Plan(FakeAgent agent, Vector3 destination)
        {
            return _service.Plan(
                agent,
                Vector3.right,
                destination,
                System.Array.Empty<RadarContact>(),
                0.5f,
                8f,
                _mapRange);
        }

        private void SetRangeValue(string fieldName, Vector2 value)
        {
            FieldInfo field = _mapRange.GetType().BaseType?.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_mapRange, value);
        }

        private sealed class FakeAgent : IShipNavigationAgent
        {
            public FakeAgent(Vector3 position, float height, float radius)
            {
                NavigationPosition = position;
                NavigationHeight = height;
                NavigationRadius = radius;
            }

            public Vector3 NavigationPosition { get; }
            public float NavigationHeight { get; }
            public float NavigationRadius { get; }
        }
    }
}
