using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Components.Weapon
{
    [BurstCompile]
    internal struct WeaponTargetSelectionJob : IJobParallelFor
    {
        internal struct Input
        {
            public float3 Origin;
            public quaternion ParentRotation;
            public float MaxDistance;
            public float MinYaw;
            public float MaxYaw;
            public int CandidateStart;
            public int CandidateCount;
        }

        internal struct Result
        {
            public int CandidateIndex;
            public int Visited;
            public int HasInRangeAim;
            public float4 SelectedAim;
            public float4 LastInRangeAim;
        }

        [ReadOnly] public NativeArray<Input> Inputs;
        [ReadOnly] public NativeArray<float3> CandidatePositions;
        [WriteOnly] public NativeArray<Result> Results;

        public void Execute(int index)
        {
            Input input = Inputs[index];
            Result result = new Result { CandidateIndex = -1 };
            quaternion inverseParentRotation = math.inverse(input.ParentRotation);

            for (int i = input.CandidateStart; i < input.CandidateStart + input.CandidateCount; i++)
            {
                result.Visited++;
                float3 direction = CandidatePositions[i] - input.Origin;
                if (math.length(direction) > input.MaxDistance) continue;

                quaternion aim = quaternion.LookRotationSafe(direction, new float3(0f, 1f, 0f));
                result.HasInRangeAim = 1;
                result.LastInRangeAim = aim.value;

                float3 localDirection = math.mul(inverseParentRotation, math.normalizesafe(direction, new float3(0f, 0f, 1f)));
                float localYaw = math.degrees(math.atan2(localDirection.x, localDirection.z));
                if (localYaw > input.MaxYaw || localYaw < input.MinYaw) continue;

                result.CandidateIndex = i;
                result.SelectedAim = aim.value;
                Results[index] = result;
                return;
            }

            Results[index] = result;
        }
    }
}
