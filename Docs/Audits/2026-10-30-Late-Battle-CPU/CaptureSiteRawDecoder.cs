using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.Profiling;
using UnityEditorInternal;

// Reads the already-loaded native Profiler recording; never starts recording, loads a scene or enters Play Mode.
// unity command run_script --file Docs/Audits/2026-10-30-Late-Battle-CPU/CaptureSiteRawDecoder.cs
//   --entry CaptureSiteRawDecoder.Run --args '["Logs/CaptureSites/raw-descendants.json",525,614]' --json
public static class CaptureSiteRawDecoder
{
    private const string CAPTURE_TICK = "EmpireAtWar.Services.CaptureSites.CaptureSitesSystem.Tick()";

    public static object Run(string outputPath, int firstFrame, int lastFrame)
    {
        int maximumFrame = -1;
        int maximumSample = -1;
        double maximumMs = -1;
        var samples = new List<object>();
        for (int frame = firstFrame; frame <= lastFrame; frame++)
        {
            using (var view = ProfilerDriver.GetRawFrameDataView(frame, 0))
            {
                if (!view.valid) continue;
                for (int sample = 0; sample < view.sampleCount; sample++)
                {
                    if (view.GetSampleName(sample) != CAPTURE_TICK) continue;
                    double duration = view.GetSampleTimeMs(sample);
                    samples.Add(new { frame, sample, inclusive_ms = duration, self_ms = SelfMs(view, sample) });
                    if (duration <= maximumMs) continue;
                    maximumMs = duration;
                    maximumFrame = frame;
                    maximumSample = sample;
                }
            }
        }
        if (maximumFrame < 0) throw new InvalidOperationException("No capture-site Tick samples in the requested loaded frames.");

        using (var view = ProfilerDriver.GetRawFrameDataView(maximumFrame, 0))
        {
            int end = maximumSample + view.GetSampleChildrenCountRecursive(maximumSample);
            var tree = new List<object>();
            var directChildren = new List<object>();
            var parents = new Stack<int>();
            for (int sample = maximumSample; sample <= end; sample++)
            {
                while (parents.Count > 0 && parents.Peek() < sample) parents.Pop();
                object row = new
                {
                    sample,
                    depth = parents.Count,
                    marker_id = view.GetSampleMarkerId(sample),
                    marker = view.GetSampleName(sample),
                    inclusive_ms = view.GetSampleTimeMs(sample),
                    self_ms = SelfMs(view, sample),
                    direct_child_count = view.GetSampleChildrenCount(sample),
                    descendant_count = view.GetSampleChildrenCountRecursive(sample)
                };
                tree.Add(row);
                if (parents.Count == 1) directChildren.Add(row);
                int descendants = view.GetSampleChildrenCountRecursive(sample);
                if (descendants > 0) parents.Push(sample + descendants);
            }

            var frameTotals = Totals(view, 0, view.sampleCount - 1);
            var result = new
            {
                captured_at_utc = DateTime.UtcNow.ToString("O"),
                first_frame = firstFrame,
                last_frame = lastFrame,
                thread_index = view.threadIndex,
                thread_name = view.threadName,
                maximum_frame = maximumFrame,
                maximum_sample = maximumSample,
                maximum_inclusive_ms = maximumMs,
                maximum_self_ms = SelfMs(view, maximumSample),
                frame_time_ms = view.frameTimeMs,
                note = "Raw sample ancestry, unlike top-40 summaries. Inclusive parent/child times overlap; do not add them. Self time can contain uninstrumented managed/native work or OS suspension; absent GC markers do not prove zero GC activity.",
                capture_samples = samples,
                direct_children = directChildren,
                full_subtree = tree,
                nested_marker_totals = Totals(view, maximumSample + 1, end).OrderByDescending(row => row.SelfMs).ToArray(),
                frame_top_markers_by_self = frameTotals.OrderByDescending(row => row.SelfMs).Take(40).ToArray(),
                frame_gc_markers = frameTotals.Where(row => row.Marker != null &&
                    (row.Marker.StartsWith("GC.", StringComparison.Ordinal) ||
                     row.Marker.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0)).ToArray(),
                capture_gc_alloc_bytes = AllocationBytes(view, maximumSample, end),
                frame_gc_alloc_bytes = AllocationBytes(view, 0, view.sampleCount - 1)
            };
            string path = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(result, Formatting.Indented));
            return new { path, maximumFrame, maximumSample, maximumMs, descendant_count = end - maximumSample };
        }
    }

    private static double SelfMs(RawFrameDataView view, int sample)
    {
        double self = view.GetSampleTimeMs(sample);
        int child = sample + 1;
        for (int i = 0; i < view.GetSampleChildrenCount(sample); i++)
        {
            self -= view.GetSampleTimeMs(child);
            child += 1 + view.GetSampleChildrenCountRecursive(child);
        }
        return Math.Max(0, self);
    }

    private static List<MarkerTotal> Totals(RawFrameDataView view, int first, int last)
    {
        var totals = new Dictionary<int, MarkerTotal>();
        for (int sample = first; sample <= last; sample++)
        {
            int id = view.GetSampleMarkerId(sample);
            if (!totals.TryGetValue(id, out MarkerTotal total))
            {
                total = new MarkerTotal { MarkerId = id, Marker = view.GetSampleName(sample) };
                totals.Add(id, total);
            }
            total.Calls++;
            total.InclusiveMs += view.GetSampleTimeMs(sample);
            total.SelfMs += SelfMs(view, sample);
        }
        return totals.Values.ToList();
    }

    private static long AllocationBytes(RawFrameDataView view, int first, int last)
    {
        long bytes = 0;
        for (int sample = first; sample <= last; sample++)
            if (view.GetSampleName(sample) == "GC.Alloc") bytes += view.GetSampleMetadataAsLong(sample, 0);
        return bytes;
    }

    private sealed class MarkerTotal
    {
        public int MarkerId;
        public string Marker;
        public int Calls;
        public double InclusiveMs;
        public double SelfMs;
    }
}
