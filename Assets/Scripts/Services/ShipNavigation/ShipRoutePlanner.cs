using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
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
            ShipPathGrid pathGrid,
            List<Vector3> waypoints,
            float heightTolerance,
            float clearance)
        {
            Vector3 origin = agent.NavigationPosition;
            bool isOriginClear = ShipAvoidancePlanner.IsPointClear(
                origin, contacts, agent.NavigationHeight, heightTolerance, clearance);
            waypoints.Clear();
            if (isOriginClear && IsSegmentClear(
                    origin, destination, contacts, agent.NavigationHeight,
                    heightTolerance, clearance))
            {
                waypoints.Add(origin);
                waypoints.Add(destination);
            }
            else if (!pathGrid.TryFindPath(
                         origin, destination, agent.NavigationHeight, waypoints))
            {
                return BuildStationaryPlan(origin, forward);
            }

            Vector3? detour = waypoints.Count > 2 ? waypoints[1] : (Vector3?)null;
            float minimumTurnRadius = Mathf.Max(
                agent.NavigationRadius,
                ShipRotationKinematics.CalculateMinimumTurnRadius(
                    Mathf.Max(agent.NavigationSpeed, 0f),
                    Mathf.Max(agent.NavigationRotationSpeed, Mathf.Epsilon)));
            Vector3 firstLeg = GetPlanarDirection(waypoints[1] - origin, forward);

            if (isOriginClear)
            {
                // 1) Curved route that keeps the current heading: no turn in place.
                if (waypoints.Count == 2 ||
                    Vector3.Dot(GetPlanarDirection(forward, firstLeg), firstLeg) > 0f)
                {
                    ShipBezierRoute courseRoute = BuildSmoothRoute(
                        waypoints, forward, minimumTurnRadius);
                    if (IsRouteClear(courseRoute, contacts, agent, heightTolerance, clearance))
                    {
                        return new ShipRoutePlan(destination, detour, courseRoute, 0f);
                    }
                }

                // 2) Turn in place towards the first leg, then follow a smooth route.
                ShipBezierRoute turnedRoute = BuildSmoothRoute(
                    waypoints, firstLeg, minimumTurnRadius);
                if (IsRouteClear(turnedRoute, contacts, agent, heightTolerance, clearance))
                {
                    return new ShipRoutePlan(
                        destination, detour, turnedRoute,
                        CalculateTurnDuration(agent, forward, turnedRoute));
                }
            }

            // 3) Straight legs between waypoints. Clear by construction of the path
            // grid; a ship that starts inside an obstacle's clearance uses it to escape.
            ShipBezierRoute polylineRoute = ShipBezierPath.BuildPolylineRoute(waypoints);
            if (!isOriginClear ||
                IsRouteClear(polylineRoute, contacts, agent, heightTolerance, clearance))
            {
                return new ShipRoutePlan(
                    destination, detour, polylineRoute,
                    CalculateTurnDuration(agent, forward, polylineRoute));
            }

            return BuildStationaryPlan(origin, forward);
        }

        private static ShipBezierRoute BuildSmoothRoute(
            List<Vector3> waypoints,
            Vector3 startDirection,
            float minimumTurnRadius)
        {
            return waypoints.Count == 2
                ? ShipBezierPath.BuildDirectRoute(
                    waypoints[0], startDirection, waypoints[1], minimumTurnRadius)
                : ShipBezierPath.BuildWaypointRoute(
                    waypoints, startDirection, minimumTurnRadius);
        }

        private static bool IsRouteClear(
            ShipBezierRoute route,
            IReadOnlyList<RadarContact> contacts,
            IShipNavigationAgent agent,
            float heightTolerance,
            float clearance)
        {
            return ShipAvoidancePlanner.IsRouteClear(
                route, contacts, agent.NavigationHeight, heightTolerance, clearance);
        }

        private static bool IsSegmentClear(
            Vector3 start,
            Vector3 end,
            IReadOnlyList<RadarContact> contacts,
            float shipHeight,
            float heightTolerance,
            float clearance)
        {
            return ShipAvoidancePlanner.IsRouteClear(
                ShipBezierPath.BuildPolylineRoute(new[] { start, end }),
                contacts, shipHeight, heightTolerance, clearance);
        }

        private static ShipRoutePlan BuildStationaryPlan(Vector3 origin, Vector3 forward)
        {
            return new ShipRoutePlan(
                origin,
                null,
                ShipBezierPath.BuildDirectRoute(origin, forward, origin),
                0f,
                true);
        }

        private static float CalculateTurnDuration(
            IShipNavigationAgent agent,
            Vector3 forward,
            ShipBezierRoute route)
        {
            return ShipRotationKinematics.CalculateTurnDuration(
                Quaternion.LookRotation(GetPlanarDirection(forward, Vector3.forward), Vector3.up),
                route.InitialTangent,
                Mathf.Max(agent.NavigationRotationSpeed, Mathf.Epsilon));
        }

        private static Vector3 GetPlanarDirection(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                return direction.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude > Mathf.Epsilon
                ? fallback.normalized
                : Vector3.forward;
        }
    }
}
