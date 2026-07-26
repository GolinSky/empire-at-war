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
        private readonly Dictionary<IShipNavigationAgent, ShipTrafficReservation>
            _reservations =
                new Dictionary<IShipNavigationAgent, ShipTrafficReservation>();
        private readonly List<RadarContact> _contacts = new List<RadarContact>();
        private readonly ShipTrafficScheduler _trafficScheduler =
            new ShipTrafficScheduler();

        public void Register(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (!_reservations.ContainsKey(agent))
            {
                _reservations.Add(agent, default);
            }
        }

        public void Unregister(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            _reservations.Remove(agent);
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
            if (!_reservations.ContainsKey(agent))
            {
                throw new InvalidOperationException(
                    "Ship navigation agent must be registered before planning.");
            }

            BuildSharedContacts(agent, obstacleContacts);
            Vector3 origin = agent.NavigationPosition;
            Vector3 destination = requestedDestination;
            ShipAvoidancePlanner.TryResolveDestination(
                requestedDestination,
                origin,
                _contacts,
                agent.NavigationHeight,
                heightTolerance,
                clearance,
                mapRange,
                out destination);

            Vector3? detour = null;
            if (ShipAvoidancePlanner.TryCalculateDetour(
                    origin,
                    destination,
                    _contacts,
                    agent.NavigationHeight,
                    heightTolerance,
                    clearance,
                    mapRange,
                    out Vector3 calculatedDetour))
            {
                detour = calculatedDetour;
            }

            ShipBezierRoute route = detour.HasValue
                ? ShipBezierPath.BuildAvoidanceRoute(
                    origin,
                    forward,
                    detour.Value,
                    destination)
                : ShipBezierPath.BuildDirectRoute(origin, forward, destination);
            float movementDuration =
                route.Length / Mathf.Max(agent.NavigationSpeed, Mathf.Epsilon);
            float turnDuration = ShipRotationKinematics.CalculateTurnDuration(
                Quaternion.LookRotation(
                    forward.sqrMagnitude > Mathf.Epsilon
                        ? forward
                        : Vector3.forward,
                    Vector3.up),
                route.InitialTangent,
                Mathf.Max(agent.NavigationRotationSpeed, Mathf.Epsilon));
            ShipTrafficPath trafficTrajectory = _trafficScheduler.CreateTrajectory(
                route,
                agent.NavigationRadius);
            float trafficDelay = _trafficScheduler.CalculateDelay(
                agent,
                trafficTrajectory,
                movementDuration,
                heightTolerance,
                _reservations);
            float waitDuration = Mathf.Max(turnDuration, trafficDelay);
            ShipNavigationPlan plan = new ShipNavigationPlan(
                destination,
                detour,
                route,
                waitDuration,
                movementDuration,
                _trafficScheduler.LastExactConflictCheckCount);
            _reservations[agent] = new ShipTrafficReservation(
                destination,
                trafficTrajectory,
                agent.NavigationHeight,
                agent.NavigationRadius,
                agent.NavigationSpeed,
                waitDuration,
                movementDuration);
            return plan;
        }

        public void ClearPlan(IShipNavigationAgent agent)
        {
            if (_reservations.ContainsKey(agent))
            {
                _reservations[agent] = default;
            }
        }

        private void BuildSharedContacts(
            IShipNavigationAgent planningAgent,
            IReadOnlyList<RadarContact> obstacleContacts)
        {
            _contacts.Clear();
            if (obstacleContacts != null)
            {
                for (int i = 0; i < obstacleContacts.Count; i++)
                {
                    _contacts.Add(obstacleContacts[i]);
                }
            }

            foreach (KeyValuePair<IShipNavigationAgent, ShipTrafficReservation> pair
                     in _reservations)
            {
                IShipNavigationAgent other = pair.Key;
                if (ReferenceEquals(other, planningAgent))
                {
                    continue;
                }

                _contacts.Add(new RadarContact(
                    other.NavigationPosition,
                    other.NavigationRadius,
                    true));
                if (pair.Value.HasTrajectory)
                {
                    _contacts.Add(new RadarContact(
                        pair.Value.Destination,
                        other.NavigationRadius,
                        true));
                }
            }
        }
    }
}
