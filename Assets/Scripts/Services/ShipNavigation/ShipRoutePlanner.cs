using System.Collections.Generic;
using EmpireAtWar.Utils;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal readonly struct ShipRoutePlan
    {
        public Vector3 Destination { get; }
        public Vector3? Detour { get; }
        public ShipRoute Route { get; }
        public float TurnDuration { get; }
        public bool IsStationary { get; }

        public ShipRoutePlan(
            ShipRoute route,
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
        public static ShipRoutePlan Build(
            IShipNavigationAgent agent,
            Vector3 forward,
            Vector3 destination,
            IReadOnlyList<RadarContact> contacts,
            IShipPathGraph pathGraph,
            List<Vector3> waypoints,
            float heightTolerance,
            float clearance)
        {
            Vector3 origin = agent.NavigationPosition;
            if ((destination - origin).sqrMagnitude < 0.000001f)
                return BuildStationaryPlan(origin, forward);
            bool allowEscape = !ShipAvoidancePlanner.IsPointClear(
                origin, contacts, agent.NavigationHeight, heightTolerance, clearance);
            waypoints.Clear();
            if (ShipAvoidancePlanner.IsSegmentClear(
                    origin, destination, contacts, agent.NavigationHeight,
                    heightTolerance, clearance, allowEscape) && pathGraph.IsUnpenalizedSegment(origin, destination))
            {
                waypoints.Add(origin);
                waypoints.Add(destination);
            }
            else if (!pathGraph.TryFindPath(destination, agent.NavigationHeight, waypoints))
            {
                return BuildStationaryPlan(origin, forward);
            }
            float minimumTurnRadius = ShipRotationKinematics.CalculateMinimumTurnRadius(
                Mathf.Max(agent.NavigationSpeed, 0f),
                Mathf.Max(agent.NavigationRotationSpeed, Mathf.Epsilon));
            ShipRoute route = ShipTangentRouteBuilder.Build(pathGraph, waypoints, forward, minimumTurnRadius, allowEscape);
            float turnDuration = Vector3.Angle(forward, route.InitialTangent) <= 1f ? 0f : ShipRotationKinematics.CalculateTurnDuration(
                Quaternion.LookRotation(PlanarGeometry.NormalizeDirection(forward, Vector3.forward), Vector3.up),
                route.InitialTangent, Mathf.Max(agent.NavigationRotationSpeed, Mathf.Epsilon));
            return new ShipRoutePlan(route, destination, waypoints.Count > 2 ? waypoints[1] : (Vector3?)null, turnDuration);
        }

        private static ShipRoutePlan BuildStationaryPlan(Vector3 origin, Vector3 forward)
        {
            Vector3 direction = PlanarGeometry.NormalizeDirection(forward, Vector3.forward);
            var piece = new ShipRoutePiece(origin, Mathf.Atan2(direction.x, direction.z), 0f, 0f);
            return new ShipRoutePlan(new ShipRoute(new[] { piece }), origin, null, 0f, true);
        }
    }
}
