using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal readonly struct ShipRoutePlan
    {
        public ShipRoutePlan(
            Vector3 destination,
            Vector3? detour,
            ShipBezierRoute route,
            float turnDuration,
            bool isStationary = false)
        {
            Destination = destination;
            Detour = detour;
            Route = route;
            TurnDuration = turnDuration;
            IsStationary = isStationary;
        }

        public Vector3 Destination { get; }
        public Vector3? Detour { get; }
        public ShipBezierRoute Route { get; }
        public float TurnDuration { get; }
        public bool IsStationary { get; }
    }

    internal static class ShipRoutePlanner
    {
        public static ShipRoutePlan Build(
            IShipNavigationAgent agent,
            Vector3 forward,
            Vector3 destination,
            IReadOnlyList<RadarContact> contacts,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange)
        {
            Vector3 origin = agent.NavigationPosition;
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
                    contacts,
                    agent.NavigationHeight,
                    heightTolerance,
                    clearance,
                    mapRange,
                    out Vector3 avoidancePoint,
                    forward))
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

            if (ShipAvoidancePlanner.IsRouteClear(
                    route,
                    contacts,
                    agent.NavigationHeight,
                    heightTolerance,
                    clearance))
            {
                return new ShipRoutePlan(destination, detour, route, 0f);
            }

            if (detour.HasValue && ShipAvoidancePlanner.TryCalculateDetour(
                    origin, destination, contacts, agent.NavigationHeight,
                    heightTolerance, clearance, mapRange, out Vector3 alternateDetour,
                    forward, alternateSide: true))
            {
                ShipBezierRoute alternateRoute = ShipBezierPath.BuildAvoidanceRoute(
                    origin, forward, alternateDetour, destination);
                if (ShipAvoidancePlanner.IsRouteClear(
                        alternateRoute, contacts, agent.NavigationHeight, heightTolerance, clearance))
                {
                    return new ShipRoutePlan(destination, alternateDetour, alternateRoute, 0f);
                }
            }

            Vector3 initialTravelDirection = detour.HasValue
                ? detour.Value - origin
                : destination - origin;
            if (initialTravelDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                initialTravelDirection = GetPlanarDirection(forward);
            }

            route = detour.HasValue
                ? ShipBezierPath.BuildAvoidanceRoute(
                    origin,
                    initialTravelDirection,
                    detour.Value,
                    destination)
                : ShipBezierPath.BuildDirectRoute(
                    origin,
                    initialTravelDirection,
                    destination,
                    minimumTurnRadius);
            float turnDuration =
                ShipRotationKinematics.CalculateTurnDuration(
                    Quaternion.LookRotation(
                        GetPlanarDirection(forward),
                        Vector3.up),
                    route.InitialTangent,
                    Mathf.Max(
                        agent.NavigationRotationSpeed,
                        Mathf.Epsilon));
            if (ShipAvoidancePlanner.IsRouteClear(
                    route,
                    contacts,
                    agent.NavigationHeight,
                    heightTolerance,
                    clearance))
            {
                return new ShipRoutePlan(
                    destination,
                    detour,
                    route,
                    turnDuration);
            }

            ShipBezierRoute stationaryRoute = ShipBezierPath.BuildDirectRoute(
                origin,
                forward,
                origin);
            return new ShipRoutePlan(
                origin,
                null,
                stationaryRoute,
                0f,
                true);
        }

        private static Vector3 GetPlanarDirection(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > Mathf.Epsilon
                ? direction.normalized
                : Vector3.forward;
        }
    }
}
