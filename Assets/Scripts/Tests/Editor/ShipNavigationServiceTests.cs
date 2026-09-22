using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Mvc;
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
            ShipNavigationService slowService = CreateService();
            ShipNavigationService fastService = CreateService();

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
            ShipNavigationService clearService = CreateService();
            ShipNavigationService obstructedService =
                CreateService();
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
                new[] { staticObstacle });

            ShipNavigationPlan plan = Plan(
                service,
                agent,
                new Vector3(50f, 0f, 0f));

            Assert.That(plan.Detour.HasValue, Is.True);
        }

        [Test]
        public void Plan_IgnoresShipContactsAheadAndAtOrigin()
        {
            FakeAgent agent = new FakeAgent(
                Vector3.zero,
                0f,
                5f,
                10f,
                30f);
            ShipNavigationService service = CreateService();

            ShipNavigationPlan plan = Plan(
                service,
                agent,
                new Vector3(50f, 0f, 0f),
                new[]
                {
                    new RadarContact(
                        new Vector3(20f, 0f, 0f),
                        10f,
                        true),
                    new RadarContact(Vector3.zero, 10f, true)
                });

            Assert.That(plan.IsStationary, Is.False);
            Assert.That(plan.Destination, Is.EqualTo(new Vector3(50f, 0f, 0f)));
            Assert.That(plan.Detour.HasValue, Is.False);
        }

        [Test]
        public void Plan_SelectsClearDestinationWhenFinalPositionIsReserved()
        {
            FakeAgent ship = new FakeAgent(
                Vector3.zero,
                0f,
                4f,
                10f,
                90f);
            ShipNavigationService service = CreateService();
            Vector3 destination = new Vector3(40f, 0f, 0f);
            FakeAgent occupyingShip = new FakeAgent(
                destination,
                0f,
                4f,
                10f,
                90f);
            service.Register(occupyingShip, destination);

            ShipNavigationPlan plan = Plan(
                service,
                ship,
                destination);

            Assert.That(plan.IsStationary, Is.False);
            Assert.That(plan.Destination, Is.Not.EqualTo(destination));
            Assert.That(plan.Detour.HasValue, Is.False);
        }

        [Test]
        public void TryResolveInitialFinalPosition_ShiftsOccupiedSpawnPosition()
        {
            ShipNavigationService service = CreateService();
            FakeAgent occupyingShip = new FakeAgent(
                Vector3.zero,
                0f,
                4f,
                10f,
                90f);
            FakeAgent incomingShip = new FakeAgent(
                Vector3.right * 80f,
                0f,
                4f,
                10f,
                90f);
            service.Register(occupyingShip, Vector3.zero);

            bool resolved = service.TryResolveInitialFinalPosition(
                incomingShip,
                Vector3.zero,
                _mapRange,
                0.5f,
                out Vector3 position);

            Assert.That(resolved, Is.True);
            Assert.That(position, Is.Not.EqualTo(Vector3.zero));
            Assert.DoesNotThrow(() => service.Register(incomingShip, position));
        }

        [Test]
        public void Plan_CrossingShipContacts_KeepDirectDestinations()
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
            Vector3 horizontalDestination = Vector3.right * 40f;
            Vector3 verticalDestination = Vector3.forward * 40f;
            service.Register(horizontalShip, horizontalShip.NavigationPosition);
            service.Register(verticalShip, verticalShip.NavigationPosition);

            ShipNavigationPlan horizontalPlan = service.Plan(
                horizontalShip,
                Vector3.right,
                horizontalDestination,
                new[]
                {
                    new RadarContact(
                        verticalShip.NavigationPosition,
                        verticalShip.NavigationRadius,
                        true)
                },
                0.5f,
                horizontalShip.NavigationRadius,
                _mapRange);
            ShipNavigationPlan verticalPlan = service.Plan(
                verticalShip,
                Vector3.forward,
                verticalDestination,
                new[]
                {
                    new RadarContact(
                        horizontalShip.NavigationPosition,
                        horizontalShip.NavigationRadius,
                        true)
                },
                0.5f,
                verticalShip.NavigationRadius,
                _mapRange);

            Assert.That(horizontalPlan.Destination, Is.EqualTo(horizontalDestination));
            Assert.That(verticalPlan.Destination, Is.EqualTo(verticalDestination));
            Assert.That(horizontalPlan.Detour.HasValue, Is.False);
            Assert.That(verticalPlan.Detour.HasValue, Is.False);
            Assert.That(horizontalPlan.TurnDuration, Is.Zero);
            Assert.That(verticalPlan.TurnDuration, Is.Zero);
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
            ShipNavigationService service = CreateService();
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
        public void Plan_DestinationBehind_StartsForward()
        {
            FakeAgent ship = new FakeAgent(Vector3.zero, 0f, 5f, 10f, 30f);
            ShipNavigationService service = CreateService();
            service.Register(ship, ship.NavigationPosition);

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
                new[] { obstacle });
            service.Register(ship, ship.NavigationPosition);

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

        [Test]
        public void HandleRadarContacts_WithUnchangedContacts_RetriesBlockedTarget()
        {
            GameObject gameObject = new GameObject(
                nameof(HandleRadarContacts_WithUnchangedContacts_RetriesBlockedTarget));
            try
            {
                ShipMoveComponent component = CreateReadyComponent(
                    gameObject,
                    out RecordingShipNavigationService navigationService);
                Vector3 destination = new Vector3(25f, 0f, 0f);
                SetPrivateField(
                    component,
                    "_blockedTargetPosition",
                    destination);

                component.HandleRadarContacts(System.Array.Empty<RadarContact>());

                Assert.That(navigationService.PlanCallCount, Is.EqualTo(1));
                Assert.That(navigationService.LastDestination, Is.EqualTo(destination));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SetTargetPosition_RepeatedBlockedDestination_RetriesTarget()
        {
            GameObject gameObject = new GameObject(
                nameof(SetTargetPosition_RepeatedBlockedDestination_RetriesTarget));
            try
            {
                ShipMoveComponent component = CreateReadyComponent(
                    gameObject,
                    out RecordingShipNavigationService navigationService);
                Vector3 destination = new Vector3(25f, 0f, 0f);
                SetPrivateField(component, "_blockedTargetPosition", destination);
                MethodInfo setTargetPosition = typeof(ShipMoveComponent).GetMethod(
                    "SetTargetPosition",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(setTargetPosition, Is.Not.Null);

                setTargetPosition.Invoke(component, new object[] { destination, false });

                Assert.That(navigationService.PlanCallCount, Is.EqualTo(1));
                Assert.That(navigationService.LastDestination, Is.EqualTo(destination));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RepeatedMoveDuringHyperspace_KeepsPendingReservation()
        {
            GameObject gameObject = new GameObject(nameof(RepeatedMoveDuringHyperspace_KeepsPendingReservation));
            try
            {
                ShipMoveComponent component = CreateReadyComponent(gameObject,
                    out RecordingShipNavigationService navigationService);
                SetPrivateField(component, "_isNavigationReady", false);
                Vector3 destination = new Vector3(25f, 0f, 0f);
                component.MoveToPosition(destination);
                int cancellations = navigationService.PendingCancellationCount;

                component.MoveToPosition(destination);

                Assert.That(navigationService.PlanCallCount, Is.EqualTo(1));
                Assert.That(navigationService.PendingCancellationCount, Is.EqualTo(cancellations));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void StopDuringHyperspace_ClearsBlockedOrderAndPendingReservation()
        {
            GameObject gameObject = new GameObject(nameof(StopDuringHyperspace_ClearsBlockedOrderAndPendingReservation));
            try
            {
                ShipMoveComponent component = CreateReadyComponent(gameObject,
                    out RecordingShipNavigationService navigationService);
                SetPrivateField(component, "_isNavigationReady", false);
                SetPrivateField(component, "_pendingTargetPosition", Vector3.right * 25f);
                SetPrivateField(component, "_blockedTargetPosition", Vector3.right * 25f);

                component.Stop();
                component.HandleRadarContacts(System.Array.Empty<RadarContact>());

                Assert.That(navigationService.PendingCancellationCount, Is.EqualTo(1));
                Assert.That(navigationService.PlanCallCount, Is.Zero);
                Assert.That(component.IsBlocked, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
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
            service.Register(agent, agent.NavigationPosition);
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

        private ShipMoveComponent CreateReadyComponent(
            GameObject gameObject,
            out RecordingShipNavigationService navigationService)
        {
            ShipMoveComponent component =
                gameObject.AddComponent<ShipMoveComponent>();
            MethodInfo setModel = typeof(MonoComponent<ShipMoveModel>).GetMethod(
                "SetModel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(setModel, Is.Not.Null);
            setModel.Invoke(
                component,
                new object[] { new ShipMoveModel(new FakeShipMoveData()) });
            navigationService = new RecordingShipNavigationService();
            GameObject bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(gameObject.transform);
            LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
            System.Type tweenPlayerType = typeof(ShipMoveComponent).Assembly.GetType(
                "EmpireAtWar.Components.Ship.Movement.ShipMovementTweenPlayer");
            Assert.That(tweenPlayerType, Is.Not.Null);
            ConstructorInfo tweenPlayerConstructor = tweenPlayerType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(Transform),
                    typeof(Transform),
                    typeof(LineRenderer),
                    typeof(Ease),
                    typeof(Ease)
                },
                null);
            Assert.That(tweenPlayerConstructor, Is.Not.Null);
            object tweenPlayer = tweenPlayerConstructor.Invoke(new object[]
            {
                gameObject.transform,
                bodyObject.transform,
                lineRenderer,
                Ease.Linear,
                Ease.Linear
            });
            SetPrivateField(component, "_mapModel", new FakeMapModel(_mapRange));
            SetPrivateField(component, "_movementMediator", new FakeShipMovementMediator());
            SetPrivateField(component, "_shipNavigationService", navigationService);
            SetPrivateField(component, "_tweenPlayer", tweenPlayer);
            SetPrivateField(component, "_isNavigationReady", true);
            return component;
        }

        private static void SetPrivateField(
            ShipMoveComponent component,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(ShipMoveComponent).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(component, value);
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

        private sealed class FakeMapModel : IMapModelObserver
        {
            public FakeMapModel(Vector2Range sizeRange)
            {
                SizeRange = sizeRange;
            }

            public Vector2Range SizeRange { get; }

            public Vector3 GetStationPosition(FactionType factionType)
            {
                return Vector3.zero;
            }
        }

        private sealed class FakeShipMovementMediator : IShipMovementMediator
        {
            public void OnPositionChanged(Vector3 position)
            {
            }

            public void OnLookAtTarget(Vector3 targetPosition)
            {
            }

            public void OnStopped()
            {
            }
        }

        private sealed class FakeShipMoveData : IShipMoveData
        {
            public float Speed => 10f;
            public float Height => 0f;
            public float RotationSpeed => 30f;
            public float HyperSpaceDuration => 1f;
            public float BodyRotationMaxAngle => 10f;
            public float NavigationRadius => 5f;
        }

        private sealed class RecordingShipNavigationService : IShipNavigationService
        {
            public string Id => nameof(RecordingShipNavigationService);
            public int PlanCallCount { get; private set; }
            public int PendingCancellationCount { get; private set; }
            public Vector3 LastDestination { get; private set; }

            public void Register(
                IShipNavigationAgent agent,
                Vector3 initialFinalPosition)
            {
            }

            public void Unregister(IShipNavigationAgent agent)
            {
            }

            public void Stop(IShipNavigationAgent agent)
            {
            }

            public void CancelPendingDestination(IShipNavigationAgent agent)
            {
                PendingCancellationCount++;
            }

            public bool IsPositionClear(Vector3 position, float navigationRadius)
            {
                return true;
            }

            public bool IsPositionClear(
                IShipNavigationAgent agent,
                Vector3 position,
                float navigationRadius)
            {
                return true;
            }

            public bool TryResolveInitialFinalPosition(
                IShipNavigationAgent agent,
                Vector3 requestedPosition,
                Vector2Range mapRange,
                float heightTolerance,
                out Vector3 resolvedPosition)
            {
                resolvedPosition = requestedPosition;
                return true;
            }

            public ShipNavigationPlan Plan(
                IShipNavigationAgent agent,
                Vector3 forward,
                Vector3 requestedDestination,
                IReadOnlyList<RadarContact> obstacleContacts,
                float heightTolerance,
                float clearance,
                Vector2Range mapRange,
                bool preserveCourse = false,
                bool reserveAsPending = false)
            {
                PlanCallCount++;
                LastDestination = requestedDestination;
                return new ShipNavigationPlan(
                    requestedDestination,
                    null,
                    ShipBezierPath.BuildDirectRoute(
                        agent.NavigationPosition,
                        forward,
                        requestedDestination),
                    0f,
                    0f,
                    isDeferred: true);
            }
        }
    }
}
