using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public static class WeaponTargetSelector
    {
        public static bool TryCalculateAim(Vector3 targetPosition, Vector3 origin,
            Quaternion parentRotation, float maxDistance, float minYaw, float maxYaw,
            out Quaternion worldRotation, out bool isInRange)
        {
            isInRange = !(Vector3.Distance(targetPosition, origin) > maxDistance);
            if (!isInRange)
            {
                worldRotation = default;
                return false;
            }

            Vector3 direction = targetPosition - origin;
            worldRotation = Quaternion.LookRotation(direction, Vector3.up);
            float localYaw = (Quaternion.Inverse(parentRotation) * worldRotation).eulerAngles.y;
            if (localYaw > 180f) localYaw -= 360f;
            return !(localYaw > maxYaw || localYaw < minYaw);
        }
    }
}
