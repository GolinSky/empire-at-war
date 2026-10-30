using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.Components.Weapon
{
    [BurstCompile]
    internal struct TargetOriginReadJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<float3> Positions;
        [WriteOnly] public NativeArray<quaternion> ParentRotations;

        public void Execute(int index, TransformAccess transform)
        {
            Positions[index] = transform.position;
            // World = parent * local, including an unparented transform (identity parent).
            quaternion world = transform.rotation;
            quaternion local = transform.localRotation;
            ParentRotations[index] = math.mul(world, math.inverse(local));
        }
    }
}
