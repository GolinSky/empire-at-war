using System.IO;
using EmpireAtWar.Services.Timing;
using UnityEditor;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    [EditorToolInfo("Capture detailed battle frames or open the folder containing automatic FPS history.")]
    public static class BattlePerformanceCaptureMenu
    {
        private const string MENU = "Tools/Empire At War/Diagnostics/Performance/";
        private const string CAPTURE_MENU_PATH = MENU + "Capture Battle (10 Seconds)";
        private const string EXTENDED_MENU_PATH = MENU + "Capture Battle (60 Seconds)";
        private const string STOP_MENU_PATH = MENU + "Stop and Save Battle Capture";

        [MenuItem(MENU + "Open FPS History Folder")]
        private static void OpenHistory()
        {
            Directory.CreateDirectory(BattlePerformanceCapture.CaptureDirectory);
            UnityEngine.Application.OpenURL(new System.Uri(BattlePerformanceCapture.CaptureDirectory).AbsoluteUri);
        }

        static BattlePerformanceCaptureMenu()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () => StopAll("assembly_reload");
            EditorApplication.pauseStateChanged += state =>
            {
                if (state != PauseState.Paused) return;
                BattlePerformanceCapture.InterruptHitchWindow();
                BattlePerformanceCapture.StopCapture("editor_pause");
            };
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(CAPTURE_MENU_PATH)]
        private static void StartCapture()
        {
            BattlePerformanceCapture.StartCapture();
        }

        [MenuItem(CAPTURE_MENU_PATH, true)]
        private static bool ValidateStartCapture()
        {
            return EditorApplication.isPlaying && !EditorApplication.isPaused && !BattlePerformanceCapture.IsCapturing;
        }

        [MenuItem(EXTENDED_MENU_PATH)]
        private static void StartExtended() => BattlePerformanceCapture.StartCapture(BattlePerformanceCapture.EXTENDED_SECONDS);

        [MenuItem(EXTENDED_MENU_PATH, true)]
        private static bool ValidateExtended() => ValidateStartCapture();

        [MenuItem(STOP_MENU_PATH)]
        private static void StopAndSave() => BattlePerformanceCapture.StopCapture();

        [MenuItem(STOP_MENU_PATH, true)]
        private static bool ValidateStop() => BattlePerformanceCapture.IsCapturing;

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopAll("play_mode_exit");
            }
        }

        private static void StopAll(string reason)
        {
            BattlePerformanceCapture.StopHitchMonitoring();
            BattlePerformanceCapture.StopCapture(reason);
        }
    }
}
