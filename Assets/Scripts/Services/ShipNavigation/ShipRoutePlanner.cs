using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal readonly struct ShipRoutePlan
    {
        public Vector3 Destination { get; }
        public Vector3? Detour { get; }
        public ShipBezierRoute Route { get; }
        public float TurnDuration { get; }
        public bool IsStationary { get; }

        public ShipRoutePlan(
            ShipBezierRoute route,
            Vector3 destination,
            Vector3? detour,
            float turnDuration,
            bool isStationary = false)
        {
            Destination = destination;
            Detour = detour;
            Route = route;
            TurnDuration = turnDuration;
            IsStationary = isStationary;
        }
    }

    internal static class ShipRoutePlanner
    {
        private static readonly float[] HANDLE_SCALES = { 1f, 0.5f, 0.25f };

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
            // A ship that starts inside an obstacle's clearance (e.g. beside an idle
            // ship) may still curve, as long as it only moves away until it is out.
            bool allowEscape = !isOriginClear;
            waypoints.Clear();
            if (IsSegmentClear(
                    origin, destination, contacts, agent.NavigationHeight,
                    heightTolerance, clearance, allowEscape))
            {
                waypoints.Add(origin);
                waypoints.Add(destination);
            }
            else if (!pathGrid.TryFindPath(
                         destination, agent.NavigationHeight, waypoints))
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

            // Each smooth option retries with tighter curves: full-size handles swing
            // wide of the waypoint legs and can cut into an obstacle's clearance.
            // 1) Curved route that keeps the current heading: no turn in place.
            if (waypoints.Count == 2 ||
                Vector3.Dot(GetPlanarDirection(forward, firstLeg), firstLeg) > 0f)
            {
                for (int i = 0; i < HANDLE_SCALES.Length; i++)
                {
                    ShipBezierRoute courseRoute = BuildSmoothRoute(
                        waypoints, forward, minimumTurnRadius, HANDLE_SCALES[i]);
                    if (IsRouteClear(courseRoute, contacts, agent, heightTolerance,
                            clearance, allowEscape))
                    {
                        return new ShipRoutePlan(destination: destination, detour: detour, route: courseRoute, turnDuration: 0f);
                    }
                }
            }

            // 2) Turn in place towards the first leg, then follow a smooth route.
            for (int i = 0; i < HANDLE_SCALES.Length; i++)
            {
                ShipBezierRoute turnedRoute = BuildSmoothRoute(
                    waypoints, firstLeg, minimumTurnRadius, HANDLE_SCALES[i]);
                if (IsRouteClear(turnedRoute, contacts, agent, heightTolerance,
                        clearance, allowEscape))
                {
                    return new ShipRoutePlan(
                        destination: destination, detour: detour, route: turnedRoute,
                        turnDuration: CalculateTurnDuration(agent, forward, turnedRoute));
                }
            }

            // 3) Straight legs between waypoints. Clear by construction of the path
            // grid; a ship that starts inside an obstacle's clearance uses it to escape.
            ShipBezierRoute polylineRoute = ShipBezierPath.BuildPolylineRoute(waypoints);
            if (!isOriginClear ||
                IsRouteClear(polylineRoute, contacts, agent, heightTolerance, clearance))
            {
                return new ShipRoutePlan(
                    destination: destination, detour: detour, route: polylineRoute,
                    turnDuration: CalculateTurnDuration(agent, forward, polylineRoute));
            }

            return BuildStationaryPlan(origin, forward);
        }

        private static ShipBezierRoute BuildSmoothRoute(
            List<Vector3> waypoints,
            Vector3 startDirection,
            float minimumTurnRadius,
            float handleScale)
        {
            return waypoints.Count == 2
                ? ShipBezierPath.BuildDirectRoute(
                    waypoints[0], startDirection, waypoints[1], minimumTurnRadius, handleScale)
                : ShipBezierPath.BuildWaypointRoute(
                    waypoints, startDirection, minimumTurnRadius, handleScale);
        }

        private static bool IsRouteClear(
            ShipBezierRoute route,
            IReadOnlyList<RadarContact> contacts,
            IShipNavigationAgent agent,
            float heightTolerance,
            float clearance,
            bool allowEscape = false)
        {
            return ShipAvoidancePlanner.IsRouteClear(
                route, contacts, agent.NavigationHeight, heightTolerance, clearance,
                allowEscape);
        }

        private static bool IsSegmentClear(
            Vector3 start,
            Vector3 end,
            IReadOnlyList<RadarContact> contacts,
            float shipHeight,
            float heightTolerance,
            float clearance,
            bool allowEscape)
        {
            return ShipAvoidancePlanner.IsRouteClear(
                ShipBezierPath.BuildPolylineRoute(new[] { start, end }),
                contacts, shipHeight, heightTolerance, clearance, allowEscape);
        }

        private static ShipRoutePlan BuildStationaryPlan(Vector3 origin, Vector3 forward)
        {
            return new ShipRoutePlan(
                destination: origin,
                detour: null,
                route: ShipBezierPath.BuildDirectRoute(origin, forward, origin),
                turnDuration: 0f,
                isStationary: true);
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
