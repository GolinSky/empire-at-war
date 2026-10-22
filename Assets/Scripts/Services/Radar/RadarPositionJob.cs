using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.Services.Radar
{
    [BurstCompile]
    internal struct RadarPositionJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<float> PositionX;
        [WriteOnly] public NativeArray<float> PositionZ;

        public void Execute(int index, TransformAccess transform)
        {
            float3 position = transform.isValid ? (float3)transform.position : new float3(float.NaN);
            PositionX[index] = position.x;
            PositionZ[index] = position.z;
        }
    }
}
