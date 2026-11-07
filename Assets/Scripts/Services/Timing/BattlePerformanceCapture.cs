using System;
using System.IO;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Services.Analytics;
using EmpireAtWar.Services.Versioning;
using Unity.Profiling;
using UnityEngine;

namespace EmpireAtWar.Services.Timing
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public sealed class BattlePerformanceCapture : MonoBehaviour
    {
        private const int QUICK_SECONDS = 10;
        public const int EXTENDED_SECONDS = 60;
        private const int BUFFERED_FRAMES_PER_SECOND = 300;

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
        private const int NAVIGATION_PLAN = 32;
        private const int NAVIGATION_CANDIDATES = 33;
        private const int NAVIGATION_ROUTE_BUILD = 34;
        private const int NAVIGATION_PATH_SEARCH = 35;
        private const int NAVIGATION_NEAREST_REACHABLE = 36;
        private const int RADAR_SNAPSHOT = 37;
        private const int RADAR_JOB = 38;
        private const int RADAR_APPLY = 39;
        private const int SHIP_ORDER_TICK = 40;
        private const int SHIP_AUDIO = 41;
        private const int SQUADRON_ORDER = 42;
        private const int SQUADRON_PILOT = 43;
        private const int SQUADRON_FLIGHT = 44;
        private const int DUE_SORT = 45;
        private const int DUE_LOOKUP = 46;
        private const int DUE_EMISSION = 47;
        private const int DUE_IMPACT = 48;
        private const int SFX_APPLY_MIX = 49;
        private const int NAVIGATION_GRID_BUILD = 50;
        private const int NAVIGATION_PRESERVE_COURSE = 51;
        private const int NAVIGATION_NEW_COURSE = 52;
        private const int NAVIGATION_ARRIVAL = 53;
        private const int TARGET_FALLBACK_NON_FINITE = 54;
        private const int TARGET_FALLBACK_PARENT_TILT = 55;
        private const int TARGET_FALLBACK_RANGE_BOUNDARY = 56;
        private const int TARGET_FALLBACK_YAW_BOUNDARY = 57;
        private const int TARGET_FALLBACK_DEGENERATE_DIRECTION = 58;
        private const int TARGET_FALLBACK_TARGET_VERSION = 59;
        private const int TARGET_FALLBACK_INVALID_CANDIDATE = 60;
        private const int TARGET_FALLBACK_OUT_OF_SPAN = 61;
        private const int NAVIGATION_STATIONARY_REUSE = 62;
        private const int METRIC_COUNT = 63;
        private const int FIRST_MARKER_METRIC = SHIP_TICK;

        private static readonly string[] _metricNames =
        {
            "frame_duration_ns", "main_thread_ns", "render_thread_ns", "gpu_frame_ns", "gc_allocated_bytes", "draw_calls", "setpass_calls", "batches", "triangles",
            "ship_tick_ns", "weapon_tick_ns", "weapon_try_fire_ns", "projectile_get_or_create_ns", "projectile_instantiate_ns", "projectile_turret_update_ns", "projectile_laser_update_ns", "radar_scan_ns",
            "target_batch_ns", "target_batch_capture_ns", "target_batch_prepare_ns", "target_batch_serial_ns", "target_batch_job_ns", "target_batch_schedule_ns", "target_batch_complete_ns", "target_batch_apply_ns",
            "due_batch_ns", "due_batch_prepare_ns", "due_batch_serial_ns", "due_batch_job_ns", "due_batch_schedule_ns", "due_batch_complete_ns", "due_batch_apply_ns",
            "navigation_plan_ns", "navigation_candidates_ns", "navigation_route_build_ns", "navigation_flood_ns", "navigation_nearest_reachable_ns",
            "radar_snapshot_ns", "radar_job_ns", "radar_apply_ns",
            "ship_order_tick_ns",
            "ship_audio_ns",
            "squadron_order_ns",
            "squadron_pilot_ns",
            "squadron_flight_ns",
            "due_sort_ns",
            "due_lookup_ns",
            "due_emission_ns",
            "due_impact_ns",
            "sfx_apply_mix_ns",
            "navigation_grid_build_ns",
            "navigation_preserve_course_ns",
            "navigation_new_course_ns",
            "navigation_arrival_ns",
            "target_fallback_non_finite_ns",
            "target_fallback_parent_tilt_ns",
            "target_fallback_range_boundary_ns",
            "target_fallback_yaw_boundary_ns",
            "target_fallback_degenerate_direction_ns",
            "target_fallback_target_version_ns",
            "target_fallback_invalid_candidate_ns",
            "target_fallback_out_of_span_ns",
            "navigation_stationary_reuse_ns",
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
            BattleProfilerMarkers.DUE_BATCH_APPLY,
            BattleProfilerMarkers.NAVIGATION_PLAN,
            BattleProfilerMarkers.NAVIGATION_CANDIDATES,
            BattleProfilerMarkers.NAVIGATION_ROUTE_BUILD,
            BattleProfilerMarkers.NAVIGATION_PATH_SEARCH,
            BattleProfilerMarkers.NAVIGATION_NEAREST_REACHABLE,
            BattleProfilerMarkers.RADAR_SNAPSHOT,
            BattleProfilerMarkers.RADAR_JOB,
            BattleProfilerMarkers.RADAR_APPLY,
            BattleProfilerMarkers.SHIP_ORDER_TICK,
            BattleProfilerMarkers.SHIP_AUDIO,
            BattleProfilerMarkers.SQUADRON_ORDER,
            BattleProfilerMarkers.SQUADRON_PILOT,
            BattleProfilerMarkers.SQUADRON_FLIGHT,
            BattleProfilerMarkers.DUE_SORT,
            BattleProfilerMarkers.DUE_LOOKUP,
            BattleProfilerMarkers.DUE_EMISSION,
            BattleProfilerMarkers.DUE_IMPACT,
            BattleProfilerMarkers.SFX_APPLY_MIX,
            BattleProfilerMarkers.NAVIGATION_GRID_BUILD,
            BattleProfilerMarkers.NAVIGATION_PRESERVE_COURSE,
            BattleProfilerMarkers.NAVIGATION_NEW_COURSE,
            BattleProfilerMarkers.NAVIGATION_ARRIVAL,
            BattleProfilerMarkers.TARGET_FALLBACK_NON_FINITE,
            BattleProfilerMarkers.TARGET_FALLBACK_PARENT_TILT,
            BattleProfilerMarkers.TARGET_FALLBACK_RANGE_BOUNDARY,
            BattleProfilerMarkers.TARGET_FALLBACK_YAW_BOUNDARY,
            BattleProfilerMarkers.TARGET_FALLBACK_DEGENERATE_DIRECTION,
            BattleProfilerMarkers.TARGET_FALLBACK_TARGET_VERSION,
            BattleProfilerMarkers.TARGET_FALLBACK_INVALID_CANDIDATE,
            BattleProfilerMarkers.TARGET_FALLBACK_OUT_OF_SPAN,
            BattleProfilerMarkers.NAVIGATION_STATIONARY_REUSE,
        };
        private static readonly string[] _workloadNames =
        {
            "completed_unity_frame", "target_requests", "target_candidates", "target_serial_calls", "target_job_calls", "target_fallbacks",
            "target_input_capacity", "target_position_capacity", "target_result_capacity", "target_buffer_growths",
            "due_records", "due_flagged", "due_committed", "due_serial_calls", "due_job_calls",
            "due_input_capacity", "due_result_capacity", "due_buffer_growths", "pending_sequences", "pending_impacts"
        };
        private static BattlePerformanceCapture _instance;
        private readonly ProfilerRecorder[] _recorders = new ProfilerRecorder[METRIC_COUNT];
        private readonly bool[] _recorderAvailable = new bool[METRIC_COUNT];
        private readonly BattleCaptureFrame _latest = new BattleCaptureFrame(METRIC_COUNT, _markerNames.Length, _workloadNames.Length);
        private BattleHitchCapture _hitches;
        private bool _skipFirstHitchFrame;
        private long[] _samples;
        private long[] _markerSampleCounts;
        private long[] _workloadSamples;
        private float[] _timestamps;
        private BattlePerformanceCaptureMetadata _metadata;

        private CombatWorkload _pendingWorkload;
        private DateTime _captureStartUtc;

        private float _captureEndTime;
        private float _captureStartTime;

        private int _frameCount;
        private int _frameLimit;

        private bool _capturing;
        private bool _quitting;
        private bool _skipFirstFrame;

        public static bool IsCapturing => _instance != null && _instance._capturing;
        internal static bool IsSampling => _instance != null && (_instance._capturing || _instance._hitches != null);
        public static string CaptureDirectory => Path.Combine(Application.persistentDataPath, "BattleCaptures");

        internal static void RecordCombatWorkload(CombatWorkload workload)
        {
            if (IsSampling) _instance._pendingWorkload = workload;
        }

        internal static void StartHitchMonitoring(string matchId)
        {
            var host = GetOrCreateInstance();
            host.EnsureRecorders();
            host._hitches = new BattleHitchCapture(Path.Combine(CaptureDirectory, "Hitches", $"hitch_{matchId}.csv"),
                _metricNames, _workloadNames, FIRST_MARKER_METRIC, UNAVAILABLE);
            host._skipFirstHitchFrame = true;
        }

        internal static bool TryObserveHitchFrame()
        {
            var host = _instance;
            if (host == null || host._hitches == null) return false; // Editor teardown can precede the final battle tick.
            if (host._skipFirstHitchFrame)
            {
                host._skipFirstHitchFrame = false;
                return false;
            }
            host._hitches.Observe(host.ReadCompletedFrame());
            return true;
        }

        internal static void InterruptHitchWindow()
        {
            if (_instance == null || _instance._hitches == null) return;
            _instance._hitches.FinishWindow();
            _instance._skipFirstHitchFrame = true;
        }

        internal static void StopHitchMonitoring()
        {
            if (_instance == null || _instance._hitches == null) return;
            _instance._hitches.FinishWindow();
            _instance._hitches = null;
            _instance.ReleaseIdleRecorders();
        }

        public static void StartCapture() => StartCapture(QUICK_SECONDS);

        public static void StartCapture(int seconds)
        {
            if (seconds < 1 || seconds > EXTENDED_SECONDS) throw new ArgumentOutOfRangeException(nameof(seconds));
            GetOrCreateInstance().StartCaptureInternal(seconds);
        }

        public static void StopCapture() => StopCapture("stopped");

        internal static void StopCapture(string reason)
        {
            if (_instance != null)
            {
                _instance.StopCaptureInternal(reason);
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
            if (_frameCount >= _frameLimit)
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

            StopHitchMonitoring();
            DisposeRecorders();
            _instance = null;
        }

        private void OnApplicationQuit()
        {
            StopHitchMonitoring();
            if (_capturing)
            {
                StopCaptureInternal("application_quit");
            }

            _quitting = true;
            DisposeRecorders();
        }

        private void StartCaptureInternal(int seconds)
        {
            if (_capturing)
            {
                StopCaptureInternal("restarted");
            }

            _frameLimit = seconds * BUFFERED_FRAMES_PER_SECOND;
            if (_timestamps == null || _timestamps.Length < _frameLimit)
            {
                _samples = new long[_frameLimit * METRIC_COUNT];
                _markerSampleCounts = new long[_frameLimit * _markerNames.Length];
                _workloadSamples = new long[_frameLimit * _workloadNames.Length];
                _timestamps = new float[_frameLimit];
            }
            _frameCount = 0;
            _metadata = CaptureMetadata();
            _captureStartTime = Time.realtimeSinceStartup;
            _captureEndTime = _captureStartTime + seconds;
            _captureStartUtc = DateTime.UtcNow;
            _skipFirstFrame = true;

            EnsureRecorders();
            _capturing = true;
            Debug.Log($"Battle performance capture started for {seconds} seconds (up to {_frameLimit} frames).");
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
                    string directory = CaptureDirectory;
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
                    ReleaseIdleRecorders();
                }

                return;
            }

            Debug.Log($"Battle performance capture ended before the first sample ({reason}); no files written.");
            ReleaseIdleRecorders();
        }

        private void EnsureRecorders()
        {
            if (_recorderAvailable[FRAME_DURATION]) return;
            _latest.UnityFrame = -1;
            _pendingWorkload = default;
            StartRecorders();
            _recorderAvailable[FRAME_DURATION] = true;
        }

        private void ReleaseIdleRecorders()
        {
            if (!_capturing && _hitches == null) DisposeRecorders();
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
            StartMarkerRecorder(NAVIGATION_PLAN, BattleProfilerMarkers.NavigationPlan);
            StartMarkerRecorder(NAVIGATION_CANDIDATES, BattleProfilerMarkers.NavigationCandidates);
            StartMarkerRecorder(NAVIGATION_ROUTE_BUILD, BattleProfilerMarkers.NavigationRouteBuild);
            StartMarkerRecorder(NAVIGATION_PATH_SEARCH, BattleProfilerMarkers.NavigationPathSearch);
            StartMarkerRecorder(NAVIGATION_NEAREST_REACHABLE, BattleProfilerMarkers.NavigationNearestReachable);
            StartMarkerRecorder(RADAR_SNAPSHOT, BattleProfilerMarkers.RadarSnapshot);
            StartMarkerRecorder(RADAR_JOB, BattleProfilerMarkers.RadarJob);
            StartMarkerRecorder(RADAR_APPLY, BattleProfilerMarkers.RadarApply);
            StartMarkerRecorder(SHIP_ORDER_TICK, BattleProfilerMarkers.ShipOrderTick);
            StartMarkerRecorder(SHIP_AUDIO, BattleProfilerMarkers.ShipAudio);
            StartMarkerRecorder(SQUADRON_ORDER, BattleProfilerMarkers.SquadronOrder);
            StartMarkerRecorder(SQUADRON_PILOT, BattleProfilerMarkers.SquadronPilot);
            StartMarkerRecorder(SQUADRON_FLIGHT, BattleProfilerMarkers.SquadronFlight);
            StartMarkerRecorder(DUE_SORT, BattleProfilerMarkers.DueSort);
            StartMarkerRecorder(DUE_LOOKUP, BattleProfilerMarkers.DueLookup);
            StartMarkerRecorder(DUE_EMISSION, BattleProfilerMarkers.DueEmission);
            StartMarkerRecorder(DUE_IMPACT, BattleProfilerMarkers.DueImpact);
            StartMarkerRecorder(SFX_APPLY_MIX, BattleProfilerMarkers.SfxApplyMix);
            StartMarkerRecorder(NAVIGATION_GRID_BUILD, BattleProfilerMarkers.NavigationGridBuild);
            StartMarkerRecorder(NAVIGATION_PRESERVE_COURSE, BattleProfilerMarkers.NavigationPreserveCourse);
            StartMarkerRecorder(NAVIGATION_NEW_COURSE, BattleProfilerMarkers.NavigationNewCourse);
            StartMarkerRecorder(NAVIGATION_ARRIVAL, BattleProfilerMarkers.NavigationArrival);
            StartMarkerRecorder(TARGET_FALLBACK_NON_FINITE, BattleProfilerMarkers.TargetFallbackNonFinite);
            StartMarkerRecorder(TARGET_FALLBACK_PARENT_TILT, BattleProfilerMarkers.TargetFallbackParentTilt);
            StartMarkerRecorder(TARGET_FALLBACK_RANGE_BOUNDARY, BattleProfilerMarkers.TargetFallbackRangeBoundary);
            StartMarkerRecorder(TARGET_FALLBACK_YAW_BOUNDARY, BattleProfilerMarkers.TargetFallbackYawBoundary);
            StartMarkerRecorder(TARGET_FALLBACK_DEGENERATE_DIRECTION, BattleProfilerMarkers.TargetFallbackDegenerateDirection);
            StartMarkerRecorder(TARGET_FALLBACK_TARGET_VERSION, BattleProfilerMarkers.TargetFallbackTargetVersion);
            StartMarkerRecorder(TARGET_FALLBACK_INVALID_CANDIDATE, BattleProfilerMarkers.TargetFallbackInvalidCandidate);
            StartMarkerRecorder(TARGET_FALLBACK_OUT_OF_SPAN, BattleProfilerMarkers.TargetFallbackOutOfSpan);
            StartMarkerRecorder(NAVIGATION_STATIONARY_REUSE, BattleProfilerMarkers.NavigationStationaryReuse);
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
            BattleCaptureFrame frame = ReadCompletedFrame();
            _timestamps[_frameCount] = Time.realtimeSinceStartup - _captureStartTime;
            frame.Metrics.CopyTo(_samples, _frameCount * METRIC_COUNT);
            frame.MarkerCounts.CopyTo(_markerSampleCounts, _frameCount * _markerNames.Length);
            frame.Workload.CopyTo(_workloadSamples, _frameCount * _workloadNames.Length);
            _frameCount++;
        }

        private BattleCaptureFrame ReadCompletedFrame()
        {
            int frame = Time.frameCount - 1;
            if (_latest.UnityFrame == frame) return _latest;
            _latest.UnityFrame = frame;
            _latest.RealtimeSeconds = Time.realtimeSinceStartupAsDouble;
            float seconds = Time.unscaledDeltaTime;
            _latest.IsHitch = seconds > FrameTimeHistogram.HITCH_SECONDS;
            _latest.Metrics[FRAME_DURATION] = (long)(seconds * 1000000000d);
            for (int i = MAIN_THREAD; i < METRIC_COUNT; i++)
            {
                ProfilerRecorder recorder = _recorders[i];
                // A capacity-one recorder can wrap before Count is observed.
                if (!_recorderAvailable[i] || (recorder.Count == 0 && !recorder.WrappedAround))
                {
                    _latest.Metrics[i] = UNAVAILABLE;
                    if (i >= FIRST_MARKER_METRIC) _latest.MarkerCounts[i - FIRST_MARKER_METRIC] = -1;
                }
                else if (i >= FIRST_MARKER_METRIC)
                {
                    ProfilerRecorderSample sample = recorder.GetSample(0);
                    _latest.Metrics[i] = sample.Value;
                    _latest.MarkerCounts[i - FIRST_MARKER_METRIC] = sample.Count;
                }
                else
                {
                    long value = recorder.LastValue;
                    _latest.Metrics[i] = i <= GPU_FRAME && value <= 0 ? UNAVAILABLE : value;
                }
            }

            if (_pendingWorkload.CompletedUnityFrame != frame) _pendingWorkload = default;
            _pendingWorkload.WriteTo(_latest.Workload, 0);
            _pendingWorkload = default;
            return _latest;
        }

        internal static string CameraResolution
        {
            get
            {
                UnityEngine.Camera camera = UnityEngine.Camera.main;
                return camera != null ? $"{camera.pixelWidth}x{camera.pixelHeight}" : "unavailable";
            }
        }

        internal static BattlePerformanceCaptureMetadata CaptureMetadata()
        {
            string[] qualityNames = QualitySettings.names;
            int qualityLevel = QualitySettings.GetQualityLevel();
            string quality = qualityLevel >= 0 && qualityLevel < qualityNames.Length ? qualityNames[qualityLevel] : "unknown";
            string gitStatus = Application.isEditor ? ReadGit("status --porcelain --untracked-files=normal") : "unavailable";
            return new BattlePerformanceCaptureMetadata(
                unityVersion: Application.unityVersion,
                runtime: Application.isEditor ? "Editor" : "Development Player",
                quality: quality,
                resolution: CameraResolution,
                renderScale: UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is
                    UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset pipeline ? pipeline.renderScale : 1f,
                timeScale: Time.timeScale,
                vSyncCount: QualitySettings.vSyncCount,
                targetFrameRate: Application.targetFrameRate,
                graphicsDevice: SystemInfo.graphicsDeviceName,
                processor: SystemInfo.processorType,
                sourceRevision: Application.isEditor ? ReadGit("rev-parse --verify HEAD") : BuildInfo.LoadCurrent().Commit,
                sourceState: gitStatus == "unavailable" ? "unavailable" : gitStatus.Length == 0 ? "clean" : "dirty",
                buildGuid: Application.buildGUID,
                scenario: "unspecified",
                targetThreshold: CombatAttackCoordinator.JOB_SELECTION_THRESHOLD,
                targetBatchSize: CombatAttackCoordinator.TARGET_JOB_BATCH_SIZE,
                dueThreshold: CombatAttackCoordinator.JOB_PROGRESSION_THRESHOLD,
                dueBatchSize: CombatAttackCoordinator.DUE_JOB_BATCH_SIZE);
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
                var readOutput = process.StandardOutput.ReadToEndAsync();
                if (!process.WaitForExit(2000))
                {
                    process.Kill();
                    return "unavailable";
                }

                return process.ExitCode == 0 ? readOutput.GetAwaiter().GetResult().Trim() : "unavailable";
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
    }
#endif
}
