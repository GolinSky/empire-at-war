using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.ViewComponents.Health
{
    /// <summary>
    /// Clips the segment Origin → Origin + Direction against every shell plane (inside where dot(xyz, p) &lt;= w).
    /// Enter[0] is the segment fraction where it enters the shell, or -1 when it misses or starts inside.
    /// </summary>
    [BurstCompile]
    internal struct ShieldSurfaceClipJob : IJob
    {
        public const float MISS = -1f;

        [ReadOnly] public NativeArray<float4x4> Planes;
        public float3 Origin;
        public float3 Direction;
        [WriteOnly] public NativeArray<float> Enter;

        public void Execute()
        {
            float enter = float.NegativeInfinity;
            float exit = float.PositiveInfinity;
            bool4 miss = false;
            for (int i = 0; i < Planes.Length; i++)
            {
                // Columns hold x/y/z/w for four consecutive planes.
                float4x4 planes = Planes[i];
                float4 approach = planes.c0 * Direction.x + planes.c1 * Direction.y + planes.c2 * Direction.z;
                float4 gap = planes.c3 - (planes.c0 * Origin.x + planes.c1 * Origin.y + planes.c2 * Origin.z);
                bool4 entering = approach < 0f;
                bool4 exiting = approach > 0f;
                float4 fraction = gap / approach;
                float4 entries = math.select(new float4(float.NegativeInfinity), fraction, entering);
                float4 exits = math.select(new float4(float.PositiveInfinity), fraction, exiting);
                // Preserve plane order for equal values, including positive/negative zero.
                enter = math.max(math.max(math.max(math.max(enter, entries.x), entries.y), entries.z), entries.w);
                exit = math.min(math.min(math.min(math.min(exit, exits.x), exits.y), exits.z), exits.w);
                // Also preserves the original else branch for an unordered (NaN) approach.
                miss |= !(entering | exiting) & (gap < 0f);
            }

            // An origin inside the shield must not shoot through the hull to its far surface.
            Enter[0] = math.select(enter, MISS, math.any(miss) | enter > exit | enter < 0f | enter > 1f);
        }
    }
}
