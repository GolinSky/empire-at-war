using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// <c>GraphicsSettingsApplier</c> swaps each quality level's URP asset for a DontSave runtime clone during play.
    /// If the clone is still assigned (or already destroyed) when play stops, the level's pipeline becomes empty and
    /// the next settings save writes <c>customRenderPipeline: {fileID: 0}</c>. This restores the edit-mode
    /// assignments after every play session.
    /// </summary>
    [InitializeOnLoad]
    public static class QualityPipelinePlayModeGuard
    {
        private const string SNAPSHOT_KEY = "EmpireAtWar.QualityPipelinePlayModeGuard.Snapshot";
        private const char SEPARATOR = '|';
        private const string QUALITY_SETTINGS_PATH = "ProjectSettings/QualitySettings.asset";

        static QualityPipelinePlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) Capture();
            else if (change == PlayModeStateChange.EnteredEditMode) Restore();
        }

        /// <summary>
        /// Remembers each level's pipeline asset path (empty when the level holds a runtime clone or nothing);
        /// survives the domain reload into play mode.
        /// </summary>
        public static void Capture()
        {
            string[] paths = new string[QualitySettings.count];
            for (int level = 0; level < paths.Length; level++)
                paths[level] = AssetDatabase.GetAssetPath(QualitySettings.GetRenderPipelineAssetAt(level));
            SessionState.SetString(SNAPSHOT_KEY, string.Join(SEPARATOR, paths));
        }

        /// <summary>Puts back every level whose pipeline differs from the captured asset and saves the settings.</summary>
        public static void Restore()
        {
            string snapshot = SessionState.GetString(SNAPSHOT_KEY, string.Empty);
            if (snapshot.Length == 0) return;
            SessionState.EraseString(SNAPSHOT_KEY);

            string[] paths = snapshot.Split(SEPARATOR);
            int currentLevel = QualitySettings.GetQualityLevel();
            List<string> restored = new List<string>();
            for (int level = 0; level < paths.Length && level < QualitySettings.count; level++)
            {
                // A level that held a runtime clone or nothing at capture has no known asset: never write it.
                if (paths[level].Length == 0) continue;
                RenderPipelineAsset expected = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(paths[level]);
                if (QualitySettings.GetRenderPipelineAssetAt(level) == expected) continue;
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.renderPipeline = expected;
                restored.Add($"{QualitySettings.names[level]} → {expected.name}");
            }

            QualitySettings.SetQualityLevel(currentLevel, false);
            if (restored.Count == 0) return;

            EditorUtility.SetDirty(AssetDatabase.LoadAllAssetsAtPath(QUALITY_SETTINGS_PATH)[0]);
            AssetDatabase.SaveAssets();
            Debug.LogWarning("[QualityPipelinePlayModeGuard] Play mode left runtime pipelines assigned; restored " +
                             string.Join(", ", restored));
        }
    }
}
