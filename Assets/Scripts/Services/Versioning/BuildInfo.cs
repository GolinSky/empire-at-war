using System;
using System.IO;
using UnityEngine;

namespace EmpireAtWar.Services.Versioning
{
    /// <summary>
    /// Identity of the running player. The build writes it next to the player data;
    /// the version itself comes from PlayerSettings.bundleVersion.
    /// </summary>
    [Serializable]
    public sealed class BuildInfo
    {
        public const string FILE_NAME = "build-info.json";
        private const int SHORT_COMMIT_LENGTH = 7;

        [SerializeField] private string version;
        [SerializeField] private string build;
        [SerializeField] private string commit;
        [SerializeField] private string builtAtUtc;

        public string Version => version;
        public string Build => build;
        public string Commit => commit;
        public string BuiltAtUtc => builtAtUtc;

        public BuildInfo(string version, string build, string commit, string builtAtUtc)
        {
            this.version = version;
            this.build = build;
            this.commit = commit;
            this.builtAtUtc = builtAtUtc;
        }

        public string ShortCommit => commit.Length > SHORT_COMMIT_LENGTH ? commit.Substring(0, SHORT_COMMIT_LENGTH) : commit;

        public string DisplayText => $"v{version} | Build {build} | {ShortCommit}";

        public static BuildInfo FromJson(string json) => JsonUtility.FromJson<BuildInfo>(json);

        public string ToJson() => JsonUtility.ToJson(this, true);

        /// <summary>Editor play mode has no build stamp; a player without one is a broken build and throws.</summary>
        public static BuildInfo LoadCurrent()
        {
#if UNITY_EDITOR
            return new BuildInfo(Application.version, "Editor", "dev", string.Empty);
#else
            return FromJson(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, FILE_NAME)));
#endif
        }
    }
}
