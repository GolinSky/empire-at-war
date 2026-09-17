using EmpireAtWar.Components.Weapon;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class WeaponTargetSelectionJobTests
    {
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void ScheduledJob_SelectsFirstEligibleCandidate(int requestCount)
        {
            Vector3[] positions = { new Vector3(15f, 0f, 0f), new Vector3(0f, 0f, 5f), new Vector3(0f, 0f, 4f) };
            WeaponTargetSelectionJob.Result result = Run(requestCount, Input(positions.Length), positions);

            Assert.That(result.RequiresSerialFallback, Is.Zero);
            Assert.That(result.CandidateIndex, Is.EqualTo(1));
            Assert.That(result.Visited, Is.EqualTo(2));
            Assert.That(Quaternion.Angle(ToQuaternion(result.SelectedAim), Quaternion.identity), Is.LessThan(0.01f));
        }

        [Test]
        public void ScheduledJob_PreservesLastInRangeAimWhenNoCandidatePassesArc()
        {
            Vector3[] positions = { new Vector3(5f, 0f, 5f), new Vector3(-5f, 0f, 5f) };
            WeaponTargetSelectionJob.Input input = Input(positions.Length);
            input.MinYaw = -10f;
            input.MaxYaw = 10f;
            WeaponTargetSelectionJob.Result result = Run(8, input, positions);

            Assert.That(result.RequiresSerialFallback, Is.Zero);
            Assert.That(result.CandidateIndex, Is.EqualTo(-1));
            Assert.That(result.HasInRangeAim, Is.EqualTo(1));
            Quaternion expected = Quaternion.LookRotation(positions[1], Vector3.up);
            Assert.That(Quaternion.Angle(ToQuaternion(result.LastInRangeAim), expected), Is.LessThan(0.01f));
        }

        [Test]
        public void ScheduledJob_ReportsNoAimForEmptyCandidateList()
        {
            WeaponTargetSelectionJob.Result result = Run(8, Input(0), new Vector3[0]);

            Assert.That(result.CandidateIndex, Is.EqualTo(-1));
            Assert.That(result.Visited, Is.Zero);
            Assert.That(result.HasInRangeAim, Is.Zero);
            Assert.That(result.RequiresSerialFallback, Is.Zero);
        }

        [TestCase(3f, 2f, 5f, 0f)]
        [TestCase(-3f, 1f, 4f, 45f)]
        [TestCase(1f, -4f, 5f, -30f)]
        public void ScheduledJob_MatchesSerialAimForOrdinaryGeometry(float x, float y, float z, float parentYaw)
        {
            Vector3 position = new Vector3(x, y, z);
            Quaternion parentRotation = Quaternion.Euler(0f, parentYaw, 0f);
            WeaponTargetSelectionJob.Input input = Input(1);
            input.ParentRotation = new quaternion(parentRotation.x, parentRotation.y,
                parentRotation.z, parentRotation.w);
            WeaponTargetSelectionJob.Result result = Run(8, input, new[] { position });
            bool eligible = WeaponTargetSelector.TryCalculateAim(position, Vector3.zero,
                parentRotation, 10f, -180f, 180f, out Quaternion serialAim, out _);

            Assert.That(eligible, Is.True);
            Assert.That(result.RequiresSerialFallback, Is.Zero);
            Assert.That(result.CandidateIndex, Is.Zero);
            Assert.That(Quaternion.Angle(ToQuaternion(result.SelectedAim), serialAim), Is.LessThan(0.1f));
        }

        [TestCase(0f, 10f, 0f)]
        [TestCase(0f, -10f, 0f)]
        [TestCase(0f, 0f, 0f)]
        [TestCase(0.00001f, 10f, 0f)]
        public void ScheduledJob_RequestsSerialFallbackForDegenerateDirection(float x, float y, float z)
        {
            Vector3[] positions = { new Vector3(x, y, z) };
            WeaponTargetSelectionJob.Result result = Run(8, Input(1), positions);

            Assert.That(result.RequiresSerialFallback, Is.EqualTo(1));
            Assert.That(result.CandidateIndex, Is.EqualTo(-1));
        }

        [Test]
        public void ScheduledJob_RequestsSerialFallbackForTiltedParent()
        {
            Vector3[] positions = { new Vector3(0f, 0f, 5f) };
            WeaponTargetSelectionJob.Input input = Input(1);
            Quaternion rotation = Quaternion.Euler(20f, 30f, 0f);
            input.ParentRotation = new quaternion(rotation.x, rotation.y, rotation.z, rotation.w);

            Assert.That(Run(8, input, positions).RequiresSerialFallback, Is.EqualTo(1));
        }

        [Test]
        public void ScheduledJob_RequestsSerialFallbackAtInclusiveRangeBoundary()
        {
            Vector3[] positions = { new Vector3(0f, 0f, 10f) };
            Assert.That(Run(8, Input(1), positions).RequiresSerialFallback, Is.EqualTo(1));
            Assert.That(WeaponTargetSelector.TryCalculateAim(positions[0], Vector3.zero,
                Quaternion.identity, 10f, -180f, 180f, out _, out _), Is.True);
        }

        [Test]
        public void ScheduledJob_RequestsSerialFallbackAtYawWrap()
        {
            Vector3[] positions = { Vector3.back * 5f };
            Assert.That(Run(8, Input(1), positions).RequiresSerialFallback, Is.EqualTo(1));
        }

        private static WeaponTargetSelectionJob.Input Input(int candidateCount) => new WeaponTargetSelectionJob.Input
        {
            Origin = float3.zero,
            ParentRotation = quaternion.identity,
            MaxDistance = 10f,
            MinYaw = -180f,
            MaxYaw = 180f,
            CandidateCount = candidateCount
        };

        private static WeaponTargetSelectionJob.Result Run(int requestCount,
            WeaponTargetSelectionJob.Input input, Vector3[] positions)
        {
            NativeArray<WeaponTargetSelectionJob.Input> inputs =
                new NativeArray<WeaponTargetSelectionJob.Input>(requestCount, Allocator.TempJob);
            NativeArray<float3> candidatePositions =
                new NativeArray<float3>(positions.Length == 0 ? 1 : positions.Length, Allocator.TempJob);
            NativeArray<WeaponTargetSelectionJob.Result> results =
                new NativeArray<WeaponTargetSelectionJob.Result>(requestCount, Allocator.TempJob);
            try
            {
                for (int i = 0; i < requestCount; i++) inputs[i] = input;
                for (int i = 0; i < positions.Length; i++) candidatePositions[i] = positions[i];

                new WeaponTargetSelectionJob
                {
                    Inputs = inputs,
                    CandidatePositions = candidatePositions,
                    Results = results
                }.Schedule(requestCount, 1).Complete();
                return results[0];
            }
            finally
            {
                inputs.Dispose();
                candidatePositions.Dispose();
                results.Dispose();
            }
        }

        private static Quaternion ToQuaternion(float4 value) =>
            new Quaternion(value.x, value.y, value.z, value.w);
    }
}
