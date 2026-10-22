using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>Seeded hardpoint candidate spans shared by every <see cref="BranchVariant"/> of the target filter.</summary>
    internal sealed class TargetFilterBenchmarkData : IDisposable
    {
        private const int GROUPS_PER_REQUEST = 16;
        private const float STRIKECRAFT_RATIO = 0.3f;
        private const float RESTRICTED_RATIO = 0.1f;

        private NativeArray<TargetFilterRequest> _requests;
        private NativeArray<int> _candidateSlots;
        private NativeArray<int> _candidateGroups;
        private NativeArray<byte> _groupEligible;
        private NativeArray<byte> _slotFlags;
        private NativeArray<int2> _results;

        /// <param name="eligibleRatio">Share of eligible groups and of valid slots; drives how predictable the filter is.</param>
        public TargetFilterBenchmarkData(int requestCount, int candidatesPerRequest, int slotCount, float eligibleRatio,
            uint seed)
        {
            Random random = new Random(seed);
            _requests = new NativeArray<TargetFilterRequest>(requestCount, Allocator.Persistent);
            for (int i = 0; i < requestCount; i++)
            {
                float restriction = random.NextFloat();
                _requests[i] = new TargetFilterRequest
                {
                    CandidateStart = i * candidatesPerRequest,
                    CandidateCount = candidatesPerRequest,
                    GroupStart = i * GROUPS_PER_REQUEST,
                    StrikecraftOnly = restriction < RESTRICTED_RATIO ? 1 : 0,
                    ShipsOnly = restriction >= RESTRICTED_RATIO && restriction < 2f * RESTRICTED_RATIO ? 1 : 0,
                    Preference = random.NextInt(TargetFilterFlags.PREFERENCE_SHIPS + 1)
                };
            }

            int candidateCount = requestCount * candidatesPerRequest;
            _candidateSlots = new NativeArray<int>(candidateCount, Allocator.Persistent);
            _candidateGroups = new NativeArray<int>(candidateCount, Allocator.Persistent);
            for (int i = 0; i < candidateCount; i++)
            {
                _candidateSlots[i] = random.NextInt(slotCount);
                _candidateGroups[i] = random.NextInt(GROUPS_PER_REQUEST);
            }

            _groupEligible = new NativeArray<byte>(requestCount * GROUPS_PER_REQUEST, Allocator.Persistent);
            for (int i = 0; i < _groupEligible.Length; i++)
            {
                _groupEligible[i] = (byte)(random.NextFloat() < eligibleRatio ? 1 : 0);
            }

            _slotFlags = new NativeArray<byte>(slotCount, Allocator.Persistent);
            for (int i = 0; i < slotCount; i++)
            {
                bool valid = random.NextFloat() < eligibleRatio;
                bool strikecraft = random.NextFloat() < STRIKECRAFT_RATIO;
                _slotFlags[i] = (byte)((valid ? TargetFilterFlags.SLOT_VALID : 0) |
                                       (strikecraft ? TargetFilterFlags.SLOT_STRIKECRAFT : 0));
            }

            _results = new NativeArray<int2>(requestCount, Allocator.Persistent);
        }

        public NativeArray<int2> Results => _results;

        /// <summary>Runs one variant on the calling thread, so timings compare kernels without scheduling noise.</summary>
        public void Run(BranchVariant variant)
        {
            switch (variant)
            {
                case BranchVariant.If:
                    new TargetFilterIfJob
                    {
                        Requests = _requests,
                        CandidateSlots = _candidateSlots,
                        CandidateGroups = _candidateGroups,
                        GroupEligible = _groupEligible,
                        SlotFlags = _slotFlags,
                        Results = _results
                    }.Run(_requests.Length);
                    break;
                case BranchVariant.Ternary:
                    new TargetFilterTernaryJob
                    {
                        Requests = _requests,
                        CandidateSlots = _candidateSlots,
                        CandidateGroups = _candidateGroups,
                        GroupEligible = _groupEligible,
                        SlotFlags = _slotFlags,
                        Results = _results
                    }.Run(_requests.Length);
                    break;
                case BranchVariant.Select:
                    new TargetFilterSelectJob
                    {
                        Requests = _requests,
                        CandidateSlots = _candidateSlots,
                        CandidateGroups = _candidateGroups,
                        GroupEligible = _groupEligible,
                        SlotFlags = _slotFlags,
                        Results = _results
                    }.Run(_requests.Length);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(variant), variant, null);
            }
        }

        /// <summary>Plain C# filter over the same input, independent of every job variant.</summary>
        public int2[] ComputeReference()
        {
            int2[] results = new int2[_requests.Length];
            for (int index = 0; index < _requests.Length; index++)
            {
                TargetFilterRequest request = _requests[index];
                int first = -1;
                int accepted = 0;
                // Preferred candidates are counted in the first pass, the rest in the second.
                for (int pass = 0; pass < 2; pass++)
                for (int i = request.CandidateStart; i < request.CandidateStart + request.CandidateCount; i++)
                {
                    byte flags = _slotFlags[_candidateSlots[i]];
                    bool strikecraft = (flags & TargetFilterFlags.SLOT_STRIKECRAFT) != 0;
                    bool preferred = TargetFilterFlags.IsPreferred(request.Preference, flags);
                    if (preferred != (pass == 0) ||
                        _groupEligible[request.GroupStart + _candidateGroups[i]] == 0 ||
                        (flags & TargetFilterFlags.SLOT_VALID) == 0 ||
                        request.StrikecraftOnly != 0 && !strikecraft ||
                        request.ShipsOnly != 0 && strikecraft)
                    {
                        continue;
                    }

                    accepted++;
                    if (first < 0) first = i;
                }

                results[index] = new int2(first, accepted);
            }

            return results;
        }

        public void Dispose()
        {
            _requests.Dispose();
            _candidateSlots.Dispose();
            _candidateGroups.Dispose();
            _groupEligible.Dispose();
            _slotFlags.Dispose();
            _results.Dispose();
        }
    }
}
