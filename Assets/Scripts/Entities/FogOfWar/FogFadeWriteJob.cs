using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Models.FogOfWar
{
    /// <summary>Fades visibility and writes the R8 mask in the same pass.</summary>
    [BurstCompile]
    internal struct FogFadeWriteJob : IJob
    {
        private const float BYTE_MAX = 255f;

        public NativeArray<float> Current;
        [ReadOnly] public NativeArray<float> Target;
        [WriteOnly] public NativeArray<byte> Pixels;
        [WriteOnly] public NativeArray<int> Changed;
        public float MaxDelta;

        public void Execute()
        {
            int changed = 0;
            for (int i = 0; i < Current.Length; i++)
            {
                float delta = Target[i] - Current[i];
                float next = FogFadeJob.Step(Current[i], Target[i], delta, MaxDelta);
                Current[i] = next;
                Pixels[i] = (byte)(next * BYTE_MAX + 0.5f);
                changed |= math.select(0, 1, delta != 0f);
            }

            Changed[0] = changed;
        }
    }
}
