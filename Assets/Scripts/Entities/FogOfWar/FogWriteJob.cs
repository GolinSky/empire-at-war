using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace EmpireAtWar.Models.FogOfWar
{
    /// <summary>Converts visibility to one byte per cell.</summary>
    [BurstCompile]
    internal struct FogWriteJob : IJob
    {
        private const float BYTE_MAX = 255f;

        [ReadOnly] public NativeArray<float> Current;
        [WriteOnly] public NativeArray<byte> Pixels;

        public void Execute()
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                Pixels[i] = (byte)(Current[i] * BYTE_MAX + 0.5f);
            }
        }
    }
}
