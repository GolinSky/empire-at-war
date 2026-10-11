using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Models.FogOfWar
{
    /// <summary>Moves every cell toward its target by at most <see cref="MaxDelta"/>; Changed[0] is 1 when any cell moved.</summary>
    [BurstCompile]
    internal struct FogFadeJob : IJob
    {
        public NativeArray<float> Current;
        [ReadOnly] public NativeArray<float> Target;
        public float MaxDelta;
        public NativeArray<int> Changed;

        public void Execute()
        {
            int changed = 0;
            for (int i = 0; i < Current.Length; i++)
            {
                float delta = Target[i] - Current[i];
                if (delta == 0f) continue;

                Current[i] = math.abs(delta) <= MaxDelta
                    ? Target[i]
                    : Current[i] + (delta > 0f ? MaxDelta : -MaxDelta);
                changed = 1;
            }

            Changed[0] = changed;
        }
    }
}
