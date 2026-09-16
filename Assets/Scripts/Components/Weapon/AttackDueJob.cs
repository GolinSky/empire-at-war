using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace EmpireAtWar.Components.Weapon
{
    [BurstCompile]
    internal struct AttackDueJob : IJobParallelFor
    {
        internal struct Input
        {
            public float DueTime;
            public int EarliestFrame;
        }

        [ReadOnly] public NativeArray<Input> Inputs;
        [WriteOnly] public NativeArray<byte> Results;
        public float Now;
        public int Frame;

        public void Execute(int index)
        {
            Input input = Inputs[index];
            Results[index] = (byte)(input.EarliestFrame <= Frame && input.DueTime <= Now ? 1 : 0);
        }
    }
}
