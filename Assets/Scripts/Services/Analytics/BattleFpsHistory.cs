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
        private const string HEADER = "started_utc,run_id,kind,end_running_s,window_s,frames,avg_fps,p95_ms,p99_ms,max_ms,over_50ms,worst_at_running_s,worst_observed_utc,worst_completed_unity_frame,memory_mb,entities,quality,resolution,vsync,target_fps,time_scale,runtime,unity,gpu,processor,source_revision,source_state,build_guid,game_mode,map_type,map_size,map_seed,profiler_frames,detail_capture_frames,gc_used_mb,gc_reserved_mb,gc_collections";

        private readonly BattleDefinition _battle;
        private BattlePerformanceCaptureMetadata _metadata;
        private DateTime _startedUtc;
        private double _runningSeconds;
        private DateTime _worstObservedUtc;
        private int _worstCompletedFrame;
        private int _profilerFrames;
        private int _detailCaptureFrames;
        private int _lastCollections;
        private int _windowCollections;

        public BattleFpsHistory(BattleDefinition battle) => _battle = battle;

        public void Initialize()
        {
            _metadata = BattlePerformanceCapture.CaptureMetadata();
            _startedUtc = DateTime.UtcNow;
            _lastCollections = GC.CollectionCount(0);
        }

        internal void ResetCollectionBaseline() => _lastCollections = GC.CollectionCount(0);

        public void ObserveFrame(bool isWorst)
        {
            int collections = GC.CollectionCount(0);
            _windowCollections += collections - _lastCollections;
            _lastCollections = collections;
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
                EnsureSchema(path);
                using var writer = new StreamWriter(path, true);
                if (writer.BaseStream.Length == 0) writer.WriteLine(HEADER);
                writer.Write(FormattableString.Invariant($"{_startedUtc:O},{sample.MatchId},battle_window,{_runningSeconds:F6},{frames.TotalSeconds:F6},{frames.Count},{sample.AverageFps:F3},{1000 * frames.PercentileSeconds(0.95):F3},{1000 * frames.PercentileSeconds(0.99):F3},{1000 * frames.WorstSeconds:F3},{frames.HitchCount},{worstAt:F6},"));
                writer.Write(FormattableString.Invariant($"{_worstObservedUtc:O},{_worstCompletedFrame},{sample.MemoryMb},{sample.UnitCount},{Quote(QualitySettings.names[sample.QualityLevel])},{BattlePerformanceCapture.CameraResolution},{QualitySettings.vSyncCount},{Application.targetFrameRate},{Time.timeScale:F3},"));
                writer.WriteLine(FormattableString.Invariant($"{_metadata.Runtime},{_metadata.UnityVersion},{Quote(_metadata.GraphicsDevice)},{Quote(_metadata.Processor)},{_metadata.SourceRevision},{_metadata.SourceState},{_metadata.BuildGuid},{_battle.Mode},{_battle.Map.Type},{_battle.Map.Size},{_battle.Map.Seed},{_profilerFrames},{_detailCaptureFrames},{Profiler.GetMonoUsedSizeLong() / 1048576d:F3},{Profiler.GetMonoHeapSizeLong() / 1048576d:F3},{_windowCollections}"));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"FPS history window could not be saved to {path}: {e.Message}");
            }
            _profilerFrames = _detailCaptureFrames = _windowCollections = 0;
        }

        internal static void EnsureSchema(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0) return;
            using (var reader = new StreamReader(path))
            {
                string existing = reader.ReadLine();
                if (existing == HEADER) return;
                if (existing + ",gc_used_mb,gc_reserved_mb,gc_collections" != HEADER)
                    throw new IOException($"Unsupported FPS history schema in {path}.");
            }
            string temporary = path + ".schema-update";
            using (var reader = new StreamReader(path))
            using (var writer = new StreamWriter(temporary, false))
            {
                reader.ReadLine();
                writer.WriteLine(HEADER);
                string line;
                while ((line = reader.ReadLine()) != null)
                    writer.WriteLine(line + ",unavailable,unavailable,unavailable");
            }
            File.Replace(temporary, path, path + ".schema-v1-backup");
        }

        private static string Quote(string text) => "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
#endif
