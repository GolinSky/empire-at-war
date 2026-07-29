using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Movement
{
    public sealed class ShipNavigationServiceTests
    {
        private Vector2Range _mapRange;

        [SetUp]
        public void SetUp()
        {
            _mapRange = new Vector2Range();
            SetRangeValue("<Min>k__BackingField", new Vector2(-100f, -100f));
            SetRangeValue("<Max>k__BackingField", new Vector2(100f, 100f));
        }

        [Test]
        public void Plan_UsesConfiguredShipSpeed()
        {
            FakeAgent slow = new FakeAgent(Vector3.zero, 0f, 4f, 5f, 30f);
            FakeAgent fast = new FakeAgent(Vector3.zero, 0f, 4f, 20f, 30f);
            ShipNavigationService slowService = CreateService(slow);
            ShipNavigationService fastService = CreateService(fast);

            ShipNavigationPlan slowPlan = Plan(
                slowService,
                slow,
                new Vector3(50f, 0f, 0f));
            ShipNavigationPlan fastPlan = Plan(
                fastService,
                fast,
                new Vector3(50f, 0f, 0f));

            Assert.That(
                slowPlan.MovementDuration,
                Is.EqualTo(fastPlan.MovementDuration * 4f).Within(0.01f));
        }

        [Test]
        public void Plan_AvoidsMapObstacleCrossingRoute()
        {
            FakeAgent clearAgent = new FakeAgent(
                Vector3.zero,
                0f,
                5f,
                10f,
                30f);
            FakeAgent obstructedAgent = new FakeAgent(
                Vector3.zero,
                0f,
                5f,
                10f,
                30f);
            ShipNavigationService clearService = CreateService(clearAgent);
            ShipNavigationService obstructedService =
                CreateService(obstructedAgent);
            Vector3 destination = new Vector3(50f, 0f, 0f);

            ShipNavigationPlan clearPlan = Plan(
                clearService,
                clearAgent,
                destination);
            ShipNavigationPlan obstructedPlan = Plan(
                obstructedService,
                obstructedAgent,
                destination,
                new[]
                {
                    new RadarContact(
                        new Vector3(20f, 0f, 0f),
                        10f,
                        false)
                });

            Assert.That(
                obstructedPlan.Destination,
                Is.EqualTo(clearPlan.Destination));
            Assert.That(obstructedPlan.Detour.HasValue, Is.True);
            Assert.That(obstructedPlan.WaitDuration, Is.Zero);
            Assert.That(obstructedPlan.TrafficConflictChecks, Is.Zero);
            Assert.That(
                obstructedPlan.Route.Length,
                Is.GreaterThan(clearPlan.Route.Length));
            Assert.That(
                Mathf.Abs(obstructedPlan.Detour.Value.z),
                Is.GreaterThanOrEqualTo(20f));
            for (int i = 0; i < obstructedPlan.Trajectory.Length; i++)
            {
                Vector3 sample = obstructedPlan.Trajectory[i];
                float planarDistance = Vector2.Distance(
                    new Vector2(sample.x, sample.z),
                    new Vector2(20f, 0f));
                Assert.That(
                    planarDistance,
                    Is.GreaterThanOrEqualTo(14.999f),
                    $"Trajectory sample {i} entered the obstacle clearance.");
            }
        }

        [Test]
        public void Plan_AvoidsStaticMapObstacleBeforeRadarContact()
        {
            FakeAgent agent = new FakeAgent(
                Vector3.zero,
                0f,
                5f,
                10f,
                30f);
            RadarContact staticObstacle = new RadarContact(
                new Vector3(20f, 0f, 0f),
                10f,
                false);
            ShipNavigationService service = CreateService(
                agent,
                new[] { staticObstacle });

            ShipNavigationPlan plan = Plan(
                service,
                agent,
                new Vector3(50f, 0f, 0f));

            Assert.That(plan.Detour.HasValue, Is.True);
        }

        [Test]
        public void Plan_AvoidsShipContactAtSameHeight()
        {
            FakeAgent agent = new FakeAgent(
                Vector3.zero,
                0f,
                5f,
                10f,
                30f);
            ShipNavigationService service = CreateService(agent);

            ShipNavigationPlan plan = Plan(
                service,
                agent,
                new Vector3(50f, 0f, 0f),
                new[]
                {
                    new RadarContact(
                        new Vector3(20f, 0f, 0f),
                        10f,
                        true)
                });

            Assert.That(plan.Detour.HasValue, Is.True);
        }

        [Test]
        public void Plan_ResolvesDestinationInsideMapObstacle()
        {
            FakeAgent agent = new FakeAgent(
                Vector3.zero,
                0f,
                5f,
                10f,
                30f);
            ShipNavigationService service = CreateService(agent);
            Vector3 obstacleCenter = new Vector3(20f, 0f, 0f);

            ShipNavigationPlan plan = Plan(
                service,
                agent,
                obstacleCenter,
                new[]
                {
                    new RadarContact(obstacleCenter, 10f, false)
                });

            Assert.That(
                Vector3.Distance(plan.Destination, obstacleCenter),
                Is.GreaterThanOrEqualTo(14.999f));
        }

        [Test]
        public void Plan_ReservesDistinctFinalDestinations()
        {
            FakeAgent first = new FakeAgent(
                Vector3.back * 20f,
                0f,
                8f,
                10f,
                30f);
            FakeAgent second = new FakeAgent(
                Vector3.forward * 20f,
                0f,
                8f,
                10f,
                30f);
            ShipNavigationService service = CreateService();
            service.Register(first);
            service.Register(second);
            Vector3 destination = new Vector3(50f, 0f, 0f);

            ShipNavigationPlan firstPlan = Plan(service, first, destination);
            ShipNavigationPlan secondPlan = Plan(service, second, destination);

            Assert.That(secondPlan.Destination, Is.Not.EqualTo(firstPlan.Destination));
            Assert.That(
                Vector3.Distance(
                    secondPlan.Destination,
                    firstPlan.Destination),
                Is.GreaterThanOrEqualTo(
                    first.NavigationRadius +
                    second.NavigationRadius -
                    0.001f));
        }

        [Test]
        public void Plan_CrossingReservedTrajectory_UsesBoundedStartDelay()
        {
            FakeAgent horizontalShip = new FakeAgent(
                Vector3.left * 40f,
                0f,
                4f,
                10f,
                90f);
            FakeAgent verticalShip = new FakeAgent(
                Vector3.back * 40f,
                0f,
                4f,
                10f,
                90f);
            ShipNavigationService service = CreateService();
            service.Register(horizontalShip);
            service.Register(verticalShip);

            service.Plan(
                horizontalShip,
                Vector3.right,
                Vector3.right * 40f,
                System.Array.Empty<RadarContact>(),
                0.5f,
                horizontalShip.NavigationRadius,
                _mapRange);
            ShipNavigationPlan verticalPlan = service.Plan(
                verticalShip,
                Vector3.forward,
                Vector3.forward * 40f,
                System.Array.Empty<RadarContact>(),
                0.5f,
                verticalShip.NavigationRadius,
                _mapRange);

            Assert.That(verticalPlan.TrafficConflictChecks, Is.GreaterThan(0));
            Assert.That(verticalPlan.WaitDuration, Is.GreaterThan(0f));
            Assert.That(verticalPlan.WaitDuration, Is.LessThanOrEqualTo(0.5f));
        }

        [Test]
        public void Plan_CoMovingReservedTrajectory_DoesNotDelaySecondShip()
        {
            FakeAgent leadingShip = new FakeAgent(
                Vector3.zero,
                0f,
                4f,
                10f,
                90f);
            FakeAgent followingShip = new FakeAgent(
                Vector3.back * 20f,
                0f,
                4f,
                10f,
                90f);
            ShipNavigationService service = CreateService();
            service.Register(leadingShip);
            service.Register(followingShip);

            service.Plan(
                leadingShip,
                Vector3.forward,
                Vector3.forward * 80f,
                System.Array.Empty<RadarContact>(),
                0.5f,
                leadingShip.NavigationRadius,
                _mapRange);
            ShipNavigationPlan followingPlan = service.Plan(
                followingShip,
                Vector3.forward,
                Vector3.forward * 60f,
                System.Array.Empty<RadarContact>(),
                0.5f,
                followingShip.NavigationRadius,
                _mapRange);

            Assert.That(followingPlan.WaitDuration, Is.Zero);
        }

        [Test]
        public void Plan_DestinationBehind_HasNoWaitAndStartsForward()
        {
            FakeAgent ship = new FakeAgent(Vector3.zero, 0f, 5f, 10f, 30f);
            ShipNavigationService service = CreateService(ship);

            ShipNavigationPlan plan = service.Plan(
                ship,
                Vector3.forward,
                Vector3.back * 40f,
                System.Array.Empty<RadarContact>(),
                0.5f,
                ship.NavigationRadius,
                _mapRange);
            Vector3 earlyPosition =
                plan.Route.EvaluateNormalizedDistance(0.05f, out _);

            Assert.That(plan.WaitDuration, Is.Zero);
            Assert.That(plan.TurnDuration, Is.Zero);
            Assert.That(plan.Detour.HasValue, Is.False);
            Assert.That(
                Vector3.Dot(earlyPosition, Vector3.forward),
                Is.GreaterThan(0f));
            Assert.That(
                Mathf.Abs(plan.Trajectory[plan.Trajectory.Length / 2].x),
                Is.GreaterThan(1f));
        }

        [Test]
        public void Plan_ObstacleAheadAndDestinationBehind_TurnsInPlace()
        {
            FakeAgent ship = new FakeAgent(
                Vector3.zero,
                0f,
                2f,
                10f,
                30f);
            RadarContact obstacle = new RadarContact(
                Vector3.forward * 8f,
                3f,
                false);
            ShipNavigationService service = CreateService(
                ship,
                new[] { obstacle });

            ShipNavigationPlan plan = service.Plan(
                ship,
                Vector3.forward,
                Vector3.back * 20f,
                System.Array.Empty<RadarContact>(),
                0.5f,
                ship.NavigationRadius,
                _mapRange);
            Vector3 earlyPosition =
                plan.Route.EvaluateNormalizedDistance(0.05f, out _);

            Assert.That(plan.TurnDuration, Is.GreaterThan(0f));
            Assert.That(
                Vector3.Dot(earlyPosition, Vector3.forward),
                Is.LessThanOrEqualTo(0f));
            Assert.That(
                ShipAvoidancePlanner.IsRouteClear(
                    plan.Route,
                    new[] { obstacle },
                    ship.NavigationHeight,
                    0.5f,
                    ship.NavigationRadius),
                Is.True);
        }

        [Test]
        public void HandleRadarContacts_AfterRelease_DoesNotPlan()
        {
            GameObject gameObject = new GameObject(
                nameof(HandleRadarContacts_AfterRelease_DoesNotPlan));
            try
            {
                ShipMoveComponent component =
                    gameObject.AddComponent<ShipMoveComponent>();
                FieldInfo releasedField = typeof(ShipMoveComponent).GetField(
                    "_isReleased",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(releasedField, Is.Not.Null);
                releasedField.SetValue(component, true);

                Assert.DoesNotThrow(
                    () => component.HandleRadarContacts(
                        System.Array.Empty<RadarContact>()));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private ShipNavigationService CreateService(
            FakeAgent agent,
            IReadOnlyList<RadarContact> staticObstacles = null)
        {
            ShipNavigationService service = CreateService(staticObstacles);
            service.Register(agent);
            return service;
        }

        private static ShipNavigationService CreateService(
            IReadOnlyList<RadarContact> staticObstacles = null)
        {
            return new ShipNavigationService(
                new FakeMapObstacleContactProvider(
                    staticObstacles ??
                    System.Array.Empty<RadarContact>()));
        }

        private ShipNavigationPlan Plan(
            ShipNavigationService service,
            FakeAgent agent,
            Vector3 destination,
            IReadOnlyList<RadarContact> contacts = null)
        {
            return service.Plan(
                agent,
                Vector3.right,
                destination,
                contacts ?? System.Array.Empty<RadarContact>(),
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

        private sealed class FakeMapObstacleContactProvider :
            IMapObstacleContactProvider
        {
            private readonly IReadOnlyList<RadarContact> _contacts;

            public FakeMapObstacleContactProvider(
                IReadOnlyList<RadarContact> contacts)
            {
                _contacts = contacts;
            }

            public string Id => nameof(FakeMapObstacleContactProvider);

            public void CopyContacts(List<RadarContact> destination)
            {
                destination.Clear();
                for (int i = 0; i < _contacts.Count; i++)
                {
                    destination.Add(_contacts[i]);
                }
            }
        }
    }
}
