using EmpireAtWar.Services.Timing;
using UnityEditor;

namespace EmpireAtWar.Editor
{
    [InitializeOnLoad]
    [EmpireAtWar.Editor.EditorToolInfo("Capture a 10-second battle performance sample. Requires Play Mode.")]
    public static class BattlePerformanceCaptureMenu
    {
        private const string CAPTURE_MENU_PATH = "Tools/Empire At War/Diagnostics/Performance/Capture Battle (10 Seconds)";

        static BattlePerformanceCaptureMenu()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BattlePerformanceCapture.StopCapture;
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
            return EditorApplication.isPlaying;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                BattlePerformanceCapture.StopCapture();
            }
        }
    }
}
