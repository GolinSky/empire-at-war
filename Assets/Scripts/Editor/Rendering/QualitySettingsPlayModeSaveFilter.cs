using System;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// Keeps play-mode quality changes (runtime URP clones, per-session tweaks) out of
    /// <c>ProjectSettings/QualitySettings.asset</c>; a save during play would store the DontSave clones as empty.
    /// </summary>
    public sealed class QualitySettingsPlayModeSaveFilter : AssetModificationProcessor
    {
        private const string QUALITY_SETTINGS_PATH = "ProjectSettings/QualitySettings.asset";

        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (!Application.isPlaying || Array.IndexOf(paths, QUALITY_SETTINGS_PATH) < 0) return paths;
            Debug.LogWarning("[QualitySettingsPlayModeSaveFilter] Skipped saving QualitySettings during play mode.");
            return Array.FindAll(paths, path => path != QUALITY_SETTINGS_PATH);
        }
    }
}
