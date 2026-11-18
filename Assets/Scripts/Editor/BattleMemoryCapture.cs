using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using Unity.Profiling.Memory;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    internal static class BattleMemoryCapture
    {
        private static string _directory;
        private static double _elapsed, _lastTick;
        private static int _snapshotIndex;
        private static bool _scheduled, _capturing;
        private static readonly Queue<(string Snapshot, string Output)> _exports = new();
        private static readonly List<string> _completed = new();
        private static IEnumerator _export;
        private static Task _comparison;
        internal static bool IsBusy => _scheduled || _capturing || _export != null || _exports.Count > 0;

        static BattleMemoryCapture()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingPlayMode) _scheduled = false; };
            AssemblyReloadEvents.beforeAssemblyReload += () => { _scheduled = false; DisposeExport(); };
        }

        [MenuItem("Tools/Empire At War/Diagnostics/Performance/Schedule Memory Snapshots (5 and 30 min)")]
        private static void ScheduleMenu() => Schedule();

        [CliCommand("battle_memory_snapshots", "Schedules full snapshots at 5/30 unpaused minutes. Never changes Play Mode.")]
        public static object Schedule([CliArg("directory", "Shared output directory; empty creates a timestamped run.")] string directory = "")
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                throw new InvalidOperationException("Memory capture requires an already-running, unpaused Play Mode session.");
            if (IsBusy) throw new InvalidOperationException("Memory capture or export already active.");
            _directory = string.IsNullOrEmpty(directory) ? Path.Combine("Logs/BattleAudit", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-memory") : directory;
            Directory.CreateDirectory(_directory);
            _scheduled = true; _snapshotIndex = 0; _elapsed = 0; _lastTick = EditorApplication.timeSinceStartup; _completed.Clear();
            return new { status = "scheduled", directory = _directory, snapshots_at_running_seconds = new[] { 300, 1800 } };
        }

        [CliCommand("battle_memory_export", "Exports type/direct-owner shallow totals from an existing .snap through Memory Profiler 1.1.12.")]
        public static object Export(
            [CliArg("snapshot", "Path to an existing Memory Profiler .snap file.")] string snapshot,
            [CliArg("output", "JSON output path for type and direct-owner totals.")] string output)
        {
            if (!File.Exists(snapshot)) throw new FileNotFoundException("Memory snapshot not found.", snapshot);
            string parent = Path.GetDirectoryName(Path.GetFullPath(output));
            Directory.CreateDirectory(parent);
            _exports.Enqueue((Path.GetFullPath(snapshot), Path.GetFullPath(output)));
            return new { status = "queued", snapshot, output };
        }

        [CliCommand("battle_memory_cancel", "Cancels future snapshots; an already requested native snapshot completes and is retained.")]
        public static object Cancel()
        {
            _scheduled = false;
            return new { status = "cancelled", native_snapshot_in_flight = _capturing };
        }

        private static void Tick()
        {
            try
            {
                if (_comparison != null && _comparison.IsCompleted)
                {
                    if (_comparison.IsFaulted) Debug.LogException(_comparison.Exception);
                    _comparison = null;
                }
                if (_scheduled)
                {
                    double now = EditorApplication.timeSinceStartup;
                    if (EditorApplication.isPlaying && !EditorApplication.isPaused && Time.timeScale > 0) _elapsed += now - _lastTick;
                    _lastTick = now;
                    if (!_capturing && _elapsed >= (_snapshotIndex == 0 ? 300 : 1800)) Capture();
                }
                // Crawling a snapshot is deliberately deferred until Play Mode stops: it allocates heavily.
                // Capture files persist immediately; exported totals do not contaminate live hitch attribution.
                if (EditorApplication.isPlaying) return;
                if (_export == null && _exports.Count > 0)
                {
                    var next = _exports.Dequeue();
                    _export = BattleMemorySnapshotExporter.Export(next.Snapshot, next.Output);
                    _completed.Add(next.Output);
                }
                if (_export != null && !_export.MoveNext()) DisposeExport();
                if (_export == null && _exports.Count == 0 && _completed.Count == 2 && _comparison == null)
                {
                    string output = Path.Combine(Path.GetDirectoryName(_completed[1]), "memory-diff.json");
                    _comparison = BattleProfilerDecoder.RunAudit(Path.ChangeExtension(output, ".txt"), "memory", _completed[0], _completed[1], output);
                    _completed.Clear();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception); _scheduled = false; _completed.Clear(); DisposeExport();
            }
        }

        private static void Capture()
        {
            _capturing = true;
            int index = _snapshotIndex++;
            if (_snapshotIndex == 2) _scheduled = false;
            string path = Path.GetFullPath(Path.Combine(_directory, index == 0 ? "memory-05min.snap" : "memory-30min.snap"));
            double requestedAt = _elapsed;
            MemoryProfiler.TakeSnapshot(path, (saved, success) =>
            {
                _capturing = false;
                BattleProfilerDecoder.WriteJson(Path.ChangeExtension(saved, ".json"), new
                { success, requested_at_running_seconds = requestedAt, completed_utc = DateTime.UtcNow,
                    note = "Full Editor-process snapshot; snapshot acquisition stalls/allocates and is excluded from clean timing comparisons. Type/direct-owner export runs after user exits Play Mode." });
                if (!success) { Debug.LogError("Memory snapshot failed: " + saved); return; }
                Export(saved, Path.ChangeExtension(saved, ".types.json"));
            }, CaptureFlags.ManagedObjects | CaptureFlags.NativeObjects | CaptureFlags.NativeAllocations);
        }

        private static void DisposeExport()
        {
            if (_export is IDisposable disposable) disposable.Dispose();
            _export = null;
        }
    }
}
