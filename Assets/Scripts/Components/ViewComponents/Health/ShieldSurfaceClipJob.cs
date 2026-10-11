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

        [ReadOnly] public NativeArray<float4> Planes;
        public float3 Origin;
        public float3 Direction;
        [WriteOnly] public NativeArray<float> Enter;

        public void Execute()
        {
            float enter = float.NegativeInfinity;
            float exit = float.PositiveInfinity;
            for (int i = 0; i < Planes.Length; i++)
            {
                float3 normal = Planes[i].xyz;
                float approach = math.dot(normal, Direction);
                float gap = Planes[i].w - math.dot(normal, Origin);
                if (approach < 0f) enter = math.max(enter, gap / approach);
                else if (approach > 0f) exit = math.min(exit, gap / approach);
                else if (gap < 0f)
                {
                    Enter[0] = MISS;
                    return;
                }
            }

            // An origin inside the shield must not shoot through the hull to its far surface.
            Enter[0] = enter > exit || enter < 0f || enter > 1f ? MISS : enter;
        }
    }
}
