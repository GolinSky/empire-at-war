using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class CombatAttackCoordinatorDueTests
    {
        private const int BENCHMARK_SAMPLES = 40;

        private enum DueBenchmarkVariant { ExistingSerial, CollectedSerial, Job }

        [TestCase(63)]
        [TestCase(64)]
        [TestCase(65)]
        public void LateTick_CommitsEqualTimeImpactsInSequenceOrder(int count)
        {
            CombatAttackCoordinator coordinator = new CombatAttackCoordinator();
            GameObject targetObject = new GameObject("Target");
            List<int> committed = new List<int>();
            try
            {
                FakeTarget target = new FakeTarget(targetObject.transform);
                for (int i = 0; i < count; i++)
                {
                    int order = i;
                    RecordingOwner owner = new RecordingOwner(() => committed.Add(order));
                    coordinator.Register(owner);
                    coordinator.ScheduleImpact(owner, null, target, default, 0f);
                }

                MakeImpactsDue(coordinator);
                coordinator.LateTick();

                Assert.That(committed.Count, Is.EqualTo(count));
                for (int i = 0; i < count; i++) Assert.That(committed[i], Is.EqualTo(i));
                Assert.That(coordinator.PendingImpacts, Is.Zero);
            }
            finally
            {
                coordinator.Dispose();
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void LateTick_CancelledImpactDoesNotCommitAfterEarlierCallback()
        {
            CombatAttackCoordinator coordinator = new CombatAttackCoordinator();
            GameObject targetObject = new GameObject("Target");
            try
            {
                FakeTarget target = new FakeTarget(targetObject.transform);
                RecordingOwner cancelledOwner = new RecordingOwner(() => Assert.Fail("Cancelled impact committed."));
                RecordingOwner firstOwner = new RecordingOwner(() => coordinator.Unregister(cancelledOwner));
                coordinator.Register(firstOwner);
                coordinator.Register(cancelledOwner);
                coordinator.ScheduleImpact(firstOwner, null, target, default, 0f);
                for (int i = 0; i < 64; i++)
                    coordinator.ScheduleImpact(cancelledOwner, null, target, default, 0f);

                MakeImpactsDue(coordinator);
                coordinator.LateTick();

                Assert.That(firstOwner.Commits, Is.EqualTo(1));
                Assert.That(cancelledOwner.Commits, Is.Zero);
                Assert.That(coordinator.PendingImpacts, Is.Zero);
            }
            finally
            {
                coordinator.Dispose();
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test, Explicit]
        public void BenchmarkSerialAndJobDuePipelinesOnMatchedImpacts()
        {
            StringBuilder csv = new StringBuilder("records,due,variant,run,median_ns,p95_ns\n");
            int[] counts = { 1, 31, 32, 33, 63, 64, 65, 256 };
            foreach (int count in counts)
            {
                int[] dueCounts = { 0, count / 10, count };
                foreach (int dueCount in dueCounts)
                {
                    for (int run = 0; run < 3; run++)
                    {
                        BenchmarkVariant(count, dueCount, DueBenchmarkVariant.ExistingSerial, run, csv);
                        BenchmarkVariant(count, dueCount, DueBenchmarkVariant.CollectedSerial, run, csv);
                        BenchmarkVariant(count, dueCount, DueBenchmarkVariant.Job, run, csv);
                    }
                }
            }

            string directory = Path.Combine(Application.persistentDataPath, "BattleCaptures");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "due_microbenchmark_" +
                DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv");
            File.WriteAllText(path, csv.ToString());
            UnityEngine.Debug.Log("Due pipeline microbenchmark saved to " + path);
        }

        private static void BenchmarkVariant(int count, int dueCount, DueBenchmarkVariant variant,
            int run, StringBuilder csv)
        {
            CombatAttackCoordinator coordinator = new CombatAttackCoordinator();
            GameObject targetObject = new GameObject("BenchmarkTarget");
            try
            {
                FakeTarget target = new FakeTarget(targetObject.transform);
                RecordingOwner owner = new RecordingOwner(() => { });
                coordinator.Register(owner);
                long[] samples = new long[BENCHMARK_SAMPLES];
                for (int iteration = -5; iteration < BENCHMARK_SAMPLES; iteration++)
                {
                    for (int i = 0; i < count; i++)
                        coordinator.ScheduleImpact(owner, null, target, default, 0f);
                    SetImpactDueCounts(coordinator, dueCount);
                    int commitsBefore = owner.Commits;
                    long start = Stopwatch.GetTimestamp();
                    switch (variant)
                    {
                        case DueBenchmarkVariant.ExistingSerial:
                            coordinator.ProcessDueEventsSerial(Time.time, Time.frameCount);
                            break;
                        case DueBenchmarkVariant.CollectedSerial:
                            coordinator.ProcessDueEventsSerialCollected(Time.time, Time.frameCount);
                            break;
                        case DueBenchmarkVariant.Job:
                            coordinator.ProcessDueEventsBatched(Time.time, Time.frameCount);
                            break;
                    }
                    long elapsed = Stopwatch.GetTimestamp() - start;
                    Assert.That(owner.Commits - commitsBefore, Is.EqualTo(dueCount));
                    coordinator.Unregister(owner);
                    coordinator.Register(owner);
                    if (iteration >= 0) samples[iteration] =
                        (long)(elapsed * (1000000000d / Stopwatch.Frequency));
                }

                Array.Sort(samples);
                csv.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5}\n", count,
                    dueCount, variant, run, samples[BENCHMARK_SAMPLES / 2],
                    samples[(int)Math.Ceiling(BENCHMARK_SAMPLES * 0.95d) - 1]);
            }
            finally
            {
                coordinator.Dispose();
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void MakeImpactsDue(CombatAttackCoordinator coordinator)
        {
            SetImpactDueCounts(coordinator, int.MaxValue);
        }

        private static void SetImpactDueCounts(CombatAttackCoordinator coordinator, int dueCount)
        {
            IList impacts = (IList)typeof(CombatAttackCoordinator)
                .GetField("_impacts", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(coordinator);
            for (int i = 0; i < impacts.Count; i++)
            {
                object record = impacts[i];
                record.GetType().GetField("DueTime").SetValue(record,
                    i < dueCount ? Time.time - 1f : Time.time + 1000f);
                record.GetType().GetField("EarliestFrame").SetValue(record, Time.frameCount);
                impacts[i] = record;
            }
        }

        private sealed class RecordingOwner : IWeaponPresenter
        {
            private readonly Action _onCommit;

            public RecordingOwner(Action onCommit) => _onCommit = onCommit;
            public int Commits { get; private set; }

            public void ApplyDamage(AttackData attackData, IHardPointModel unitView,
                WeaponType weaponType, float attackDelay) => throw new InvalidOperationException();

            public bool CommitImpact(AttackData attackData, IHardPointModel hardPointModel,
                WeaponType weaponType, int targetId)
            {
                Commits++;
                _onCommit();
                return true;
            }
        }

        private sealed class FakeTarget : IHardPointModel
        {
            public FakeTarget(Transform transform) => Transform = transform;
            public event Action OnHardPointHealthChanged { add { } remove { } }
            public event Action OnDestroyed { add { } remove { } }
            public HardPointType HardPointType => HardPointType.Any;
            public float HealthPercentage => 1f;
            public int Id => 1;
            public int Generation => 1;
            public bool IsDestroyed => false;
            public Vector3 Position => Transform.position;
            public Transform Transform { get; }
        }
    }
}
