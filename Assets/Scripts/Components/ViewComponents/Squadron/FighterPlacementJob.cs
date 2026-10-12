using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.ViewComponents.Squadrons
{
    /// <summary>Places each fighter root on its flight path, nose along its heading; index = pose slot.</summary>
    [BurstCompile]
    internal struct FighterPlacementJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<FighterPose> Poses;

        public void Execute(int index, TransformAccess transform)
        {
            FighterPose pose = Poses[index];
            if (pose.IsActive == 0 || !transform.isValid) return;
            transform.position = pose.Position;
            transform.rotation = quaternion.LookRotationSafe(pose.Forward, math.up());
        }
    }
}
