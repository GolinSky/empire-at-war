using EmpireAtWar.Services.Tutorial;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Tutorial
{
    /// <summary>Clears saved tutorial hint progress so the next match starts from the first hint.</summary>
    public static class HintProgressMenu
    {
        private const string MENU_PATH = "Tools/Empire At War/Tutorial/Reset Hint Progress";

        [MenuItem(MENU_PATH)]
        private static void ResetHintProgress()
        {
            new JsonHintProgressRepository(Application.persistentDataPath).Reset();
            Debug.Log("Tutorial hint progress reset. The next match starts from the first hint.");
        }

        // In Play Mode the running match holds its own copy and would save it back; use Cheats ▸ Tutorial instead.
        [MenuItem(MENU_PATH, true)]
        private static bool CanResetHintProgress() => !EditorApplication.isPlaying;
    }
}
