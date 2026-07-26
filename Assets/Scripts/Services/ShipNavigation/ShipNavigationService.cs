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
    }

    public readonly struct ShipNavigationPlan
    {
        public ShipNavigationPlan(Vector3 destination, Vector3? detour, Vector3[] trajectory)
        {
            Destination = destination;
            Detour = detour;
            Trajectory = trajectory;
        }

        public Vector3 Destination { get; }
        public Vector3? Detour { get; }
        public Vector3[] Trajectory { get; }
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
        private const int TRAJECTORY_SAMPLE_STEP = 2;

        private readonly Dictionary<IShipNavigationAgent, Reservation> _reservations =
            new Dictionary<IShipNavigationAgent, Reservation>();
        private readonly List<RadarContact> _contacts = new List<RadarContact>();

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
                throw new InvalidOperationException("Ship navigation agent must be registered before planning.");
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

            Vector3[] trajectory = detour.HasValue
                ? ShipBezierPath.BuildAvoidance(origin, forward, detour.Value, destination)
                : ShipBezierPath.BuildDirect(origin, forward, destination);
            _reservations[agent] = new Reservation(destination, trajectory);
            return new ShipNavigationPlan(destination, detour, trajectory);
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
                    if (!obstacleContacts[i].IsShip)
                    {
                        _contacts.Add(obstacleContacts[i]);
                    }
                }
            }

            foreach (KeyValuePair<IShipNavigationAgent, Reservation> pair in _reservations)
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
                Reservation reservation = pair.Value;
                if (!reservation.HasTrajectory)
                {
                    continue;
                }

                _contacts.Add(new RadarContact(
                    reservation.Destination,
                    other.NavigationRadius,
                    true));
                for (int i = TRAJECTORY_SAMPLE_STEP;
                     i < reservation.Trajectory.Length - 1;
                     i += TRAJECTORY_SAMPLE_STEP)
                {
                    _contacts.Add(new RadarContact(
                        reservation.Trajectory[i],
                        other.NavigationRadius,
                        true));
                }
            }
        }

        private readonly struct Reservation
        {
            public Reservation(Vector3 destination, Vector3[] trajectory)
            {
                Destination = destination;
                Trajectory = trajectory;
            }

            public Vector3 Destination { get; }
            public Vector3[] Trajectory { get; }
            public bool HasTrajectory => Trajectory != null && Trajectory.Length > 0;
        }
    }
}
