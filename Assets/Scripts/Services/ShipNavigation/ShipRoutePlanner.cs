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
            Vector3? detour,
            ShipBezierRoute route,
            float turnDuration)
        {
            Detour = detour;
            Route = route;
            TurnDuration = turnDuration;
        }

        public Vector3? Detour { get; }
        public ShipBezierRoute Route { get; }
        public float TurnDuration { get; }
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

            if (ShipAvoidancePlanner.IsRouteClear(
                    route,
                    contacts,
                    agent.NavigationHeight,
                    heightTolerance,
                    clearance))
            {
                return new ShipRoutePlan(detour, route, 0f);
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
            return new ShipRoutePlan(detour, route, turnDuration);
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
