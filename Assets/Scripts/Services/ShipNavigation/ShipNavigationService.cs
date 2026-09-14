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
            float movementDuration,
            bool isStationary = false,
            bool isDeferred = false)
        {
            Destination = destination;
            Detour = detour;
            Route = route ?? throw new ArgumentNullException(nameof(route));
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
        ShipNavigationPlan Plan(
            IShipNavigationAgent agent,
            Vector3 forward,
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange,
            bool preserveCourse = false);
    }

    public sealed class ShipNavigationService : Service, IShipNavigationService
    {
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
            if (obstacleContacts == null)
            {
                throw new ArgumentNullException(nameof(obstacleContacts));
            }

            Vector3 origin = agent.NavigationPosition;
            BuildNavigationContacts(obstacleContacts);

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
            ShipNavigationPlan plan = new ShipNavigationPlan(
                routePlan.Destination,
                routePlan.Detour,
                routePlan.Route,
                routePlan.TurnDuration,
                movementDuration,
                routePlan.IsStationary,
                preserveCourse && (routePlan.IsStationary || routePlan.TurnDuration > Mathf.Epsilon));
            return plan;
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
