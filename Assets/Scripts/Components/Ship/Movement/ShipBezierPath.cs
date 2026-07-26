using System;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public static class ShipBezierPath
    {
        private const float CONTROL_DISTANCE_FACTOR = 0.35f;

        public static ShipBezierRoute BuildDirectRoute(
            Vector3 origin,
            Vector3 originForward,
            Vector3 destination)
        {
            Vector3 route = destination - origin;
            route.y = 0f;
            float distance = route.magnitude;
            Vector3 routeDirection = distance <= Mathf.Epsilon
                ? GetPlanarDirection(originForward, Vector3.forward)
                : route / distance;
            Vector3 startDirection = GetPlanarDirection(
                originForward,
                routeDirection);
            Vector3 p1 =
                origin + startDirection * distance * CONTROL_DISTANCE_FACTOR;
            Vector3 p2 =
                destination - routeDirection * distance * CONTROL_DISTANCE_FACTOR;
            return new ShipBezierRoute(new[]
            {
                new CubicBezierSegment(origin, p1, p2, destination)
            });
        }

        public static ShipBezierRoute BuildAvoidanceRoute(
            Vector3 origin,
            Vector3 originForward,
            Vector3 detour,
            Vector3 destination)
        {
            Vector3 route = destination - origin;
            route.y = 0f;
            if (route.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new ArgumentException(
                    "Avoidance route requires different origin and destination positions.");
            }

            Vector3 routeDirection = route.normalized;
            Vector3 startDirection = GetPlanarDirection(
                originForward,
                routeDirection);
            Vector3 arrivalDirection = GetPlanarDirection(
                destination - detour,
                routeDirection);
            Vector3 detourDirection = GetPlanarDirection(
                destination - origin,
                routeDirection);
            float firstDistance = Vector3.Distance(origin, detour);
            float secondDistance = Vector3.Distance(detour, destination);

            CubicBezierSegment first = new CubicBezierSegment(
                origin,
                origin + startDirection * firstDistance * CONTROL_DISTANCE_FACTOR,
                detour - detourDirection * firstDistance * CONTROL_DISTANCE_FACTOR,
                detour);
            CubicBezierSegment second = new CubicBezierSegment(
                detour,
                detour + detourDirection * secondDistance * CONTROL_DISTANCE_FACTOR,
                destination - arrivalDirection * secondDistance * CONTROL_DISTANCE_FACTOR,
                destination);
            return new ShipBezierRoute(new[] { first, second });
        }

        public static Vector3[] BuildDirect(
            Vector3 origin,
            Vector3 originForward,
            Vector3 destination)
        {
            return BuildDirectRoute(origin, originForward, destination).Samples;
        }

        public static Vector3[] BuildAvoidance(
            Vector3 origin,
            Vector3 originForward,
            Vector3 detour,
            Vector3 destination)
        {
            return BuildAvoidanceRoute(
                origin,
                originForward,
                detour,
                destination).Samples;
        }

        private static Vector3 GetPlanarDirection(
            Vector3 requestedDirection,
            Vector3 fallback)
        {
            requestedDirection.y = 0f;
            if (requestedDirection.sqrMagnitude > Mathf.Epsilon)
            {
                return requestedDirection.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude > Mathf.Epsilon
                ? fallback.normalized
                : Vector3.forward;
        }
    }
}
