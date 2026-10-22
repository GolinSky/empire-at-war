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

        // Integers through 2^24 convert to float exactly; preserve double sqrt above that range.
        internal static float Distance(int distanceSquared)
        {
            const int MAX_EXACT_FLOAT_INTEGER = 1 << 24;
            return distanceSquared <= MAX_EXACT_FLOAT_INTEGER
                ? math.sqrt((float)distanceSquared)
                : (float)math.sqrt((double)distanceSquared);
        }

        public void Execute(int y)
        {
            int rowOffset = y * Resolution;
            for (int i = 0; i < Circles.Length; i++)
            {
                FogRevealCircle circle = Circles[i];
                int outerRadius = (int)math.ceil(circle.Radius * (1f + EdgeSoftness));
                int dy = y - circle.CenterY;
                if (dy < -outerRadius || dy > outerRadius) continue;

                int dySquared = dy * dy;
                int sqrOuterRadius = outerRadius * outerRadius;
                const int MAX_SAFE_SQUARE_ROOT = 46340;
                // The public model accepts arbitrary integer circles. Preserve the legacy
                // overflow behavior outside the range where the row-span proof applies.
                bool useRowSpan = outerRadius <= MAX_SAFE_SQUARE_ROOT;
                int minX;
                int maxX;
                if (useRowSpan)
                {
                    // Double precision keeps the integer row boundary exact for large circles.
                    int halfSpan = (int)math.sqrt((double)(sqrOuterRadius - dySquared));
                    minX = math.max(circle.CenterX - halfSpan, 0);
                    maxX = math.min(circle.CenterX + halfSpan, Resolution - 1);
                }
                else
                {
                    minX = math.clamp(circle.CenterX - outerRadius, 0, Resolution - 1);
                    maxX = math.clamp(circle.CenterX + outerRadius, 0, Resolution - 1);
                }
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - circle.CenterX;
                    int distSqr = dx * dx + dySquared;
                    if (!useRowSpan && distSqr > sqrOuterRadius) continue;
                    float distance = Distance(distSqr);
                    float visibility =
                        FogVisibilityModel.SoftVisibility(distance, circle.Radius, EdgeSoftness) * Intensity;
                    int index = rowOffset + x;
                    if (visibility > Target[index]) Target[index] = visibility;
                }
            }
        }
    }
}
