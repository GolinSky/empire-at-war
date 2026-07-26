using System;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public static class ShipRotationKinematics
    {
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
