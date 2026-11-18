using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditorInternal;

namespace EmpireAtWar.Editor
{
    internal static class BattleProfilerDecoder
    {
        internal static void WriteCallers(string marker, int first, int last, string path)
        {
            var callers = new Dictionary<string, (long Calls, double Milliseconds)>();
            for (int frame = first; frame <= last; frame++)
            {
                using var view = ProfilerDriver.GetRawFrameDataView(frame, 0);
                if (!view.valid) continue;
                var parents = new List<(int End, string Name)>();
                for (int sample = 0; sample < view.sampleCount; sample++)
                {
                    while (parents.Count > 0 && parents[parents.Count - 1].End < sample)
                        parents.RemoveAt(parents.Count - 1);
                    string name = view.GetSampleName(sample);
                    if (name == marker)
                    {
                        string key = string.Join(" <- ", parents.AsEnumerable().Reverse().Take(14).Select(p => p.Name));
                        callers.TryGetValue(key, out var total);
                        callers[key] = (total.Calls + 1, total.Milliseconds + view.GetSampleTimeMs(sample));
                    }
                    int children = view.GetSampleChildrenCountRecursive(sample);
                    if (children > 0) parents.Add((sample + children, name));
                }
            }
            WriteJson(path, new { marker, first, last, callers = callers.OrderByDescending(p => p.Value.Milliseconds)
                .Select(p => new { caller = p.Key, calls = p.Value.Calls, milliseconds = p.Value.Milliseconds }) });
        }

        internal static void WriteAllocations(int first, int last, string path)
        {
            var frames = new List<int>();
            var totals = new Dictionary<string, (string[] Methods, long Bytes, long Calls, HashSet<int> Frames)>();
            var resolved = new Dictionary<ulong, string>();
            var stack = new List<ulong>();
            for (int frame = first; frame <= last; frame++)
            {
                using var view = ProfilerDriver.GetRawFrameDataView(frame, 0);
                if (!view.valid) continue;
                frames.Add(frame);
                for (int sample = 0; sample < view.sampleCount; sample++)
                {
                    if (view.GetSampleName(sample) != "GC.Alloc") continue;
                    stack.Clear();
                    view.GetSampleCallstack(sample, stack);
                    var methods = new List<string>(14);
                    foreach (ulong address in stack)
                    {
                        if (!resolved.TryGetValue(address, out string method))
                        {
                            var info = view.ResolveMethodInfo(address);
                            method = string.IsNullOrEmpty(info.methodName) ? string.Empty : info.methodName + ":" + info.sourceFileLine;
                            resolved.Add(address, method);
                        }
                        if (method.Length > 0) methods.Add(method);
                        if (methods.Count == 14) break;
                    }
                    if (methods.Count == 0) methods.Add("<no resolved stack>");
                    string key = string.Join(" <- ", methods);
                    if (!totals.TryGetValue(key, out var total))
                        total = (methods.ToArray(), 0, 0, new HashSet<int>());
                    total.Frames.Add(frame);
                    totals[key] = (total.Methods, total.Bytes + view.GetSampleMetadataAsLong(sample, 0), total.Calls + 1, total.Frames);
                }
            }
            WriteJson(path, new { first, last, frames, scope = "main-thread GC.Alloc; up to 14 resolved stack frames",
                stacks = totals.OrderByDescending(p => p.Value.Bytes).Select(p => new
                { methods = p.Value.Methods, bytes = p.Value.Bytes, calls = p.Value.Calls, frames = p.Value.Frames.OrderBy(f => f).ToArray() }) });
        }

        internal static void WriteJson(string path, object result) => File.WriteAllText(path, JsonConvert.SerializeObject(result, Formatting.Indented));

        internal static Task RunAudit(string outputPath, params string[] arguments)
        {
            string script = Path.GetFullPath("Tools/BattleAudit/battle_audit.py");
            return Task.Run(() =>
            {
                var start = new ProcessStartInfo("python", string.Join(" ", new[] { script }.Concat(arguments).Select(Quote)))
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                using var process = Process.Start(start);
                var errors = process.StandardError.ReadToEndAsync();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                string error = errors.GetAwaiter().GetResult();
                File.WriteAllText(outputPath, output + (error.Length > 0 ? "\n" + error : string.Empty));
                if (process.ExitCode != 0) throw new InvalidOperationException($"battle_audit.py exited {process.ExitCode}: {error}");
            });
        }

        private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
