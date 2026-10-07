using EmpireAtWar.Entities.CinematicCamera.Model;
using UnityEngine;

namespace EmpireAtWar.Services.Camera
{
    public static class CinematicShotSolver
    {
        private const float WIDE_SHOT_YAW = 40f;

        private const float FRAMING_MARGIN = 1.25f;
        // Low/high shots have the shortest camera offset: approximately 0.9 times the framing distance.
        private const float MIN_SHOT_DISTANCE_FACTOR = 0.9f;

        public static float CalculateFramingDistance(float classDistance, float radius, float fieldOfView)
        {
            float halfFieldOfView = fieldOfView * 0.5f * Mathf.Deg2Rad;
            float hullDistance = radius * FRAMING_MARGIN /
                                 (Mathf.Sin(halfFieldOfView) * MIN_SHOT_DISTANCE_FACTOR);
            return Mathf.Max(classDistance, hullDistance);
        }

        public static Pose InterpolatePose(
            Vector3 currentPosition, Pose desired, Vector3 lookPoint, float blend)
        {
            Vector3 currentOffset = currentPosition - lookPoint;
            Vector3 desiredOffset = desired.position - lookPoint;
            Vector3 offset = Vector3.Slerp(currentOffset, desiredOffset, blend);
            float distance = Mathf.Max(offset.magnitude, desiredOffset.magnitude);
            Vector3 direction = offset.sqrMagnitude > Mathf.Epsilon ? offset.normalized : desiredOffset.normalized;
            Vector3 position = lookPoint + direction * distance;
            return new Pose(position, Quaternion.LookRotation(lookPoint - position, Vector3.up));
        }

        public static Pose Solve(
            CinematicShot shot,
            Vector3 anchor,
            Quaternion anchorRotation,
            Vector3 focusOffset,
            float framingDistance,
            float wideDistanceMultiplier,
            float fieldOfView,
            float progress)
        {
            Vector3 forward = anchorRotation * Vector3.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float d = framingDistance;
            float side = shot.Side;

            Vector3 position;
            Vector3 lookPoint;
            switch (shot.Type)
            {
                case CinematicShotType.Side:
                    position = anchor + right * side * d + Vector3.up * 0.15f * d +
                               forward * Mathf.Lerp(-0.3f, 0.3f, progress) * d;
                    lookPoint = anchor;
                    break;
                case CinematicShotType.FrontRear:
                    position = anchor + forward * side * 1.1f * d + right * 0.3f * d + Vector3.up * 0.2f * d;
                    lookPoint = anchor;
                    break;
                case CinematicShotType.LowHigh:
                    position = anchor - forward * 0.7f * d + right * side * 0.35f * d + Vector3.up * side * 0.45f * d;
                    lookPoint = anchor;
                    break;
                case CinematicShotType.Chase:
                    position = anchor - forward * 0.9f * d + Vector3.up * 0.25f * d;
                    lookPoint = anchor;
                    break;
                case CinematicShotType.Wide:
                    float wideDistance = d * wideDistanceMultiplier +
                        focusOffset.magnitude * FRAMING_MARGIN / Mathf.Sin(fieldOfView * 0.5f * Mathf.Deg2Rad);
                    Vector3 center = anchor + focusOffset;
                    Vector3 direction = Quaternion.Euler(0f, WIDE_SHOT_YAW * side, 0f) * -forward;
                    position = center + direction * wideDistance + Vector3.up * 0.5f * wideDistance;
                    lookPoint = center;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(shot), shot.Type, null);
            }

            return new Pose(position, Quaternion.LookRotation(lookPoint - position, Vector3.up));
        }
    }
}
