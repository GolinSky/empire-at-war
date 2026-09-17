using System;
using System.IO;
using EmpireAtWar.Components.Weapon;
using Unity.Profiling;
using UnityEngine;

namespace EmpireAtWar.Services.Timing
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public sealed class BattlePerformanceCapture : MonoBehaviour
    {
        private const int CAPTURE_SECONDS = 10;
        private const int MAX_CAPTURE_FRAMES = 3000;
        private const long UNAVAILABLE = long.MinValue;

        private const int FRAME_DURATION = 0;
        private const int MAIN_THREAD = 1;
        private const int RENDER_THREAD = 2;
        private const int GPU_FRAME = 3;
        private const int GC_ALLOCATED = 4;
        private const int DRAW_CALLS = 5;
        private const int SETPASS_CALLS = 6;
        private const int BATCHES = 7;
        private const int TRIANGLES = 8;
        private const int SHIP_TICK = 9;
        private const int WEAPON_TICK = 10;
        private const int WEAPON_TRY_FIRE = 11;
        private const int PROJECTILE_GET_OR_CREATE = 12;
        private const int PROJECTILE_INSTANTIATE = 13;
        private const int PROJECTILE_TURRET_UPDATE = 14;
        private const int PROJECTILE_LASER_UPDATE = 15;
        private const int RADAR_SCAN = 16;
        private const int TARGET_BATCH = 17;
        private const int TARGET_BATCH_CAPTURE = 18;
        private const int TARGET_BATCH_PREPARE = 19;
        private const int TARGET_BATCH_SERIAL = 20;
        private const int TARGET_BATCH_JOB = 21;
        private const int TARGET_BATCH_SCHEDULE = 22;
        private const int TARGET_BATCH_COMPLETE = 23;
        private const int TARGET_BATCH_APPLY = 24;
        private const int DUE_BATCH = 25;
        private const int DUE_BATCH_PREPARE = 26;
        private const int DUE_BATCH_SERIAL = 27;
        private const int DUE_BATCH_JOB = 28;
        private const int DUE_BATCH_SCHEDULE = 29;
        private const int DUE_BATCH_COMPLETE = 30;
        private const int DUE_BATCH_APPLY = 31;
        private const int METRIC_COUNT = 32;
        private const int FIRST_MARKER_METRIC = SHIP_TICK;

        private static readonly string[] _metricNames =
        {
            "frame_duration_ns", "main_thread_ns", "render_thread_ns", "gpu_frame_ns", "gc_allocated_bytes", "draw_calls", "setpass_calls", "batches", "triangles",
            "ship_tick_ns", "weapon_tick_ns", "weapon_try_fire_ns", "projectile_get_or_create_ns", "projectile_instantiate_ns", "projectile_turret_update_ns", "projectile_laser_update_ns", "radar_scan_ns",
            "target_batch_ns", "target_batch_capture_ns", "target_batch_prepare_ns", "target_batch_serial_ns", "target_batch_job_ns", "target_batch_schedule_ns", "target_batch_complete_ns", "target_batch_apply_ns",
            "due_batch_ns", "due_batch_prepare_ns", "due_batch_serial_ns", "due_batch_job_ns", "due_batch_schedule_ns", "due_batch_complete_ns", "due_batch_apply_ns"
        };

        private static readonly string[] _markerNames =
        {
            BattleProfilerMarkers.SHIP_TICK,
            BattleProfilerMarkers.WEAPON_TICK,
            BattleProfilerMarkers.WEAPON_TRY_FIRE,
            BattleProfilerMarkers.PROJECTILE_GET_OR_CREATE,
            BattleProfilerMarkers.PROJECTILE_INSTANTIATE,
            BattleProfilerMarkers.PROJECTILE_TURRET_UPDATE,
            BattleProfilerMarkers.PROJECTILE_LASER_UPDATE,
            BattleProfilerMarkers.RADAR_SCAN,
            BattleProfilerMarkers.TARGET_BATCH,
            BattleProfilerMarkers.TARGET_BATCH_CAPTURE,
            BattleProfilerMarkers.TARGET_BATCH_PREPARE,
            BattleProfilerMarkers.TARGET_BATCH_SERIAL,
            BattleProfilerMarkers.TARGET_BATCH_JOB,
            BattleProfilerMarkers.TARGET_BATCH_SCHEDULE,
            BattleProfilerMarkers.TARGET_BATCH_COMPLETE,
            BattleProfilerMarkers.TARGET_BATCH_APPLY,
            BattleProfilerMarkers.DUE_BATCH,
            BattleProfilerMarkers.DUE_BATCH_PREPARE,
            BattleProfilerMarkers.DUE_BATCH_SERIAL,
            BattleProfilerMarkers.DUE_BATCH_JOB,
            BattleProfilerMarkers.DUE_BATCH_SCHEDULE,
            BattleProfilerMarkers.DUE_BATCH_COMPLETE,
            BattleProfilerMarkers.DUE_BATCH_APPLY
        };

        private static readonly string[] _workloadNames =
        {
            "completed_unity_frame", "target_requests", "target_candidates", "target_serial_calls", "target_job_calls", "target_fallbacks",
            "target_input_capacity", "target_position_capacity", "target_result_capacity", "target_buffer_growths",
            "due_records", "due_flagged", "due_committed", "due_serial_calls", "due_job_calls",
            "due_input_capacity", "due_result_capacity", "due_buffer_growths", "pending_sequences", "pending_impacts"
        };

        internal struct CombatWorkload
        {
            public int CompletedUnityFrame;
            public int TargetRequests;
            public int TargetCandidates;
            public int TargetSerialCalls;
            public int TargetJobCalls;
            public int TargetFallbacks;
            public int TargetInputCapacity;
            public int TargetPositionCapacity;
            public int TargetResultCapacity;
            public int TargetBufferGrowths;
            public int DueRecords;
            public int DueFlagged;
            public int DueCommitted;
            public int DueSerialCalls;
            public int DueJobCalls;
            public int DueInputCapacity;
            public int DueResultCapacity;
            public int DueBufferGrowths;
            public int PendingSequences;
            public int PendingImpacts;

            public void WriteTo(long[] samples, int offset)
            {
                samples[offset++] = CompletedUnityFrame;
                samples[offset++] = TargetRequests;
                samples[offset++] = TargetCandidates;
                samples[offset++] = TargetSerialCalls;
                samples[offset++] = TargetJobCalls;
                samples[offset++] = TargetFallbacks;
                samples[offset++] = TargetInputCapacity;
                samples[offset++] = TargetPositionCapacity;
                samples[offset++] = TargetResultCapacity;
                samples[offset++] = TargetBufferGrowths;
                samples[offset++] = DueRecords;
                samples[offset++] = DueFlagged;
                samples[offset++] = DueCommitted;
                samples[offset++] = DueSerialCalls;
                samples[offset++] = DueJobCalls;
                samples[offset++] = DueInputCapacity;
                samples[offset++] = DueResultCapacity;
                samples[offset++] = DueBufferGrowths;
                samples[offset++] = PendingSequences;
                samples[offset] = PendingImpacts;
            }
        }

        private static BattlePerformanceCapture _instance;

        private readonly ProfilerRecorder[] _recorders = new ProfilerRecorder[METRIC_COUNT];
        private readonly bool[] _recorderAvailable = new bool[METRIC_COUNT];
        private long[] _samples;
        private long[] _markerSampleCounts;
        private long[] _workloadSamples;
        private CombatWorkload _pendingWorkload;
        private float[] _timestamps;
        private int _frameCount;
        private float _captureEndTime;
        private float _captureStartTime;
        private DateTime _captureStartUtc;
        private bool _capturing;
        private bool _quitting;
        private bool _skipFirstFrame;
        private BattlePerformanceCaptureMetadata _metadata;

        public static bool IsCapturing => _instance != null && _instance._capturing;

        internal static void RecordCombatWorkload(CombatWorkload workload)
        {
            if (IsCapturing) _instance._pendingWorkload = workload;
        }

        public static void StartCapture()
        {
            GetOrCreateInstance().StartCaptureInternal();
        }

        public static void StopCapture()
        {
            if (_instance != null)
            {
                _instance.StopCaptureInternal("stopped");
            }
        }

        private static BattlePerformanceCapture GetOrCreateInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            GameObject host = new GameObject(nameof(BattlePerformanceCapture));
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<BattlePerformanceCapture>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _samples = new long[MAX_CAPTURE_FRAMES * METRIC_COUNT];
            _markerSampleCounts = new long[MAX_CAPTURE_FRAMES * _markerNames.Length];
            _workloadSamples = new long[MAX_CAPTURE_FRAMES * _workloadNames.Length];
            _timestamps = new float[MAX_CAPTURE_FRAMES];
        }

        private void Update()
        {
            if (!_capturing)
            {
                return;
            }

            if (_skipFirstFrame)
            {
                _skipFirstFrame = false;
                return;
            }

            CaptureFrame();
            if (_frameCount >= MAX_CAPTURE_FRAMES)
            {
                StopCaptureInternal("frame_limit");
            }
            else if (Time.realtimeSinceStartup >= _captureEndTime)
            {
                StopCaptureInternal("duration_complete");
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            if (_capturing && !_quitting)
            {
                StopCaptureInternal("play_mode_exit");
            }

            DisposeRecorders();
            _instance = null;
        }

        private void OnApplicationQuit()
        {
            if (_capturing)
            {
                StopCaptureInternal("application_quit");
            }

            _quitting = true;
            DisposeRecorders();
        }

        private void StartCaptureInternal()
        {
            if (_capturing)
            {
                StopCaptureInternal("restarted");
            }

            DisposeRecorders();
            _frameCount = 0;
            _pendingWorkload = default;
            _metadata = CaptureMetadata();
            _captureStartTime = Time.realtimeSinceStartup;
            _captureEndTime = _captureStartTime + CAPTURE_SECONDS;
            _captureStartUtc = DateTime.UtcNow;
            _skipFirstFrame = true;

            StartRecorders();
            _recorderAvailable[FRAME_DURATION] = true;
            _capturing = true;
            Debug.Log($"Battle performance capture started for {CAPTURE_SECONDS} seconds.");
        }

        private void StopCaptureInternal(string reason)
        {
            if (!_capturing)
            {
                return;
            }

            _capturing = false;
            float durationSeconds = Time.realtimeSinceStartup - _captureStartTime;
            if (_frameCount > 0)
            {
                try
                {
                    string directory = Path.Combine(Application.persistentDataPath, "BattleCaptures");
                    BattlePerformanceCaptureReport.Write(
                        directory,
                        _captureStartUtc,
                        durationSeconds,
                        reason,
                        _frameCount,
                        _timestamps,
                        _samples,
                        _markerSampleCounts,
                        _workloadSamples,
                        _recorderAvailable,
                        _metricNames,
                        _markerNames,
                        _workloadNames,
                        FIRST_MARKER_METRIC,
                        _metadata,
                        UNAVAILABLE);
                    Debug.Log($"Battle performance capture saved to {directory}.");
                }
                finally
                {
                    DisposeRecorders();
                }

                return;
            }

            DisposeRecorders();
        }

        private void StartRecorders()
        {
            StartRecorder(MAIN_THREAD, ProfilerCategory.Render, "CPU Main Thread Frame Time", ProfilerRecorderOptions.Default);
            StartRecorder(RENDER_THREAD, ProfilerCategory.Render, "CPU Render Thread Frame Time", ProfilerRecorderOptions.Default);
            StartRecorder(GPU_FRAME, ProfilerCategory.Render, "GPU Frame Time", ProfilerRecorderOptions.Default);
            StartRecorder(GC_ALLOCATED, ProfilerCategory.Memory, "GC Allocated In Frame", ProfilerRecorderOptions.Default);
            StartRecorder(DRAW_CALLS, ProfilerCategory.Render, "Draw Calls Count", ProfilerRecorderOptions.Default);
            StartRecorder(SETPASS_CALLS, ProfilerCategory.Render, "SetPass Calls Count", ProfilerRecorderOptions.Default);
            StartRecorder(BATCHES, ProfilerCategory.Render, "Batches Count", ProfilerRecorderOptions.Default);
            StartRecorder(TRIANGLES, ProfilerCategory.Render, "Triangles Count", ProfilerRecorderOptions.Default);
            StartMarkerRecorder(SHIP_TICK, BattleProfilerMarkers.ShipTick);
            StartMarkerRecorder(WEAPON_TICK, BattleProfilerMarkers.WeaponTick);
            StartMarkerRecorder(WEAPON_TRY_FIRE, BattleProfilerMarkers.WeaponTryFire);
            StartMarkerRecorder(PROJECTILE_GET_OR_CREATE, BattleProfilerMarkers.ProjectileGetOrCreate);
            StartMarkerRecorder(PROJECTILE_INSTANTIATE, BattleProfilerMarkers.ProjectileInstantiate);
            StartMarkerRecorder(PROJECTILE_TURRET_UPDATE, BattleProfilerMarkers.ProjectileTurretUpdate);
            StartMarkerRecorder(PROJECTILE_LASER_UPDATE, BattleProfilerMarkers.ProjectileLaserUpdate);
            StartMarkerRecorder(RADAR_SCAN, BattleProfilerMarkers.RadarScan);
            StartMarkerRecorder(TARGET_BATCH, BattleProfilerMarkers.TargetBatch);
            StartMarkerRecorder(TARGET_BATCH_CAPTURE, BattleProfilerMarkers.TargetBatchCapture);
            StartMarkerRecorder(TARGET_BATCH_PREPARE, BattleProfilerMarkers.TargetBatchPrepare);
            StartMarkerRecorder(TARGET_BATCH_SERIAL, BattleProfilerMarkers.TargetBatchSerial);
            StartMarkerRecorder(TARGET_BATCH_JOB, BattleProfilerMarkers.TargetBatchJob);
            StartMarkerRecorder(TARGET_BATCH_SCHEDULE, BattleProfilerMarkers.TargetBatchSchedule);
            StartMarkerRecorder(TARGET_BATCH_COMPLETE, BattleProfilerMarkers.TargetBatchComplete);
            StartMarkerRecorder(TARGET_BATCH_APPLY, BattleProfilerMarkers.TargetBatchApply);
            StartMarkerRecorder(DUE_BATCH, BattleProfilerMarkers.DueBatch);
            StartMarkerRecorder(DUE_BATCH_PREPARE, BattleProfilerMarkers.DueBatchPrepare);
            StartMarkerRecorder(DUE_BATCH_SERIAL, BattleProfilerMarkers.DueBatchSerial);
            StartMarkerRecorder(DUE_BATCH_JOB, BattleProfilerMarkers.DueBatchJob);
            StartMarkerRecorder(DUE_BATCH_SCHEDULE, BattleProfilerMarkers.DueBatchSchedule);
            StartMarkerRecorder(DUE_BATCH_COMPLETE, BattleProfilerMarkers.DueBatchComplete);
            StartMarkerRecorder(DUE_BATCH_APPLY, BattleProfilerMarkers.DueBatchApply);
        }

        private void StartRecorder(int index, ProfilerCategory category, string name, ProfilerRecorderOptions options)
        {
            try
            {
                _recorders[index] = ProfilerRecorder.StartNew(category, name, 1, options);
                _recorderAvailable[index] = _recorders[index].Valid;
            }
            catch (ArgumentException)
            {
                _recorders[index] = default;
                _recorderAvailable[index] = false;
            }
        }

        private void StartMarkerRecorder(int index, ProfilerMarker marker)
        {
            try
            {
                _recorders[index] = ProfilerRecorder.StartNew(marker, 1, ProfilerRecorderOptions.Default);
                _recorderAvailable[index] = _recorders[index].Valid;
            }
            catch (ArgumentException)
            {
                _recorders[index] = default;
                _recorderAvailable[index] = false;
            }
        }

        private void CaptureFrame()
        {
            int sampleOffset = _frameCount * METRIC_COUNT;
            _timestamps[_frameCount] = Time.realtimeSinceStartup - _captureStartTime;
            _samples[sampleOffset + FRAME_DURATION] = (long)(Time.unscaledDeltaTime * 1000000000d);
            for (int i = MAIN_THREAD; i < METRIC_COUNT; i++)
            {
                _samples[sampleOffset + i] = ReadLastValue(i);
            }

            int markerOffset = _frameCount * _markerNames.Length;
            for (int i = FIRST_MARKER_METRIC; i < METRIC_COUNT; i++)
            {
                _markerSampleCounts[markerOffset + i - FIRST_MARKER_METRIC] = ReadSampleCount(i);
            }

            _pendingWorkload.WriteTo(_workloadSamples, _frameCount * _workloadNames.Length);
            _pendingWorkload = default;

            _frameCount++;
        }

        private long ReadLastValue(int index)
        {
            ProfilerRecorder recorder = _recorders[index];
            // A capacity-one recorder can wrap before Count is observed.
            if (!recorder.Valid || (recorder.Count == 0 && !recorder.WrappedAround))
            {
                return UNAVAILABLE;
            }

            long value = recorder.LastValue;
            return index >= MAIN_THREAD && index <= GPU_FRAME && value <= 0 ? UNAVAILABLE : value;
        }

        private long ReadSampleCount(int index)
        {
            ProfilerRecorder recorder = _recorders[index];
            // A capacity-one recorder can wrap before Count is observed.
            return recorder.Valid && (recorder.Count > 0 || recorder.WrappedAround) ? recorder.GetSample(0).Count : -1;
        }

        private static BattlePerformanceCaptureMetadata CaptureMetadata()
        {
            string[] qualityNames = QualitySettings.names;
            int qualityLevel = QualitySettings.GetQualityLevel();
            string quality = qualityLevel >= 0 && qualityLevel < qualityNames.Length ? qualityNames[qualityLevel] : "unknown";
            string gitStatus = Application.isEditor ? ReadGit("status --porcelain --untracked-files=normal") : "unavailable";
            return new BattlePerformanceCaptureMetadata(
                Application.unityVersion,
                Application.isEditor ? "Editor" : "Development Player",
                quality,
                $"{Screen.width}x{Screen.height}",
                Time.timeScale,
                QualitySettings.vSyncCount,
                Application.targetFrameRate,
                SystemInfo.graphicsDeviceName,
                SystemInfo.processorType,
                Application.isEditor ? ReadGit("rev-parse --verify HEAD") : "unavailable_in_player",
                gitStatus == "unavailable" ? "unavailable" : gitStatus.Length == 0 ? "clean" : "dirty",
                Application.buildGUID,
                "unspecified",
                CombatAttackCoordinator.JOB_SELECTION_THRESHOLD,
                CombatAttackCoordinator.TARGET_JOB_BATCH_SIZE,
                CombatAttackCoordinator.JOB_PROGRESSION_THRESHOLD,
                CombatAttackCoordinator.DUE_JOB_BATCH_SIZE);
        }

        private static string ReadGit(string arguments)
        {
            try
            {
                string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
                using System.Diagnostics.Process process = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo("git", arguments)
                    {
                        WorkingDirectory = projectDirectory,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                string output = process.StandardOutput.ReadToEnd().Trim();
                if (!process.WaitForExit(2000))
                {
                    process.Kill();
                    return "unavailable";
                }

                return process.ExitCode == 0 ? output : "unavailable";
            }
            catch (Exception)
            {
                return "unavailable";
            }
        }

        private void DisposeRecorders()
        {
            for (int i = 0; i < _recorders.Length; i++)
            {
                if (_recorders[i].Valid)
                {
                    _recorders[i].Dispose();
                }

                _recorders[i] = default;
                _recorderAvailable[i] = false;
            }
        }
    }
#endif
}
