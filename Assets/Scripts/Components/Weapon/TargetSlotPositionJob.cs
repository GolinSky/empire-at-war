using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>Reads one stripe of ship slots' world positions in parallel on worker threads; index = slot.</summary>
    [BurstCompile]
    internal struct TargetSlotPositionJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<float3> Positions;
        /// <summary>1 = ship slot read here; 0 = fighter slot, read from its flight-model pose instead.</summary>
        [ReadOnly] public NativeArray<byte> TransformSlots;
        public int Stripe;
        public int StripeCount;

        public void Execute(int index, TransformAccess transform)
        {
            if (TransformSlots[index] == 0 || index % StripeCount != Stripe) return;
            // A freed slot may keep a destroyed transform; nothing reads that slot.
            if (!transform.isValid) return;
            Positions[index] = transform.position;
        }
    }
}
