using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Models.FogOfWar
{
    /// <summary>Clears reveal targets; cells seen before keep <see cref="BaseVisibility"/>.</summary>
    [BurstCompile]
    internal struct FogResetJob : IJob
    {
        [ReadOnly] public NativeArray<float> Current;
        public NativeArray<float> Target;
        public float BaseVisibility;

        public void Execute()
        {
            for (int i = 0; i < Target.Length; i++)
            {
                Target[i] = math.select(0f, BaseVisibility, Current[i] > 0f);
            }
        }
    }
}
