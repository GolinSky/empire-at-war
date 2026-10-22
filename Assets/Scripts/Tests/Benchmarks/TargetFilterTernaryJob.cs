using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary><see cref="TargetFilterIfJob"/> with one combined <c>&amp;</c> predicate and <c>?:</c> updates.</summary>
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    internal struct TargetFilterTernaryJob : IJobParallelFor
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
            bool anyPreference = request.Preference == TargetFilterFlags.PREFERENCE_ANY;
            bool prefersStrikecraft = request.Preference == TargetFilterFlags.PREFERENCE_STRIKECRAFT;
            int passes = anyPreference ? 1 : 2;
            for (int pass = 0; pass < passes; pass++)
            for (int i = request.CandidateStart; i < request.CandidateStart + request.CandidateCount; i++)
            {
                byte flags = SlotFlags[CandidateSlots[i]];
                bool strikecraft = (flags & TargetFilterFlags.SLOT_STRIKECRAFT) != 0;
                bool preferred = anyPreference | (prefersStrikecraft == strikecraft);
                bool accept = (GroupEligible[request.GroupStart + CandidateGroups[i]] != 0) &
                              ((flags & TargetFilterFlags.SLOT_VALID) != 0) &
                              ((request.StrikecraftOnly == 0) | strikecraft) &
                              ((request.ShipsOnly == 0) | !strikecraft) &
                              (preferred == (pass == 0));
                accepted += accept ? 1 : 0;
                first = accept & (first < 0) ? i : first;
            }

            Results[index] = new int2(first, accepted);
        }
    }
}
