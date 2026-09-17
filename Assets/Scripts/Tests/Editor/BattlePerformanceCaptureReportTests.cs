using System;
using System.IO;
using System.Reflection;
using EmpireAtWar.Services.Timing;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Timing
{
    public sealed class BattlePerformanceCaptureReportTests
    {
        [Test]
        public void CombatWorkload_MapsValuesToNamedColumns()
        {
            string[] names = (string[])typeof(BattlePerformanceCapture)
                .GetField("_workloadNames", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            long[] samples = new long[names.Length];
            new BattlePerformanceCapture.CombatWorkload
            {
                CompletedUnityFrame = 42,
                TargetRequests = 8,
                DueRecords = 64,
                PendingImpacts = 3
            }.WriteTo(samples, 0);

            Assert.That(samples[Array.IndexOf(names, "completed_unity_frame")], Is.EqualTo(42));
            Assert.That(samples[Array.IndexOf(names, "target_requests")], Is.EqualTo(8));
            Assert.That(samples[Array.IndexOf(names, "due_records")], Is.EqualTo(64));
            Assert.That(samples[Array.IndexOf(names, "pending_impacts")], Is.EqualTo(3));
        }

        [Test]
        public void Write_IncludesJobPhasesWorkloadAndSourceMetadata()
        {
            string directory = Path.Combine(Path.GetTempPath(), "battle-capture-test-" + Guid.NewGuid());
            try
            {
                BattlePerformanceCaptureReport.Write(
                    directory, new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc), 1f, "test", 1,
                    new[] { 1f },
                    new long[] { 1000000, 200000, 300000 },
                    new long[] { 1, 1 },
                    new long[] { 42, 8, 64 },
                    new[] { true, true, true },
                    new[] { "frame_duration_ns", "target_batch_ns", "due_batch_ns" },
                    new[] { "Battle.Attack.TargetBatch", "Battle.Attack.DueBatch" },
                    new[] { "completed_unity_frame", "target_requests", "due_records" },
                    1,
                    new BattlePerformanceCaptureMetadata("6000.4", "Editor", "High", "100x100", 1f,
                        0, 60, "GPU", "CPU", "abc123", "dirty", "build", "test", 8, 1, 64, 32),
                    long.MinValue);

                string csv = File.ReadAllText(Path.Combine(directory, "battle_20260917_120000_000.csv"));
                string summary = File.ReadAllText(Path.Combine(directory, "battle_20260917_120000_000_summary.txt"));
                Assert.That(csv, Does.Contain("target_batch_ms,target_batch_sample_count"));
                Assert.That(csv, Does.Contain("due_batch_ms,due_batch_sample_count"));
                Assert.That(csv, Does.Contain("completed_unity_frame,target_requests,due_records"));
                Assert.That(summary, Does.Contain("completed_unity_frame first=42 last=42"));
                Assert.That(summary, Does.Contain("source_revision=abc123"));
                Assert.That(summary, Does.Contain("target_job_threshold=8"));
                Assert.That(summary, Does.Contain("due_job_batch_size=32"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
