using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    internal static class RenderAuditRenderGraphTool
    {
        private static CaptureOperation _activeCapture;

        static RenderAuditRenderGraphTool()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () => Cancel("assembly_reload");
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    Cancel("play_mode_exit");
                }
            };
        }

        [CliCommand("render_audit_render_graph", "Captures actual Render Graph pass and resource data to Logs/RenderAudit.")]
        public static object CaptureRenderGraph(
            [CliArg("frames", "Number of rendered frames to capture.")] int frames = 2,
            [CliArg("timeout_seconds", "Real-time timeout for the capture.")] int timeoutSeconds = 15)
        {
            var directory = RenderAuditCaptureStatus.CreateCaptureDirectory("render-graph");
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
                return RenderAuditCaptureStatus.CreateResult("error", "Render Graph capture is already running.");
            }

            if (!EditorApplication.isPlaying)
            {
                return RenderAuditCaptureStatus.CreateResult("unsupported", "Render Graph capture requires an already-running player; this tool does not enter Play Mode.");
            }

            if (EditorApplication.isPaused)
            {
                return RenderAuditCaptureStatus.CreateResult("unsupported", "Render Graph capture requires an unpaused player; this tool preserves the current pause state.");
            }

            if (frames < 1 || timeoutSeconds < 1)
            {
                return RenderAuditCaptureStatus.CreateResult("error", "frames and timeout_seconds must both be at least one.");
            }

            try
            {
                var operation = new CaptureOperation(directory, frames, timeoutSeconds, completed);
                _activeCapture = operation;
                operation.Start();
            }
            catch (Exception exception)
            {
                _activeCapture = null;
                return RenderAuditCaptureStatus.CreateResult("unsupported", $"Render Graph debug session API is unavailable: {exception.Message}");
            }

            var result = RenderAuditCaptureStatus.CreateResult("running", "Render Graph capture started.");
            result["phase"] = "render_graph";
            result["capture_directory"] = directory;
            return result;
        }

        internal static void Cancel(string reason)
        {
            _activeCapture?.Cancel(reason);
        }

        private sealed class CaptureOperation
        {
            private readonly string _directory;
            private readonly int _framesRequested;
            private readonly double _deadline;
            private readonly Action<Dictionary<string, object>> _completed;
            private readonly Type _sessionType;
            private readonly MethodInfo _getRegisteredGraphs;
            private readonly MethodInfo _getExecutions;
            private readonly MethodInfo _getDebugData;
            private readonly MethodInfo _toJson;
            private readonly MethodInfo _endSession;
            private readonly PropertyInfo _currentSession;
            private readonly List<object> _frameIdentities = new();
            private object _ownedSession;
            private int _validExecutionCount;

            internal CaptureOperation(string directory, int framesRequested, int timeoutSeconds, Action<Dictionary<string, object>> completed)
            {
                _directory = directory;
                _framesRequested = framesRequested;
                _deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
                _completed = completed;
                _sessionType = FindType("UnityEngine.Rendering.RenderGraphModule.RenderGraphDebugSession");
                var debugSerializationType = FindType("UnityEngine.Rendering.RenderGraphModule.RenderGraph+DebugDataSerialization");
                _getRegisteredGraphs = _sessionType?.GetMethod("GetRegisteredGraphs", BindingFlags.Public | BindingFlags.Static);
                _getExecutions = _sessionType?.GetMethod("GetExecutions", BindingFlags.Public | BindingFlags.Static);
                _getDebugData = _sessionType?.GetMethod("GetDebugData", BindingFlags.Public | BindingFlags.Static);
                _endSession = _sessionType?.GetMethod("EndSession", BindingFlags.Public | BindingFlags.Static);
                _currentSession = _sessionType?.GetProperty("currentDebugSession", BindingFlags.Public | BindingFlags.Static);
                _toJson = debugSerializationType?.GetMethod("ToJson", BindingFlags.Public | BindingFlags.Static);
                if (_sessionType == null || _getRegisteredGraphs == null || _getExecutions == null || _getDebugData == null || _endSession == null || _currentSession == null || _toJson == null)
                {
                    throw new InvalidOperationException("Expected Unity 6000 Render Graph debug session methods were not found.");
                }
            }

            internal void Start()
            {
                var currentSession = _currentSession.GetValue(null);
                if (currentSession == null)
                {
                    var localSessionType = FindType("UnityEngine.Rendering.RenderGraphModule.RenderGraphEditorLocalDebugSession");
                    var create = _sessionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .SingleOrDefault(method => method.Name == "Create" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
                    if (localSessionType == null || create == null)
                    {
                        throw new InvalidOperationException("Expected Unity 6000 local Render Graph debug session type was not found.");
                    }

                    create.MakeGenericMethod(localSessionType).Invoke(null, null);
                    _ownedSession = _currentSession.GetValue(null) ?? throw new InvalidOperationException("Render Graph debug session creation did not produce a current session.");
                }
                else
                {
                    var localSessionType = FindType("UnityEngine.Rendering.RenderGraphModule.RenderGraphEditorLocalDebugSession");
                    if (localSessionType == null || !localSessionType.IsInstanceOfType(currentSession))
                    {
                        throw new InvalidOperationException("The active Render Graph debug session is not Unity's local Editor session, so its data could be stale or remote.");
                    }
                }

                RenderPipelineManager.endFrameRendering += HandleEndFrameRendering;
                EditorApplication.update += Tick;
                EditorApplication.QueuePlayerLoopUpdate();
            }

            internal void Cancel(string reason)
            {
                Complete("cancelled", $"Render Graph capture cancelled: {reason}.");
            }

            private void HandleEndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
            {
                var frameNumber = _frameIdentities.Count + 1;
                var artifact = $"render-graph-frame-{frameNumber:D2}.json";
                var snapshot = ExportSnapshot();
                RenderAuditCaptureStatus.WriteJson(_directory, artifact, snapshot);
                _validExecutionCount += (int)snapshot["valid_execution_count"];
                _frameIdentities.Add(new Dictionary<string, object>
                {
                    ["rendered_at_utc"] = DateTime.UtcNow.ToString("O"),
                    ["unity_frame"] = Time.frameCount,
                    ["artifact"] = artifact,
                    ["camera_count"] = cameras.Length,
                    ["valid_execution_count"] = snapshot["valid_execution_count"]
                });
            }

            private void Tick()
            {
                if (EditorApplication.timeSinceStartup >= _deadline)
                {
                    Complete("timeout", "Render Graph capture timed out before the requested rendered frames completed.");
                    return;
                }

                if (_frameIdentities.Count >= _framesRequested)
                {
                    Complete(_validExecutionCount > 0 ? "completed" : "unsupported", _validExecutionCount > 0 ? "Captured actual Render Graph pass and resource data." : "Render Graph frames rendered, but no valid debug data was produced by the active pipeline.");
                    return;
                }

                EditorApplication.QueuePlayerLoopUpdate();
            }

            private Dictionary<string, object> ExportSnapshot()
            {
                var graphs = new List<object>();
                var validExecutionCount = 0;
                foreach (var graphName in (IEnumerable)_getRegisteredGraphs.Invoke(null, null))
                {
                    var executions = new List<object>();
                    foreach (var execution in (IEnumerable)_getExecutions.Invoke(null, new[] { graphName }))
                    {
                        var executionType = execution.GetType();
                        var executionName = (string)executionType.GetProperty("name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(execution);
                        var executionId = executionType.GetProperty("id", BindingFlags.Public | BindingFlags.Instance)?.GetValue(execution);
                        var debugData = _getDebugData.Invoke(null, new[] { graphName, executionId });
                        var debugDataType = debugData.GetType();
                        var valid = (bool)debugDataType.GetField("valid", BindingFlags.Public | BindingFlags.Instance).GetValue(debugData);
                        var json = (string)_toJson.Invoke(null, new[] { debugData });
                        if (valid)
                        {
                            validExecutionCount++;
                        }

                        executions.Add(new Dictionary<string, object>
                        {
                            ["name"] = executionName,
                            ["id"] = executionId.ToString(),
                            ["valid"] = valid,
                            ["debug_data"] = string.IsNullOrEmpty(json) ? null : JToken.Parse(json)
                        });
                    }

                    graphs.Add(new Dictionary<string, object>
                    {
                        ["name"] = graphName,
                        ["executions"] = executions
                    });
                }

                return new Dictionary<string, object>
                {
                    ["captured_at_utc"] = DateTime.UtcNow.ToString("O"),
                    ["unity_frame"] = Time.frameCount,
                    ["graphs"] = graphs,
                    ["valid_execution_count"] = validExecutionCount
                };
            }

            private void Complete(string status, string message)
            {
                RenderPipelineManager.endFrameRendering -= HandleEndFrameRendering;
                EditorApplication.update -= Tick;
                try
                {
                    if (_ownedSession != null && ReferenceEquals(_currentSession.GetValue(null), _ownedSession))
                    {
                        _endSession.Invoke(null, null);
                    }
                }
                finally
                {
                    _activeCapture = null;
                    var result = RenderAuditCaptureStatus.CreateResult(status, message);
                    result["phase"] = "render_graph";
                    result["rendered_frame_identities"] = _frameIdentities;
                    result["valid_execution_count"] = _validExecutionCount;
                    RenderAuditCaptureStatus.WriteJson(_directory, "render-graph-summary.json", result);
                    _completed?.Invoke(result);
                }
            }

            private static Type FindType(string fullName)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType(fullName, false);
                    if (type != null)
                    {
                        return type;
                    }
                }

                return null;
            }
        }
    }
}
