using System;

namespace EmpireAtWar.Models.FogOfWar
{
    public static class FogVisibilityModel
    {
        public static float CalculateSoftVisibility(
            float distance,
            float radius,
            float edgeSoftness)
        {
            if (distance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distance));
            }

            if (radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            if (edgeSoftness < 0f || edgeSoftness > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(edgeSoftness));
            }

            if (edgeSoftness == 0f)
            {
                return distance <= radius ? 1f : 0f;
            }

            float featherWidth = radius * edgeSoftness;
            float innerRadius = radius - featherWidth;
            float outerRadius = radius + featherWidth;

            if (distance <= innerRadius)
            {
                return 1f;
            }

            if (distance >= outerRadius)
            {
                return 0f;
            }

            float transition = (distance - innerRadius) /
                               (outerRadius - innerRadius);
            float smoothTransition = transition * transition *
                                     (3f - 2f * transition);
            return 1f - smoothTransition;
        }
    }
}
