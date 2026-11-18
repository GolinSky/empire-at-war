#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace EmpireAtWar.Services.Timing
{
    internal sealed class BattleBuiltinRecorders : IDisposable
    {
        internal static readonly string[] Columns = {
            "gc_collect_ns", "player_loop_ns", "editor_loop_ns", "initialization_ns", "early_update_ns",
            "fixed_update_ns", "pre_update_ns", "update_ns", "pre_late_update_ns", "post_late_update_ns",
            "physics_fixed_update_ns", "script_update_ns", "script_late_update_ns", "loading_read_ns",
            "loading_awake_ns", "present_wait_ns", "vsync_wait_ns", "canvas_rebuild_ns",
            "uncovered_ns", "coordinator_ns", "script_update_remainder_ns", "attribution_overhead_ns" };
        internal static readonly string[] CounterColumns = {
            "gc_collections", "gc_allocation_count", "gc_used_bytes", "gc_reserved_bytes", "total_used_bytes" };
        private static readonly string[][] _candidates = {
            new[] { "GC.Collect" }, new[] { "PlayerLoop" }, new[] { "EditorLoop" },
            new[] { "Initialization" }, new[] { "EarlyUpdate" }, new[] { "FixedUpdate" },
            new[] { "PreUpdate" }, new[] { "Update" }, new[] { "PreLateUpdate" }, new[] { "PostLateUpdate" },
            new[] { "FixedUpdate.PhysicsFixedUpdate", "Physics.Simulate" },
            new[] { "Update.ScriptRunBehaviourUpdate", "BehaviourUpdate" },
            new[] { "PreLateUpdate.ScriptRunBehaviourLateUpdate", "LateBehaviourUpdate" },
            new[] { "Loading.ReadObject", "Loading.ReadObjectThreaded" },
            new[] { "Loading.AwakeFromLoad", "Loading.AwakeFromLoadQueue" },
            new[] { "Gfx.WaitForPresentOnGfxThread", "Gfx.PresentFrame" },
            new[] { "WaitForTargetFPS", "Gfx.WaitForVsync" },
            new[] { "Canvas.BuildBatch", "Canvas.SendWillRenderCanvases" },
            new[] { "GC Allocation In Frame Count" }, new[] { "GC Used Memory" },
            new[] { "GC Reserved Memory" }, new[] { "Total Used Memory" } };
        private static bool _dumped;
        private readonly ProfilerRecorder[] _recorders = new ProfilerRecorder[22];
        internal readonly string[] SelectedNames = new string[22];
        // Timing slots 18–21 are computed values; native counters have a separate availability check in ReadValue.
        internal bool IsAvailable(int index) => index >= 18 || _recorders[index].Valid;
        private int _collections = GC.CollectionCount(0);
        private int _collectionFrame = -1;
        private int _collectionDelta;

        internal BattleBuiltinRecorders()
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var descriptions = new Dictionary<string, ProfilerRecorderDescription>();
            var dump = new StringBuilder("category\tname\tunit\n");
            foreach (var handle in handles)
            {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                descriptions[description.Name] = description;
                dump.Append(description.Category).Append('\t').Append(description.Name).Append('\t').Append(description.UnitType).Append('\n');
            }
            if (!_dumped)
            {
                string root = Application.isEditor ? Path.GetDirectoryName(Application.dataPath) : Application.persistentDataPath;
                string path = Path.Combine(root, "Logs", "BattleAudit", "profiler-markers.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, dump.ToString());
                _dumped = true;
            }
            for (int i = 0; i < _candidates.Length; i++)
            {
                foreach (string candidate in _candidates[i])
                {
                    if (!descriptions.TryGetValue(candidate, out var description)) continue;
                    if (i < 18 && description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;
                    _recorders[i] = ProfilerRecorder.StartNew(description.Category, description.Name, 1,
                        ProfilerRecorderOptions.Default);
                    SelectedNames[i] = description.Name;
                    break;
                }
            }
        }

        internal string DescribeSelection()
        {
            var text = new StringBuilder();
            for (int i = 0; i < SelectedNames.Length; i++)
                text.Append(i < 18 ? Columns[i] : CounterColumns[i - 17]).Append('=')
                    .AppendLine(SelectedNames[i] ?? "unavailable");
            text.AppendLine("uncovered_ns = frame_duration_ns - player_loop_ns - editor_loop_ns; loops may overlap; negative values retained.");
            text.AppendLine("script_update_remainder_ns = max(0, script_update_ns - Tick/PostTick callback time); native scopes may not enclose VContainer callbacks.");
            return text.ToString();
        }

        internal void Read(BattleCaptureFrame frame, int firstMetric, int firstMarker, int firstCounter, long unavailable)
        {
            for (int i = 0; i < 18; i++)
            {
                long value = ReadValue(i, unavailable);
                frame.Metrics[firstMetric + i] = value;
                frame.MarkerCounts[firstMetric + i - firstMarker] = value == unavailable ? -1 : _recorders[i].GetSample(0).Count;
            }
            if (_collectionFrame != frame.UnityFrame)
            {
                int collections = GC.CollectionCount(0);
                _collectionDelta = collections - _collections;
                _collections = collections;
                _collectionFrame = frame.UnityFrame;
            }
            frame.Workload[firstCounter] = _collectionDelta;
            for (int i = 0; i < 4; i++) frame.Workload[firstCounter + i + 1] = ReadValue(18 + i, unavailable);
            long player = frame.Metrics[firstMetric + 1], editor = frame.Metrics[firstMetric + 2];
            SetDerived(frame, firstMetric + 18, firstMarker, player == unavailable || editor == unavailable ? unavailable : frame.Metrics[0] - player - editor);
            SetDerived(frame, firstMetric + 19, firstMarker, frame.CoordinatorNanoseconds);
            long scripts = frame.Metrics[firstMetric + 11];
            SetDerived(frame, firstMetric + 20, firstMarker, scripts == unavailable ? unavailable : Math.Max(0, scripts - frame.ScriptUpdateCoordinatorNanoseconds));
            SetDerived(frame, firstMetric + 21, firstMarker, frame.AttributionOverheadNanoseconds);
        }
        private static void SetDerived(BattleCaptureFrame frame, int metric, int firstMarker, long value)
        {
            frame.Metrics[metric] = value;
            frame.MarkerCounts[metric - firstMarker] = -1; // Derived values have no native sample count.
        }
        private long ReadValue(int index, long unavailable)
        {
            var recorder = _recorders[index];
            return !recorder.Valid || (recorder.Count == 0 && !recorder.WrappedAround) ? unavailable : recorder.GetSample(0).Value;
        }
        public void Dispose()
        {
            for (int i = 0; i < _recorders.Length; i++)
                if (_recorders[i].Valid) _recorders[i].Dispose();
        }
    }
}
#endif
