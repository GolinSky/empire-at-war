using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.Services.Audio
{
    [BurstCompile]
    internal struct ShipAmbiencePositionJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<float2> Positions;

        public void Execute(int index, TransformAccess transform)
        {
            Positions[index] = transform.isValid
                ? new float2(transform.position.x, transform.position.z)
                : new float2(float.PositiveInfinity);
        }
    }
}
