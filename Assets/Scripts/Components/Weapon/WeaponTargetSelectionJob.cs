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
        internal const int FALLBACK_NON_FINITE = 0;
        internal const int FALLBACK_PARENT_TILT = 1;
        internal const int FALLBACK_RANGE_BOUNDARY = 2;
        internal const int FALLBACK_YAW_BOUNDARY = 3;
        internal const int FALLBACK_DEGENERATE_DIRECTION = 4;
        internal const int FALLBACK_TARGET_VERSION = 5;
        internal const int FALLBACK_INVALID_CANDIDATE = 6;
        internal const int FALLBACK_OUT_OF_SPAN = 7;

        [ReadOnly] public NativeArray<Input> Inputs;
        [ReadOnly] public NativeArray<float3> CandidatePositions;
        [ReadOnly] public NativeArray<byte> CandidateIsStrikecraft;
        [WriteOnly] public NativeArray<Result> Results;

        public void Execute(int index)
        {
            Input input = Inputs[index];
            Result result = new Result { CandidateIndex = -1 };
            if (!math.all(math.isfinite(input.Origin)) ||
                !math.all(math.isfinite(input.ParentRotation.value)) ||
                !math.isfinite(input.MaxDistance) || !math.isfinite(input.MinYaw) || !math.isfinite(input.MaxYaw))
            {
                result.RequiresSerialFallback = 1;
                result.FallbackReason = FALLBACK_NON_FINITE;
                Results[index] = result;
                return;
            }
            quaternion inverseParentRotation = math.inverse(input.ParentRotation);
            for (int i = input.CandidateStart; i < input.CandidateStart + input.CandidateCount; i++)
            {
                if (input.StrikecraftOnly != 0 && CandidateIsStrikecraft[i] == 0) continue;
                result.Visited++;
                float3 direction = CandidatePositions[i] - input.Origin;
                // Planar, like WeaponTargetSelector: ships on different height tiers must stay in range.
                float distance = math.length(new float2(direction.x, direction.z));
                if (!math.all(math.isfinite(direction)) || math.length(direction) <= 0.00001f ||
                    math.lengthsq(new float2(direction.x, direction.z)) <= math.lengthsq(direction) * 0.0000000001f)
                {
                    result.RequiresSerialFallback = 1;
                    result.FallbackReason = FALLBACK_DEGENERATE_DIRECTION;
                    Results[index] = result;
                    return;
                }
                if (math.abs(distance - input.MaxDistance) <= 0.0001f * math.max(1f, math.abs(input.MaxDistance)))
                {
                    result.RequiresSerialFallback = 1;
                    result.FallbackReason = FALLBACK_RANGE_BOUNDARY;
                    Results[index] = result;
                    return;
                }
                if (distance > input.MaxDistance) continue;

                quaternion aim = quaternion.LookRotationSafe(direction, new float3(0f, 1f, 0f));
                result.HasInRangeAim = 1;
                result.LastInRangeAim = aim.value;
                quaternion localAim = math.mul(inverseParentRotation, aim);
                float sinPitch = 2f * (localAim.value.y * localAim.value.z - localAim.value.x * localAim.value.w);
                // Unity and Mathematics choose different Euler branches near gimbal lock.
                if (math.abs(sinPitch) >= 0.99999f)
                {
                    result.RequiresSerialFallback = 1;
                    result.FallbackReason = FALLBACK_DEGENERATE_DIRECTION;
                    Results[index] = result;
                    return;
                }
                float localYaw = math.degrees(math.EulerZXY(localAim).y);
                if (math.abs(localYaw) >= 179.999f ||
                    math.abs(localYaw - input.MinYaw) <= 0.001f ||
                    math.abs(localYaw - input.MaxYaw) <= 0.001f)
                {
                    result.RequiresSerialFallback = 1;
                    result.FallbackReason = FALLBACK_YAW_BOUNDARY;
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

        internal struct Input
        {
            public float3 Origin;
            public quaternion ParentRotation;

            public float MaxDistance;
            public float MinYaw;
            public float MaxYaw;

            public int CandidateStart;
            public int CandidateCount;
            public int StrikecraftOnly;
        }

        internal struct Result
        {
            public float4 SelectedAim;
            public float4 LastInRangeAim;

            public int CandidateIndex;
            public int Visited;
            public int RequiresSerialFallback;
            public int FallbackReason;
            public int HasInRangeAim;
        }
    }
}
