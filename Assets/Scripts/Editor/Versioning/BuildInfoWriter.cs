using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using EmpireAtWar.Services.Versioning;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace EmpireAtWar.Editor.Versioning
{
    /// <summary>
    /// Stamps every player with its version, Jenkins build number and commit.
    /// Writes straight into the player's StreamingAssets so the project tree stays clean.
    /// </summary>
    public sealed class BuildInfoWriter : IPostprocessBuildWithReport
    {
        private const string BUILD_NUMBER_VARIABLE = "BUILD_NUMBER";
        private const string COMMIT_VARIABLE = "EAW_BUILD_COMMIT";
        private const string LOCAL_BUILD = "local";

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            Write(report.summary.outputPath, report.summary.platform);
            if (report.summary.platform == BuildTarget.StandaloneOSX &&
                UnityEngine.Application.platform == UnityEngine.RuntimePlatform.OSXEditor)
            {
                RunCodesign("--force --sign -", report.summary.outputPath);
                RunCodesign("--verify --deep --strict", report.summary.outputPath);
            }
        }

        public static void Write(string outputPath, BuildTarget target = BuildTarget.StandaloneWindows64)
        {
            string dataFolder = target switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => Path.Combine(
                    Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + "_Data", "StreamingAssets"),
                BuildTarget.StandaloneOSX => Path.Combine(outputPath, "Contents", "Resources", "Data", "StreamingAssets"),
                _ => throw new BuildFailedException($"BuildInfo stamping does not support {target}.")
            };
            Directory.CreateDirectory(dataFolder);
            File.WriteAllText(Path.Combine(dataFolder, BuildInfo.FILE_NAME), Create().ToJson());
        }

        private static BuildInfo Create()
        {
            string build = Environment.GetEnvironmentVariable(BUILD_NUMBER_VARIABLE);
            string commit = Environment.GetEnvironmentVariable(COMMIT_VARIABLE);
            return new BuildInfo(
                PlayerSettings.bundleVersion,
                string.IsNullOrEmpty(build) ? LOCAL_BUILD : build,
                string.IsNullOrEmpty(commit) ? ReadHeadCommit() : commit,
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
        }

        private static void RunCodesign(string arguments, string appPath)
        {
            string escapedPath = Path.GetFullPath(appPath).Replace("\\", "\\\\").Replace("\"", "\\\"");
            ProcessStartInfo start = new ProcessStartInfo("/usr/bin/codesign", $"{arguments} \"{escapedPath}\"")
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process codesign = Process.Start(start);
            string error = codesign.StandardError.ReadToEnd();
            codesign.WaitForExit();
            if (codesign.ExitCode != 0)
            {
                throw new BuildFailedException($"codesign {arguments} failed with exit code {codesign.ExitCode}: {error}");
            }
        }

        private static string ReadHeadCommit()
        {
            ProcessStartInfo start = new ProcessStartInfo("git", "rev-parse HEAD")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process git = Process.Start(start);
            string commit = git.StandardOutput.ReadToEnd().Trim();
            git.WaitForExit();
            if (git.ExitCode != 0)
            {
                throw new BuildFailedException($"git rev-parse HEAD failed with exit code {git.ExitCode}.");
            }
            return commit;
        }
    }
}
