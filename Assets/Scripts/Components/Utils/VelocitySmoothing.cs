using UnityEngine;

namespace EmpireAtWar.Utils
{
    public static class VelocitySmoothing
    {
        public static Vector2 MoveTowardsTarget(
            Vector2 currentVelocity,
            Vector2 inputDirection,
            float maximumSpeed,
            float acceleration,
            float deceleration,
            float deltaTime)
        {
            Vector2 targetVelocity = Vector2.ClampMagnitude(inputDirection, 1f) * maximumSpeed;
            float changeRate = targetVelocity.sqrMagnitude > Mathf.Epsilon
                ? acceleration
                : deceleration;
            return Vector2.MoveTowards(
                currentVelocity,
                targetVelocity,
                changeRate * deltaTime);
        }
    }
}
