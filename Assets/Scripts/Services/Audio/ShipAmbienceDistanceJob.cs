using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.Audio
{
    [BurstCompile]
    internal struct ShipAmbienceDistanceJob : IJob
    {
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<byte> Audible;
        public float2 Focus;
        public int Count;
        [WriteOnly] public NativeArray<float> MinimumDistanceSquared;

        public void Execute()
        {
            float minimum = float.PositiveInfinity;
            for (int i = 0; i < Count; i++)
                if (Audible[i] != 0)
                    minimum = math.min(minimum, math.distancesq(Focus, Positions[i]));
            MinimumDistanceSquared[0] = minimum;
        }
    }
}
