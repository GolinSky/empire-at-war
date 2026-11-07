#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Timing;
using UnityEngine;
using UnityEngine.Profiling;

namespace EmpireAtWar.Services.Analytics
{
    internal sealed class BattleFpsHistory
    {
        private const string HEADER = "started_utc,run_id,kind,end_running_s,window_s,frames,avg_fps,p95_ms,p99_ms,max_ms,over_50ms,worst_at_running_s,worst_observed_utc,worst_completed_unity_frame,memory_mb,entities,quality,resolution,vsync,target_fps,time_scale,runtime,unity,gpu,processor,source_revision,source_state,build_guid,game_mode,map_type,map_size,map_seed,profiler_frames,detail_capture_frames";

        private readonly BattleDefinition _battle;
        private BattlePerformanceCaptureMetadata _metadata;
        private DateTime _startedUtc;
        private double _runningSeconds;
        private DateTime _worstObservedUtc;
        private int _worstCompletedFrame;
        private int _profilerFrames;
        private int _detailCaptureFrames;

        public BattleFpsHistory(BattleDefinition battle) => _battle = battle;

        public void Initialize()
        {
            _metadata = BattlePerformanceCapture.CaptureMetadata();
            _startedUtc = DateTime.UtcNow;
        }

        public void ObserveFrame(bool isWorst)
        {
            if (isWorst)
            {
                _worstObservedUtc = DateTime.UtcNow;
                _worstCompletedFrame = Time.frameCount - 1;
            }
            if (Profiler.enabled) _profilerFrames++;
            if (BattlePerformanceCapture.IsCapturing) _detailCaptureFrames++;
        }

        public void WriteWindow(PerformanceSampleData sample, FrameTimeHistogram frames)
        {
            double worstAt = _runningSeconds + frames.WorstEndSeconds;
            _runningSeconds += frames.TotalSeconds;
            string directory = BattlePerformanceCapture.CaptureDirectory;
            string path = Path.Combine(directory, Application.isEditor ? "fps_history_editor.csv" : "fps_history_player.csv");
            try
            {
                Directory.CreateDirectory(directory);
                using var writer = new StreamWriter(path, true);
                if (writer.BaseStream.Length == 0) writer.WriteLine(HEADER);
                writer.Write(FormattableString.Invariant($"{_startedUtc:O},{sample.MatchId},battle_window,{_runningSeconds:F6},{frames.TotalSeconds:F6},{frames.Count},{sample.AverageFps:F3},{1000 * frames.PercentileSeconds(0.95):F3},{1000 * frames.PercentileSeconds(0.99):F3},{1000 * frames.WorstSeconds:F3},{frames.HitchCount},{worstAt:F6},"));
                writer.Write(FormattableString.Invariant($"{_worstObservedUtc:O},{_worstCompletedFrame},{sample.MemoryMb},{sample.UnitCount},{Quote(QualitySettings.names[sample.QualityLevel])},{BattlePerformanceCapture.CameraResolution},{QualitySettings.vSyncCount},{Application.targetFrameRate},{Time.timeScale:F3},"));
                writer.WriteLine(FormattableString.Invariant($"{_metadata.Runtime},{_metadata.UnityVersion},{Quote(_metadata.GraphicsDevice)},{Quote(_metadata.Processor)},{_metadata.SourceRevision},{_metadata.SourceState},{_metadata.BuildGuid},{_battle.Mode},{_battle.Map.Type},{_battle.Map.Size},{_battle.Map.Seed},{_profilerFrames},{_detailCaptureFrames}"));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"FPS history window could not be saved to {path}: {e.Message}");
            }
            _profilerFrames = _detailCaptureFrames = 0;
        }

        private static string Quote(string text) => "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
#endif
