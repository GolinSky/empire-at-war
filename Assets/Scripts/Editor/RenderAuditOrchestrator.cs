using System;
using System.Collections.Generic;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    internal static class RenderAuditOrchestrator
    {
        private static CaptureOperation _activeCapture;

        static RenderAuditOrchestrator()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () => CancelCapture("assembly_reload");
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    CancelCapture("play_mode_exit");
                }
            };
        }

[MenuItem("Tools/Render Audit/Capture All (2 frames)")]
        private static void CaptureFromMenu()
        {
            Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(StartCapture(2, 180)));
        }

        [MenuItem("Tools/Render Audit/Cancel Capture")]
        private static void CancelFromMenu()
        {
            CancelActiveCapture();
            RenderAuditProfilerTool.Cancel("menu");
            RenderAuditRenderGraphTool.Cancel("menu");
            RenderAuditFrameDebuggerTool.Cancel("menu");
        }

        [CliCommand("render_audit_capture", "Runs bounded profiler, Render Graph, Rendering Debugger, and Frame Debugger capture phases.")]
        public static object StartCapture(
            [CliArg("frames", "Rendered frames for each frame-based phase.")] int frames = 2,
            [CliArg("timeout_seconds", "Real-time timeout across all phases.")] int timeoutSeconds = 30)
        {
            if (_activeCapture != null || RenderAuditProfilerTool.IsCapturing || RenderAuditRenderGraphTool.IsCapturing || RenderAuditFrameDebuggerTool.IsCapturing)
            {
                return RenderAuditCaptureStatus.CreateResult("error", "A Render Audit capture is already running.");
            }

            var directory = RenderAuditCaptureStatus.CreateCaptureDirectory("capture");
            if (!EditorApplication.isPlaying)
            {
                var unsupported = RenderAuditCaptureStatus.CreateResult("unsupported", "The all-tools capture requires an already-running player to count rendered frames; it does not enter Play Mode.");
                unsupported["capture_directory"] = directory;
                RenderAuditCaptureStatus.WriteJson(directory, "capture-status.json", unsupported);
                return unsupported;
            }

            if (frames < 1 || timeoutSeconds < 1)
            {
                var error = RenderAuditCaptureStatus.CreateResult("error", "frames and timeout_seconds must both be at least one.");
                error["capture_directory"] = directory;
                RenderAuditCaptureStatus.WriteJson(directory, "capture-status.json", error);
                return error;
            }

            _activeCapture = new CaptureOperation(directory, frames, timeoutSeconds);
            _activeCapture.Start();
            return GetCaptureStatus();
        }

        [CliCommand("render_audit_capture_status", "Returns the active all-tools Render Audit capture status.")]
        public static object GetCaptureStatus()
        {
            return _activeCapture?.GetStatus() ?? RenderAuditCaptureStatus.ReadLastStatus() ?? RenderAuditCaptureStatus.CreateResult("idle", "No all-tools Render Audit capture has run in this Editor session.");
        }

        [CliCommand("render_audit_capture_cancel", "Cancels the active all-tools Render Audit capture and restores owned debug state.")]
        public static object CancelActiveCapture()
        {
            if (_activeCapture == null)
            {
                return RenderAuditCaptureStatus.CreateResult("idle", "No all-tools Render Audit capture is running.");
            }

            _activeCapture.Cancel("command");
            return _activeCapture?.GetStatus() ?? RenderAuditCaptureStatus.CreateResult("cancelled", "All-tools Render Audit capture cancelled.");
        }

        private static void CancelCapture(string reason)
        {
            _activeCapture?.Cancel(reason);
        }

        private sealed class CaptureOperation
        {
            private readonly string _directory;
            private readonly int _frames;
            private readonly double _deadline;
            private readonly List<object> _phases = new();
            private int _phaseIndex;
            private bool _finished;

            internal CaptureOperation(string directory, int frames, int timeoutSeconds)
            {
                _directory = directory;
                _frames = frames;
                _deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            }

            internal void Start()
            {
                WriteStatus("running", "All-tools Render Audit capture started.");
                StartNextPhase();
            }

            internal void Cancel(string reason)
            {
                if (_finished)
                {
                    return;
                }

                _finished = true;
                RenderAuditProfilerTool.Cancel(reason);
                RenderAuditRenderGraphTool.Cancel(reason);
                RenderAuditFrameDebuggerTool.Cancel(reason);
                Complete("cancelled", $"All-tools Render Audit capture cancelled: {reason}.");
            }

            internal Dictionary<string, object> GetStatus()
            {
                var status = RenderAuditCaptureStatus.CreateResult(_finished ? "completed" : "running", _finished ? "All-tools Render Audit capture finished." : "All-tools Render Audit capture is running.");
                status["capture_directory"] = _directory;
                status["status_path"] = RenderAuditCaptureStatus.GetStatusPath(_directory);
                status["phases"] = _phases;
                status["active_phase"] = _finished ? null : GetPhaseName();
                return status;
            }

            private void StartNextPhase()
            {
                if (_finished)
                {
                    return;
                }

                if (EditorApplication.timeSinceStartup >= _deadline)
                {
                    Complete("timeout", "All-tools Render Audit capture reached its real-time timeout.");
                    return;
                }

                switch (_phaseIndex)
                {
                    case 0:
                        StartProfilerPhase();
                        break;
                    case 1:
                        StartRenderGraphPhase();
                        break;
                    case 2:
                        FinishPhase(RenderAuditRenderingDebuggerTool.Capture(_directory, "rendering_debugger"));
                        break;
                    case 3:
                        StartFrameDebuggerPhase();
                        break;
                    default:
                        Complete(BuildFinalStatus(), "All requested Render Audit phases completed.");
                        break;
                }
            }

            private void StartProfilerPhase()
            {
                var result = RenderAuditProfilerTool.BeginCapture(_directory, _frames, GetRemainingSeconds(), FinishPhase);
                if ((string)result["status"] != "running")
                {
                    FinishPhase(result);
                }
            }

            private void StartRenderGraphPhase()
            {
                var result = RenderAuditRenderGraphTool.BeginCapture(_directory, _frames, GetRemainingSeconds(), FinishPhase);
                if ((string)result["status"] != "running")
                {
                    FinishPhase(result);
                }
            }

            private void StartFrameDebuggerPhase()
            {
                var result = RenderAuditFrameDebuggerTool.BeginCapture(_directory, "frame_debugger", _frames, GetRemainingSeconds(), 5000, FinishPhase);
                if ((string)result["status"] != "running")
                {
                    FinishPhase(result);
                }
            }

            private void FinishPhase(Dictionary<string, object> result)
            {
                if (_finished)
                {
                    return;
                }

                result["phase"] = GetPhaseName();
                _phases.Add(result);
                _phaseIndex++;
                WriteStatus("running", $"Completed {result["phase"]} phase with status {result["status"]}.");
                StartNextPhase();
            }

            private void Complete(string status, string message)
            {
                if (!_finished)
                {
                    _finished = true;
                }

                var result = RenderAuditCaptureStatus.CreateResult(status, message);
                result["capture_directory"] = _directory;
                result["phases"] = _phases;
                result["frame_identity_note"] = "Each frame-based artifact records its own rendered frame identity and timestamp. Phases do not promise identical frames.";
                RenderAuditCaptureStatus.WriteJson(_directory, "capture-status.json", result);
                _activeCapture = null;
            }

            private void WriteStatus(string status, string message)
            {
                var result = RenderAuditCaptureStatus.CreateResult(status, message);
                result["capture_directory"] = _directory;
                result["phases"] = _phases;
                result["active_phase"] = GetPhaseName();
                RenderAuditCaptureStatus.WriteJson(_directory, "capture-status.json", result);
            }

            private int GetRemainingSeconds()
            {
                return Math.Max(1, (int)Math.Ceiling(_deadline - EditorApplication.timeSinceStartup));
            }

            private string GetPhaseName()
            {
                return _phaseIndex switch
                {
                    0 => "profiler",
                    1 => "render_graph",
                    2 => "rendering_debugger",
                    3 => "frame_debugger",
                    _ => null
                };
            }

            private string BuildFinalStatus()
            {
                var completedCount = 0;
                foreach (Dictionary<string, object> phase in _phases)
                {
                    if ((string)phase["status"] == "completed")
                    {
                        completedCount++;
                    }
                }

                return completedCount == _phases.Count ? "completed" : completedCount == 0 ? "unsupported" : "partial";
            }
        }
    }
}
