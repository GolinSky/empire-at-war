using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using UnityEngine;
using EmpireAtWar.Models.SkirmishCamera;

namespace EmpireAtWar.Components.Ship.Movement
{
    public static class ShipAvoidancePlanner
    {
        private const int DESTINATION_CANDIDATE_COUNT = 24;

        public static Vector3 ClampToMap(Vector3 point, Vector2Range mapRange, float margin)
        {
            point.x = Mathf.Clamp(point.x, mapRange.Min.x + margin, mapRange.Max.x - margin);
            point.z = Mathf.Clamp(point.z, mapRange.Min.y + margin, mapRange.Max.y - margin);
            return point;
        }

        public static bool TryCalculateDetour(
            Vector3 origin,
            Vector3 destination,
            IReadOnlyList<RadarContact> contacts,
            float shipHeight,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange,
            out Vector3 detour)
        {
            Vector2 start = new Vector2(origin.x, origin.z);
            Vector2 end = new Vector2(destination.x, destination.z);
            Vector2 route = end - start;
            float routeLength = route.magnitude;
            if (routeLength <= Mathf.Epsilon)
            {
                detour = destination;
                return false;
            }

            Vector2 direction = route / routeLength;
            for (int i = 0; i < contacts.Count; i++)
            {
                RadarContact contact = contacts[i];
                if (contact.IsShip && Mathf.Abs(contact.Position.y - shipHeight) > heightTolerance)
                {
                    continue;
                }

                Vector2 center = new Vector2(contact.Position.x, contact.Position.z);
                float projectedDistance = Mathf.Clamp(Vector2.Dot(center - start, direction), 0f, routeLength);
                Vector2 closest = start + direction * projectedDistance;
                float safeRadius = contact.Radius + clearance;
                if ((center - closest).sqrMagnitude >= safeRadius * safeRadius)
                {
                    continue;
                }

                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                Vector2 left = center + perpendicular * safeRadius;
                Vector2 right = center - perpendicular * safeRadius;
                Vector3 leftPoint = ClampToMap(new Vector3(left.x, shipHeight, left.y), mapRange, clearance);
                Vector3 rightPoint = ClampToMap(new Vector3(right.x, shipHeight, right.y), mapRange, clearance);
                detour = Vector3.Distance(origin, leftPoint) <= Vector3.Distance(origin, rightPoint)
                    ? leftPoint
                    : rightPoint;
                return true;
            }

            detour = destination;
            return false;
        }

        public static bool TryResolveDestination(
            Vector3 requestedDestination,
            Vector3 origin,
            IReadOnlyList<RadarContact> contacts,
            float shipHeight,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange,
            out Vector3 resolvedDestination)
        {
            requestedDestination.y = shipHeight;
            requestedDestination = ClampToMap(requestedDestination, mapRange, clearance);
            if (IsPointClear(
                    requestedDestination,
                    contacts,
                    shipHeight,
                    heightTolerance,
                    clearance))
            {
                resolvedDestination = requestedDestination;
                return false;
            }

            float bestDistance = float.PositiveInfinity;
            resolvedDestination = requestedDestination;
            for (int contactIndex = 0; contactIndex < contacts.Count; contactIndex++)
            {
                RadarContact contact = contacts[contactIndex];
                if (!IsRelevant(contact, shipHeight, heightTolerance))
                {
                    continue;
                }

                float safeRadius = contact.Radius + clearance;
                Vector3 fromCenter = requestedDestination - contact.Position;
                fromCenter.y = 0f;
                if (fromCenter.sqrMagnitude <= Mathf.Epsilon)
                {
                    fromCenter = origin - contact.Position;
                    fromCenter.y = 0f;
                }

                float startAngle = fromCenter.sqrMagnitude <= Mathf.Epsilon
                    ? 0f
                    : Mathf.Atan2(fromCenter.z, fromCenter.x);
                for (int candidateIndex = 0;
                     candidateIndex < DESTINATION_CANDIDATE_COUNT;
                     candidateIndex++)
                {
                    float angle = startAngle +
                                  candidateIndex * Mathf.PI * 2f / DESTINATION_CANDIDATE_COUNT;
                    Vector3 candidate = new Vector3(
                        contact.Position.x + Mathf.Cos(angle) * safeRadius,
                        shipHeight,
                        contact.Position.z + Mathf.Sin(angle) * safeRadius);
                    candidate = ClampToMap(candidate, mapRange, clearance);
                    if (!IsPointClear(
                            candidate,
                            contacts,
                            shipHeight,
                            heightTolerance,
                            clearance))
                    {
                        continue;
                    }

                    float distance = (candidate - requestedDestination).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        resolvedDestination = candidate;
                    }
                }
            }

            return bestDistance < float.PositiveInfinity;
        }

        private static bool IsPointClear(
            Vector3 point,
            IReadOnlyList<RadarContact> contacts,
            float shipHeight,
            float heightTolerance,
            float clearance)
        {
            Vector2 point2D = new Vector2(point.x, point.z);
            for (int i = 0; i < contacts.Count; i++)
            {
                RadarContact contact = contacts[i];
                if (!IsRelevant(contact, shipHeight, heightTolerance))
                {
                    continue;
                }

                Vector2 center = new Vector2(contact.Position.x, contact.Position.z);
                float safeRadius = contact.Radius + clearance;
                if ((point2D - center).sqrMagnitude < safeRadius * safeRadius)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsRelevant(
            RadarContact contact,
            float shipHeight,
            float heightTolerance)
        {
            return !contact.IsShip ||
                   Mathf.Abs(contact.Position.y - shipHeight) <= heightTolerance;
        }
    }
}
