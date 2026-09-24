using System;
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
            if (mapRange == null)
            {
                throw new ArgumentNullException(nameof(mapRange));
            }

            if (margin < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(margin));
            }

            point.x = Mathf.Clamp(point.x, mapRange.Min.x + margin, mapRange.Max.x - margin);
            point.z = Mathf.Clamp(point.z, mapRange.Min.y + margin, mapRange.Max.y - margin);
            return point;
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
            if (contacts == null)
            {
                throw new ArgumentNullException(nameof(contacts));
            }

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
                if (!IsRelevant(contact))
                {
                    continue;
                }

                if (!BlocksPoint(
                        requestedDestination,
                        contact,
                        shipHeight,
                        heightTolerance,
                        clearance))
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

        public static bool IsRouteClear(
            ShipBezierRoute route,
            IReadOnlyList<RadarContact> contacts,
            float shipHeight,
            float heightTolerance,
            float clearance)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (contacts == null)
            {
                throw new ArgumentNullException(nameof(contacts));
            }

            Vector3[] samples = route.Samples;
            for (int contactIndex = 0;
                 contactIndex < contacts.Count;
                 contactIndex++)
            {
                RadarContact contact = contacts[contactIndex];
                if (!IsRelevant(contact))
                {
                    continue;
                }

                Vector2 center = new Vector2(
                    contact.Position.x,
                    contact.Position.z);
                float safeRadius = contact.Radius + clearance;
                float safeRadiusSquared = safeRadius * safeRadius;
                for (int sampleIndex = 1;
                     sampleIndex < samples.Length;
                     sampleIndex++)
                {
                    Vector2 start = new Vector2(
                        samples[sampleIndex - 1].x,
                        samples[sampleIndex - 1].z);
                    Vector2 end = new Vector2(
                        samples[sampleIndex].x,
                        samples[sampleIndex].z);
                    if (DistanceToSegmentSquared(center, start, end) <
                        safeRadiusSquared)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool IsPointClear(
            Vector3 point,
            IReadOnlyList<RadarContact> contacts,
            float shipHeight,
            float heightTolerance,
            float clearance)
        {
            for (int i = 0; i < contacts.Count; i++)
            {
                RadarContact contact = contacts[i];
                if (!IsRelevant(contact))
                {
                    continue;
                }

                if (BlocksPoint(
                        point,
                        contact,
                        shipHeight,
                        heightTolerance,
                        clearance))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool BlocksPoint(
            Vector3 point,
            RadarContact contact,
            float shipHeight,
            float heightTolerance,
            float clearance)
        {
            if (!IsRelevant(contact))
            {
                return false;
            }

            Vector2 point2D = new Vector2(point.x, point.z);
            Vector2 center = new Vector2(
                contact.Position.x,
                contact.Position.z);
            float safeRadius = contact.Radius + clearance;
            return (point2D - center).sqrMagnitude <
                   safeRadius * safeRadius;
        }

        private static bool IsRelevant(RadarContact contact)
        {
            return !contact.IsShip;
        }

        private static float DistanceToSegmentSquared(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return (point - start).sqrMagnitude;
            }

            float parameter = Mathf.Clamp01(
                Vector2.Dot(point - start, segment) / lengthSquared);
            return (point - (start + segment * parameter)).sqrMagnitude;
        }
    }
}
