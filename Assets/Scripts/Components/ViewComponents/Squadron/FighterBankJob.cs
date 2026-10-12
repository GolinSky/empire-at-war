using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.ViewComponents.Squadrons
{
    /// <summary>Rolls each fighter body into its turn; index = pose slot.</summary>
    [BurstCompile]
    internal struct FighterBankJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<FighterPose> Poses;

        public void Execute(int index, TransformAccess transform)
        {
            FighterPose pose = Poses[index];
            if (pose.IsActive == 0 || !transform.isValid) return;
            transform.localRotation = math.mul(
                quaternion.AxisAngle(new float3(0f, 0f, 1f), math.radians(pose.Bank)), pose.BodyRestRotation);
        }
    }
}
