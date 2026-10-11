using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Models.FogOfWar
{
    /// <summary>Raises reveal targets inside one soft-edged vision circle.</summary>
    [BurstCompile]
    internal struct FogRevealJob : IJob
    {
        public NativeArray<float> Target;
        public int Resolution;
        public int CenterX;
        public int CenterY;
        public int Radius;
        public float EdgeSoftness;
        public float Intensity;

        public void Execute()
        {
            int outerRadius = (int)math.ceil(Radius * (1f + EdgeSoftness));
            int minX = math.clamp(CenterX - outerRadius, 0, Resolution - 1);
            int maxX = math.clamp(CenterX + outerRadius, 0, Resolution - 1);
            int minY = math.clamp(CenterY - outerRadius, 0, Resolution - 1);
            int maxY = math.clamp(CenterY + outerRadius, 0, Resolution - 1);
            int sqrOuterRadius = outerRadius * outerRadius;

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - CenterY;
                int rowOffset = y * Resolution;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - CenterX;
                    int distSqr = dx * dx + dy * dy;
                    if (distSqr > sqrOuterRadius) continue;

                    // Double sqrt matches the former cached falloff table exactly.
                    float distance = (float)math.sqrt((double)distSqr);
                    float visibility = FogVisibilityModel.SoftVisibility(distance, Radius, EdgeSoftness) * Intensity;
                    int index = rowOffset + x;
                    if (visibility > Target[index]) Target[index] = visibility;
                }
            }
        }
    }
}
