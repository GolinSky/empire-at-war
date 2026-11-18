using System;
using System.IO;
using System.Threading.Tasks;
using EmpireAtWar.Services.Timing;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    internal static class BattleAttributionRun
    {
        private const string MENU = "Tools/Empire At War/Diagnostics/Performance/";
        private static string _directory;
        private static DateTime _startedUtc;
        private static double _started;
        private static int _firstFrame;
        private static bool _active, _allocationPhase, _finishing;

        static BattleAttributionRun()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Cancel();
            };
            AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            EditorApplication.quitting += Cancel;
        }

        [MenuItem(MENU + "Repeatable Attribution Run (4 min)")]
        private static void StartMenu() => Start();

        [CliCommand("battle_attribution_run", "Collects 3 minutes of hitches then a 1-minute allocation sweep into one report folder.")]
        public static object Start()
        {
            if (_active || _finishing || BattlePerformanceCapture.IsCapturing)
                throw new InvalidOperationException("An attribution run or detailed capture is already active.");
            _directory = Path.Combine("Logs/BattleAudit", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-attribution");
            BattleProfilerFlightRecorder.Arm(directory: _directory);
            _startedUtc = DateTime.UtcNow;
            _started = EditorApplication.timeSinceStartup;
            _firstFrame = Time.frameCount;
            _allocationPhase = false;
            _active = true;
            try
            {
                StartDetail();
                BattleProfilerDecoder.WriteJson(Path.Combine(_directory, "run.json"), new
                {
                    started_utc = _startedUtc, first_unity_frame = _firstFrame,
                    hitch_seconds = 180, allocation_seconds = 60, runtime = "Editor",
                    quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                    resolution = new[] { Screen.width, Screen.height }, time_scale = Time.timeScale,
                    instrument_windows = "Detailed CSV first 60 seconds and allocation phase only. CSV export, Profiler saves and stack recording add overhead; exclude their boundary frames from gameplay targets.",
                    acceptance = "Replay attribution, matched Player comparison and long memory pair pending. Record save and camera separately."
                });
            }
            catch { Cancel(); throw; }
            return new { status = "running", directory = _directory };
        }

        [MenuItem(MENU + "Cancel Attribution Run")]
        [CliCommand("battle_attribution_run_cancel", "Stops only the measurement tools, leaving Play Mode unchanged.")]
        public static void Cancel()
        {
            if (_active) _ = Finish("cancelled");
        }

        private static void Tick()
        {
            if (!_active) return;
            try
            {
                if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.timeScale == 0)
                { _ = Finish("interrupted"); return; }
                if (!BattleProfilerFlightRecorder.IsArmed)
                { _ = Finish("profiler-disarmed"); return; }
                double elapsed = EditorApplication.timeSinceStartup - _started;
                if (!_allocationPhase && elapsed >= 180)
                {
                    _allocationPhase = true;
                    BattlePerformanceCapture.StopCapture();
                    BattleProfilerFlightRecorder.StartAllocationSweep(60, _directory);
                    StartDetail();
                }
                if (_allocationPhase && elapsed >= 240 && !BattleProfilerFlightRecorder.IsSweeping)
                    _ = Finish("completed");
            }
            catch (Exception exception) { Debug.LogException(exception); _ = Finish("error"); }
        }

        private static void StartDetail()
        {
            BattlePerformanceCapture.StartCapture(60);
        }

        private static async Task Finish(string reason)
        {
            if (!_active) return;
            _active = false;
            _finishing = true;
            int lastFrame = Time.frameCount - 1;
            string source = BattlePerformanceCapture.CaptureDirectory;
            try
            {
                BattlePerformanceCapture.StopCapture();
                BattleProfilerFlightRecorder.Disarm(reason);
                BattleProfilerDecoder.WriteJson(Path.Combine(_directory, "run-status.json"), new
                { status = reason, last_unity_frame = lastFrame, ended_utc = DateTime.UtcNow });
                await BattleProfilerFlightRecorder.CompleteAnalysis();
                await BattleProfilerDecoder.RunAudit(Path.Combine(_directory, "archive-log.txt"), "archive", source,
                    _directory, _firstFrame.ToString(), lastFrame.ToString(), _startedUtc.ToString("O"));
                await BattleProfilerDecoder.RunAudit(Path.Combine(_directory, "report-log.txt"), "report", _directory);
                Debug.Log("Battle attribution run saved: " + Path.GetFullPath(_directory));
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { _finishing = false; }
        }
    }
}
