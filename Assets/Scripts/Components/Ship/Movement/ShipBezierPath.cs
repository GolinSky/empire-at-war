using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public static class ShipBezierPath
    {
        private const int SAMPLES_PER_SEGMENT = 12;
        private const float CONTROL_DISTANCE_FACTOR = 0.35f;

        public static Vector3[] BuildDirect(
            Vector3 origin,
            Vector3 originForward,
            Vector3 destination)
        {
            Vector3 route = destination - origin;
            route.y = 0f;
            float distance = route.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return new[] { origin, destination };
            }

            Vector3 routeDirection = route / distance;
            Vector3 startDirection = originForward;
            startDirection.y = 0f;
            startDirection = startDirection.sqrMagnitude <= Mathf.Epsilon
                ? routeDirection
                : startDirection.normalized;
            Vector3 p1 = origin + startDirection * distance * CONTROL_DISTANCE_FACTOR;
            Vector3 p2 = destination - routeDirection * distance * CONTROL_DISTANCE_FACTOR;
            List<Vector3> points = new List<Vector3>(SAMPLES_PER_SEGMENT + 1);
            AppendCubic(points, origin, p1, p2, destination, true);
            return points.ToArray();
        }

        public static Vector3[] BuildAvoidance(
            Vector3 origin,
            Vector3 originForward,
            Vector3 detour,
            Vector3 destination)
        {
            Vector3 routeDirection = destination - origin;
            routeDirection.y = 0f;
            if (routeDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new ArgumentException("Avoidance route requires different origin and destination positions.");
            }

            routeDirection.Normalize();
            Vector3 startDirection = originForward;
            startDirection.y = 0f;
            if (startDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                startDirection = routeDirection;
            }
            else
            {
                startDirection.Normalize();
            }

            float firstDistance = Vector3.Distance(origin, detour);
            float secondDistance = Vector3.Distance(detour, destination);
            Vector3 firstControl = origin + startDirection * firstDistance * CONTROL_DISTANCE_FACTOR;
            Vector3 detourApproachControl =
                detour - routeDirection * firstDistance * CONTROL_DISTANCE_FACTOR;
            Vector3 detourExitControl =
                detour + routeDirection * secondDistance * CONTROL_DISTANCE_FACTOR;
            Vector3 destinationControl =
                destination - routeDirection * secondDistance * CONTROL_DISTANCE_FACTOR;

            List<Vector3> points = new List<Vector3>(SAMPLES_PER_SEGMENT * 2 + 1);
            AppendCubic(
                points,
                origin,
                firstControl,
                detourApproachControl,
                detour,
                true);
            AppendCubic(
                points,
                detour,
                detourExitControl,
                destinationControl,
                destination,
                false);
            return points.ToArray();
        }

        private static void AppendCubic(
            ICollection<Vector3> points,
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            bool includeStart)
        {
            int firstSample = includeStart ? 0 : 1;
            for (int i = firstSample; i <= SAMPLES_PER_SEGMENT; i++)
            {
                float t = i / (float)SAMPLES_PER_SEGMENT;
                float inverse = 1f - t;
                points.Add(
                    inverse * inverse * inverse * p0 +
                    3f * inverse * inverse * t * p1 +
                    3f * inverse * t * t * p2 +
                    t * t * t * p3);
            }
        }
    }
}
