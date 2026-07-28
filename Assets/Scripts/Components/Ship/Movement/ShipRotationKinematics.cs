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

        public static float CalculateBankAngle(
            Quaternion currentRotation,
            Vector3 targetDirection,
            float degreesPerSecond,
            float deltaTime,
            float maximumBankAngle)
        {
            if (degreesPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(degreesPerSecond));
            }

            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (maximumBankAngle < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumBankAngle));
            }

            if (targetDirection.sqrMagnitude <= Mathf.Epsilon ||
                deltaTime <= Mathf.Epsilon)
            {
                return 0f;
            }

            float requestedTurn = Vector3.SignedAngle(
                currentRotation * Vector3.forward,
                targetDirection,
                Vector3.up);
            float maximumTurnStep = degreesPerSecond * deltaTime;
            float turnRatio = Mathf.Clamp(
                requestedTurn / maximumTurnStep,
                -1f,
                1f);
            return -turnRatio * maximumBankAngle;
        }

        public static Quaternion Step(
            Quaternion currentRotation,
            Vector3 targetDirection,
            float degreesPerSecond,
            float deltaTime)
        {
            if (degreesPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(degreesPerSecond));
            }

            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return currentRotation;
            }

            Quaternion targetRotation = Quaternion.LookRotation(
                targetDirection.normalized,
                Vector3.up);
            return Quaternion.RotateTowards(
                currentRotation,
                targetRotation,
                degreesPerSecond * deltaTime);
        }
    }
}
