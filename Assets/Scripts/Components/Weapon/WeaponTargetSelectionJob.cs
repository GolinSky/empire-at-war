using System.Runtime.CompilerServices;
using EmpireAtWar.Components.AttackComponent;
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

        // Group eligibility values. A doomed group already has a killing volley in flight; it is picked only when
        // no other group of the same preference is in reach.
        internal const byte GROUP_INELIGIBLE = 0;
        internal const byte GROUP_ELIGIBLE = 1;
        internal const byte GROUP_DOOMED = 2;

        [ReadOnly] public NativeArray<Input> Inputs;
        // Each candidate is a hardpoint slot plus its target group, local to the owner's group block.
        [ReadOnly] public NativeArray<int> CandidateSlots;
        [ReadOnly] public NativeArray<int> CandidateGroups;
        [ReadOnly] public NativeArray<byte> GroupEligible;
        // Indexed by slot (see TargetSelectionCapture).
        [ReadOnly] public NativeArray<float3> SlotPositions;
        [ReadOnly] public NativeArray<byte> SlotFlags;
        [WriteOnly] public NativeArray<Result> Results;

        public void Execute(int index)
        {
            Input input = Inputs[index];
            Result result = new Result { CandidateIndex = -1, NearestDistance = float.PositiveInfinity };
            if (!math.all(math.isfinite(input.Origin)) ||
                !math.all(math.isfinite(input.ParentRotation.value)) ||
                !math.isfinite(input.MaxDistance) || !math.isfinite(input.MinYaw) || !math.isfinite(input.MaxYaw))
            {
                result.RequiresSerialFallback = 1;
                result.FallbackReason = FALLBACK_NON_FINITE;
                Results[index] = result;
                return;
            }
            float nearestDistanceSquared = float.PositiveInfinity;
            float rangeTolerance = 0.0001f * math.max(1f, math.abs(input.MaxDistance));
            // Leave a generous rounding margin around the exact serial-fallback band.
            float farDistance = math.max(0f, input.MaxDistance) + 2f * rangeTolerance;
            float farDistanceSquared = farDistance * farDistance;
            quaternion inverseParentRotation = math.inverse(input.ParentRotation);
            // Preferred targets first; the rest only when none is in reach. Each candidate is visited once.
            // The first group with a hardpoint in reach is the target; its nearest reachable hardpoint is hit.
            bool hasDoomed = input.HasDoomed != 0;
            int passes = GetPassCount(input.Preference, hasDoomed);
            int bestGroup = -1;
            float bestDistanceSquared = float.PositiveInfinity;
            for (int pass = 0; pass < passes && result.CandidateIndex < 0; pass++)
            for (int i = input.CandidateStart; i < input.CandidateStart + input.CandidateCount; i++)
            {
                // A group's candidates are contiguous, so leaving the chosen group ends the search.
                if (result.CandidateIndex >= 0 && CandidateGroups[i] != bestGroup) break;
                byte eligibility = GroupEligible[input.GroupStart + CandidateGroups[i]];
                if (eligibility == GROUP_INELIGIBLE) continue;
                int slot = CandidateSlots[i];
                byte flags = SlotFlags[slot];
                if ((flags & TargetSelectionCapture.SLOT_VALID) == 0) continue;
                if (input.StrikecraftOnly != 0 && (flags & TargetSelectionCapture.SLOT_STRIKECRAFT) == 0) continue;
                if (input.ShipsOnly != 0 && (flags & TargetSelectionCapture.SLOT_STRIKECRAFT) != 0) continue;
                if (!MatchesPass(pass, hasDoomed, IsPreferred(input.Preference, flags), eligibility)) continue;
                result.Visited++;
                float3 direction = SlotPositions[slot] - input.Origin;
                // Planar, like WeaponTargetSelector: ships on different height tiers must stay in range.
                float distanceSquared = math.lengthsq(new float2(direction.x, direction.z));
                float directionSquared = math.lengthsq(direction);
                nearestDistanceSquared = math.min(nearestDistanceSquared, distanceSquared);
                // Exact float cutoff for the original rounded sqrt <= 0.00001f comparison.
                if (!math.all(math.isfinite(direction)) ||
                    directionSquared <= 0.0000000001f ||
                    distanceSquared <= directionSquared * 0.0000000001f)
                {
                    result.RequiresSerialFallback = 1;
                    result.FallbackReason = FALLBACK_DEGENERATE_DIRECTION;
                    result.NearestDistance = math.sqrt(nearestDistanceSquared);
                    Results[index] = result;
                    return;
                }
                if (distanceSquared > farDistanceSquared) continue;
                float distance = math.sqrt(distanceSquared);
                if (math.abs(distance - input.MaxDistance) <= rangeTolerance)
                {
                    result.RequiresSerialFallback = 1;
                    result.FallbackReason = FALLBACK_RANGE_BOUNDARY;
                    result.NearestDistance = math.sqrt(nearestDistanceSquared);
                    Results[index] = result;
                    return;
                }
                if (distance > input.MaxDistance || distanceSquared >= bestDistanceSquared) continue;

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
                    result.NearestDistance = math.sqrt(nearestDistanceSquared);
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
                    result.NearestDistance = math.sqrt(nearestDistanceSquared);
                    Results[index] = result;
                    return;
                }
                if (localYaw > input.MaxYaw || localYaw < input.MinYaw) continue;
                result.CandidateIndex = i;
                result.SelectedAim = aim.value;
                bestGroup = CandidateGroups[i];
                bestDistanceSquared = distanceSquared;
            }
            result.NearestDistance = math.sqrt(nearestDistanceSquared);
            Results[index] = result;
        }

        /// <summary>One pass per preference tier, doubled when doomed groups need their own fallback pass.</summary>
        internal static int GetPassCount(int preference, bool hasDoomed) =>
            (preference == (int)TargetPreference.Any ? 1 : 2) * (hasDoomed ? 2 : 1);

        // Pass order: preferred live, preferred doomed, other live, other doomed.
        internal static bool MatchesPass(int pass, bool hasDoomed, bool preferred, byte eligibility) =>
            MatchesPreference(pass, hasDoomed, preferred) && MatchesDoomedTier(pass, hasDoomed, eligibility);

        internal static bool MatchesPreference(int pass, bool hasDoomed, bool preferred) =>
            preferred == (pass / (hasDoomed ? 2 : 1) == 0);

        internal static bool MatchesDoomedTier(int pass, bool hasDoomed, byte eligibility) =>
            (eligibility == GROUP_DOOMED) == (hasDoomed && pass % 2 == 1);

        internal static bool IsPreferred(int preference, byte slotFlags) =>
            preference == (int)TargetPreference.Any ||
            preference == (int)TargetPreference.Strikecraft == ((slotFlags & TargetSelectionCapture.SLOT_STRIKECRAFT) != 0);

        internal struct Input
        {
            public float3 Origin;
            public quaternion ParentRotation;

            public float MaxDistance;
            public float MinYaw;
            public float MaxYaw;

            public int CandidateStart;
            public int CandidateCount;
            public int GroupStart;
            public int StrikecraftOnly;
            public int ShipsOnly;
            // A TargetPreference value.
            public int Preference;
            // Nonzero when any of the owner's groups is doomed, which adds the fallback passes.
            public int HasDoomed;
        }

        internal struct Result
        {
            public float4 SelectedAim;
            public float4 LastInRangeAim;
            // Planar distance to the nearest eligible candidate visited; infinity when none was visited.
            public float NearestDistance;

            public int CandidateIndex;
            public int Visited;
            public int RequiresSerialFallback;
            public int FallbackReason;
            public int HasInRangeAim;
        }
    }
}
