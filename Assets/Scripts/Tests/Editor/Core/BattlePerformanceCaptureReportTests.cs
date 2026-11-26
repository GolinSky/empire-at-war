using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.Analytics;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Timing
{
    [Category("Core")]
    public sealed class BattlePerformanceCaptureReportTests
    {
        [Test]
        public void RollingCapture_MergesNearbyHitchesAndDoesNotCarryContextAcrossPause()
        {
            string directory = Path.Combine(Path.GetTempPath(), "battle-hitch-" + Guid.NewGuid());
            string path = Path.Combine(directory, "hitch.csv");
            var capture = new BattleHitchCapture(path, new[] { "frame_duration_ns" }, Array.Empty<string>(), 1, long.MinValue);
            var frame = new BattleCaptureFrame(1, 0, 0);
            void Observe(int id, bool hitch)
            {
                frame.UnityFrame = id;
                frame.IsHitch = hitch;
                frame.Metrics[0] = hitch ? 60000000 : 10000000;
                capture.Observe(frame);
            }

            try
            {
                for (int i = 0; i < 115; i++) Observe(i, i == 40 || i == 55);
                capture.FinishWindow();
                for (int i = 200; i < 210; i++) Observe(i, i == 204);
                capture.FinishWindow();

                var rows = File.ReadLines(path).Skip(1).Select(line => line.Split(',')).ToArray();
                Assert.That(rows.Select(row => int.Parse(row[0])), Is.EqualTo(Enumerable.Range(10, 76).Concat(Enumerable.Range(200, 10))));
                Assert.That(rows.Where(row => row[2] == "1").Select(row => row[0]), Is.EqualTo(new[] { "40", "55", "204" }));
            }
            finally
            {
                capture.FinishWindow();
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void HitchCsv_WritesInstrumentBusyColumn()
        {
            string directory = Path.Combine(Path.GetTempPath(), "battle-busy-" + Guid.NewGuid());
            string path = Path.Combine(directory, "hitch.csv");
            var capture = new BattleHitchCapture(path, new[] { "frame_duration_ns" }, Array.Empty<string>(), 1, long.MinValue);
            var frame = new BattleCaptureFrame(1, 0, 0) { UnityFrame = 7, IsHitch = true, InstrumentBusy = true };
            try
            {
                capture.Observe(frame);
                capture.FinishWindow();
                string[] lines = File.ReadAllLines(path);
                string[] header = lines[0].Split(',');
                Assert.That(lines[1].Split(',')[Array.IndexOf(header, "instrument_busy")], Is.EqualTo("1"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void CsvCounters_PreserveCounterUnitsUnavailableAndAttributionColumns()
        {
            string[] metrics = { "frame_duration_ns", "gc_collect_ns", "uncovered_ns" };
            string[] counters = { "gc_collections", "gc_used_bytes" };
            var frame = new BattleCaptureFrame(3, 2, 2);
            frame.Metrics[0] = 250000000;
            frame.Metrics[1] = 200000000;
            frame.Metrics[2] = -1000000;
            frame.MarkerCounts[0] = 1;
            frame.MarkerCounts[1] = -1;
            frame.Workload[0] = 1;
            frame.Workload[1] = long.MinValue;
            frame.AllocationTop[0] = new BattleSystemSample { Name = "Game.System.Tick()", Bytes = 4096 };
            frame.TimeTop[0] = new BattleSystemSample { Name = "Game.System.Tick()", Nanoseconds = 2000000 };
            using var writer = new StringWriter();
            BattlePerformanceCaptureReport.WriteCsvHeader(writer, "frame", metrics, counters, 1);
            writer.Write("42");
            BattlePerformanceCaptureReport.WriteCsvCounters(writer, 0, frame.Metrics, frame.MarkerCounts,
                frame.Workload, 3, 2, 1, long.MinValue, frame);
            string[] lines = writer.ToString().Trim().Split('\n');
            string[] header = lines[0].TrimEnd('\r').Split(',');
            string[] row = lines[1].TrimEnd('\r').Split(',');
            Assert.That(row.Length, Is.EqualTo(header.Length));
            Assert.That(row[Array.IndexOf(header, "gc_collect_ms")], Is.EqualTo("200.000000"));
            Assert.That(row[Array.IndexOf(header, "uncovered_ms")], Is.EqualTo("-1.000000"));
            Assert.That(row[Array.IndexOf(header, "gc_collections")], Is.EqualTo("1"));
            Assert.That(row[Array.IndexOf(header, "gc_used_bytes")], Is.EqualTo("unavailable"));
            Assert.That(row[Array.IndexOf(header, "alloc_top1_name")], Is.EqualTo("\"Game.System.Tick()\""));
            Assert.That(row[Array.IndexOf(header, "alloc_top1_bytes")], Is.EqualTo("4096"));
            Assert.That(row[Array.IndexOf(header, "time_top1_ms")], Is.EqualTo("2.000000"));
        }

        [Test]
        public void FpsHistory_UpgradesExistingRowsWithoutMixingColumnCounts()
        {
            string directory = Path.Combine(Path.GetTempPath(), "fps-schema-" + Guid.NewGuid());
            string path = Path.Combine(directory, "history.csv");
            try
            {
                Directory.CreateDirectory(directory);
                string header = (string)typeof(BattleFpsHistory).GetField("HEADER", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
                string oldHeader = header.Substring(0, header.IndexOf(",gc_used_mb", StringComparison.Ordinal));
                File.WriteAllText(path, oldHeader + "\nold-row\n");
                BattleFpsHistory.EnsureSchema(path);
                Assert.That(File.ReadLines(path).First(), Is.EqualTo(header));
                Assert.That(File.ReadLines(path).Last(), Is.EqualTo("old-row,unavailable,unavailable,unavailable"));
                Assert.That(File.ReadAllText(path + ".schema-v1-backup"), Does.Contain(oldHeader));
                BattleFpsHistory.EnsureSchema(path);
                Assert.That(File.ReadLines(path).Count(), Is.EqualTo(2));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void WorkloadSummary_DoesNotReportMissingCounterAsZero()
        {
            var report = new System.Text.StringBuilder();
            typeof(BattlePerformanceCaptureReport).GetMethod("AppendWorkloadSummary", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { report, 0, 2, new[] { "gc_used_bytes" }, new[] { long.MinValue, long.MinValue } });
            Assert.That(report.ToString(), Is.EqualTo("gc_used_bytes unavailable\n"));
        }

        [Test]
        public void RuntimeSchema_AlignsBuiltinMarkersCountersAndMetricCount()
        {
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var type = typeof(BattlePerformanceCapture);
            string[] metrics = (string[])type.GetField("_metricNames", flags).GetValue(null);
            string[] markers = (string[])type.GetField("_markerNames", flags).GetValue(null);
            string[] workload = (string[])type.GetField("_workloadNames", flags).GetValue(null);
            int count = (int)type.GetField("METRIC_COUNT", flags).GetRawConstantValue();
            int first = (int)type.GetField("FIRST_MARKER_METRIC", flags).GetRawConstantValue();
            Assert.That(metrics.Length, Is.EqualTo(count));
            Assert.That(markers.Length, Is.EqualTo(count - first));
            Assert.That(metrics, Does.Contain("player_loop_ns"));
            Assert.That(metrics, Does.Contain("attribution_overhead_ns"));
            int firstCounter = (int)type.GetField("FIRST_BUILTIN_COUNTER", flags).GetRawConstantValue();
            Assert.That(workload.Skip(firstCounter), Is.EqualTo(BattleBuiltinRecorders.CounterColumns));
        }

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
                    new BattlePerformanceCaptureMetadata(unityVersion: "6000.4", runtime: "Editor", quality: "High", resolution: "100x100", renderScale: 0.77f, timeScale: 1f,
                        vSyncCount: 0, targetFrameRate: 60, graphicsDevice: "GPU", processor: "CPU", sourceRevision: "abc123", sourceState: "dirty", buildGuid: "build", scenario: "test", targetThreshold: 8, targetBatchSize: 1, dueThreshold: 64),
                    long.MinValue);

                string csv = File.ReadAllText(Path.Combine(directory, "battle_20260917_120000_000.csv"));
                string summary = File.ReadAllText(Path.Combine(directory, "battle_20260917_120000_000_summary.txt"));
                Assert.That(csv, Does.Contain("target_batch_ms,target_batch_sample_count"));
                Assert.That(csv, Does.Contain("due_batch_ms,due_batch_sample_count"));
                Assert.That(csv, Does.Contain("completed_unity_frame,target_requests,due_records"));
                Assert.That(summary, Does.Contain("completed_unity_frame first=42 last=42"));
                Assert.That(summary, Does.Contain("source_revision=abc123"));
                Assert.That(summary, Does.Contain("render_scale=0.770"));
                Assert.That(summary, Does.Contain("target_job_threshold=8"));
                Assert.That(summary, Does.Contain("due_collected_threshold=64"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
