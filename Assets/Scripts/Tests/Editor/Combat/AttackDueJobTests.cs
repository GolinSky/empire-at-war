using EmpireAtWar.Components.Weapon;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class AttackDueJobTests
    {
        [Test]
        public void ScheduledJob_MatchesShortCircuitPredicateAtNumericBoundaries()
        {
            float[] times = { float.NegativeInfinity, -1f, -0f, 0f, 1f, float.PositiveInfinity, float.NaN };
            int[] frames = { int.MinValue, -1, 0, 1, int.MaxValue };
            using var inputs = new NativeArray<AttackDueJob.Input>(times.Length * frames.Length, Allocator.TempJob);
            using var results = new NativeArray<byte>(inputs.Length, Allocator.TempJob);
            var values = new AttackDueJob.Input[inputs.Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = new AttackDueJob.Input { DueTime = times[i % times.Length], EarliestFrame = frames[i / times.Length] };
            inputs.CopyFrom(values);

            foreach (float now in times)
            foreach (int frame in frames)
            {
                new AttackDueJob { Inputs = inputs, Results = results, Now = now, Frame = frame }
                    .Schedule(inputs.Length, 32).Complete();
                for (int i = 0; i < values.Length; i++)
                    Assert.That(results[i], Is.EqualTo((byte)(values[i].EarliestFrame <= frame &&
                        values[i].DueTime <= now ? 1 : 0)), $"Index {i}, now {now}, frame {frame}");
            }
        }

        [TestCase(1)]
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(33)]
        [TestCase(63)]
        [TestCase(64)]
        [TestCase(65)]
        public void ScheduledJob_MatchesSerialDueCheckAcrossBatchSizes(int count)
        {
            NativeArray<AttackDueJob.Input> inputs =
                new NativeArray<AttackDueJob.Input>(count, Allocator.TempJob);
            NativeArray<byte> results = new NativeArray<byte>(count, Allocator.TempJob);
            try
            {
                for (int i = 0; i < count; i++)
                    inputs[i] = new AttackDueJob.Input
                {
                    DueTime = i % 3 == 0 ? 5f : 6f,
                    EarliestFrame = i % 2 == 0 ? 10 : 11
                };

                new AttackDueJob { Inputs = inputs, Results = results, Now = 5f, Frame = 10 }
                    .Schedule(count, 32).Complete();

                for (int i = 0; i < count; i++)
                    Assert.That(results[i], Is.EqualTo((byte)(inputs[i].DueTime <= 5f &&
                        inputs[i].EarliestFrame <= 10 ? 1 : 0)), $"Index {i}");
            }
            finally
            {
                inputs.Dispose();
                results.Dispose();
            }
        }
    }
}
