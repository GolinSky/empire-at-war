using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>Reads every target slot's world position in parallel on worker threads; index = slot.</summary>
    [BurstCompile]
    internal struct TargetSlotPositionJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<float3> Positions;

        public void Execute(int index, TransformAccess transform)
        {
            // A freed slot may keep a destroyed transform; nothing reads that slot.
            if (!transform.isValid) return;
            Positions[index] = transform.position;
        }
    }
}
