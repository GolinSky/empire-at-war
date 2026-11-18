using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    internal static class BattleProfilerFlightRecorder
    {
        private const string MENU = "Tools/Empire At War/Diagnostics/Performance/";
        private const string STATE_KEY = "EmpireAtWar.BattleProfiler.";
        private const int HISTORY_FRAMES = 300;
        private static readonly PropertyInfo _history = typeof(ProfilerDriver).Assembly.GetType("UnityEditor.Profiling.ProfilerUserSettings", true)
            .GetProperty("frameCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly PropertyInfo _memoryMode = typeof(ProfilerDriver).GetProperty("memoryRecordMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly List<string> _allocations = new();
        private static readonly List<Task> _analysis = new();
        private static string _directory;
        private static double _threshold, _cooldown, _lastSave, _sweepEnd, _nextSweep;
        private static int _saveLimit, _saved, _pendingHitch = -1, _allocationFirst = -1, _lastSeen = -1;
        private static bool _sweeping, _sweepOwnsSession;
        internal static bool IsArmed { get; private set; }
        internal static bool IsSweeping => _sweeping;
        internal static Task CompleteAnalysis() => Task.WhenAll(_analysis.ToArray());

        static BattleProfilerFlightRecorder()
        {
            Restore();
            AssemblyReloadEvents.beforeAssemblyReload += () => Disarm("assembly-reload");
            EditorApplication.quitting += () => Disarm("editor-exit");
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingPlayMode) Disarm("play-mode-exit"); };
            EditorApplication.update += Tick;
        }

        [MenuItem(MENU + "Arm Hitch Profiler")]
        private static void ArmMenu() => Arm();
        [MenuItem(MENU + "Arm Hitch Profiler (GC Allocation Stacks)")]
        private static void ArmAllocationsMenu() => Arm(gcStacks: true);
        [MenuItem(MENU + "Disarm Hitch Profiler")]
        private static void DisarmMenu() => Disarm("user");
        [MenuItem(MENU + "Allocation Sweep (5 min)")]
        private static void SweepMenu() => StartAllocationSweep();

        [CliCommand("battle_hitch_profiler_arm", "Arms a bounded rolling native Profiler flight recorder without changing Play Mode.")]
        public static object Arm(
            [CliArg("threshold_ms", "Completed native Profiler frame threshold in milliseconds.")] double thresholdMs = 200,
            [CliArg("max_saves", "Maximum hitch snapshots in this session.")] int maxSaves = 5,
            [CliArg("cooldown_seconds", "Minimum real seconds between saves.")] double cooldownSeconds = 30,
            [CliArg("gc_stacks", "Record GC allocation call stacks while armed (higher overhead).")] bool gcStacks = false,
            [CliArg("directory", "Shared output directory; empty creates a timestamped run.")] string directory = "")
        {
            if (IsArmed || RenderAuditProfilerTool.IsCapturing || Profiler.enableBinaryLog)
                throw new InvalidOperationException("Another profiler capture owns the Profiler. Disarm or finish it first.");
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                throw new InvalidOperationException("Arm requires an already-running, unpaused Play Mode session.");
            if (thresholdMs <= 0 || maxSaves < 1 || cooldownSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(thresholdMs), "Threshold and limit must be positive; cooldown cannot be negative.");
            _directory = string.IsNullOrEmpty(directory) ? Path.Combine("Logs/BattleAudit", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")) : directory;
            Directory.CreateDirectory(_directory);
            string revision = ReadRevision();
            SessionState.SetBool(STATE_KEY + "active", true);
            SessionState.SetBool(STATE_KEY + "enabled", ProfilerDriver.enabled);
            SessionState.SetBool(STATE_KEY + "editor", ProfilerDriver.profileEditor);
            SessionState.SetInt(STATE_KEY + "history", (int)_history.GetValue(null));
            try
            {
                ProfilerDriver.enabled = false;
                ProfilerDriver.ClearAllFrames();
                _threshold = thresholdMs; _saveLimit = maxSaves; _cooldown = cooldownSeconds; _lastSave = double.NegativeInfinity;
                _saved = 0; _pendingHitch = -1; _lastSeen = ProfilerDriver.lastFrameIndex;
                _history.SetValue(null, HISTORY_FRAMES);
                SetMemoryMode(gcStacks);
                ProfilerDriver.profileEditor = true;
                ProfilerDriver.enabled = true;
                IsArmed = true;
                BattleProfilerDecoder.WriteJson(Path.Combine(_directory, "profiler-session.json"), new
                { threshold_ms = thresholdMs, max_saves = maxSaves, cooldown_seconds = cooldownSeconds, history_frames = HISTORY_FRAMES, gc_stacks = gcStacks, profile_editor = true,
                    native_data_scope = "SaveProfile exports the whole rolling buffer; JSON and caller trees select hitch-60 through hitch+10.",
                    started_utc = DateTime.UtcNow, source_revision = revision, unity_version = Application.unityVersion });
                return new { status = "armed", directory = _directory };
            }
            catch { IsArmed = false; Restore(); throw; }
        }

        [CliCommand("battle_allocation_sweep", "Records 60 main-thread allocation frames every 30 seconds, then merges game callers.")]
        public static object StartAllocationSweep(
            [CliArg("duration_seconds", "Duration of the allocation sweep.")] int durationSeconds = 300,
            [CliArg("directory", "Output directory for a standalone sweep.")] string directory = "")
        {
            if (durationSeconds < 1) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (_sweeping) throw new InvalidOperationException("Allocation sweep already active.");
            _sweepOwnsSession = !IsArmed;
            if (_sweepOwnsSession) Arm(gcStacks: true, directory: directory);
            _sweeping = true; _sweepEnd = EditorApplication.timeSinceStartup + durationSeconds; _nextSweep = EditorApplication.timeSinceStartup;
            _allocationFirst = -1; _allocations.Clear(); SetMemoryMode(true);
            return new { status = "sweeping", directory = _directory, duration_seconds = durationSeconds };
        }

        [CliCommand("battle_hitch_profiler_disarm", "Disarms the flight recorder and always clears GC allocation stack recording.")]
        public static object Disarm([CliArg("reason", "Reason recorded in the session status.")] string reason = "user")
        {
            if (!IsArmed) return new { status = "idle" };
            try
            {
                _sweepOwnsSession = false;
                if (_sweeping) FinishSweep();
                BattleProfilerDecoder.WriteJson(Path.Combine(_directory, "profiler-status.json"), new
                { status = "disarmed", reason, saved = _saved, pending_hitch = _pendingHitch, completed_utc = DateTime.UtcNow });
            }
            finally { IsArmed = false; _pendingHitch = -1; Restore(); }
            return new { status = "disarmed", directory = _directory };
        }

        private static void Tick()
        {
            for (int i = _analysis.Count - 1; i >= 0; i--)
            {
                if (!_analysis[i].IsCompleted) continue;
                if (_analysis[i].IsFaulted) Debug.LogException(_analysis[i].Exception);
                _analysis.RemoveAt(i);
            }
            if (!IsArmed) return;
            try
            {
                if (!EditorApplication.isPlaying) { Disarm("play-mode-stopped"); return; }
                if (EditorApplication.isPaused) return;
                int last = ProfilerDriver.lastFrameIndex;
                double now = EditorApplication.timeSinceStartup;
                if (_sweeping && _allocationFirst < 0 && now >= _nextSweep && now < _sweepEnd)
                { _allocationFirst = Math.Max(last, _lastSeen) + 1; _nextSweep = now + 30; }
                int first = Math.Max(_lastSeen + 1, ProfilerDriver.firstFrameIndex);
                for (int frame = first; frame <= last; frame++)
                {
                    using var view = ProfilerDriver.GetRawFrameDataView(frame, 0);
                    if (view.valid && view.frameTimeMs > _threshold && _pendingHitch < 0 && _saved < _saveLimit && now - _lastSave >= _cooldown)
                        _pendingHitch = frame;
                }
                _lastSeen = Math.Max(_lastSeen, last);
                if (_pendingHitch >= 0 && last >= _pendingHitch + 10) SaveHitch();
                if (_allocationFirst >= 0 && last >= _allocationFirst + 59)
                {
                    string path = Path.Combine(_directory, $"allocation_{_allocationFirst}.json");
                    ProfilerDriver.enabled = false;
                    try { BattleProfilerDecoder.WriteAllocations(_allocationFirst, _allocationFirst + 59, path); }
                    finally
                    {
                        ProfilerDriver.enabled = true;
                        _lastSeen = ProfilerDriver.lastFrameIndex + 1;
                    }
                    _allocations.Add(path); _allocationFirst = -1;
                }
                if (_sweeping && now >= _sweepEnd && _allocationFirst < 0) FinishSweep();
            }
            catch (Exception exception) { Debug.LogException(exception); Disarm("error"); }
        }

        private static void SaveHitch()
        {
            int hitch = _pendingHitch;
            _pendingHitch = -1; _saved++;
            int first = Math.Max(ProfilerDriver.firstFrameIndex, hitch - 60), last = hitch + 10;
            string prefix = Path.Combine(_directory, $"hitch_{hitch}");
            // Export/decoding can take seconds and allocate gigabytes. Never record that work into
            // the next buffer, where it would trigger another hitch and recursively amplify itself.
            // An in-progress allocation window must restart after the stopped interval too.
            _allocationFirst = -1;
            ProfilerDriver.enabled = false;
            try
            {
                if (!ProfilerDriver.SaveProfile(prefix + ".data")) throw new IOException("ProfilerDriver.SaveProfile failed: " + prefix);
                var summaries = RenderAuditProfilerTool.SummarizeFrames(Enumerable.Range(first, last - first + 1));
                BattleProfilerDecoder.WriteJson(prefix + ".json", new { hitch_profiler_frame = hitch, first, last, profiler_frame_summaries = summaries });
                using var view = ProfilerDriver.GetRawFrameDataView(hitch, 0);
                string marker = FindTopMarker(view);
                BattleProfilerDecoder.WriteCallers(marker, first, last, prefix + "-callers.json");
                _analysis.Add(WriteHitchReport(prefix, marker, first, last));
            }
            finally
            {
                _lastSave = EditorApplication.timeSinceStartup;
                ProfilerDriver.enabled = true;
                // Ignore the frame crossing the recording boundary as well as the stopped interval.
                _lastSeen = ProfilerDriver.lastFrameIndex + 1;
            }
        }

        private static async Task WriteHitchReport(string prefix, string marker, int first, int last)
        {
            await BattleProfilerDecoder.RunAudit(prefix + "-profiler.txt", "profiler", prefix + ".json").ConfigureAwait(false);
            await BattleProfilerDecoder.RunAudit(prefix + "-decoder.txt", "decoder", "callers", marker, first.ToString(), last.ToString(), prefix + "-callers.cs").ConfigureAwait(false);
            File.WriteAllText(prefix + ".md", $"# Hitch {Path.GetFileName(prefix)}\n\nProfiler range: {first}–{last}. Native `.data` also retains surrounding buffer frames.\n\n```text\n" +
                File.ReadAllText(prefix + "-profiler.txt") + "\n```\n\n## Dominant marker caller tree\n\n```json\n" + File.ReadAllText(prefix + "-callers.json") + "\n```\n");
        }

        internal static string FindTopMarker(UnityEditor.Profiling.RawFrameDataView view)
        {
            string name = "<no samples>"; double maximum = -1;
            for (int sample = 0; sample < view.sampleCount; sample++)
            {
                double self = view.GetSampleTimeMs(sample);
                int child = sample + 1;
                for (int i = 0; i < view.GetSampleChildrenCount(sample); i++)
                { self -= view.GetSampleTimeMs(child); child += view.GetSampleChildrenCountRecursive(child) + 1; }
                if (self > maximum) { maximum = self; name = view.GetSampleName(sample); }
            }
            return name;
        }

        private static void FinishSweep()
        {
            _sweeping = false; _allocationFirst = -1; SetMemoryMode(false);
            if (_allocations.Count > 0)
                _analysis.Add(BattleProfilerDecoder.RunAudit(Path.Combine(_directory, "allocation-analysis.txt"),
                    new[] { "allocations", Path.Combine(_directory, "allocation-ranking.json") }.Concat(_allocations).ToArray()));
            if (_sweepOwnsSession) { _sweepOwnsSession = false; Disarm("allocation-sweep-completed"); }
        }

        private static void SetMemoryMode(bool enabled) => _memoryMode.SetValue(null, Enum.Parse(_memoryMode.PropertyType, enabled ? "GCAlloc" : "None"));

        private static void Restore()
        {
            if (!SessionState.GetBool(STATE_KEY + "active", false)) return;
            SetMemoryMode(false);
            ProfilerDriver.profileEditor = SessionState.GetBool(STATE_KEY + "editor", false);
            ProfilerDriver.enabled = SessionState.GetBool(STATE_KEY + "enabled", false);
            _history.SetValue(null, SessionState.GetInt(STATE_KEY + "history", 300));
            SessionState.EraseBool(STATE_KEY + "active");
        }

        private static string ReadRevision()
        {
            string head = File.ReadAllText(".git/HEAD").Trim();
            if (!head.StartsWith("ref: ", StringComparison.Ordinal)) return head;
            string path = Path.Combine(".git", head.Substring(5));
            return File.Exists(path) ? File.ReadAllText(path).Trim() : head;
        }
    }
}
