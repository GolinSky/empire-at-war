using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.Services.Radar
{
    [BurstCompile]
    internal struct RadarPositionJob : IJobParallelForTransform
    {
        // Pre-filled with NaN: a destroyed transform keeps NaN and fails every range test.
        [WriteOnly] public NativeArray<float3> Positions;

        public void Execute(int index, TransformAccess transform)
        {
            if (transform.isValid)
            {
                Positions[index] = transform.position;
            }
        }
    }
}
