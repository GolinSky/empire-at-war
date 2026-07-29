using System;
using System.Collections.Generic;
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
            float waitDuration,
            float movementDuration,
            int trafficConflictChecks)
        {
            Destination = destination;
            Detour = detour;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            WaitDuration = waitDuration;
            MovementDuration = movementDuration;
            TrafficConflictChecks = trafficConflictChecks;
        }

        public Vector3 Destination { get; }
        public Vector3? Detour { get; }
        public ShipBezierRoute Route { get; }
        public Vector3[] Trajectory => Route.Samples;
        public float WaitDuration { get; }
        public float MovementDuration { get; }
        public int TrafficConflictChecks { get; }
        public float TotalDuration => WaitDuration + MovementDuration;
    }

    public interface IShipNavigationService : IService
    {
        void Register(IShipNavigationAgent agent);
        void Unregister(IShipNavigationAgent agent);
        ShipNavigationPlan Plan(
            IShipNavigationAgent agent,
            Vector3 forward,
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange);
        void ClearPlan(IShipNavigationAgent agent);
    }

    public sealed class ShipNavigationService : Service, IShipNavigationService
    {
        private readonly HashSet<IShipNavigationAgent> _agents =
            new HashSet<IShipNavigationAgent>();
        private readonly List<RadarContact> _mapObstacleContacts =
            new List<RadarContact>();
        private readonly IMapObstacleContactProvider _mapObstacleContactProvider;

        public ShipNavigationService(
            IMapObstacleContactProvider mapObstacleContactProvider)
        {
            _mapObstacleContactProvider = mapObstacleContactProvider ??
                throw new ArgumentNullException(
                    nameof(mapObstacleContactProvider));
        }

        public void Register(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            _agents.Add(agent);
        }

        public void Unregister(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            _agents.Remove(agent);
        }

        public ShipNavigationPlan Plan(
            IShipNavigationAgent agent,
            Vector3 forward,
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange)
        {
            if (!_agents.Contains(agent))
            {
                throw new InvalidOperationException(
                    "Ship navigation agent must be registered before planning.");
            }

            if (obstacleContacts == null)
            {
                throw new ArgumentNullException(nameof(obstacleContacts));
            }

            Vector3 origin = agent.NavigationPosition;
            _mapObstacleContactProvider.CopyContacts(_mapObstacleContacts);
            for (int i = 0; i < obstacleContacts.Count; i++)
            {
                RadarContact contact = obstacleContacts[i];
                if (!contact.IsShip &&
                    !ContainsEquivalentObstacle(contact))
                {
                    _mapObstacleContacts.Add(contact);
                }
            }

            ShipAvoidancePlanner.TryResolveDestination(
                requestedDestination,
                origin,
                _mapObstacleContacts,
                agent.NavigationHeight,
                heightTolerance,
                clearance,
                mapRange,
                out Vector3 destination);
            float minimumTurnRadius =
                Mathf.Max(
                    agent.NavigationRadius,
                    ShipRotationKinematics.CalculateMinimumTurnRadius(
                        Mathf.Max(agent.NavigationSpeed, 0f),
                        Mathf.Max(
                            agent.NavigationRotationSpeed,
                            Mathf.Epsilon)));
            Vector3? detour = null;
            ShipBezierRoute route;
            if (ShipAvoidancePlanner.TryCalculateDetour(
                    origin,
                    destination,
                    _mapObstacleContacts,
                    agent.NavigationHeight,
                    heightTolerance,
                    clearance,
                    mapRange,
                    out Vector3 avoidancePoint))
            {
                detour = avoidancePoint;
                route = ShipBezierPath.BuildAvoidanceRoute(
                    origin,
                    forward,
                    avoidancePoint,
                    destination);
            }
            else
            {
                route = ShipBezierPath.BuildDirectRoute(
                    origin,
                    forward,
                    destination,
                    minimumTurnRadius);
            }

            float movementDuration =
                route.Length / Mathf.Max(agent.NavigationSpeed, Mathf.Epsilon);
            return new ShipNavigationPlan(
                destination,
                detour,
                route,
                0f,
                movementDuration,
                0);
        }

        public void ClearPlan(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }
        }

        private bool ContainsEquivalentObstacle(RadarContact contact)
        {
            for (int i = 0; i < _mapObstacleContacts.Count; i++)
            {
                RadarContact existing = _mapObstacleContacts[i];
                if (existing.Position == contact.Position &&
                    Mathf.Approximately(existing.Radius, contact.Radius))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
