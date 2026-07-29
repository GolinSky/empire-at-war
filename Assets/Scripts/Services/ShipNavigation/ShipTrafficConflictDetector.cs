using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal static class ShipTrafficConflictDetector
    {
        public static bool TryGetSegmentConflict(
            Vector3 firstStart,
            Vector3 firstEnd,
            Vector3 secondStart,
            Vector3 secondEnd,
            float safeDistanceSquared,
            out float firstParameter,
            out float secondParameter)
        {
            Vector2 firstStart2D = ToPlanar(firstStart);
            Vector2 firstEnd2D = ToPlanar(firstEnd);
            Vector2 secondStart2D = ToPlanar(secondStart);
            Vector2 secondEnd2D = ToPlanar(secondEnd);
            Vector2 firstDirection = firstEnd2D - firstStart2D;
            Vector2 secondDirection = secondEnd2D - secondStart2D;
            Vector2 startDelta = secondStart2D - firstStart2D;
            float denominator = Cross(firstDirection, secondDirection);
            if (Mathf.Abs(denominator) > Mathf.Epsilon)
            {
                firstParameter =
                    Cross(startDelta, secondDirection) / denominator;
                secondParameter =
                    Cross(startDelta, firstDirection) / denominator;
                if (firstParameter >= 0f && firstParameter <= 1f &&
                    secondParameter >= 0f && secondParameter <= 1f)
                {
                    return true;
                }
            }

            float bestDistance = PointSegmentDistanceSquared(
                firstStart2D,
                secondStart2D,
                secondEnd2D,
                out float secondAtFirstStart);
            firstParameter = 0f;
            secondParameter = secondAtFirstStart;

            float distance = PointSegmentDistanceSquared(
                firstEnd2D,
                secondStart2D,
                secondEnd2D,
                out float secondAtFirstEnd);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                firstParameter = 1f;
                secondParameter = secondAtFirstEnd;
            }

            distance = PointSegmentDistanceSquared(
                secondStart2D,
                firstStart2D,
                firstEnd2D,
                out float firstAtSecondStart);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                firstParameter = firstAtSecondStart;
                secondParameter = 0f;
            }

            distance = PointSegmentDistanceSquared(
                secondEnd2D,
                firstStart2D,
                firstEnd2D,
                out float firstAtSecondEnd);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                firstParameter = firstAtSecondEnd;
                secondParameter = 1f;
            }

            return bestDistance < safeDistanceSquared;
        }

        private static float PointSegmentDistanceSquared(
            Vector2 point,
            Vector2 start,
            Vector2 end,
            out float parameter)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                parameter = 0f;
                return (point - start).sqrMagnitude;
            }

            parameter = Mathf.Clamp01(
                Vector2.Dot(point - start, segment) / lengthSquared);
            return (point - (start + segment * parameter)).sqrMagnitude;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

        private static Vector2 ToPlanar(Vector3 point)
        {
            return new Vector2(point.x, point.z);
        }
    }
}
