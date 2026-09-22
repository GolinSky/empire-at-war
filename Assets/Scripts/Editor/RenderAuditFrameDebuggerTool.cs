using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    internal static class RenderAuditFrameDebuggerTool
    {
        private const int LOCAL_EDITOR_CONNECTION = -1;
        private static CaptureOperation _activeCapture;
        private static Type _utilityType;
        private static PropertyInfo _enabled;
        private static MethodInfo _setEnabled;
        private static PropertyInfo _count;
        private static PropertyInfo _eventsHash;
        private static PropertyInfo _eventDataHash;
        private static MethodInfo _getFrameEvents;
        private static MethodInfo _getFrameEventData;
        private static MethodInfo _getFrameEventInfoName;
        private static Type _eventDataType;

        static RenderAuditFrameDebuggerTool()
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

[MenuItem("Tools/Render Audit/Capture Frame Debugger (2 frames)")]
        private static void CaptureFromMenu()
        {
            Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(CaptureFrameDebugger(2, 120)));
        }

        [CliCommand("render_audit_frame_debugger", "Captures fresh Frame Debugger event data to Logs/RenderAudit.")]
        public static object CaptureFrameDebugger(
            [CliArg("frames", "Number of distinct stepped frames to capture.")] int frames = 2,
            [CliArg("timeout_seconds", "Real-time timeout for the capture.")] int timeoutSeconds = 15,
            [CliArg("max_events", "Maximum events to export for each captured frame.")] int maxEvents = 5000)
        {
            var directory = RenderAuditCaptureStatus.CreateCaptureDirectory("frame-debugger");
            var started = BeginCapture(directory, "standalone", frames, timeoutSeconds, maxEvents, result => RenderAuditCaptureStatus.WriteJson(directory, "capture-status.json", result));
            started["status_path"] = RenderAuditCaptureStatus.GetStatusPath(directory);
            RenderAuditCaptureStatus.WriteJson(directory, "capture-status.json", started);
            return started;
        }

        internal static bool IsCapturing => _activeCapture != null;

        internal static Dictionary<string, object> BeginCapture(string directory, string phase, int frames, int timeoutSeconds, int maxEvents, Action<Dictionary<string, object>> completed)
        {
            if (_activeCapture != null)
            {
                return RenderAuditCaptureStatus.CreateResult("error", "Frame Debugger capture is already running.");
            }

            if (!EditorApplication.isPlaying)
            {
                return RenderAuditCaptureStatus.CreateResult("unsupported", "Frame Debugger capture requires an already-running player; this tool does not enter Play Mode.");
            }

            if (frames < 1 || timeoutSeconds < 1 || maxEvents < 1)
            {
                return RenderAuditCaptureStatus.CreateResult("error", "frames, timeout_seconds, and max_events must all be at least one.");
            }

            try
            {
                ResolveApi();
                if (!IsLocallySupported())
                {
                    return RenderAuditCaptureStatus.CreateResult("unsupported", "Frame Debugger is not locally supported by the current graphics device.");
                }

                _activeCapture = new CaptureOperation(directory, phase, frames, timeoutSeconds, maxEvents, completed);
                _activeCapture.Start();
                var result = RenderAuditCaptureStatus.CreateResult("running", "Frame Debugger capture started.");
                result["phase"] = phase;
                result["capture_directory"] = directory;
                return result;
            }
            catch (Exception exception)
            {
                _activeCapture = null;
                return RenderAuditCaptureStatus.CreateResult("unsupported", $"Frame Debugger API is unavailable: {exception.Message}");
            }
        }

        internal static void Cancel(string reason)
        {
            _activeCapture?.Cancel(reason);
        }

        private static Dictionary<string, object> CaptureEvent(int index, List<object> eventDescriptors)
        {
            var entry = new Dictionary<string, object>
            {
                ["index"] = index,
                ["name"] = _getFrameEventInfoName?.Invoke(null, new object[] { index })
            };
            if (eventDescriptors != null && index < eventDescriptors.Count)
            {
                entry["descriptor"] = CapturePublicFields(eventDescriptors[index]);
            }

            var data = Activator.CreateInstance(_eventDataType, true);
            var parameters = new object[] { index, data };
            if (_getFrameEventData.Invoke(null, parameters) is bool success && success)
            {
                entry["data"] = CapturePublicFields(parameters[1]);
            }
            else
            {
                entry["data_error"] = "GetFrameEventData returned false.";
            }

            return entry;
        }

        private static Dictionary<string, object> CapturePublicFields(object value)
        {
            var fields = new Dictionary<string, object>();
            foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                fields[field.Name] = SerializeValue(field.GetValue(value));
            }

            return fields;
        }

        private static object SerializeValue(object value)
        {
            if (value == null || value is string || value is bool || value is char || value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
            {
                return value;
            }

            if (value is Enum)
            {
                return value.ToString();
            }

            if (value is UnityEngine.Object unityObject)
            {
                return new Dictionary<string, object>
                {
                    ["name"] = unityObject.name,
                    ["instance_id"] = unityObject.GetInstanceID()
                };
            }

            if (value is IEnumerable enumerable)
            {
                var values = new List<object>();
                foreach (var item in enumerable)
                {
                    values.Add(SerializeValue(item));
                }

                return values;
            }

            return value.ToString();
        }

        private static List<object> ToList(IEnumerable values)
        {
            var list = new List<object>();
            foreach (var value in values)
            {
                list.Add(value);
            }

            return list;
        }

        private static bool IsLocallySupported()
        {
            var locallySupported = _utilityType.GetProperty("locallySupported", BindingFlags.Public | BindingFlags.Static);
            return locallySupported == null || (bool)locallySupported.GetValue(null);
        }

        private static void ResolveApi()
        {
            if (_utilityType != null)
            {
                return;
            }

            _utilityType = FindType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility")
                ?? FindType("UnityEditorInternal.FrameDebuggerUtility");
            _eventDataType = FindType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerEventData")
                ?? FindType("UnityEditorInternal.FrameDebuggerEventData");
            var frameDebuggerType = typeof(UnityEngine.Object).Assembly.GetType("UnityEngine.FrameDebugger", true);
            _enabled = frameDebuggerType.GetProperty("enabled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _setEnabled = _utilityType?.GetMethod("SetEnabled", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool), typeof(int) }, null);
            _count = _utilityType?.GetProperty("count", BindingFlags.Public | BindingFlags.Static);
            _eventsHash = _utilityType?.GetProperty("eventsHash", BindingFlags.Public | BindingFlags.Static);
            _eventDataHash = _utilityType?.GetProperty("eventDataHash", BindingFlags.Public | BindingFlags.Static);
            _getFrameEvents = _utilityType?.GetMethod("GetFrameEvents", BindingFlags.Public | BindingFlags.Static);
            _getFrameEventData = _utilityType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "GetFrameEventData" && method.GetParameters().Length == 2);
            _getFrameEventInfoName = _utilityType?.GetMethod("GetFrameEventInfoName", BindingFlags.Public | BindingFlags.Static);
            if (_utilityType == null || _eventDataType == null || _enabled == null || _setEnabled == null || _count == null || _eventsHash == null || _eventDataHash == null || _getFrameEventData == null)
            {
                throw new InvalidOperationException("Expected Unity 6000 Frame Debugger methods were not found.");
            }
        }

        private static void InvokeSetEnabled(bool enabled)
        {
            _setEnabled.Invoke(null, new object[] { enabled, LOCAL_EDITOR_CONNECTION });
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

        private sealed class CaptureOperation
        {
            private readonly string _directory;
            private readonly string _phase;
            private readonly int _framesRequested;
            private readonly int _maxEvents;
            private readonly DateTime _deadline;
            private readonly Action<Dictionary<string, object>> _completed;
            private readonly List<object> _frameIdentities = new();
            private readonly List<object> _capturedEvents = new();
            private List<object> _descriptors;
            private bool _wasPaused;
            private bool _finished;
            private int _expectedUnityFrame;
            private int _stepDelay;
            private int _captureUnityFrame;
            private int _eventIndex;
            private int _repaintsRemaining;
            private PropertyInfo _limit;
            private EditorWindow _gameView;
            private Action _setSceneRepaintDirty;

            internal CaptureOperation(string directory, string phase, int framesRequested, int timeoutSeconds, int maxEvents, Action<Dictionary<string, object>> completed)
            {
                _directory = directory;
                _phase = phase;
                _framesRequested = framesRequested;
                _maxEvents = maxEvents;
                _deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
                _completed = completed;
            }

            internal void Start()
            {
                if ((bool)_enabled.GetValue(null))
                    throw new InvalidOperationException("Disable the existing Frame Debugger session before starting an audit.");

                _limit = _utilityType.GetProperty("limit", BindingFlags.Public | BindingFlags.Static);
                _gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView", true));
                _setSceneRepaintDirty = (Action)Delegate.CreateDelegate(typeof(Action), typeof(EditorApplication).GetMethod("SetSceneRepaintDirty", BindingFlags.NonPublic | BindingFlags.Static));
                _wasPaused = EditorApplication.isPaused;
                EditorApplication.isPaused = true;
                EditorApplication.update += Tick;
                RequestFrameStep();
            }

            internal void Cancel(string reason)
            {
                Complete("cancelled", $"Frame Debugger capture cancelled: {reason}.");
            }

            private void Tick()
            {
                try
                {
                    if (DateTime.UtcNow >= _deadline)
                    {
                        Complete("timeout", $"Frame Debugger timed out at frame {_frameIdentities.Count + 1}, event {_eventIndex}; completed frames remain on disk.");
                        return;
                    }

                    if (!(bool)_enabled.GetValue(null))
                    {
                        if (_stepDelay > 0)
                        {
                            if (--_stepDelay == 0)
                            {
                                _expectedUnityFrame = Time.frameCount + 1;
                                EditorApplication.Step();
                            }
                            EditorApplication.QueuePlayerLoopUpdate();
                            Repaint();
                            return;
                        }
                        if (Time.frameCount < _expectedUnityFrame)
                        {
                            EditorApplication.QueuePlayerLoopUpdate();
                            Repaint();
                            return;
                        }
                        _captureUnityFrame = Time.frameCount;
                        InvokeSetEnabled(true);
                        _repaintsRemaining = 4;
                        Repaint();
                        return;
                    }

                    if (_repaintsRemaining > 0)
                    {
                        _repaintsRemaining--;
                        Repaint();
                        return;
                    }

                    if (_descriptors == null)
                    {
                        if ((int)_count.GetValue(null) == 0)
                        {
                            Repaint();
                            return;
                        }

                        _descriptors = ToList((IEnumerable)_getFrameEvents.Invoke(null, null)).Take((int)_count.GetValue(null)).ToList();
                        _eventIndex = 0;
                        SelectEvent();
                        return;
                    }

                    // Editor UI events can disappear between replays. Refresh the list when
                    // Unity changes its authoritative event count instead of waiting on a removed event.
                    var eventCount = (int)_count.GetValue(null);
                    if (eventCount != _descriptors.Count)
                    {
                        _descriptors = null;
                        _capturedEvents.Clear();
                        Repaint();
                        return;
                    }

                    var entry = CaptureEvent(_eventIndex, _descriptors);
                    if (entry.ContainsKey("data_error"))
                    {
                        Repaint();
                        return;
                    }

                    _capturedEvents.Add(entry);
                    _eventIndex++;
                    if (_eventIndex < Math.Min(_descriptors.Count, _maxEvents))
                    {
                        SelectEvent();
                        return;
                    }

                    SaveFrame();
                    if (_frameIdentities.Count == _framesRequested)
                        Complete("completed", "Captured selected-event Frame Debugger data for all requested frames.");
                    else
                        RequestFrameStep();
                }
                catch (Exception exception)
                {
                    Complete("error", exception.ToString());
                }
            }

            private void Repaint()
            {
                _setSceneRepaintDirty.Invoke();
                _gameView.Repaint();
            }

            private void SelectEvent()
            {
                _limit.SetValue(null, _eventIndex + 1);
                _repaintsRemaining = 1;
                Repaint();
            }

            private void SaveFrame()
            {
                var artifact = $"frame-debugger-frame-{_frameIdentities.Count + 1:D2}.json";
                var frame = new Dictionary<string, object>
                {
                    ["captured_at_utc"] = DateTime.UtcNow.ToString("O"),
                    ["unity_frame"] = _captureUnityFrame,
                    ["event_count"] = _descriptors.Count,
                    ["exported_event_count"] = _capturedEvents.Count,
                    ["events"] = _capturedEvents,
                    ["truncated"] = _descriptors.Count > _maxEvents
                };
                RenderAuditCaptureStatus.WriteJson(_directory, artifact, frame);
                _frameIdentities.Add(new Dictionary<string, object>
                {
                    ["unity_frame"] = _captureUnityFrame,
                    ["event_count"] = _descriptors.Count,
                    ["exported_event_count"] = _capturedEvents.Count,
                    ["artifact"] = artifact
                });
            }

            private void RequestFrameStep()
            {
                InvokeSetEnabled(false);
                _descriptors = null;
                _capturedEvents.Clear();
                _stepDelay = 2;
                EditorApplication.QueuePlayerLoopUpdate();
                Repaint();
            }

            private void Complete(string status, string message)
            {
                if (_finished)
                    return;
                _finished = true;
                EditorApplication.update -= Tick;
                try
                {
                    InvokeSetEnabled(false);
                    EditorApplication.isPaused = _wasPaused;
                    Repaint();
                }
                finally
                {
                    _activeCapture = null;
                    var result = RenderAuditCaptureStatus.CreateResult(status, message);
                    result["phase"] = _phase;
                    result["frame_count"] = _frameIdentities.Count;
                    result["frame_identities"] = _frameIdentities;
                    RenderAuditCaptureStatus.WriteJson(_directory, "frame-debugger-summary.json", result);
                    _completed.Invoke(result);
                }
            }
        }
    }
}
