using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    public interface IShipNavigationAgent
    {
        Vector3 NavigationPosition { get; }
        float NavigationHeight { get; }
        float NavigationRadius { get; }
        float NavigationSpeed { get; }
        float NavigationRotationSpeed { get; }
    }

    public readonly struct ShipNavigationPlan
    {
        public ShipNavigationPlan(
            Vector3 destination,
            Vector3? detour,
            ShipBezierRoute route,
            float turnDuration,
            float movementDuration,
            bool isStationary = false,
            bool isDeferred = false)
        {
            Destination = destination;
            Detour = detour;
            Route = route;
            TurnDuration = turnDuration;
            MovementDuration = movementDuration;
            IsStationary = isStationary;
            IsDeferred = isDeferred;
        }

        public Vector3 Destination { get; }
        public Vector3? Detour { get; }
        public ShipBezierRoute Route { get; }
        public Vector3[] Trajectory => Route.Samples;
        public float TurnDuration { get; }
        public float MovementDuration { get; }
        public bool IsStationary { get; }
        public bool IsDeferred { get; }
    }

    public interface IShipNavigationService : IService
    {
        void Register(IShipNavigationAgent agent, Vector3 initialFinalPosition);
        void Unregister(IShipNavigationAgent agent);
        void Stop(IShipNavigationAgent agent);
        void CancelPendingDestination(IShipNavigationAgent agent);
        bool IsPositionClear(Vector3 position, float navigationRadius);
        bool IsPositionClear(
            IShipNavigationAgent agent,
            Vector3 position,
            float navigationRadius);
        bool TryResolveInitialFinalPosition(
            IShipNavigationAgent agent,
            Vector3 requestedPosition,
            Vector2Range mapRange,
            float heightTolerance,
            out Vector3 resolvedPosition);

        ShipNavigationPlan Plan(
            IShipNavigationAgent agent,
            Vector3 forward,
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange,
            bool preserveCourse = false,
            bool reserveAsPending = false);
    }

    public sealed class ShipNavigationService : Service, IShipNavigationService
    {
        private const int DESTINATION_CANDIDATE_RING_COUNT = 16;
        private const int DESTINATION_CANDIDATES_PER_RING = 8;

        private readonly List<RadarContact> _mapObstacleContacts =
            new List<RadarContact>();
        private readonly IMapObstacleContactProvider _mapObstacleContactProvider;
        private readonly Dictionary<IShipNavigationAgent, int> _registrationIds =
            new Dictionary<IShipNavigationAgent, int>();
        private readonly ShipDestinationRegistry _destinationRegistry =
            new ShipDestinationRegistry();
        private readonly List<Vector3> _waypoints = new List<Vector3>();

        public ShipNavigationService(
            IMapObstacleContactProvider mapObstacleContactProvider)
        {
            _mapObstacleContactProvider = mapObstacleContactProvider;
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
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (obstacleContacts == null)
            {
                throw new ArgumentNullException(nameof(obstacleContacts));
            }

            int registrationId = GetRegistrationId(agent);
            Vector3 origin = agent.NavigationPosition;
            BuildNavigationContacts(obstacleContacts);
            AddIdleAgentContacts(agent);

            using (ShipPathGrid pathGrid = new ShipPathGrid(
                       _mapObstacleContacts, clearance, mapRange))
            {
                float candidateSpacing = agent.NavigationRadius * 2f;
                for (int candidateIndex = 0;
                     candidateIndex <=
                     DESTINATION_CANDIDATE_RING_COUNT * DESTINATION_CANDIDATES_PER_RING;
                     candidateIndex++)
                {
                    Vector3 candidate = GetDestinationCandidate(
                        requestedDestination,
                        candidateSpacing,
                        candidateIndex);
                    candidate = ShipAvoidancePlanner.ClampToMap(
                        candidate,
                        mapRange,
                        clearance);
                    ShipAvoidancePlanner.TryResolveDestination(
                        candidate,
                        origin,
                        _mapObstacleContacts,
                        agent.NavigationHeight,
                        heightTolerance,
                        clearance,
                        mapRange,
                        out Vector3 destination);
                    if (!ShipAvoidancePlanner.IsPointClear(
                            destination,
                            _mapObstacleContacts,
                            agent.NavigationHeight,
                            heightTolerance,
                            clearance) ||
                        !_destinationRegistry.HasClearance(
                            registrationId,
                            ToFormationPoint(destination),
                            clearance))
                    {
                        continue;
                    }

                    ShipRoutePlan routePlan = ShipRoutePlanner.Build(
                        agent,
                        forward,
                        destination,
                        _mapObstacleContacts,
                        pathGrid,
                        _waypoints,
                        heightTolerance,
                        clearance);
                    if (routePlan.IsStationary)
                    {
                        continue;
                    }

                    bool isDeferred = reserveAsPending ||
                        (preserveCourse &&
                         routePlan.TurnDuration > Mathf.Epsilon);
                    if (isDeferred)
                    {
                        _destinationRegistry.ReservePendingFinalPosition(
                            registrationId,
                            ToFormationPoint(routePlan.Destination));
                    }
                    else
                    {
                        _destinationRegistry.CommitActiveFinalPosition(
                            registrationId,
                            ToFormationPoint(routePlan.Destination));
                    }

                    float movementDuration =
                        routePlan.Route.Length /
                        Mathf.Max(agent.NavigationSpeed, Mathf.Epsilon);
                    return new ShipNavigationPlan(
                        routePlan.Destination,
                        routePlan.Detour,
                        routePlan.Route,
                        routePlan.TurnDuration,
                        movementDuration,
                        false,
                        isDeferred);
                }
            }

            if (!preserveCourse)
            {
                _destinationRegistry.Stop(registrationId);
            }

            ShipBezierRoute stationaryRoute = ShipBezierPath.BuildDirectRoute(
                origin,
                forward,
                origin);
            return new ShipNavigationPlan(
                origin,
                null,
                stationaryRoute,
                0f,
                0f,
                true,
                preserveCourse || reserveAsPending);
        }

        public void Register(
            IShipNavigationAgent agent,
            Vector3 initialFinalPosition)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (_registrationIds.ContainsKey(agent))
            {
                throw new InvalidOperationException(
                    "The ship navigation agent is already registered.");
            }

            int registrationId = _destinationRegistry.Register(
                () => ToFormationPoint(agent.NavigationPosition),
                agent.NavigationRadius,
                ToFormationPoint(initialFinalPosition));
            _registrationIds.Add(agent, registrationId);
        }

        public void Unregister(IShipNavigationAgent agent)
        {
            int registrationId = GetRegistrationId(agent);
            _destinationRegistry.Unregister(registrationId);
            _registrationIds.Remove(agent);
        }

        public void Stop(IShipNavigationAgent agent)
        {
            _destinationRegistry.Stop(GetRegistrationId(agent));
        }

        public void CancelPendingDestination(IShipNavigationAgent agent)
        {
            _destinationRegistry.CancelPendingFinalPosition(
                GetRegistrationId(agent));
        }

        public bool IsPositionClear(Vector3 position, float navigationRadius)
        {
            return _destinationRegistry.HasClearance(
                ToFormationPoint(position),
                navigationRadius);
        }

        public bool IsPositionClear(
            IShipNavigationAgent agent,
            Vector3 position,
            float navigationRadius)
        {
            return _destinationRegistry.HasClearance(
                GetRegistrationId(agent),
                ToFormationPoint(position),
                navigationRadius);
        }

        public bool TryResolveInitialFinalPosition(
            IShipNavigationAgent agent,
            Vector3 requestedPosition,
            Vector2Range mapRange,
            float heightTolerance,
            out Vector3 resolvedPosition)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (mapRange == null)
            {
                throw new ArgumentNullException(nameof(mapRange));
            }

            BuildNavigationContacts(Array.Empty<RadarContact>());
            float clearance = agent.NavigationRadius;
            for (int candidateIndex = 0;
                 candidateIndex <=
                 DESTINATION_CANDIDATE_RING_COUNT * DESTINATION_CANDIDATES_PER_RING;
                 candidateIndex++)
            {
                Vector3 candidate = GetDestinationCandidate(
                    requestedPosition,
                    clearance * 2f,
                    candidateIndex);
                candidate = ShipAvoidancePlanner.ClampToMap(
                    candidate,
                    mapRange,
                    clearance);
                ShipAvoidancePlanner.TryResolveDestination(
                    candidate,
                    requestedPosition,
                    _mapObstacleContacts,
                    agent.NavigationHeight,
                    heightTolerance,
                    clearance,
                    mapRange,
                    out Vector3 destination);
                if (ShipAvoidancePlanner.IsPointClear(destination, _mapObstacleContacts,
                        agent.NavigationHeight, heightTolerance, clearance) &&
                    _destinationRegistry.HasClearance(
                        ToFormationPoint(destination),
                        clearance))
                {
                    resolvedPosition = destination;
                    return true;
                }
            }

            resolvedPosition = default;
            return false;
        }

        private void BuildNavigationContacts(
            IReadOnlyList<RadarContact> radarContacts)
        {
            _mapObstacleContactProvider.CopyContacts(_mapObstacleContacts);
            _mapObstacleContacts.RemoveAll(contact => contact.IsShip);
            for (int i = 0; i < radarContacts.Count; i++)
            {
                RadarContact contact = radarContacts[i];
                if (!contact.IsShip)
                {
                    AddContactIfUnique(contact);
                }
            }
        }

        // Idle ships are treated as static obstacles for route planning; moving
        // ships are kept apart by the final-position reservations instead.
        private void AddIdleAgentContacts(IShipNavigationAgent plannedAgent)
        {
            foreach (KeyValuePair<IShipNavigationAgent, int> pair in _registrationIds)
            {
                if (pair.Key != plannedAgent &&
                    _destinationRegistry.IsIdle(pair.Value))
                {
                    _mapObstacleContacts.Add(new RadarContact(
                        pair.Key.NavigationPosition,
                        pair.Key.NavigationRadius,
                        false));
                }
            }
        }

        private void AddContactIfUnique(RadarContact contact)
        {
            if (!ContainsEquivalentContact(contact))
            {
                _mapObstacleContacts.Add(contact);
            }
        }

        private bool ContainsEquivalentContact(RadarContact contact)
        {
            for (int i = 0; i < _mapObstacleContacts.Count; i++)
            {
                RadarContact existing = _mapObstacleContacts[i];
                if (existing.Position == contact.Position &&
                    Mathf.Approximately(existing.Radius, contact.Radius) &&
                    existing.IsShip == contact.IsShip)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetRegistrationId(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (!_registrationIds.TryGetValue(agent, out int registrationId))
            {
                throw new InvalidOperationException(
                    "The ship navigation agent is not registered.");
            }

            return registrationId;
        }

        private static Vector3 GetDestinationCandidate(
            Vector3 requestedDestination,
            float spacing,
            int candidateIndex)
        {
            if (candidateIndex == 0)
            {
                return requestedDestination;
            }

            int ring = (candidateIndex - 1) / DESTINATION_CANDIDATES_PER_RING + 1;
            int slot = (candidateIndex - 1) % DESTINATION_CANDIDATES_PER_RING;
            float angle = slot * Mathf.PI * 2f / DESTINATION_CANDIDATES_PER_RING;
            return requestedDestination + new Vector3(
                Mathf.Cos(angle) * spacing * ring,
                0f,
                Mathf.Sin(angle) * spacing * ring);
        }

        private static FormationPoint ToFormationPoint(Vector3 position)
        {
            return new FormationPoint(position.x, position.z);
        }
    }
}
