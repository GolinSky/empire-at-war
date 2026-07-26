using System.Collections.Generic;
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
            FakeAgent first = new FakeAgent(Vector3.zero, 0f, 8f, 10f, 30f);
            FakeAgent second = new FakeAgent(
                new Vector3(0f, 0f, 20f),
                0f,
                8f,
                10f,
                30f);
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
        public void Plan_WaitsForAnotherShipsReservedTrajectory()
        {
            FakeAgent horizontal = new FakeAgent(
                new Vector3(-50f, 0f, 0f),
                0f,
                8f,
                10f,
                30f);
            FakeAgent vertical = new FakeAgent(
                new Vector3(0f, 0f, -50f),
                0f,
                8f,
                10f,
                30f);
            _service.Register(horizontal);
            _service.Register(vertical);

            Plan(horizontal, new Vector3(50f, 0f, 0f));
            ShipNavigationPlan verticalPlan = Plan(vertical, new Vector3(0f, 0f, 50f));

            Assert.That(verticalPlan.Detour.HasValue, Is.False);
            Assert.That(verticalPlan.WaitDuration, Is.GreaterThan(0f));
        }

        [Test]
        public void Plan_UsesConfiguredShipSpeed()
        {
            FakeAgent slow = new FakeAgent(Vector3.zero, 0f, 4f, 5f, 30f);
            FakeAgent fast = new FakeAgent(
                new Vector3(0f, 10f, 0f),
                10f,
                4f,
                20f,
                30f);
            _service.Register(slow);
            _service.Register(fast);

            ShipNavigationPlan slowPlan = Plan(slow, new Vector3(50f, 0f, 0f));
            ShipNavigationPlan fastPlan = Plan(fast, new Vector3(50f, 10f, 0f));

            Assert.That(
                slowPlan.MovementDuration,
                Is.EqualTo(fastPlan.MovementDuration * 4f).Within(0.01f));
        }

        [Test]
        public void Plan_DetectsCrossingBetweenBezierDebugSamples()
        {
            FakeAgent horizontal = new FakeAgent(
                new Vector3(-80f, 0f, 0f),
                0f,
                0.25f,
                10f,
                30f);
            FakeAgent vertical = new FakeAgent(
                new Vector3(8f, 0f, -80f),
                0f,
                0.25f,
                9.09f,
                30f);
            _service.Register(horizontal);
            _service.Register(vertical);

            Plan(horizontal, new Vector3(80f, 0f, 0f));
            ShipNavigationPlan verticalPlan = Plan(
                vertical,
                new Vector3(8f, 0f, 80f),
                Vector3.forward);

            Assert.That(verticalPlan.WaitDuration, Is.GreaterThan(0f));
        }

        [Test]
        public void Plan_ObstacleDetourDoesNotWaitForPreviousShipsWholeRoute()
        {
            FakeAgent centerShip = new FakeAgent(
                new Vector3(-60f, 0f, 0f),
                0f,
                4f,
                10f,
                30f);
            FakeAgent upperShip = new FakeAgent(
                new Vector3(-60f, 0f, 10f),
                0f,
                4f,
                10f,
                30f);
            RadarContact[] obstacle =
            {
                new RadarContact(Vector3.zero, 10f, false)
            };
            _service.Register(centerShip);
            _service.Register(upperShip);

            ShipNavigationPlan firstPlan = Plan(
                centerShip,
                new Vector3(60f, 0f, 0f),
                Vector3.right,
                obstacle);
            ShipNavigationPlan secondPlan = Plan(
                upperShip,
                new Vector3(60f, 0f, 10f),
                Vector3.right,
                obstacle);

            Assert.That(firstPlan.Detour.HasValue, Is.True);
            Assert.That(secondPlan.Detour.HasValue, Is.True);
            Assert.That(secondPlan.WaitDuration, Is.GreaterThan(0f));
            Assert.That(
                secondPlan.WaitDuration,
                Is.LessThan(firstPlan.MovementDuration * 0.5f));
        }

        [Test]
        public void Plan_MultiShipObstacleCommandStartsFleetBeforeFirstShipArrives()
        {
            const int SHIP_COUNT = 6;
            RadarContact[] obstacle =
            {
                new RadarContact(Vector3.zero, 10f, false)
            };
            FakeAgent[] ships = new FakeAgent[SHIP_COUNT];
            for (int i = 0; i < SHIP_COUNT; i++)
            {
                ships[i] = new FakeAgent(
                    new Vector3(-60f, 0f, i * 2f),
                    0f,
                    4f,
                    10f,
                    30f);
                _service.Register(ships[i]);
            }

            float firstMovementDuration = 0f;
            float maximumWait = 0f;
            for (int i = 0; i < SHIP_COUNT; i++)
            {
                ShipNavigationPlan plan = Plan(
                    ships[i],
                    new Vector3(60f, 0f, i * 2f),
                    Vector3.right,
                    obstacle);
                if (i == 0)
                {
                    firstMovementDuration = plan.MovementDuration;
                }

                Assert.That(plan.MovementDuration, Is.GreaterThan(0f));
                maximumWait = Mathf.Max(maximumWait, plan.WaitDuration);
            }

            Assert.That(maximumWait, Is.LessThan(firstMovementDuration));
        }

        [Test]
        public void Plan_MultiShipCommandPrunesMostSegmentComparisons()
        {
            const int SHIP_COUNT = 20;
            ShipNavigationPlan lastPlan = default;
            for (int i = 0; i < SHIP_COUNT; i++)
            {
                FakeAgent ship = new FakeAgent(
                    new Vector3(-60f, 0f, i * 2f),
                    0f,
                    4f,
                    10f,
                    30f);
                _service.Register(ship);
                lastPlan = Plan(
                    ship,
                    new Vector3(60f, 0f, i * 2f),
                    Vector3.right);
            }

            int segmentCount = lastPlan.Trajectory.Length - 1;
            int naiveComparisonCount =
                segmentCount * segmentCount * (SHIP_COUNT - 1);

            Assert.That(lastPlan.TrafficConflictChecks, Is.GreaterThan(0));
            Assert.That(
                lastPlan.TrafficConflictChecks,
                Is.LessThan(naiveComparisonCount / 2));
        }

        private ShipNavigationPlan Plan(FakeAgent agent, Vector3 destination)
        {
            return Plan(agent, destination, Vector3.right);
        }

        private ShipNavigationPlan Plan(
            FakeAgent agent,
            Vector3 destination,
            Vector3 forward)
        {
            return Plan(
                agent,
                destination,
                forward,
                System.Array.Empty<RadarContact>());
        }

        private ShipNavigationPlan Plan(
            FakeAgent agent,
            Vector3 destination,
            Vector3 forward,
            IReadOnlyList<RadarContact> contacts)
        {
            return _service.Plan(
                agent,
                forward,
                destination,
                contacts,
                0.5f,
                agent.NavigationRadius,
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
            public FakeAgent(
                Vector3 position,
                float height,
                float radius,
                float speed,
                float rotationSpeed)
            {
                NavigationPosition = position;
                NavigationHeight = height;
                NavigationRadius = radius;
                NavigationSpeed = speed;
                NavigationRotationSpeed = rotationSpeed;
            }

            public Vector3 NavigationPosition { get; }
            public float NavigationHeight { get; }
            public float NavigationRadius { get; }
            public float NavigationSpeed { get; }
            public float NavigationRotationSpeed { get; }
        }
    }
}
