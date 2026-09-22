using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    internal static class RenderAuditProfilerTool
    {
        private const string SESSION_ACTIVE_KEY = "EmpireAtWar.RenderAudit.Profiler.Active";
        private const string SESSION_DRIVER_ENABLED_KEY = "EmpireAtWar.RenderAudit.Profiler.DriverEnabled";
        private const string SESSION_DRIVER_PROFILE_EDITOR_KEY = "EmpireAtWar.RenderAudit.Profiler.DriverProfileEditor";

        private static CaptureOperation _activeCapture;

        static RenderAuditProfilerTool()
        {
            RestoreStateIfNeeded();
            AssemblyReloadEvents.beforeAssemblyReload += () => Cancel("assembly_reload");
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    Cancel("play_mode_exit");
                }
            };
        }

[MenuItem("Tools/Render Audit/Capture Profiler (2 frames)")]
        private static void CaptureFromMenu()
        {
            Debug.Log(JsonConvert.SerializeObject(CaptureProfiler()));
        }

        [CliCommand("render_audit_profiler", "Captures profiler data and per-thread frame summaries to Logs/RenderAudit.")]
        public static object CaptureProfiler(
            [CliArg("frames", "Number of rendered frames to capture.")] int frames = 2,
            [CliArg("timeout_seconds", "Real-time timeout for the capture.")] int timeoutSeconds = 15)
        {
            var directory = RenderAuditCaptureStatus.CreateCaptureDirectory("profiler");
            var started = BeginCapture(directory, frames, timeoutSeconds, result => RenderAuditCaptureStatus.WriteJson(directory, "capture-status.json", result));
            started["status_path"] = RenderAuditCaptureStatus.GetStatusPath(directory);
            RenderAuditCaptureStatus.WriteJson(directory, "capture-status.json", started);
            return started;
        }

        internal static bool IsCapturing => _activeCapture != null;

        internal static Dictionary<string, object> BeginCapture(string directory, int frames, int timeoutSeconds, Action<Dictionary<string, object>> completed)
        {
            if (_activeCapture != null)
            {
                return RenderAuditCaptureStatus.CreateResult("error", "Profiler capture is already running.");
            }

            if (!EditorApplication.isPlaying)
            {
                return RenderAuditCaptureStatus.CreateResult("unsupported", "Profiler capture requires an already-running player; this tool does not enter Play Mode.");
            }

            if (EditorApplication.isPaused)
            {
                return RenderAuditCaptureStatus.CreateResult("unsupported", "Profiler capture requires an unpaused player; this tool preserves the current pause state.");
            }

            if (frames < 1 || timeoutSeconds < 1)
            {
                return RenderAuditCaptureStatus.CreateResult("error", "frames and timeout_seconds must both be at least one.");
            }

            if (Profiler.enableBinaryLog)
            {
                return RenderAuditCaptureStatus.CreateResult("unsupported", "Profiler binary logging is already enabled, so this tool cannot safely own and restore its output target.");
            }

            var operation = new CaptureOperation(directory, frames, timeoutSeconds, completed);
            _activeCapture = operation;
            operation.Start();
            var result = RenderAuditCaptureStatus.CreateResult("running", "Profiler capture started.");
            result["phase"] = "profiler";
            result["capture_directory"] = directory;
            return result;
        }

        internal static void Cancel(string reason)
        {
            _activeCapture?.Cancel(reason);
        }

        private static void SaveState()
        {
            SessionState.SetBool(SESSION_ACTIVE_KEY, true);
            SessionState.SetBool(SESSION_DRIVER_ENABLED_KEY, ProfilerDriver.enabled);
            SessionState.SetBool(SESSION_DRIVER_PROFILE_EDITOR_KEY, ProfilerDriver.profileEditor);
        }

        private static void RestoreStateIfNeeded()
        {
            if (!SessionState.GetBool(SESSION_ACTIVE_KEY, false))
            {
                return;
            }

            ProfilerDriver.profileEditor = SessionState.GetBool(SESSION_DRIVER_PROFILE_EDITOR_KEY, false);
            ProfilerDriver.enabled = SessionState.GetBool(SESSION_DRIVER_ENABLED_KEY, false);
            ClearSavedState();
        }

        private static void ClearSavedState()
        {
            SessionState.EraseBool(SESSION_ACTIVE_KEY);
            SessionState.EraseBool(SESSION_DRIVER_ENABLED_KEY);
            SessionState.EraseBool(SESSION_DRIVER_PROFILE_EDITOR_KEY);
        }

