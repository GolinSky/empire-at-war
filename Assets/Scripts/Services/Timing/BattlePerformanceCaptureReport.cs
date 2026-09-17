using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace EmpireAtWar.Services.Timing
{
    internal static class BattlePerformanceCaptureReport
    {
        private const int FRAME_DURATION = 0;
        private const int MAIN_THREAD = 1;
        private const int RENDER_THREAD = 2;
        private const int GPU_FRAME = 3;

        public static void Write(
            string directory,
            DateTime captureStartUtc,
            float durationSeconds,
            string stopReason,
            int frameCount,
            float[] timestamps,
            long[] samples,
            long[] markerSampleCounts,
            long[] workloadSamples,
            bool[] recorderAvailable,
            string[] metricNames,
            string[] markerNames,
            string[] workloadNames,
            int firstMarkerMetric,
            BattlePerformanceCaptureMetadata metadata,
            long unavailable)
        {
            Directory.CreateDirectory(directory);
            string prefix = $"battle_{captureStartUtc:yyyyMMdd_HHmmss_fff}";
            WriteCsv(Path.Combine(directory, $"{prefix}.csv"), frameCount, timestamps, samples, markerSampleCounts,
                workloadSamples, metricNames, workloadNames, firstMarkerMetric, unavailable);
            WriteSummary(Path.Combine(directory, $"{prefix}_summary.txt"), captureStartUtc, durationSeconds, stopReason,
                frameCount, samples, markerSampleCounts, workloadSamples, recorderAvailable, metricNames, markerNames,
                workloadNames, firstMarkerMetric, metadata, unavailable);
        }

        private static void WriteCsv(
            string path,
            int frameCount,
            float[] timestamps,
            long[] samples,
            long[] markerSampleCounts,
            long[] workloadSamples,
            string[] metricNames,
            string[] workloadNames,
            int firstMarkerMetric,
            long unavailable)
        {
            using StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8);
            writer.Write("frame,seconds");
            for (int i = 0; i < metricNames.Length; i++)
            {
                writer.Write(',');
                writer.Write(ToCsvName(metricNames[i], IsTimingMetric(i, firstMarkerMetric)));
                if (i >= firstMarkerMetric)
                {
                    writer.Write(',');
                    writer.Write(metricNames[i].Replace("_ns", string.Empty));
                    writer.Write("_sample_count");
                }
            }

            for (int i = 0; i < workloadNames.Length; i++)
            {
                writer.Write(',');
                writer.Write(workloadNames[i]);
            }

            writer.WriteLine();
            for (int frame = 0; frame < frameCount; frame++)
            {
                writer.Write(frame.ToString(CultureInfo.InvariantCulture));
                writer.Write(',');
                writer.Write(timestamps[frame].ToString("F6", CultureInfo.InvariantCulture));
                int sampleOffset = frame * metricNames.Length;
                int markerOffset = frame * (metricNames.Length - firstMarkerMetric);
                for (int metric = 0; metric < metricNames.Length; metric++)
                {
                    writer.Write(',');
                    writer.Write(FormatValue(samples[sampleOffset + metric], IsTimingMetric(metric, firstMarkerMetric), unavailable));
                    if (metric >= firstMarkerMetric)
                    {
                        writer.Write(',');
                        long count = markerSampleCounts[markerOffset + metric - firstMarkerMetric];
                        writer.Write(count < 0 ? "unavailable" : count.ToString(CultureInfo.InvariantCulture));
                    }
                }

                int workloadOffset = frame * workloadNames.Length;
                for (int workload = 0; workload < workloadNames.Length; workload++)
                {
                    writer.Write(',');
                    writer.Write(workloadSamples[workloadOffset + workload].ToString(CultureInfo.InvariantCulture));
                }

                writer.WriteLine();
            }
        }

        private static void WriteSummary(
            string path,
            DateTime captureStartUtc,
            float durationSeconds,
            string stopReason,
            int frameCount,
            long[] samples,
            long[] markerSampleCounts,
            long[] workloadSamples,
            bool[] recorderAvailable,
            string[] metricNames,
            string[] markerNames,
            string[] workloadNames,
            int firstMarkerMetric,
            BattlePerformanceCaptureMetadata metadata,
            long unavailable)
        {
            StringBuilder report = new StringBuilder(4096);
            report.AppendLine("Battle performance capture");
            report.AppendFormat(CultureInfo.InvariantCulture, "started_utc={0:O}\nframes={1}\nduration_seconds={2:F3}\nstop_reason={3}\n", captureStartUtc, frameCount, durationSeconds, stopReason);
            report.AppendFormat(CultureInfo.InvariantCulture, "unity={0}\nruntime={1}\nquality={2}\nresolution={3}\ntime_scale={4:F3}\nvsync_count={5}\ntarget_frame_rate={6}\ngraphics_device={7}\nprocessor={8}\n", metadata.UnityVersion, metadata.Runtime, metadata.Quality, metadata.Resolution, metadata.TimeScale, metadata.VSyncCount, metadata.TargetFrameRate, metadata.GraphicsDevice, metadata.Processor);
            report.AppendFormat(CultureInfo.InvariantCulture,
                "source_revision={0}\nsource_state={1}\nbuild_guid={2}\nscenario={3}\ntarget_variant=automatic\ntarget_job_threshold={4}\ntarget_job_batch_size={5}\ndue_variant=automatic\ndue_job_threshold={6}\ndue_job_batch_size={7}\n",
                metadata.SourceRevision, metadata.SourceState, metadata.BuildGuid, metadata.Scenario,
                metadata.TargetThreshold, metadata.TargetBatchSize, metadata.DueThreshold, metadata.DueBatchSize);
            report.AppendLine("Job schedule and completion markers measure main-thread dispatch and wait; inspect CPU Timeline for worker execution.");
            report.AppendLine("CSV rows sample the preceding completed Unity frame; completed_unity_frame identifies its combat workload.");
            report.AppendLine("cpu_main_thread and cpu_render_thread include waits. GPU values can arrive later than CPU values.");
            report.AppendLine("unavailable means the recorder or its sample was not collected; it is never a measured zero.");
            report.AppendLine("Editor Play Mode adds profiling and editor overhead. Repeat important captures in a Development Player before changing architecture.");
            report.AppendLine();

            for (int metric = 0; metric < metricNames.Length; metric++)
            {
                AppendMetricSummary(report, metric, frameCount, metricNames.Length, samples, markerSampleCounts, recorderAvailable[metric], metricNames[metric], firstMarkerMetric, unavailable);
            }

            report.AppendFormat(CultureInfo.InvariantCulture, "{0} first={1} last={2}\n",
                workloadNames[0], workloadSamples[0],
                workloadSamples[(frameCount - 1) * workloadNames.Length]);
            for (int workload = 1; workload < workloadNames.Length; workload++)
                AppendWorkloadSummary(report, workload, frameCount, workloadNames, workloadSamples);

            AppendFramePacingSummary(report, frameCount, metricNames.Length, samples);

            report.AppendLine();
            report.AppendLine("Custom marker order:");
            for (int i = 0; i < markerNames.Length; i++)
            {
                report.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}\n", metricNames[firstMarkerMetric + i], markerNames[i]);
            }

            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
        }

        private static void AppendWorkloadSummary(StringBuilder report, int index, int frameCount,
            string[] workloadNames, long[] workloadSamples)
        {
            long total = 0;
            long maximum = 0;
            for (int frame = 0; frame < frameCount; frame++)
            {
                long value = workloadSamples[frame * workloadNames.Length + index];
                total += value;
                if (value > maximum) maximum = value;
            }

            report.AppendFormat(CultureInfo.InvariantCulture, "{0} total={1} max={2}\n",
                workloadNames[index], total, maximum);
        }

        private static void AppendMetricSummary(
            StringBuilder report,
            int metric,
            int frameCount,
            int metricCount,
            long[] samples,
            long[] markerSampleCounts,
            bool recorderAvailable,
            string metricName,
            int firstMarkerMetric,
            long unavailable)
        {
            long[] values = new long[frameCount];
            int availableFrames = 0;
            double total = 0d;
            for (int frame = 0; frame < frameCount; frame++)
            {
                long value = samples[frame * metricCount + metric];
                if (value == unavailable)
                {
                    continue;
                }

                values[availableFrames++] = value;
                total += value;
            }

            report.Append(metricName);
            report.Append(recorderAvailable ? " recorder=available" : " recorder=unavailable");
            report.AppendFormat(CultureInfo.InvariantCulture, " sample_frames={0}/{1}", availableFrames, frameCount);
            if (availableFrames == 0)
            {
                report.AppendLine();
                return;
            }

            Array.Sort(values, 0, availableFrames);
            int p95Index = PercentileIndex(availableFrames, 0.95d);
            int p99Index = PercentileIndex(availableFrames, 0.99d);
            bool timing = IsTimingMetric(metric, firstMarkerMetric);
            report.AppendFormat(CultureInfo.InvariantCulture, " avg={0} p95={1} p99={2} max={3}", FormatSummary(total / availableFrames, timing), FormatSummary(values[p95Index], timing), FormatSummary(values[p99Index], timing), FormatSummary(values[availableFrames - 1], timing));
            if (metric >= firstMarkerMetric)
            {
                long totalSamples = 0;
                int markerIndex = metric - firstMarkerMetric;
                for (int frame = 0; frame < frameCount; frame++)
                {
                    long count = markerSampleCounts[frame * (metricCount - firstMarkerMetric) + markerIndex];
                    if (count > 0)
                    {
                        totalSamples += count;
                    }
                }

                report.AppendFormat(CultureInfo.InvariantCulture, " total_sample_count={0}", totalSamples);
            }

            report.AppendLine();
        }

        private static bool IsTimingMetric(int metric, int firstMarkerMetric)
        {
            return metric == FRAME_DURATION || metric == MAIN_THREAD || metric == RENDER_THREAD || metric == GPU_FRAME || metric >= firstMarkerMetric;
        }

        private static void AppendFramePacingSummary(StringBuilder report, int frameCount, int metricCount, long[] samples)
        {
            long[] durations = new long[frameCount];
            for (int frame = 0; frame < frameCount; frame++)
            {
                durations[frame] = samples[frame * metricCount + FRAME_DURATION];
            }

            Array.Sort(durations);
            long median = durations[PercentileIndex(frameCount, 0.50d)];
            long p95 = durations[PercentileIndex(frameCount, 0.95d)];
            long p99 = durations[PercentileIndex(frameCount, 0.99d)];
            long max = durations[frameCount - 1];
            report.AppendFormat(CultureInfo.InvariantCulture, "frame_pacing median={0} p95={1} p99={2} max={3}\n", FormatSummary(median, true), FormatSummary(p95, true), FormatSummary(p99, true), FormatSummary(max, true));
            report.AppendFormat(CultureInfo.InvariantCulture, "fps_at_frame_pacing median={0:F1} p95={1:F1} p99={2:F1} max={3:F1}\n", ToFps(median), ToFps(p95), ToFps(p99), ToFps(max));
        }

        private static int PercentileIndex(int count, double percentile)
        {
            return Math.Min(count - 1, (int)Math.Ceiling(count * percentile) - 1);
        }

        private static double ToFps(long durationNanoseconds)
        {
            return durationNanoseconds > 0 ? 1000000000d / durationNanoseconds : 0d;
        }

        private static string ToCsvName(string metricName, bool timing)
        {
            return timing ? metricName.Replace("_ns", "_ms") : metricName;
        }

        private static string FormatValue(long value, bool timing, long unavailable)
        {
            if (value == unavailable)
            {
                return "unavailable";
            }

            return timing
                ? (value / 1000000d).ToString("F6", CultureInfo.InvariantCulture)
                : value.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatSummary(double value, bool timing)
        {
            return timing
                ? $"{value / 1000000d:F3}ms"
                : value.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}
