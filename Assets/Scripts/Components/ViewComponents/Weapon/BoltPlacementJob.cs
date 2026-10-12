using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Places every flying bolt on its muzzle-to-aim line; index = bolt in <see cref="BoltFlightBatch"/>.</summary>
    [BurstCompile]
    internal struct BoltPlacementJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<BoltPose> Poses;

        public void Execute(int index, TransformAccess transform)
        {
            if (!transform.isValid) return;
            BoltPose pose = Poses[index];
            transform.position = pose.Start + pose.Path * pose.Progress;
            if (math.lengthsq(pose.Path) > 0f) transform.rotation = quaternion.LookRotationSafe(pose.Path, math.up());
        }
    }
}
