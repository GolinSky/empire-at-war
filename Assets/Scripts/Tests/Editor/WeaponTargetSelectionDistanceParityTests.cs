using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Input = EmpireAtWar.Components.Weapon.WeaponTargetSelectionJob.Input;
using Result = EmpireAtWar.Components.Weapon.WeaponTargetSelectionJob.Result;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class WeaponTargetSelectionDistanceParityTests
    {
        [Test]
        public void ScheduledJob_MatchesOriginalForRandomOriginsTiltsRangesAndFilters()
        {
            const int REQUEST_COUNT = 512;
            const int CANDIDATE_COUNT = 16;
            var random = new Random(0x7192u);
            var inputs = new Input[REQUEST_COUNT];
            var positions = new float3[REQUEST_COUNT * CANDIDATE_COUNT];
            var flags = new byte[positions.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = new Input
                {
                    Origin = random.NextFloat3(-100f, 100f),
                    ParentRotation = quaternion.EulerZXY(random.NextFloat3(-3f, 3f)),
                    MaxDistance = random.NextFloat(-1f, 200f),
                    MinYaw = random.NextFloat(-180f, -1f),
                    MaxYaw = random.NextFloat(1f, 180f),
                    CandidateStart = i * CANDIDATE_COUNT,
                    CandidateCount = i % 31 == 0 ? 0 : CANDIDATE_COUNT,
                    Preference = i % 3,
                    StrikecraftOnly = i % 7 == 0 ? 1 : 0,
                    ShipsOnly = i % 11 == 0 ? 1 : 0
                };
                for (int j = 0; j < CANDIDATE_COUNT; j++)
                {
                    int slot = i * CANDIDATE_COUNT + j;
                    positions[slot] = inputs[i].Origin + random.NextFloat3(-250f, 250f);
                    flags[slot] = (byte)(random.NextInt(0, 5) == 0 ? 0 :
                        TargetSelectionCapture.SLOT_VALID | (j % 2 == 0 ? TargetSelectionCapture.SLOT_STRIKECRAFT : 0));
                }
            }
            AssertParity(inputs, positions, flags);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(0.001f)]
        [TestCase(1f)]
        [TestCase(10f)]
        [TestCase(100000f)]
        [TestCase(float.MaxValue)]
        public void ScheduledJob_MatchesOriginalAtRangeAndFallbackBandBoundaries(float range)
        {
            float tolerance = 0.0001f * math.max(1f, math.abs(range));
            float[] boundaries = { range, range - tolerance, range + tolerance, range + 2f * tolerance };
            var inputs = new Input[boundaries.Length * 9];
            var positions = new float3[inputs.Length];
            int index = 0;
            foreach (float boundary in boundaries)
            for (int offset = -4; offset <= 4; offset++)
            {
                inputs[index] = CreateInput(index, range);
                positions[index] = new float3(0f, 0f, math.asfloat(math.asint(boundary) + offset));
                index++;
            }
            AssertParity(inputs, positions);
        }

        [Test]
        public void DegenerateSquaredCutoff_MatchesAdjacentRoundedSquareRoots()
        {
            const float SQUARED_CUTOFF = 0.0000000001f;
            const float DISTANCE_CUTOFF = 0.00001f;
            Assert.That(math.sqrt(SQUARED_CUTOFF), Is.LessThanOrEqualTo(DISTANCE_CUTOFF));
            float nextSquared = math.asfloat(math.asint(SQUARED_CUTOFF) + 1);
            Assert.That(math.sqrt(nextSquared), Is.GreaterThan(DISTANCE_CUTOFF));
            float axisSquared = DISTANCE_CUTOFF * DISTANCE_CUTOFF;
            float3[] positions =
            {
                new float3(DISTANCE_CUTOFF, math.sqrt(SQUARED_CUTOFF - axisSquared), 0f),
                new float3(DISTANCE_CUTOFF, math.sqrt(nextSquared - axisSquared), 0f)
            };
            Assert.That(math.lengthsq(positions[0]), Is.EqualTo(SQUARED_CUTOFF));
            Assert.That(math.lengthsq(positions[1]), Is.EqualTo(nextSquared));
            AssertParity(new[] { CreateInput(0, 1f), CreateInput(1, 1f) }, positions);
        }
        [Test]
        public void ScheduledJob_MatchesOriginalAroundDegenerateRoundingBoundary()
        {
            const int REQUEST_COUNT = 257;
            var inputs = new Input[REQUEST_COUNT];
            var positions = new float3[REQUEST_COUNT];
            for (int i = 0; i < REQUEST_COUNT; i++)
            {
                // A non-axis vector exercises rounded dot products on both sides of the threshold.
                float component = math.asfloat(math.asint(0.00001f / math.sqrt(3f)) + i - 128);
                inputs[i] = CreateInput(i, 1f);
                positions[i] = new float3(component);
            }
            AssertParity(inputs, positions);
        }

        [Test]
        public void ScheduledJob_MatchesOriginalForNonFiniteVerticalAndOverflowDirections()
        {
            float3[] positions =
            {
                float3.zero, new float3(0f, 10f, 0f), new float3(0.00001f, 10f, 0f),
                new float3(float.NaN, 1f, 5f), new float3(1f, float.NaN, 5f),
                new float3(float.PositiveInfinity, 1f, 5f), new float3(float.MaxValue),
                new float3(0f, 0f, -5f), new float3(1e-30f), new float3(0f, 1f, 5f)
            };
            var inputs = new Input[positions.Length];
            for (int i = 0; i < inputs.Length; i++) inputs[i] = CreateInput(i, 10f);
            AssertParity(inputs, positions);
            inputs[0].MaxDistance = float.NaN;
            inputs[1].Origin = new float3(float.PositiveInfinity);
            inputs[2].ParentRotation = new quaternion(float.NaN, 0f, 0f, 1f);
            AssertParity(inputs, positions);
        }

        private static Input CreateInput(int start, float range) => new Input
        {
            ParentRotation = quaternion.identity,
            MaxDistance = range,
            MinYaw = -180f,
            MaxYaw = 180f,
            CandidateStart = start,
            CandidateCount = 1
        };

        private static void AssertParity(Input[] requests, float3[] positions, byte[] flags = null)
        {
            var candidateSlots = new int[positions.Length];
            var candidateFlags = new byte[positions.Length];
            var candidateGroups = new int[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                candidateSlots[i] = i;
                candidateFlags[i] = flags == null ? TargetSelectionCapture.SLOT_VALID : flags[i];
                candidateGroups[i] = flags != null && i % 13 == 0 ? 1 : 0;
            }
            using var inputs = new NativeArray<Input>(requests, Allocator.TempJob);
            using var slotPositions = new NativeArray<float3>(positions, Allocator.TempJob);
            using var slotFlags = new NativeArray<byte>(candidateFlags, Allocator.TempJob);
            using var slots = new NativeArray<int>(candidateSlots, Allocator.TempJob);
            using var groups = new NativeArray<int>(candidateGroups, Allocator.TempJob);
            using var eligible = new NativeArray<byte>(new byte[] { 1, 0 }, Allocator.TempJob);
            using var expected = new NativeArray<Result>(requests.Length, Allocator.TempJob);
            using var actual = new NativeArray<Result>(requests.Length, Allocator.TempJob);
            new OriginalJob
            {
                Inputs = inputs, CandidateSlots = slots, CandidateGroups = groups, GroupEligible = eligible,
                SlotPositions = slotPositions, SlotFlags = slotFlags, Results = expected
            }.Schedule(requests.Length, 1).Complete();
            new WeaponTargetSelectionJob
            {
                Inputs = inputs, CandidateSlots = slots, CandidateGroups = groups, GroupEligible = eligible,
                SlotPositions = slotPositions, SlotFlags = slotFlags, Results = actual
            }.Schedule(requests.Length, 1).Complete();
            for (int i = 0; i < requests.Length; i++)
            {
                Assert.That(actual[i].CandidateIndex, Is.EqualTo(expected[i].CandidateIndex), $"Candidate {i}");
                Assert.That(actual[i].Visited, Is.EqualTo(expected[i].Visited), $"Visited {i}");
                Assert.That(actual[i].RequiresSerialFallback, Is.EqualTo(expected[i].RequiresSerialFallback), $"Fallback {i}");
                Assert.That(actual[i].FallbackReason, Is.EqualTo(expected[i].FallbackReason), $"Reason {i}");
                Assert.That(actual[i].HasInRangeAim, Is.EqualTo(expected[i].HasInRangeAim), $"HasAim {i}");
                Assert.That(math.asint(actual[i].NearestDistance), Is.EqualTo(math.asint(expected[i].NearestDistance)), $"Distance {i}");
                Assert.That(math.asint(actual[i].SelectedAim), Is.EqualTo(math.asint(expected[i].SelectedAim)), $"SelectedAim {i}");
                Assert.That(math.asint(actual[i].LastInRangeAim), Is.EqualTo(math.asint(expected[i].LastInRangeAim)), $"LastAim {i}");
            }
        }

        // Frozen pre-optimization implementation, compiled by Burst just like the production job.
        [BurstCompile]
        internal struct OriginalJob : IJobParallelFor
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
                quaternion inverseParentRotation = math.inverse(input.ParentRotation);
                // Preferred targets first; the rest only when none is in reach. Each candidate is visited once.
                // The first group with a hardpoint in reach is the target; its nearest reachable hardpoint is hit.
                int passes = input.Preference == (int)TargetPreference.Any ? 1 : 2;
                int bestGroup = -1;
                float bestDistanceSquared = float.PositiveInfinity;
                for (int pass = 0; pass < passes && result.CandidateIndex < 0; pass++)
                for (int i = input.CandidateStart; i < input.CandidateStart + input.CandidateCount; i++)
                {
                    if (result.CandidateIndex >= 0 && CandidateGroups[i] != bestGroup) break;
                    if (GroupEligible[input.GroupStart + CandidateGroups[i]] == 0) continue;
                    int slot = CandidateSlots[i];
                    byte flags = SlotFlags[slot];
                    if ((flags & TargetSelectionCapture.SLOT_VALID) == 0) continue;
                    if (input.StrikecraftOnly != 0 && (flags & TargetSelectionCapture.SLOT_STRIKECRAFT) == 0) continue;
                    if (input.ShipsOnly != 0 && (flags & TargetSelectionCapture.SLOT_STRIKECRAFT) != 0) continue;
                    if (IsPreferred(input.Preference, flags) != (pass == 0)) continue;
                    result.Visited++;
                    float3 direction = SlotPositions[slot] - input.Origin;
                    // Planar, like WeaponTargetSelector: ships on different height tiers must stay in range.
                    float distance = math.length(new float2(direction.x, direction.z));
                    result.NearestDistance = math.min(result.NearestDistance, distance);
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
                    float distanceSquared = math.lengthsq(new float2(direction.x, direction.z));
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
                    bestGroup = CandidateGroups[i];
                    bestDistanceSquared = distanceSquared;
                }
                Results[index] = result;
            }

            internal static bool IsPreferred(int preference, byte slotFlags) =>
                preference == (int)TargetPreference.Any ||
                preference == (int)TargetPreference.Strikecraft == ((slotFlags & TargetSelectionCapture.SLOT_STRIKECRAFT) != 0);


        }
    }
}
