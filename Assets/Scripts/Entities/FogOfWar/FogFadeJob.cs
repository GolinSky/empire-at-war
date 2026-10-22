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

        internal static float Step(float current, float target, float delta, float maxDelta)
        {
            float next = math.select(
                current + math.select(-maxDelta, maxDelta, delta > 0f),
                target, math.abs(delta) <= maxDelta);
            return math.select(next, current, delta == 0f);
        }

        public void Execute()
        {
            int changed = 0;
            for (int i = 0; i < Current.Length; i++)
            {
                float delta = Target[i] - Current[i];
                Current[i] = Step(Current[i], Target[i], delta, MaxDelta);
                changed |= math.select(0, 1, delta != 0f);
            }

            Changed[0] = changed;
        }
    }
}
