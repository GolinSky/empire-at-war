using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

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
            public int RequiresSerialFallback;
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
            if (!math.all(math.isfinite(input.Origin)) ||
                !math.all(math.isfinite(input.ParentRotation.value)) ||
                !math.isfinite(input.MaxDistance) || !math.isfinite(input.MinYaw) || !math.isfinite(input.MaxYaw) ||
                math.abs(input.ParentRotation.value.x) > 0.000001f ||
                math.abs(input.ParentRotation.value.z) > 0.000001f)
            {
                result.RequiresSerialFallback = 1;
                Results[index] = result;
                return;
            }

            quaternion inverseParentRotation = math.inverse(input.ParentRotation);

            for (int i = input.CandidateStart; i < input.CandidateStart + input.CandidateCount; i++)
            {
                result.Visited++;
                float3 direction = CandidatePositions[i] - input.Origin;
                float distance = math.length(direction);
                if (!math.all(math.isfinite(direction)) || distance <= 0.00001f ||
                    math.lengthsq(new float2(direction.x, direction.z)) <= math.lengthsq(direction) * 0.0000000001f ||
                    math.abs(distance - input.MaxDistance) <= 0.0001f * math.max(1f, math.abs(input.MaxDistance)))
                {
                    result.RequiresSerialFallback = 1;
                    Results[index] = result;
                    return;
                }

                if (distance > input.MaxDistance) continue;

                quaternion aim = quaternion.LookRotationSafe(direction, new float3(0f, 1f, 0f));
                result.HasInRangeAim = 1;
                result.LastInRangeAim = aim.value;

                float3 localDirection = math.mul(inverseParentRotation, math.normalizesafe(direction, new float3(0f, 0f, 1f)));
                float localYaw = math.degrees(math.atan2(localDirection.x, localDirection.z));
                if (math.abs(localYaw) >= 179.999f ||
                    math.abs(localYaw - input.MinYaw) <= 0.001f ||
                    math.abs(localYaw - input.MaxYaw) <= 0.001f)
                {
                    result.RequiresSerialFallback = 1;
                    Results[index] = result;
                    return;
                }

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
