using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.Profiling;
using UnityEditorInternal;

public static class InspectJobTimeline
{
    public static object Main()
    {
        var backup = Path.Combine(Path.GetTempPath(), "jobs-audit-" + Guid.NewGuid() + ".data");
        if (!ProfilerDriver.SaveProfile(backup)) throw new InvalidOperationException("Cannot preserve Profiler buffer.");
        try
        {
            if (!ProfilerDriver.LoadProfile("Logs/RenderAudit/20260930T165740445Z-capture/profiler.data", false))
                throw new InvalidOperationException("Cannot load regression recording.");
            var frames = new List<object>();
            for (var frame = ProfilerDriver.firstFrameIndex; frame <= ProfilerDriver.lastFrameIndex; frame++)
            {
                var threads = new List<object>();
                for (var thread = 0; ; thread++)
                {
                    using var view = ProfilerDriver.GetRawFrameDataView(frame, thread);
                    if (!view.valid) break;
                    var stack = new Stack<(int End, string Name)>();
                    var interesting = new List<object>();
                    var totals = new Dictionary<string, (int Calls, double Inclusive, double Self)>();
                    var shipTicks = new List<object>();
                    for (var sample = 0; sample < view.sampleCount; sample++)
                    {
                        while (stack.Count > 0 && stack.Peek().End <= sample) stack.Pop();
                        var name = view.GetSampleName(sample) ?? "<unnamed>";
                        var end = sample + 1 + view.GetSampleChildrenCountRecursive(sample);
                        var duration = view.GetSampleTimeMs(sample);
                        double self = duration;
                        var child = sample + 1;
                        for (var index = 0; index < view.GetSampleChildrenCount(sample); index++)
                        {
                            self -= view.GetSampleTimeMs(child);
                            child += 1 + view.GetSampleChildrenCountRecursive(child);
                        }
                        if (name.Contains("Navigation") || name.Contains("Battle.Attack") || name.Contains("JobHandle") ||
                            name == "WaitForJobGroupID" || name == "GC.Alloc" || name == "Battle.Ship.Tick")
                        {
                            totals.TryGetValue(name, out var total);
                            totals[name] = (total.Calls + 1, total.Inclusive + duration, total.Self + Math.Max(0, self));
                            if (name != "GC.Alloc") interesting.Add(new {
                                sample, marker = name, inclusive_ms = duration, self_ms = Math.Max(0, self),
                                ancestors = stack.Reverse().Select(parent => parent.Name).ToArray()
                            });
                        }
                        if (name == "Battle.Ship.Tick") shipTicks.Add(new {
                            sample, inclusive_ms = duration, self_ms = Math.Max(0, self),
                            gc_alloc_calls = Enumerable.Range(sample + 1, end - sample - 1)
                                .Count(index => view.GetSampleName(index) == "GC.Alloc"),
                            nested_jobs = Enumerable.Range(sample + 1, end - sample - 1)
                                .Select(index => view.GetSampleName(index))
                                .Where(marker => marker != null && marker.Contains("Navigation"))
                                .GroupBy(marker => marker).ToDictionary(group => group.Key, group => group.Count())
                        });
                        if (end > sample + 1) stack.Push((end, name));
                    }
                    threads.Add(new {
                        name = view.threadName, frame_time_ms = view.frameTimeMs,
                        totals = totals.Select(pair => new { marker = pair.Key, calls = pair.Value.Calls,
                            inclusive_ms = pair.Value.Inclusive, self_ms = pair.Value.Self }).ToArray(),
                        ship_ticks = shipTicks, events = interesting
                    });
                }
                frames.Add(new { frame, threads });
            }
            var path = "output/RenderAuditReview_20260930/job-timeline.json";
            File.WriteAllText(path, JsonConvert.SerializeObject(new { frames }, Formatting.Indented));
            return new { path, frames = frames.Count };
        }
        finally
        {
            ProfilerDriver.LoadProfile(backup, false);
            File.Delete(backup);
        }
    }
}
