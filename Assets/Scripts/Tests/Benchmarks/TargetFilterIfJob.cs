using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>
    /// The candidate filter of <c>WeaponTargetSelectionJob.Execute</c> as written in production: a chain of
    /// <c>continue</c>. The aim work is replaced by counting, so every candidate is visited.
    /// Result: x = first accepted candidate (-1 when none), y = accepted count over both passes.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    internal struct TargetFilterIfJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<TargetFilterRequest> Requests;
        [ReadOnly] public NativeArray<int> CandidateSlots;
        [ReadOnly] public NativeArray<int> CandidateGroups;
        [ReadOnly] public NativeArray<byte> GroupEligible;
        [ReadOnly] public NativeArray<byte> SlotFlags;
        [WriteOnly] public NativeArray<int2> Results;

        public void Execute(int index)
        {
            TargetFilterRequest request = Requests[index];
            int first = -1;
            int accepted = 0;
            int passes = request.Preference == TargetFilterFlags.PREFERENCE_ANY ? 1 : 2;
            for (int pass = 0; pass < passes; pass++)
            for (int i = request.CandidateStart; i < request.CandidateStart + request.CandidateCount; i++)
            {
                if (GroupEligible[request.GroupStart + CandidateGroups[i]] == 0) continue;
                byte flags = SlotFlags[CandidateSlots[i]];
                if ((flags & TargetFilterFlags.SLOT_VALID) == 0) continue;
                if (request.StrikecraftOnly != 0 && (flags & TargetFilterFlags.SLOT_STRIKECRAFT) == 0) continue;
                if (request.ShipsOnly != 0 && (flags & TargetFilterFlags.SLOT_STRIKECRAFT) != 0) continue;
                if (TargetFilterFlags.IsPreferred(request.Preference, flags) != (pass == 0)) continue;
                accepted++;
                if (first < 0) first = i;
            }

            Results[index] = new int2(first, accepted);
        }
    }
}
