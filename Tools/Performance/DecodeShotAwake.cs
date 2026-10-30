using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditorInternal;
using UnityEngine;

public static class DecodeShotAwake
{
    private const string OUTPUT_FOLDER = "Logs/LateBattleCpu";

    private sealed class Totals
    {
        public int Calls;
        public double InclusiveMs;
        public double MaximumMs;
        public readonly List<double> Durations = new List<double>();
    }

    public static object Main() => Run(ProfilerDriver.firstFrameIndex, ProfilerDriver.lastFrameIndex);

    public static object Run(int firstFrame, int lastFrame)
    {
        var totals = new Dictionary<string, Totals>();
        var slowSamples = new List<(double Duration, object Data)>();
        int validFrames = 0;
        for (int frame = firstFrame; frame <= lastFrame; frame++)
        {
            using (var view = ProfilerDriver.GetRawFrameDataView(frame, 0))
            {
                if (!view.valid) continue;
                validFrames++;
                var parents = new List<(int End, string Name)>();
                for (int sample = 0; sample < view.sampleCount; sample++)
                {
                    while (parents.Count > 0 && parents[parents.Count - 1].End < sample)
                        parents.RemoveAt(parents.Count - 1);
                    string name = view.GetSampleName(sample);
                    if (name == "Instantiate.Awake" || name == "Instantiate.Copy" || name == "Instantiate.Produce")
                    {
                        string[] ancestors = parents.Select(parent => parent.Name).ToArray();
                        string prefabMarker = ancestors.LastOrDefault(ancestor => ancestor != null &&
                            (ancestor.StartsWith("Battle.Projectile.Instantiate.") ||
                             ancestor.StartsWith("Battle.Projectile.Probe.Instantiate.")));
                        string source = ancestors.Any(ancestor => ancestor != null &&
                            ancestor.StartsWith("Battle.Projectile.Probe.Instantiate.")) ? "probe" :
                            ancestors.Any(ancestor => ancestor != null && ancestor.StartsWith("Battle.Projectile.Prewarm."))
                                ? "shared-prewarm" : prefabMarker != null ? "shared-demand" : "unattributed";
                        string key = source + " | " + (prefabMarker ?? "no-prefab-marker") + " | " + name;
                        double duration = view.GetSampleTimeMs(sample);
                        if (!totals.TryGetValue(key, out Totals total)) totals.Add(key, total = new Totals());
                        total.Calls++;
                        total.InclusiveMs += duration;
                        total.MaximumMs = Math.Max(total.MaximumMs, duration);
                        total.Durations.Add(duration);
                        if (name == "Instantiate.Awake")
                        {
                            var children = new List<object>();
                            int child = sample + 1;
                            double childrenMs = 0;
                            for (int childIndex = 0; childIndex < view.GetSampleChildrenCount(sample); childIndex++)
                            {
                                double childMs = view.GetSampleTimeMs(child);
                                childrenMs += childMs;
                                children.Add(new { marker = view.GetSampleName(child), inclusiveMs = childMs });
                                child += 1 + view.GetSampleChildrenCountRecursive(child);
                            }
                            slowSamples.Add((duration, new
                            {
                                frame,
                                sample,
                                source,
                                prefabMarker,
                                inclusiveMs = duration,
                                selfMs = Math.Max(0, duration - childrenMs),
                                ancestors,
                                children
                            }));
                        }
                    }
                    int descendants = view.GetSampleChildrenCountRecursive(sample);
                    if (descendants > 0) parents.Add((sample + descendants, name));
                }
            }
        }
        var summary = totals.OrderByDescending(pair => pair.Value.InclusiveMs).Select(pair => new
        {
            key = pair.Key,
            calls = pair.Value.Calls,
            inclusiveMs = pair.Value.InclusiveMs,
            meanMs = pair.Value.InclusiveMs / pair.Value.Calls,
            medianMs = Median(pair.Value.Durations),
            maximumMs = pair.Value.MaximumMs
        }).ToArray();
        Directory.CreateDirectory(OUTPUT_FOLDER);
        string path = OUTPUT_FOLDER + "/shot-awake-attribution-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".json";
        var report = new
        {
            capturedUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            firstProfilerFrame = firstFrame,
            lastProfilerFrame = lastFrame,
            validFrames,
            note = "Read-only main-thread raw profiler-history decoder. Instantiate stages are siblings; do not add their parent Instantiate duration. Captures remain evolving battle windows.",
            summary,
            slowestAwake = slowSamples.OrderByDescending(sample => sample.Duration).Take(30)
                .Select(sample => sample.Data).ToArray()
        };
        File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { path, validFrames, summary };
    }

    private static double Median(List<double> values)
    {
        double[] sorted = values.OrderBy(value => value).ToArray();
        return sorted.Length % 2 == 0
            ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) * 0.5
            : sorted[sorted.Length / 2];
    }
}
