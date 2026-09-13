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
            float turnDuration,
            float waitDuration,
            float movementDuration,
            int trafficConflictChecks,
            bool isStationary = false,
            bool isDeferred = false)
        {
            Destination = destination;
            Detour = detour;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            TurnDuration = turnDuration;
            WaitDuration = waitDuration;
            MovementDuration = movementDuration;
            TrafficConflictChecks = trafficConflictChecks;
            IsStationary = isStationary;
            IsDeferred = isDeferred;
        }

        public Vector3 Destination { get; }
        public Vector3? Detour { get; }
        public ShipBezierRoute Route { get; }
        public Vector3[] Trajectory => Route.Samples;
        public float TurnDuration { get; }
        public float WaitDuration { get; }
        public float MovementDuration { get; }
        public int TrafficConflictChecks { get; }
        public bool IsStationary { get; }
        public bool IsDeferred { get; }
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
            Vector2Range mapRange,
            bool preserveCourse = false);
        void ClearPlan(IShipNavigationAgent agent);
    }

    public sealed class ShipNavigationService : Service, IShipNavigationService
    {
        private readonly List<RadarContact> _mapObstacleContacts =
            new List<RadarContact>();
        private readonly IMapObstacleContactProvider _mapObstacleContactProvider;
        private readonly ShipTrafficCoordinator _trafficCoordinator =
            new ShipTrafficCoordinator();

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

            _trafficCoordinator.Register(agent);
        }

        public void Unregister(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            _trafficCoordinator.Unregister(agent);
        }

        public ShipNavigationPlan Plan(
            IShipNavigationAgent agent,
            Vector3 forward,
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange,
            bool preserveCourse = false)
        {
            if (!_trafficCoordinator.IsRegistered(agent))
            {
                throw new InvalidOperationException(
                    "Ship navigation agent must be registered before planning.");
            }

            if (obstacleContacts == null)
            {
                throw new ArgumentNullException(nameof(obstacleContacts));
            }

            Vector3 origin = agent.NavigationPosition;
            BuildNavigationContacts(agent, obstacleContacts);

            ShipAvoidancePlanner.TryResolveDestination(
                requestedDestination,
                origin,
                _mapObstacleContacts,
                agent.NavigationHeight,
                heightTolerance,
                clearance,
                mapRange,
                out Vector3 destination);
            ShipRoutePlan routePlan = ShipRoutePlanner.Build(
                agent,
                forward,
                destination,
                _mapObstacleContacts,
                heightTolerance,
                clearance,
                mapRange);
            float movementDuration =
                routePlan.Route.Length /
                Mathf.Max(agent.NavigationSpeed, Mathf.Epsilon);
            ShipTrafficSchedule trafficSchedule = routePlan.IsStationary
                ? new ShipTrafficSchedule(0f, 0)
                : _trafficCoordinator.Reserve(
                    agent,
                    routePlan.Destination,
                    routePlan.Route,
                    routePlan.TurnDuration,
                    movementDuration,
                    heightTolerance,
                    preserveCourse);
            ShipNavigationPlan plan = new ShipNavigationPlan(
                routePlan.Destination,
                routePlan.Detour,
                routePlan.Route,
                routePlan.TurnDuration,
                trafficSchedule.WaitDuration,
                movementDuration,
                trafficSchedule.ExactConflictCheckCount,
                routePlan.IsStationary,
                preserveCourse && (routePlan.IsStationary || trafficSchedule.WaitDuration > Mathf.Epsilon));
            return plan;
        }

        public void ClearPlan(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            _trafficCoordinator.Clear(agent);
        }

        private void BuildNavigationContacts(
            IShipNavigationAgent planningAgent,
            IReadOnlyList<RadarContact> radarContacts)
        {
            _mapObstacleContactProvider.CopyContacts(_mapObstacleContacts);
            for (int i = 0; i < radarContacts.Count; i++)
            {
                AddContactIfUnique(radarContacts[i]);
            }

            _trafficCoordinator.AppendPredictedContacts(
                planningAgent,
                _mapObstacleContacts);
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
    }
}
