using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Models.FogOfWar
{
    /// <summary>
    /// Raises reveal targets inside every queued soft-edged vision circle. One index is one grid row, so rows run
    /// in parallel and each cell is written by exactly one worker.
    /// </summary>
    [BurstCompile]
    internal struct FogRevealJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> Target;
        [ReadOnly] public NativeArray<FogRevealCircle> Circles;
        public int Resolution;
        public float EdgeSoftness;
        public float Intensity;

        public void Execute(int y)
        {
            int rowOffset = y * Resolution;
            for (int i = 0; i < Circles.Length; i++)
            {
                FogRevealCircle circle = Circles[i];
                int outerRadius = (int)math.ceil(circle.Radius * (1f + EdgeSoftness));
                int dy = y - circle.CenterY;
                if (dy < -outerRadius || dy > outerRadius) continue;

                int minX = math.clamp(circle.CenterX - outerRadius, 0, Resolution - 1);
                int maxX = math.clamp(circle.CenterX + outerRadius, 0, Resolution - 1);
                int sqrOuterRadius = outerRadius * outerRadius;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - circle.CenterX;
                    int distSqr = dx * dx + dy * dy;
                    if (distSqr > sqrOuterRadius) continue;

                    // Double sqrt matches the former cached falloff table exactly.
                    float distance = (float)math.sqrt((double)distSqr);
                    float visibility =
                        FogVisibilityModel.SoftVisibility(distance, circle.Radius, EdgeSoftness) * Intensity;
                    int index = rowOffset + x;
                    if (visibility > Target[index]) Target[index] = visibility;
                }
            }
        }
    }
}
