using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public static class ShipBezierPath
    {
        private const float CONTROL_DISTANCE_FACTOR = 0.35f;
        private const float WAYPOINT_CONTROL_DISTANCE_FACTOR = 0.3f;
        private const float QUARTER_CIRCLE_CONTROL_FACTOR = 0.5522848f;
        private const float TURNAROUND_RADIUS_FACTOR = 0.25f;

        public static ShipBezierRoute BuildDirectRoute(
            Vector3 origin,
            Vector3 originForward,
            Vector3 destination,
            float minimumTurnRadius = 0f)
        {
            if (minimumTurnRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumTurnRadius));
            }

            Vector3 route = destination - origin;
            route.y = 0f;
            float distance = route.magnitude;
            Vector3 routeDirection = distance <= Mathf.Epsilon
                ? GetPlanarDirection(originForward, Vector3.forward)
                : route / distance;
            Vector3 startDirection = GetPlanarDirection(
                originForward,
                routeDirection);
            if (Vector3.Dot(startDirection, routeDirection) < 0f)
            {
                return BuildTurnaroundRoute(
                    origin,
                    startDirection,
                    destination,
                    routeDirection,
                    Mathf.Max(
                        minimumTurnRadius,
                        distance * TURNAROUND_RADIUS_FACTOR));
            }

            float controlDistance = Mathf.Min(
                distance * 0.5f,
                Mathf.Max(
                    distance * CONTROL_DISTANCE_FACTOR,
                    minimumTurnRadius * QUARTER_CIRCLE_CONTROL_FACTOR));
            Vector3 p1 = origin + startDirection * controlDistance;
            Vector3 p2 = destination - routeDirection * controlDistance;
            return new ShipBezierRoute(new[]
            {
                new CubicBezierSegment(origin, p1, p2, destination)
            });
        }

        private static ShipBezierRoute BuildTurnaroundRoute(
            Vector3 origin,
            Vector3 startDirection,
            Vector3 destination,
            Vector3 arrivalDirection,
            float turnRadius)
        {
            float turnSign = Vector3.Cross(
                startDirection,
                arrivalDirection).y;
            Vector3 turnDirection = turnSign < 0f
                ? Vector3.Cross(startDirection, Vector3.up).normalized
                : Vector3.Cross(Vector3.up, startDirection).normalized;
            Vector3 quarterTurnPoint =
                origin +
                startDirection * turnRadius +
                turnDirection * turnRadius;
            Vector3 halfTurnPoint =
                origin + turnDirection * turnRadius * 2f;
            float quarterCircleHandle =
                turnRadius * QUARTER_CIRCLE_CONTROL_FACTOR;
            CubicBezierSegment first = new CubicBezierSegment(
                origin,
                origin + startDirection * quarterCircleHandle,
                quarterTurnPoint - turnDirection * quarterCircleHandle,
                quarterTurnPoint);
            CubicBezierSegment second = new CubicBezierSegment(
                quarterTurnPoint,
                quarterTurnPoint + turnDirection * quarterCircleHandle,
                halfTurnPoint + startDirection * quarterCircleHandle,
                halfTurnPoint);

            float remainingDistance = Vector3.Distance(
                halfTurnPoint,
                destination);
            Vector3 finalArrivalDirection = GetPlanarDirection(
                destination - halfTurnPoint,
                arrivalDirection);
            float approachHandle = Mathf.Max(
                quarterCircleHandle,
                remainingDistance * CONTROL_DISTANCE_FACTOR);
            CubicBezierSegment approach = new CubicBezierSegment(
                halfTurnPoint,
                halfTurnPoint - startDirection * approachHandle,
                destination - finalArrivalDirection * approachHandle,
                destination);
            return new ShipBezierRoute(new[] { first, second, approach });
        }

        public static ShipBezierRoute BuildWaypointRoute(
            IReadOnlyList<Vector3> waypoints,
            Vector3 originForward,
            float minimumTurnRadius)
        {
            if (waypoints.Count < 2)
            {
                throw new ArgumentException(
                    "A waypoint route requires at least two waypoints.",
                    nameof(waypoints));
            }

            int lastIndex = waypoints.Count - 1;
            CubicBezierSegment[] segments = new CubicBezierSegment[lastIndex];
            for (int i = 0; i < lastIndex; i++)
            {
                Vector3 start = waypoints[i];
                Vector3 end = waypoints[i + 1];
                Vector3 legDirection = GetPlanarDirection(end - start, originForward);
                float distance = Vector3.Distance(start, end);
                Vector3 startTangent = i == 0
                    ? GetPlanarDirection(originForward, legDirection)
                    : GetPlanarDirection(end - waypoints[i - 1], legDirection);
                Vector3 endTangent = i + 1 == lastIndex
                    ? legDirection
                    : GetPlanarDirection(waypoints[i + 2] - start, legDirection);
                float startHandle = i == 0
                    ? Mathf.Min(
                        distance * 0.5f,
                        Mathf.Max(
                            distance * WAYPOINT_CONTROL_DISTANCE_FACTOR,
                            minimumTurnRadius * QUARTER_CIRCLE_CONTROL_FACTOR))
                    : distance * WAYPOINT_CONTROL_DISTANCE_FACTOR;
                float endHandle = distance * WAYPOINT_CONTROL_DISTANCE_FACTOR;
                segments[i] = new CubicBezierSegment(
                    start,
                    start + startTangent * startHandle,
                    end - endTangent * endHandle,
                    end);
            }

            return new ShipBezierRoute(segments);
        }

        public static ShipBezierRoute BuildPolylineRoute(
            IReadOnlyList<Vector3> waypoints)
        {
            if (waypoints.Count < 2)
            {
                throw new ArgumentException(
                    "A polyline route requires at least two waypoints.",
                    nameof(waypoints));
            }

            CubicBezierSegment[] segments =
                new CubicBezierSegment[waypoints.Count - 1];
            for (int i = 0; i < segments.Length; i++)
            {
                Vector3 start = waypoints[i];
                Vector3 leg = waypoints[i + 1] - start;
                segments[i] = new CubicBezierSegment(
                    start,
                    start + leg / 3f,
                    start + leg * (2f / 3f),
                    waypoints[i + 1]);
            }

            return new ShipBezierRoute(segments);
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
