#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace EmpireAtWar.Services.Timing
{
    internal static class BattleSystemAttribution
    {
        private const int HISTORY = 3;
        private static readonly Dictionary<string, int> _ids = new Dictionary<string, int>();
        private static readonly List<Entry> _entries = new List<Entry>();
        private static readonly long[] _overhead = new long[HISTORY];
        private static readonly int[] _overheadFrames = { -1, -1, -1 };
        private static readonly Dictionary<int, BattleSystemSample> _captureTotals = new Dictionary<int, BattleSystemSample>();
        private static readonly Dictionary<int, BattleSystemSample> _hitchTotals = new Dictionary<int, BattleSystemSample>();
        private static int _hitchFrames;
        private static double _hitchStartSeconds;
        private static long _hitchOverheadNanoseconds;
        internal static bool Enabled;

        private sealed class Entry
        {
            internal readonly string Name;
            internal readonly int[] Frames = { -1, -1, -1 };
            internal readonly BattleSystemSample[] Samples = new BattleSystemSample[HISTORY];
            internal Entry(string name) => Name = name;
        }

        internal static int Register(string name)
        {
            if (_ids.TryGetValue(name, out int id)) return id;
            id = _entries.Count;
            _entries.Add(new Entry(name));
            _ids.Add(name, id);
            return id;
        }

        internal readonly struct Scope : IDisposable
        {
            private readonly int _id;
            private readonly int _frame;
            private readonly long _start;
            private readonly long _bytes;
            internal Scope(int id)
            {
                if (!Enabled)
                {
                    _id = -1;
                    _frame = -1;
                    _bytes = _start = 0;
                    return;
                }
                long begin = Stopwatch.GetTimestamp();
                _id = id;
                _frame = _id < 0 ? -1 : Time.frameCount;
                _bytes = _id < 0 ? 0 : Profiler.GetMonoUsedSizeLong();
                long ready = Stopwatch.GetTimestamp();
                if (_id >= 0) AddOverhead(_frame, ready - begin);
                _start = Stopwatch.GetTimestamp();
            }
            public void Dispose()
            {
                if (_id < 0) return;
                long end = Stopwatch.GetTimestamp();
                // Unity's Mono returns 0 from GC.GetAllocatedBytesForCurrentThread; the used-heap delta is page-granular
                // (~4 KB) and reads 0 when a GC runs inside the scope.
                long bytes = Math.Max(0, Profiler.GetMonoUsedSizeLong() - _bytes);
                Record(_id, _frame, ToNanoseconds(end - _start), bytes);
                AddOverhead(_frame, Stopwatch.GetTimestamp() - end);
            }
        }

        internal static void Record(int id, int frame, long nanoseconds, long bytes)
        {
            Entry entry = _entries[id];
            int slot = frame % HISTORY;
            if (entry.Frames[slot] != frame)
            {
                entry.Frames[slot] = frame;
                entry.Samples[slot] = new BattleSystemSample { Name = entry.Name };
            }
            ref BattleSystemSample sample = ref entry.Samples[slot];
            sample.Nanoseconds += nanoseconds;
            sample.Bytes += bytes;
            sample.Calls++;
        }

        internal static Scope Measure(int id) => new Scope(id);
        private static long ToNanoseconds(long ticks) => (long)(ticks * (1000000000d / Stopwatch.Frequency));
        private static void AddOverhead(int frame, long ticks)
        {
            int slot = frame % HISTORY;
            if (_overheadFrames[slot] != frame) { _overheadFrames[slot] = frame; _overhead[slot] = 0; }
            _overhead[slot] += ticks;
        }

        internal static void ReadFrame(int frame, BattleCaptureFrame target)
        {
            long begin = Stopwatch.GetTimestamp();
            Array.Clear(target.AllocationTop, 0, 5);
            Array.Clear(target.TimeTop, 0, 5);
            target.CoordinatorNanoseconds = 0;
            target.ScriptUpdateCoordinatorNanoseconds = 0;
            int slot = frame % HISTORY;
            foreach (Entry entry in _entries)
            {
                if (entry.Frames[slot] != frame) continue;
                BattleSystemSample sample = entry.Samples[slot];
                target.CoordinatorNanoseconds += sample.Nanoseconds;
                if (sample.Name.EndsWith(".Tick()", StringComparison.Ordinal) || sample.Name.EndsWith(".PostTick()", StringComparison.Ordinal))
                    target.ScriptUpdateCoordinatorNanoseconds += sample.Nanoseconds;
                Insert(target.AllocationTop, sample, true);
                Insert(target.TimeTop, sample, false);
            }
            target.AttributionOverheadNanoseconds = (_overheadFrames[slot] == frame ? ToNanoseconds(_overhead[slot]) : 0)
                + ToNanoseconds(Stopwatch.GetTimestamp() - begin);
        }

        private static void Insert(BattleSystemSample[] top, BattleSystemSample sample, bool allocations)
        {
            long value = allocations ? sample.Bytes : sample.Nanoseconds;
            for (int i = 0; i < top.Length; i++)
            {
                if (top[i].Name != null && value <= (allocations ? top[i].Bytes : top[i].Nanoseconds)) continue;
                for (int j = top.Length - 1; j > i; j--) top[j] = top[j - 1];
                top[i] = sample;
                return;
            }
        }

        internal static void ResetFrames()
        {
            foreach (Entry entry in _entries)
                for (int i = 0; i < HISTORY; i++) entry.Frames[i] = -1;
            for (int i = 0; i < HISTORY; i++) _overheadFrames[i] = -1;
        }

        internal static void BeginCapture() => _captureTotals.Clear();
        internal static void BeginHitches() { _hitchTotals.Clear(); _hitchFrames = 0; _hitchOverheadNanoseconds = 0; _hitchStartSeconds = Time.realtimeSinceStartupAsDouble; }
        internal static void AccumulateHitch(BattleCaptureFrame frame)
        {
            Accumulate(frame.UnityFrame, _hitchTotals);
            _hitchFrames++;
            _hitchOverheadNanoseconds += frame.AttributionOverheadNanoseconds;
        }
        internal static void AccumulateCapture(int frame) => Accumulate(frame, _captureTotals);
        private static void Accumulate(int frame, Dictionary<int, BattleSystemSample> totals)
        {
            int slot = frame % HISTORY;
            for (int id = 0; id < _entries.Count; id++)
            {
                Entry entry = _entries[id];
                if (entry.Frames[slot] != frame) continue;
                BattleSystemSample sample = entry.Samples[slot];
                totals.TryGetValue(id, out BattleSystemSample total);
                total.Name = sample.Name;
                total.Bytes += sample.Bytes;
                total.Nanoseconds += sample.Nanoseconds;
                total.Calls += sample.Calls;
                totals[id] = total;
            }
        }
        internal static string FormatSummary(int frames)
        {
            using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            WriteSummary(writer, frames, _captureTotals);
            return writer.ToString();
        }
        internal static void WriteHitchSummary(string path)
        {
            if (_hitchFrames == 0) return;
            try
            {
                WriteSummary(path, _hitchFrames, _hitchTotals);
                File.AppendAllText(path, FormattableString.Invariant($"sampled_frames={_hitchFrames}\nattribution_overhead_total_ms={_hitchOverheadNanoseconds / 1000000d:F6}\nattribution_overhead_mean_ms={_hitchOverheadNanoseconds / (1000000d * _hitchFrames):F6}\nobserved_wall_seconds={Time.realtimeSinceStartupAsDouble - _hitchStartSeconds:F3}\n"));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"Hitch attribution summary could not be saved to {path}: {e.Message}");
            }
        }
        private static void WriteSummary(string path, int frames, Dictionary<int, BattleSystemSample> samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using var writer = new StreamWriter(path, true);
            WriteSummary(writer, frames, samples);
        }

        private static void WriteSummary(TextWriter writer, int frames, Dictionary<int, BattleSystemSample> samples)
        {
            writer.WriteLine("\nPer-system totals (main thread; inclusive callbacks; allocations exclude workers and MonoBehaviour callbacks):");
            writer.WriteLine("system,bytes,calls,total_ms,bytes_per_frame");
            var totals = new List<BattleSystemSample>(samples.Values);
            totals.Sort((a, b) => b.Bytes.CompareTo(a.Bytes));
            foreach (BattleSystemSample total in totals)
                writer.WriteLine(FormattableString.Invariant($"\"{total.Name}\",{total.Bytes},{total.Calls},{total.Nanoseconds / 1000000d:F6},{total.Bytes / (double)frames:F3}"));
            writer.WriteLine("attribution_overhead_ms includes clock/GC counter reads, aggregation and top-five selection; excludes CSV formatting/writes and registration.");
        }
    }
}
#endif