private static Dictionary<string, object> SummarizeRenderingCounters(Dictionary<string, object> counters)
        {
            // Unity 6.4 exposes draw categories instead of a single Draw Calls Count counter.
            var drawCategories = new[]
            {
                "Standard Draw Calls Count", "SRP Batcher Draw Calls Count", "BRG Draw Calls Count",
                "Standard Instanced Draw Calls Count", "Standard Indirect Draw Calls Count",
                "BRG Indirect Draw Calls Count", "Null Geometry Draw Calls Count",
                "Null Geometry Indirect Draw Calls Count"
            };
            long total = 0;
            var hasTotal = true;
            foreach (var name in drawCategories)
            {
                if (counters.TryGetValue(name, out var value))
                    total += Convert.ToInt64(value);
                else
                    hasTotal = false;
            }

            var metrics = new Dictionary<string, object>
            {
                ["draw_calls"] = counters.TryGetValue("Draw Calls Count", out var directTotal) ? directTotal : hasTotal ? total : null,
                ["draw_calls_source"] = "Draw Calls Count, or sum of the eight UnityStats draw-call categories.",
                ["unavailable_note"] = "Null means this frame did not expose the counter; it does not mean zero."
            };
            var names = new Dictionary<string, string>
            {
                ["batches"] = "Batches Count",
                ["dynamic_batches"] = "Dynamic Batches Count",
                ["static_batches"] = "Static Batches Count",
                ["instanced_batches"] = "Instanced Batches Count",
                ["instanced_draw_calls"] = "Standard Instanced Draw Calls Count",
                ["brg_draw_calls"] = "BRG Draw Calls Count",
                ["brg_instances"] = "BRG Instances Count",
                ["setpass_calls"] = "SetPass Calls Count",
                ["triangles"] = "Triangles Count",
                ["vertices"] = "Vertices Count",
                ["shadow_casters"] = "Shadow Casters Count"
            };
            foreach (var pair in names)
                metrics[pair.Key] = counters.TryGetValue(pair.Value, out var value) ? value : null;
            return metrics;
        }

