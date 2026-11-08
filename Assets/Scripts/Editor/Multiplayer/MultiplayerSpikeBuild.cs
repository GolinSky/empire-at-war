using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EmpireAtWar.Editor.Multiplayer
{
    /// <summary>
    /// Builds a standalone Windows player that contains only the Phase 0 multiplayer spike scene, so it can be
    /// copied to a second machine. The game's own build settings and Jenkins build are untouched.
    /// CLI: <c>Unity.exe -batchmode -quit -projectPath . -executeMethod EmpireAtWar.Editor.Multiplayer.MultiplayerSpikeBuild.Build</c>
    /// </summary>
    public static class MultiplayerSpikeBuild
    {
        private const string SCENE = "Assets/Sandbox/Multiplayer/MultiplayerSpike.unity";
        // Matches the "**/*_build*" ignore rule, so the output never shows up in git.
        private const string OUTPUT_FOLDER = "multiplayer_spike_build";
        private const string EXECUTABLE = "MultiplayerSpike.exe";
        private const string STEAM_APP_ID_FILE = "steam_appid.txt";

        [MenuItem("Tools/Empire At War/Multiplayer/Build Spike Player")]
        public static void Build()
        {
            string output = Path.GetFullPath(OUTPUT_FOLDER);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { SCENE },
                locationPathName = Path.Combine(output, EXECUTABLE),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Multiplayer spike build {report.summary.result}: {report.summary.totalErrors} errors.");

            // Steam (App ID 480 while developing) reads the App ID from beside the executable.
            File.Copy(STEAM_APP_ID_FILE, Path.Combine(output, STEAM_APP_ID_FILE), true);
            Debug.Log($"[MultiplayerSpikeBuild] Built {Path.Combine(output, EXECUTABLE)}");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(Path.Combine(output, EXECUTABLE));
        }
    }
}
