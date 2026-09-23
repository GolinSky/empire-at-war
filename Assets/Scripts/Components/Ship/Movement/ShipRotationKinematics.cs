using System;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public static class ShipRotationKinematics
    {
        public static float CalculateMinimumTurnRadius(
            float speed,
            float degreesPerSecond)
        {
            if (speed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed));
            }

            if (degreesPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(degreesPerSecond));
            }

            return speed / (degreesPerSecond * Mathf.Deg2Rad);
        }

        public static float CalculateTurnDuration(
            Quaternion currentRotation,
            Vector3 targetDirection,
            float degreesPerSecond)
        {
            if (degreesPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(degreesPerSecond));
            }

            if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            Quaternion targetRotation = Quaternion.LookRotation(
                targetDirection.normalized,
                Vector3.up);
            return Quaternion.Angle(currentRotation, targetRotation) / degreesPerSecond;
        }

        public static Quaternion StepYaw(
            Quaternion current,
            Vector3 targetDirection,
            ref float angularVelocity,
            float maxRate,
            float acceleration,
            float deltaTime)
        {
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude <= Mathf.Epsilon || deltaTime <= 0f)
            {
                return current;
            }

            float remainingAngle = Vector3.SignedAngle(
                current * Vector3.forward,
                targetDirection,
                Vector3.up);
            float velocityStep = acceleration * deltaTime;
            if (Mathf.Abs(remainingAngle) < 0.1f &&
                Mathf.Abs(angularVelocity) < velocityStep)
            {
                angularVelocity = 0f;
                return Quaternion.LookRotation(targetDirection, Vector3.up);
            }

            float brakingRate = Mathf.Sqrt(
                2f * acceleration * Mathf.Abs(remainingAngle));
            float desiredVelocity = Mathf.Sign(remainingAngle) *
                Mathf.Min(maxRate, brakingRate);
            angularVelocity = Mathf.MoveTowards(
                angularVelocity,
                desiredVelocity,
                velocityStep);
            float yawStep = angularVelocity * deltaTime;
            if (Mathf.Sign(yawStep) == Mathf.Sign(remainingAngle) &&
                Mathf.Abs(yawStep) >= Mathf.Abs(remainingAngle))
            {
                angularVelocity = 0f;
                return Quaternion.LookRotation(targetDirection, Vector3.up);
            }

            return Quaternion.AngleAxis(yawStep, Vector3.up) * current;
        }

        public static float CalculateBankFromYawRate(
            float angularVelocity,
            float maxRate,
            float maximumBankAngle)
        {
            return -maximumBankAngle *
                Mathf.Clamp(angularVelocity / maxRate, -1f, 1f);
        }
    }
}