private static Dictionary<string, object> CaptureRenderingStats()
        {
            var batchStats = typeof(UnityStats).GetProperty("batchStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
            return new Dictionary<string, object>
            {
                ["source"] = "UnityStats sampled at endFrameRendering; Editor-wide latest rendering statistics, not camera-isolated.",
                ["draw_calls"] = UnityStats.drawCalls,
                ["batches"] = batchStats.GetType().GetField("batches").GetValue(batchStats),
                ["instances"] = UnityStats.instances,
                ["instanced_draw_calls"] = UnityStats.standardInstancedDrawCalls,
                ["instanced_instances"] = UnityStats.standardInstancedInstances,
                ["brg_draw_calls"] = UnityStats.hybridBatcherDrawCalls,
                ["brg_instances"] = UnityStats.hybridBatcherInstances,
                ["indirect_draw_calls"] = UnityStats.totalIndirectDrawCalls,
                ["srp_batcher_draw_calls"] = UnityStats.srpBatcherDrawCalls,
                ["dynamic_batches"] = UnityStats.dynamicBatches,
                ["static_batches"] = UnityStats.staticBatches,
                ["instanced_batches"] = UnityStats.instancedBatches,
                ["setpass_calls"] = UnityStats.setPassCalls,
                ["triangles"] = UnityStats.triangles,
                ["vertices"] = UnityStats.vertices,
                ["shadow_casters"] = UnityStats.shadowCasters
            };
        }

        private sealed class CaptureOperation
        {
            private readonly string _directory;
            private readonly int _framesRequested;
            private readonly double _deadline;
            private readonly Action<Dictionary<string, object>> _completed;
            private readonly List<object> _frameIdentities = new();
            private readonly HashSet<int> _recordedProfilerFrames = new();
            private bool _flushPending;
            private int _firstProfilerFrameIndex;

            internal CaptureOperation(string directory, int framesRequested, int timeoutSeconds, Action<Dictionary<string, object>> completed)
            {
                _directory = directory;
                _framesRequested = framesRequested;
                _deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
                _completed = completed;
            }

            internal void Start()
            {
                SaveState();
                ProfilerDriver.enabled = true;
                ProfilerDriver.profileEditor = true;
                _firstProfilerFrameIndex = ProfilerDriver.lastFrameIndex + 1;
                RenderPipelineManager.endFrameRendering += HandleEndFrameRendering;
                ProfilerDriver.NewProfilerFrameRecorded += HandleNewProfilerFrameRecorded;
                EditorApplication.update += Tick;
                EditorApplication.QueuePlayerLoopUpdate();
            }

            internal void Cancel(string reason)
            {
                Complete("cancelled", $"Profiler capture cancelled: {reason}.");
            }

            private void HandleEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
            {
                if (_flushPending)
                {
                    return;
                }

                _frameIdentities.Add(new Dictionary<string, object>
                {
                    ["rendered_at_utc"] = DateTime.UtcNow.ToString("O"),
                    ["unity_frame"] = Time.frameCount,
                    ["camera_count"] = cameras.Length,
                    ["camera_names"] = GetCameraNames(cameras),
                    ["unity_stats"] = CaptureRenderingStats()
                });

                if (_frameIdentities.Count >= _framesRequested)
                {
                    _flushPending = true;
                }
            }

            private void Tick()
            {
                if (EditorApplication.timeSinceStartup >= _deadline)
                {
                    Complete("timeout", "Profiler capture timed out before the requested rendered frames completed.");
                    return;
                }

                if (!_flushPending)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (!HasRequestedProfilerFrames())
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                CompleteCapture();
            }

            private void HandleNewProfilerFrameRecorded(int connectionId, int newFrameIndex)
            {
                if (newFrameIndex >= _firstProfilerFrameIndex)
                {
                    _recordedProfilerFrames.Add(newFrameIndex);
                }
            }

            private void CompleteCapture()
            {
                try
                {
                    var profilePath = Path.Combine(_directory, "profiler.data");
                    var saved = ProfilerDriver.SaveProfile(profilePath);
                    var summaries = CaptureFrameSummaries(GetRequestedProfilerFrames());
                    var hasProfileData = saved && File.Exists(profilePath) && new FileInfo(profilePath).Length > 4;
                    var status = hasProfileData && summaries.Count > 0 ? "completed" : hasProfileData ? "partial" : "error";
                    var message = status == "completed" ? "Captured profiler data and frame summaries." : "Profiler capture did not produce both a non-empty editor-buffer .data export and frame summaries.";
                    var result = RenderAuditCaptureStatus.CreateResult(status, message);
                    result["phase"] = "profiler";
                    result["rendered_frame_identities"] = _frameIdentities;
                    result["profiler_frame_summaries"] = summaries;
                    result["profiler_data_artifact"] = hasProfileData ? "profiler.data" : null;
                    RenderAuditCaptureStatus.WriteJson(_directory, "profiler-summary.json", result);
                    Complete(result);
                }
                catch (Exception exception)
                {
                    Complete("error", $"Profiler capture failed: {exception.Message}");
                }
            }

            private void Complete(string status, string message)
            {
                var result = RenderAuditCaptureStatus.CreateResult(status, message);
                result["phase"] = "profiler";
                result["rendered_frame_identities"] = _frameIdentities;
                RenderAuditCaptureStatus.WriteJson(_directory, "profiler-summary.json", result);
                Complete(result);
            }

            private void Complete(Dictionary<string, object> result)
            {
                RenderPipelineManager.endFrameRendering -= HandleEndFrameRendering;
                ProfilerDriver.NewProfilerFrameRecorded -= HandleNewProfilerFrameRecorded;
                EditorApplication.update -= Tick;
                try
                {
                    RestoreStateIfNeeded();
                }
                finally
                {
                    _activeCapture = null;
                    _completed?.Invoke(result);
                }
            }

            private static List<string> GetCameraNames(Camera[] cameras)
            {
                var cameraNames = new List<string>(cameras.Length);
                foreach (var camera in cameras)
                {
                    cameraNames.Add(camera.name);
                }

                return cameraNames;
            }

            private bool HasRequestedProfilerFrames()
            {
                var frameIndices = GetRequestedProfilerFrames();
                if (frameIndices.Count < _framesRequested)
                {
                    return false;
                }

                using var view = ProfilerDriver.GetRawFrameDataView(frameIndices[frameIndices.Count - 1], 0);
                return view.valid && view.frameTimeNs > 0;
            }

            private List<int> GetRequestedProfilerFrames()
            {
                var frameIndices = new List<int>(_recordedProfilerFrames);
                frameIndices.Sort();
                var validFrameIndices = new List<int>();
                foreach (var frameIndex in frameIndices)
                {
                    using var view = ProfilerDriver.GetRawFrameDataView(frameIndex, 0);
                    if (view.valid && view.frameTimeNs > 0)
                    {
                        validFrameIndices.Add(frameIndex);
                        if (validFrameIndices.Count == _framesRequested)
                            break;
                    }
                }

                return validFrameIndices;
            }

            private static List<object> CaptureFrameSummaries(IEnumerable<int> frameIndices)
            {
                var summaries = new List<object>();
                foreach (var frameIndex in frameIndices)
                {
                    var threads = new List<object>();
                    var counters = new Dictionary<string, object>();
                    for (var threadIndex = 0; ; threadIndex++)
                    {
                        using var view = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex);
                        if (!view.valid)
                            break;

                        var markers = new List<FrameDataView.MarkerInfo>();
                        view.GetMarkers(markers);
                        foreach (var marker in markers)
                        {
                            if (marker.category == (ushort)Unity.Profiling.ProfilerCategory.Render &&
                                view.HasCounterValue(marker.id) && !counters.ContainsKey(marker.name))
                                counters.Add(marker.name, view.GetCounterValueAsLong(marker.id));
                        }

                        threads.Add(new Dictionary<string, object>
                        {
                            ["thread_index"] = view.threadIndex,
                            ["thread_name"] = view.threadName,
                            ["sample_count"] = view.sampleCount,
                            ["frame_time_ms"] = view.frameTimeMs,
                            ["frame_gpu_time_ms"] = view.frameGpuTimeMs > 0 ? (object)view.frameGpuTimeMs : null
                        });
                    }

                    if (threads.Count > 0)
                    {
                        summaries.Add(new Dictionary<string, object>
                        {
                            ["profiler_frame_index"] = frameIndex,
                            ["captured_at_utc"] = DateTime.UtcNow.ToString("O"),
                            ["rendering_counters"] = counters,
                            ["rendering_metrics"] = SummarizeRenderingCounters(counters),
                            ["counter_scope"] = "Recorded Editor profiler frame; includes Editor rendering.",
                            ["threads"] = threads
                        });
                    }
                }

                return summaries;
            }
        }
    }
}
